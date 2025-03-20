using UnityEngine;

namespace _TheGame._Scripts
{
    public class JellyMovement : MonoBehaviour
    {
        [Header("Movement Boundaries")]
        [SerializeField] private float minXpos = -7f;
        [SerializeField] private float minZpos = -3f;
        [SerializeField] private float maxXpos = 7f;
        [SerializeField] private float maxZpos = 7f;

        [Header("Fixed Positions")]
        [SerializeField] private Vector3 leftPos = new Vector3(-7f, 0, 0);
        [SerializeField] private Vector3 rightPos = new Vector3(7f, 0, 0);
        
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 30f;
        
        private Vector3 _targetPosition;
        private bool _isMoving = false;
        
        private void Awake()
        {
            _targetPosition = transform.position;
        }
        
        private void Update()
        {
            if (_isMoving)
            {
                var step = moveSpeed * Time.deltaTime;
                
                transform.position = Vector3.MoveTowards(transform.position, _targetPosition, step);
                
                if (Vector3.Distance(transform.position, _targetPosition) < 0.001f)
                {
                    transform.position = _targetPosition;
                    _isMoving = false;
                }
            }
        }

        public void ResetPosition()
        {
            StartMovementTo(Vector3.zero);
        }
        public void SlideLeftButtonOnClick()
        {
            StartMovementTo(leftPos);
        }
        
        public void RandomMovementOnClick()
        {
            var randomX = Random.Range(minXpos, maxXpos);
            var randomZ = Random.Range(minZpos, maxZpos);
            
            var randomPosition = new Vector3(randomX, transform.position.y, randomZ);
            
            StartMovementTo(randomPosition);
        }
        
        public void SlideRightButtonOnClick()
        {
            StartMovementTo(rightPos);
        }
        
        private void StartMovementTo(Vector3 destination)
        {
            _targetPosition = destination;
            _isMoving = true;
        }
    }
}