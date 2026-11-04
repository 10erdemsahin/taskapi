using System.Collections;
using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// Yüksek platformdaki şalter. Oyuncu ulaşamaz; yalnızca yoldaş kullanır.
    /// Çekildiğinde bağlı IActivatable'ı (köprü) çalıştırır – köprünün somut tipini bilmez.
    /// </summary>
    public sealed class LeverSwitch : InteractableBase
    {
        [Header("Bağlantı")]
        [Tooltip("IActivatable uygulayan bileşen (DrawBridge)")]
        [SerializeField] private MonoBehaviour targetBehaviour;

        [Header("Görsel")]
        [SerializeField] private Transform handlePivot;
        [SerializeField] private Vector3 pulledLocalEuler = new Vector3(60f, 0f, 0f);
        [SerializeField, Min(0.05f)] private float pullDuration = 0.6f;

        private IActivatable _target;
        private bool _isPulled;

        protected override bool IsAvailable => !_isPulled;

        private void Reset()
        {
            interactionPrompt = "Şalteri çek [E]";
            allowPlayer = false;      // Fiziksel engel: oyuncu bu şaltere ulaşamaz.
            allowAICompanion = true;
        }

        private void Awake()
        {
            _target = InterfaceResolver.Resolve<IActivatable>(targetBehaviour, this, nameof(targetBehaviour));
        }

        protected override void OnInteract(GameObject interactor, InteractorType interactorType)
        {
            _isPulled = true;
            StartCoroutine(PullRoutine());
        }

        private IEnumerator PullRoutine()
        {
            if (handlePivot != null)
            {
                Quaternion from = handlePivot.localRotation;
                Quaternion to = Quaternion.Euler(pulledLocalEuler);

                for (float t = 0f; t < 1f; t += Time.deltaTime / pullDuration)
                {
                    handlePivot.localRotation = Quaternion.Slerp(from, to, t);
                    yield return null;
                }

                handlePivot.localRotation = to;
            }

            if (_target != null)
            {
                _target.Activate();
            }
            else
            {
                Debug.LogWarning("Şalter çekildi ama bağlı bir mekanizma yok.", this);
            }
        }
    }
}
