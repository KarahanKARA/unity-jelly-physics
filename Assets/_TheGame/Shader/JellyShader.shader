Shader "Custom/ColorfulBlockShader"
{
    Properties
    {
        _Color ("Base Color", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.95
        _SpecularIntensity ("Specular Intensity", Range(0,5)) = 2.0
        _RimPower ("Rim Light Power", Range(0.1, 10.0)) = 3.0
        _RimIntensity ("Rim Light Intensity", Range(0, 2.0)) = 0.5
        _BrightnessBoost ("Brightness Boost", Range(0, 0.5)) = 0.2
        _HighlightSize ("Highlight Size", Range(0, 1)) = 0.1
        _HighlightIntensity ("Highlight Intensity", Range(0, 3)) = 1.5
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 200
        
        // Main pass
        Pass
        {
            Name "ForwardLit"
            Tags {"LightMode" = "UniversalForward"}
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                float3 viewDirWS    : TEXCOORD3;
            };
            
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Glossiness;
                float _SpecularIntensity;
                float _RimPower;
                float _RimIntensity;
                float _BrightnessBoost;
                float _HighlightSize;
                float _HighlightIntensity;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                // Transform positions
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(output.positionWS);
                output.uv = input.uv;
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Normals and view direction
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // Get main light
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                
                // Base lighting
                float NdotL = saturate(dot(normalWS, lightDir));
                float halfLambert = NdotL * 0.5 + 0.5; // Softer lighting
                
                // Specular calculation
                float3 halfVec = normalize(lightDir + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfVec));
                float specular = pow(NdotH, _Glossiness * 128.0) * _SpecularIntensity;
                
                // Rim lighting (edge highlight)
                float rim = 1.0 - saturate(dot(normalWS, viewDirWS));
                float rimLight = pow(rim, _RimPower) * _RimIntensity;
                
                // Add top highlight (candy-like appearance)
                float topDot = saturate(dot(float3(0, 1, 0), normalWS)); // Check if facing up
                float sideHighlight = pow(saturate(dot(float3(0.5, 0.7, 0.5), normalWS)), 4); // Angled highlight
                
                // Simplified fake SSS for more saturated colors
                float sss = pow(saturate(dot(-normalWS, lightDir)), 2) * 0.1;
                
                // Add screen-aligned highlight (candy effect)
                float2 screenUV = input.positionCS.xy / _ScreenParams.xy;
                float screenHighlight = pow(saturate(1.0 - distance(screenUV, float2(0.65, 0.35))), 8) * _HighlightSize;
                screenHighlight = screenHighlight * _HighlightIntensity;
                
                // Calculate base color with enhanced vibrancy
                float3 baseColor = _Color.rgb + _BrightnessBoost; // Boost brightness
                baseColor = pow(baseColor, 0.9); // Increase saturation slightly
                
                // Mix all lighting components
                float3 finalColor = baseColor * (halfLambert + 0.4); // Base diffuse
                finalColor += baseColor * sss; // Add subtle subsurface
                finalColor += specular * float3(1,1,1); // Add white specular
                finalColor += rimLight * baseColor * 1.3; // Add colored rim light
                finalColor += topDot * _Color.rgb * 0.2; // Add top highlight
                finalColor += sideHighlight * _Color.rgb * 0.3; // Add side highlight
                finalColor += screenHighlight * float3(1,1,1); // Add screen-aligned highlight
                
                // Final adjustments
                finalColor = saturate(finalColor); // Clamp to prevent over-saturation
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
        
        // Shadow casting pass
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
        
        // Depth pass
        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}