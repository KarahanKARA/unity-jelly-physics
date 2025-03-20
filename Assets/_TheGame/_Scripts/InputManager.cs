using UnityEngine;

namespace _TheGame._Scripts
{
    public class InputManager : MonoBehaviour
    {
        [Header("Click Effect Settings")]
        [SerializeField] private float clickForceMagnitude = 5f;
        [SerializeField] private LayerMask jellyObjectsLayer;
        
        private Camera _mainCamera;
        
        private void Start()
        {
            _mainCamera = Camera.main;
        }
        
        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleMouseClick();
            }
        }
        
        private void HandleMouseClick()
        {
            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out var hit, 100f, jellyObjectsLayer))
            {
                var jellyCube = hit.collider.GetComponent<JellyCube>();
                if (jellyCube != null)
                {
                    var hitPoint = hit.point;
                    var objectCenter = hit.collider.bounds.center;
                    var forceDirection = (objectCenter - hitPoint).normalized;
                    jellyCube.ApplyForceImpact(forceDirection, clickForceMagnitude);
                }
            }
        }
    }
}