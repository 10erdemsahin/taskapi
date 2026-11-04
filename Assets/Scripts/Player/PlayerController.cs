using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// Birinci şahıs oyuncu hareketi (CharacterController). Zıplama yüksekliği bilinçli
    /// olarak sınırlıdır: oyuncu ne uçurumu atlayabilir ne de yüksek platforma çıkabilir.
    /// Diyalog sırasında GameManager girdiyi kilitler (olay ile; doğrudan bağımlılık yok).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Referanslar")]
        [Tooltip("Kameranın bağlı olduğu Transform (genelde Main Camera).")]
        [SerializeField] private Transform cameraPivot;

        [Header("Hareket")]
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(1f)] private float sprintMultiplier = 1.6f;
        [Tooltip("Yüksek platformdan (3 m) ve uçurum genişliğinden (7 m) küçük kalmalı.")]
        [SerializeField, Min(0f)] private float jumpHeight = 1.1f;
        [SerializeField] private float gravity = -20f;

        [Header("Bakış")]
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 2f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        private CharacterController _characterController;
        private float _pitch;
        private float _verticalVelocity;
        private bool _inputLocked;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            if (cameraPivot == null && Camera.main != null)
            {
                cameraPivot = Camera.main.transform;
            }
        }

        private void Start()
        {
            GameManager manager = GameManager.Instance;
            if (manager != null)
            {
                manager.InputLockChanged += HandleInputLockChanged;
                _inputLocked = manager.IsInputLocked;
            }
        }

        private void OnDestroy()
        {
            GameManager manager = GameManager.Instance;
            if (manager != null)
            {
                manager.InputLockChanged -= HandleInputLockChanged;
            }
        }

        private void HandleInputLockChanged(bool locked) => _inputLocked = locked;

        private void Update()
        {
            if (!_inputLocked)
            {
                HandleLook();
            }

            HandleMovement();
        }

        private void HandleLook()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            transform.Rotate(0f, mouseX, 0f);

            _pitch = Mathf.Clamp(_pitch - mouseY, minPitch, maxPitch);
            if (cameraPivot != null)
            {
                cameraPivot.localEulerAngles = new Vector3(_pitch, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            bool grounded = _characterController.isGrounded;
            if (grounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f; // Zemine yapışık kalması için küçük bir itme.
            }

            Vector3 input = Vector3.zero;
            float speed = moveSpeed;

            if (!_inputLocked)
            {
                input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
                input = Vector3.ClampMagnitude(input, 1f);

                if (Input.GetKey(KeyCode.LeftShift))
                {
                    speed *= sprintMultiplier;
                }

                if (grounded && Input.GetButtonDown("Jump"))
                {
                    _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }

            _verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = transform.TransformDirection(input) * speed;
            velocity.y = _verticalVelocity;

            _characterController.Move(velocity * Time.deltaTime);
        }

        /// <summary>CharacterController aktifken position atamak yok sayılabilir; bu yüzden kapat-aç.</summary>
        public void Teleport(Vector3 position)
        {
            _characterController.enabled = false;
            transform.position = position;
            _characterController.enabled = true;
            _verticalVelocity = 0f;
        }
    }
}
