using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// Bir IActivatable (ör. DrawBridge) çalıştığında verilen collider'ları kapatır.
    /// Köprü inince uçurum kenarındaki görünmez "köprü kapısı" bariyeri bu sayede açılır.
    /// DrawBridge koduna dokunmadan yeni davranış eklenir (OCP).
    /// </summary>
    public sealed class DisableOnActivated : MonoBehaviour
    {
        [Tooltip("IActivatable uygulayan bileşen (DrawBridge)")]
        [SerializeField] private MonoBehaviour activatableBehaviour;
        [SerializeField] private Collider[] collidersToDisable;

        private IActivatable _activatable;

        private void Awake()
        {
            _activatable = InterfaceResolver.Resolve<IActivatable>(activatableBehaviour, this, nameof(activatableBehaviour));

            if (_activatable != null && _activatable.IsActivated)
            {
                DisableColliders();
            }
        }

        private void OnEnable()
        {
            if (_activatable != null) _activatable.Activated += DisableColliders;
        }

        private void OnDisable()
        {
            if (_activatable != null) _activatable.Activated -= DisableColliders;
        }

        private void DisableColliders()
        {
            if (collidersToDisable == null) return;

            foreach (Collider target in collidersToDisable)
            {
                if (target != null) target.enabled = false;
            }
        }
    }
}
