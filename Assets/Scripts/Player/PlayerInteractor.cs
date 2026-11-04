using System;
using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// Kamera merkezinden Raycast atarak IInteractable nesneleri algılar.
    /// [E] etkileşim, [F] yoldaştan yardım isteme. Ekrandaki ipucu metnini
    /// doğrudan değiştirmez; FocusChanged olayı yayınlar (UI bunu dinler).
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField, Min(0.5f)] private float interactDistance = 3.5f;
        [SerializeField] private LayerMask interactableMask = ~0;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private KeyCode companionHelpKey = KeyCode.F;

        private IInteractable _focused;
        private bool _inputLocked;

        /// <summary>Odaklanılan nesnenin ipucu metni; odak yoksa null.</summary>
        public event Action<string> FocusChanged;

        private void Awake()
        {
            if (viewCamera == null)
            {
                viewCamera = Camera.main;
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

        private void HandleInputLockChanged(bool locked)
        {
            _inputLocked = locked;
            if (locked)
            {
                SetFocus(null);
            }
        }

        private void Update()
        {
            if (_inputLocked || viewCamera == null)
            {
                return;
            }

            SetFocus(DetectInteractable());

            if (_focused != null && Input.GetKeyDown(interactKey))
            {
                _focused.Interact(gameObject, InteractorType.Player);
            }

            if (Input.GetKeyDown(companionHelpKey))
            {
                GameManager manager = GameManager.Instance;
                if (manager != null)
                {
                    manager.RequestCompanionHelp();
                }
            }
        }

        private IInteractable DetectInteractable()
        {
            Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactableMask, QueryTriggerInteraction.Ignore))
            {
                return null;
            }

            IInteractable candidate = hit.collider.GetComponentInParent<IInteractable>();
            return candidate != null && candidate.CanInteract(InteractorType.Player) ? candidate : null;
        }

        private void SetFocus(IInteractable interactable)
        {
            if (ReferenceEquals(_focused, interactable))
            {
                return;
            }

            _focused = interactable;
            FocusChanged?.Invoke(_focused?.InteractionPrompt);
        }
    }
}
