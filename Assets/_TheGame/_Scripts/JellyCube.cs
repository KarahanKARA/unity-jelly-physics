using UnityEngine;

namespace _TheGame._Scripts
{
    public class JellyCube : MonoBehaviour
    {
        [Header("Jelly Effect Settings")]
        [SerializeField] private float wobbleStrength = 0.2f; 
        [SerializeField] private float springStiffness = 10f;    
        [SerializeField] private float damping = 1f;         
        [SerializeField] private float horizontalMultiplier = 0.01f; 
        [SerializeField] private float overShootMultiplier = 1f;
        [SerializeField] private float speed = 4.0f;            
        [SerializeField] private float stretchFactor = 0.1f;  
        [SerializeField] private float returnStrength = 0.5f;
    
        [Header("Movement Randomization")]
        [SerializeField] private float xAxisRandomness = 0.3f;   
        [SerializeField] private float yAxisRandomness = 0.2f;  
        [SerializeField] private float zAxisRandomness = 0.3f;  
        [SerializeField] private float randomSmoothness = 2.5f; 
    
        private Mesh _originalMesh;        
        private Mesh _clonedMesh;          
        private Vector3[] _originalVertices;
        private Vector3[] _modifiedVertices;
        private Vector3[] _vertexVelocities;
        private int[] _vertexSeedValues;     

        private Vector3 _prevPosition;     
        private Vector3 _velocity;         

        private MeshFilter _meshFilter;    
        private Vector3 _objectCenter;     
        private Vector3[] _randomDirections;
        private float _lastMovementMagnitude;

        private void Start()
        {
            _meshFilter = GetComponent<MeshFilter>();
        
            _originalMesh = _meshFilter.sharedMesh;
            _clonedMesh = Instantiate(_originalMesh);
            _meshFilter.mesh = _clonedMesh;
        
            _originalVertices = _originalMesh.vertices;
            _modifiedVertices = new Vector3[_originalVertices.Length];
            System.Array.Copy(_originalVertices, _modifiedVertices, _originalVertices.Length);
        
            _vertexVelocities = new Vector3[_originalVertices.Length];
        
            _vertexSeedValues = new int[_originalVertices.Length];
            _randomDirections = new Vector3[_originalVertices.Length];
            
            var rand = new System.Random(GetInstanceID());
            for (var i = 0; i < _originalVertices.Length; i++)
            {
                _vertexSeedValues[i] = rand.Next(0, 10000);
            
                _randomDirections[i] = new Vector3(
                    (float)rand.NextDouble() * 2f - 1f,
                    (float)rand.NextDouble() * 2f - 1f,
                    (float)rand.NextDouble() * 2f - 1f
                ).normalized;
            }
        
            _prevPosition = transform.position;
            _objectCenter = CalculateObjectCenter();
            _lastMovementMagnitude = 0f;
        }

        private Vector3 CalculateObjectCenter()
        {
            var center = Vector3.zero;
            foreach (var vertex in _originalVertices)
            {
                center += vertex;
            }
            return center / _originalVertices.Length;
        }

        private void Update()
        {
            _velocity = (transform.position - _prevPosition) / Time.deltaTime;
            _prevPosition = transform.position;
        
            var localVelocity = transform.InverseTransformDirection(_velocity);
        
            localVelocity.y = 0;
        
            localVelocity *= speed;
        
            localVelocity.x *= horizontalMultiplier;
            localVelocity.z *= horizontalMultiplier;

            _lastMovementMagnitude = Mathf.Lerp(_lastMovementMagnitude, localVelocity.magnitude, Time.deltaTime * 5f);
        
            UpdateVerticesWithSpringPhysics(localVelocity);
        }

        private Vector3 CalculateMovementRandomOffset(int vertexIndex, Vector3 velocity)
        {
            var velocityMagnitude = velocity.magnitude;
            if (velocityMagnitude < 0.01f)
                return Vector3.zero;
            
            var seed = _vertexSeedValues[vertexIndex];
            var time = Time.time * randomSmoothness;
        
            var xRandom = Mathf.PerlinNoise(seed * 0.01f, time) * 2f - 1f;
            var yRandom = Mathf.PerlinNoise(seed * 0.01f + 100, time) * 2f - 1f;
            var zRandom = Mathf.PerlinNoise(seed * 0.01f + 200, time) * 2f - 1f;
        
            var randomOffset = new Vector3(
                xRandom * xAxisRandomness * _randomDirections[vertexIndex].x,
                yRandom * yAxisRandomness * _randomDirections[vertexIndex].y,
                zRandom * zAxisRandomness * _randomDirections[vertexIndex].z
            );
        
            return randomOffset * (_lastMovementMagnitude * 0.5f + velocityMagnitude * 0.5f);
        }

        private void UpdateVerticesWithSpringPhysics(Vector3 localVelocity)
        {
            var deltaTime = Time.deltaTime;
            var movementDirection = localVelocity.normalized;
            var velocityMagnitude = localVelocity.magnitude;
        
            for (var i = 0; i < _modifiedVertices.Length; i++)
            {
                if (_originalVertices[i].y > 0)
                {
                    var movementRandomOffset = CalculateMovementRandomOffset(i, localVelocity);
                
                    var targetOffset = -localVelocity * (wobbleStrength * overShootMultiplier);
                    targetOffset += movementRandomOffset;
                
                    var vertexToCenter = _objectCenter - _originalVertices[i];
                    vertexToCenter.y = 0;
                
                    var dot = Vector3.Dot(movementDirection, vertexToCenter.normalized);
                    var stretchAmount = dot * stretchFactor * velocityMagnitude;
                    var stretchVector = new Vector3(0, stretchAmount, 0);
                
                    var springForce = (_originalVertices[i] + targetOffset + stretchVector - _modifiedVertices[i]) * springStiffness;
                
                    var returnForce = (_originalVertices[i] - _modifiedVertices[i]) * (returnStrength * (1f - Mathf.Min(1f, velocityMagnitude * 0.5f)));
                
                    var dampingForce = -_vertexVelocities[i] * damping;
                
                    var force = springForce + dampingForce + returnForce;
                
                    var timeScale = Mathf.Clamp(1.0f + speed, 1.0f, 3.0f);
                    _vertexVelocities[i] += force * (deltaTime * timeScale);
                    _modifiedVertices[i] += _vertexVelocities[i] * (deltaTime * timeScale);
                }
                else
                {
                    _modifiedVertices[i] = _originalVertices[i];
                    _vertexVelocities[i] = Vector3.zero;
                }
            }
        
            _clonedMesh.vertices = _modifiedVertices;
            _clonedMesh.RecalculateNormals();
            _clonedMesh.RecalculateBounds();
        }
    }
}