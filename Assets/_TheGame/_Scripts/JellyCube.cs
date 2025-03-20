using UnityEngine;

namespace _TheGame._Scripts
{
    public class JellyCube : MonoBehaviour
    {
        [Header("Jelly Effect Settings")]
        [SerializeField] private float wobbleStrength = 0.25f; 
        [SerializeField] private float springStiffness = 10f;    
        [SerializeField] private float damping = 0.5f;         
        [SerializeField] private float horizontalMultiplier = 0.01f; 
        [SerializeField] private float overShootMultiplier = 1f;
        [SerializeField] private float speed = 4.0f;            
        [SerializeField] private float stretchFactor = 0.1f;  
        [SerializeField] private float returnStrength = 0.5f;
    
        [Header("Movement Randomization")]
        [SerializeField] private float xAxisRandomness = 0.05f;   
        [SerializeField] private float yAxisRandomness = 0.05f;  
        [SerializeField] private float zAxisRandomness = 0.05f;  
        [SerializeField] private float randomSmoothness = 0.5f;
        
        [Header("Wave Effect Settings")]
        [SerializeField] private float waveSpeed = 3f;
        [SerializeField] private float waveAmplitude = 0.8f;
        
        [Header("Velocity Limits")]
        [SerializeField] private float maxVelocityMagnitude = 6f;
        [SerializeField] private float clickImpactMultiplier = 5f;
        [SerializeField] private float maxAccumulatedImpact = 15f;
        [SerializeField] private float bottomVertexMovementFactor = 0.1f;
        
        private float _currentAccumulatedImpact = 0f;
        private float _impactDecayRate = 2f;
        private float _waveOffset = 0f;
    
        private Mesh _originalMesh;        
        private Mesh _clonedMesh;          
        private Vector3[] _originalVertices;
        private Vector3[] _modifiedVertices;
        private Vector3[] _vertexVelocities;
        private int[] _vertexSeedValues;     
        private bool[] _isTopVertex;

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
            _isTopVertex = new bool[_originalVertices.Length];
        
            _vertexSeedValues = new int[_originalVertices.Length];
            _randomDirections = new Vector3[_originalVertices.Length];
            
