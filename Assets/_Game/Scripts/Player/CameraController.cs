using UnityEngine;

namespace TacticsGame.Player
{
    public class CameraController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float panSpeed = 15f;
        [SerializeField] private float panBorderThickness = 10f;
        [SerializeField] private bool useEdgePanning = false;

        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 120f;
        [SerializeField] private float mouseSensitivity = 3f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 10f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 30f;

        [Header("Bounds")]
        [SerializeField] private Vector2 panLimitX = new Vector2(-50f, 50f);
        [SerializeField] private Vector2 panLimitZ = new Vector2(-50f, 50f);

        [Header("Initial Setup")]
        [SerializeField] private float initialHeight = 15f;
        [SerializeField] private float initialAngle = 45f;
        [SerializeField] private float initialYRotation = 0f;

        private Transform cameraTransform;
        private float currentZoom;
        private float currentYRotation;

        private void Start()
        {
            cameraTransform = Camera.main != null ? Camera.main.transform : null;
            if (cameraTransform == null)
            {
                Debug.LogError("[CameraController] No main camera found!");
                return;
            }
            currentZoom = initialHeight;
            currentYRotation = initialYRotation;
            UpdateCameraPosition();
        }

        private void Update()
        {
            HandlePan();
            HandleRotation();
            HandleZoom();
        }

        private void HandlePan()
        {
            Vector3 move = Vector3.zero;

            // Keyboard input
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                move += transform.forward;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                move -= transform.forward;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                move += transform.right;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                move -= transform.right;

            // Edge panning
            if (useEdgePanning)
            {
                if (Input.mousePosition.y >= Screen.height - panBorderThickness)
                    move += transform.forward;
                if (Input.mousePosition.y <= panBorderThickness)
                    move -= transform.forward;
                if (Input.mousePosition.x >= Screen.width - panBorderThickness)
                    move += transform.right;
                if (Input.mousePosition.x <= panBorderThickness)
                    move -= transform.right;
            }

            // Flatten movement to horizontal plane
            move.y = 0;
            if (move != Vector3.zero)
                move = move.normalized;

            Vector3 newPos = transform.position + move * panSpeed * Time.deltaTime;
            newPos.x = Mathf.Clamp(newPos.x, panLimitX.x, panLimitX.y);
            newPos.z = Mathf.Clamp(newPos.z, panLimitZ.x, panLimitZ.y);
            transform.position = newPos;
        }

        private void HandleRotation()
        {
            // Middle mouse button drag to rotate
            if (Input.GetMouseButton(2))
            {
                float rotateInput = Input.GetAxis("Mouse X");
                currentYRotation += rotateInput * mouseSensitivity;
            }

            // Q/E keys to rotate
            if (Input.GetKey(KeyCode.Q))
                currentYRotation -= rotationSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.E))
                currentYRotation += rotationSpeed * Time.deltaTime;

            UpdateCameraPosition();
        }

        private void HandleZoom()
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            currentZoom -= scrollInput * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            UpdateCameraPosition();
        }

        private void UpdateCameraPosition()
        {
            transform.rotation = Quaternion.Euler(0, currentYRotation, 0);

            if (cameraTransform != null)
            {
                // Position camera behind and above the pivot
                float radAngle = initialAngle * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(0, Mathf.Sin(radAngle), -Mathf.Cos(radAngle)) * currentZoom;
                cameraTransform.position = transform.position + transform.rotation * offset;
                cameraTransform.LookAt(transform.position);
            }
        }

        /// <summary>
        /// Center camera on a specific world position.
        /// </summary>
        public void FocusOn(Vector3 position)
        {
            Vector3 newPos = position;
            newPos.y = transform.position.y;
            transform.position = newPos;
        }
    }
}
