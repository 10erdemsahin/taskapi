using System;
using System.Collections;
using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// Muhafızın koruduğu kilitli kapı. Kendi başına açılmaz; GameManager, NPC'den gelen
    /// ve doğrulanan "open_door" aksiyonu sonrasında Open() çağırır.
    /// Kapı paneline NavMeshObstacle (Carve) eklenirse açıldığında NavMesh otomatik güncellenir.
    /// </summary>
    public sealed class InteractiveDoor : MonoBehaviour, IOpenable
    {
        [Tooltip("Boşsa bu Transform. Menteşe noktası olmalıdır.")]
        [SerializeField] private Transform hinge;
        [SerializeField] private Vector3 openLocalEuler = new Vector3(0f, -100f, 0f);
        [SerializeField, Min(0.1f)] private float openDuration = 1.5f;
        [SerializeField] private AudioSource openSound;

        private bool _isAnimating;

        public bool IsOpen { get; private set; }
        public event Action Opened;

        private void Awake()
        {
            if (hinge == null)
            {
                hinge = transform;
            }
        }

        public void Open()
        {
            if (IsOpen || _isAnimating)
            {
                return;
            }

            StartCoroutine(OpenRoutine());
        }

        private IEnumerator OpenRoutine()
        {
            _isAnimating = true;

            if (openSound != null)
            {
                openSound.Play();
            }

            Quaternion from = hinge.localRotation;
            Quaternion to = Quaternion.Euler(openLocalEuler);

            for (float t = 0f; t < 1f; t += Time.deltaTime / openDuration)
            {
                hinge.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            hinge.localRotation = to;
            _isAnimating = false;
            IsOpen = true;
            Opened?.Invoke();
        }
    }
}
