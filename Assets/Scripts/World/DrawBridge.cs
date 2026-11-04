using System;
using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// Asma köprü. Başta dikey (kalkık) durur; Activate ile iner ve ardından NavMesh
    /// çalışma anında (runtime) ASENKRON olarak yeniden hesaplanır, böylece yoldaş da
    /// köprüden geçebilir. Oyuncu fiziksel collider üzerinde yürür.
    /// </summary>
    public sealed class DrawBridge : MonoBehaviour, IActivatable
    {
        [Header("Dönüş")]
        [Tooltip("Boşsa bu Transform kullanılır. Menteşe (pivot) noktası olmalıdır.")]
        [SerializeField] private Transform pivot;
        [SerializeField] private Vector3 raisedLocalEuler = new Vector3(90f, 0f, 0f);
        [SerializeField] private Vector3 loweredLocalEuler = Vector3.zero;
        [SerializeField] private bool applyRaisedOnAwake = true;
        [SerializeField, Min(0.1f)] private float lowerDuration = 2.5f;
        [SerializeField] private AnimationCurve lowerCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Navigasyon")]
        [Tooltip("Köprü indikten sonra güncellenecek NavMeshSurface.")]
        [SerializeField] private NavMeshSurface navMeshSurface;

        private bool _isAnimating;

        public bool IsActivated { get; private set; }
        public event Action Activated;

        private void Awake()
        {
            if (pivot == null)
            {
                pivot = transform;
            }

            if (applyRaisedOnAwake)
            {
                pivot.localRotation = Quaternion.Euler(raisedLocalEuler);
            }
        }

        public void Activate()
        {
            if (IsActivated || _isAnimating)
            {
                return;
            }

            StartCoroutine(LowerRoutine());
        }

        private IEnumerator LowerRoutine()
        {
            _isAnimating = true;

            Quaternion from = Quaternion.Euler(raisedLocalEuler);
            Quaternion to = Quaternion.Euler(loweredLocalEuler);

            for (float t = 0f; t < 1f; t += Time.deltaTime / lowerDuration)
            {
                pivot.localRotation = Quaternion.Slerp(from, to, lowerCurve.Evaluate(t));
                yield return null;
            }

            pivot.localRotation = to;
            Physics.SyncTransforms();

            yield return RebuildNavMesh();

            _isAnimating = false;
            IsActivated = true;
            Activated?.Invoke();
        }

        private IEnumerator RebuildNavMesh()
        {
            if (navMeshSurface == null)
            {
                Debug.LogWarning("DrawBridge: NavMeshSurface atanmamış; yoldaş köprüden geçemeyebilir.", this);
                yield break;
            }

            if (navMeshSurface.navMeshData != null)
            {
                // Asenkron güncelleme: frame'i bloklamaz.
                AsyncOperation operation = navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);
                if (operation != null)
                {
                    yield return operation;
                }
            }
            else
            {
                navMeshSurface.BuildNavMesh();
            }
        }
    }
}