            var rand = new System.Random(GetInstanceID());
            for (var i = 0; i < _originalVertices.Length; i++)
            {
                _vertexSeedValues[i] = rand.Next(0, 10000);
                _isTopVertex[i] = _originalVertices[i].y > 0;
                
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
            
            var velocityMagnitude = _velocity.magnitude;
            
            var localVelocity = transform.InverseTransformDirection(_velocity);
            localVelocity.y = 0;
            localVelocity *= speed;
            localVelocity.x *= horizontalMultiplier;
            localVelocity.z *= horizontalMultiplier;

            if (localVelocity.magnitude > maxVelocityMagnitude)
            {
                localVelocity = localVelocity.normalized * maxVelocityMagnitude;
            }

            _lastMovementMagnitude = Mathf.Lerp(_lastMovementMagnitude, localVelocity.magnitude, Time.deltaTime * 5f);
            
            _waveOffset += Time.deltaTime * waveSpeed * (velocityMagnitude > 0.1f ? 1 : 0.2f);
            
            UpdateVerticesWithSpringPhysics(localVelocity);
            
            if (_currentAccumulatedImpact > 0)
            {
                _currentAccumulatedImpact -= _impactDecayRate * Time.deltaTime;
                _currentAccumulatedImpact = Mathf.Max(0, _currentAccumulatedImpact);
            }
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

        private float CalculateWaveEffect(int vertexIndex, Vector3 vertexPosition)
        {
            var horizontalDistanceFromCenter = Vector2.Distance(
                new Vector2(vertexPosition.x, vertexPosition.z), 
                new Vector2(_objectCenter.x, _objectCenter.z)
            );
            
            var vertexPhase = horizontalDistanceFromCenter + _waveOffset;
            var waveEffect = Mathf.Sin(vertexPhase) * waveAmplitude;
            
            return waveEffect * Mathf.Max(_lastMovementMagnitude, _currentAccumulatedImpact * 0.1f);
        }

        private void UpdateVerticesWithSpringPhysics(Vector3 localVelocity)
        {
            var deltaTime = Time.deltaTime;
            var movementDirection = localVelocity.normalized;
            var velocityMagnitude = localVelocity.magnitude;
        
            for (var i = 0; i < _modifiedVertices.Length; i++)
            {
                var isTop = _isTopVertex[i];
                var movementFactor = isTop ? 1f : bottomVertexMovementFactor;
                
                var movementRandomOffset = CalculateMovementRandomOffset(i, localVelocity);
                
                var targetOffset = -localVelocity * (wobbleStrength * overShootMultiplier * movementFactor);
                targetOffset += movementRandomOffset;
                
                var vertexToCenter = _objectCenter - _originalVertices[i];
                vertexToCenter.y = 0;
                
                var dot = Vector3.Dot(movementDirection, vertexToCenter.normalized);
                var stretchAmount = dot * stretchFactor * velocityMagnitude * movementFactor;
                
                var waveEffect = CalculateWaveEffect(i, _originalVertices[i]);
                var stretchVector = new Vector3(0, stretchAmount + waveEffect, 0);
                
                if (isTop || velocityMagnitude > 0.5f || _currentAccumulatedImpact > 0.5f)
                {
                    var springForce = (_originalVertices[i] + targetOffset + stretchVector - _modifiedVertices[i]) * springStiffness;
                    var returnForce = (_originalVertices[i] - _modifiedVertices[i]) * (returnStrength * (1f - Mathf.Min(1f, velocityMagnitude * 0.5f)));
                    var dampingForce = -_vertexVelocities[i] * damping;
                    
                    var force = springForce + dampingForce + returnForce;
                    
                    var timeScale = Mathf.Clamp(1.0f + speed, 1.0f, 3.0f);
                    _vertexVelocities[i] += force * (deltaTime * timeScale);
                    
                    var maxDisplacement = isTop ? 1.0f : 0.2f;
                    if (_vertexVelocities[i].magnitude > maxDisplacement)
                    {
                        _vertexVelocities[i] = _vertexVelocities[i].normalized * maxDisplacement;
                    }
                    
                    _modifiedVertices[i] += _vertexVelocities[i] * (deltaTime * timeScale);
                    
                    var maxDistance = isTop ? 0.5f : 0.1f;
                    var currentDistance = Vector3.Distance(_originalVertices[i], _modifiedVertices[i]);
                    if (currentDistance > maxDistance)
                    {
                        var direction = (_modifiedVertices[i] - _originalVertices[i]).normalized;
                        _modifiedVertices[i] = _originalVertices[i] + direction * maxDistance;
                    }
                }
                else
                {
                    _modifiedVertices[i] = Vector3.Lerp(_modifiedVertices[i], _originalVertices[i], deltaTime * 5f);
                    _vertexVelocities[i] = Vector3.Lerp(_vertexVelocities[i], Vector3.zero, deltaTime * 5f);
                }
            }
        
            _clonedMesh.vertices = _modifiedVertices;
            _clonedMesh.RecalculateNormals();
            _clonedMesh.RecalculateBounds();
        }
        
        public void ApplyForceImpact(Vector3 forceDirection, float forceMagnitude)
        {
            forceMagnitude *= clickImpactMultiplier;
            
            _currentAccumulatedImpact += forceMagnitude * 0.2f;
            _currentAccumulatedImpact = Mathf.Min(_currentAccumulatedImpact, maxAccumulatedImpact);
            
            forceMagnitude *= (1f + _currentAccumulatedImpact * 0.1f);
            
            forceMagnitude = Mathf.Min(forceMagnitude, maxVelocityMagnitude * 3f);
            
            var localForceDirection = transform.InverseTransformDirection(forceDirection);
            var tempVelocity = localForceDirection.normalized * forceMagnitude;
            
            tempVelocity.x *= horizontalMultiplier * 2f;
            tempVelocity.z *= horizontalMultiplier * 2f;
            
            UpdateVerticesWithSpringPhysics(tempVelocity);
        }
    }
}