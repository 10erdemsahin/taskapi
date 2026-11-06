using System;
using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// Toplanabilir Yankı Taşı parçası. Şalterle aynı taban sınıftan (InteractableBase) türer;
    /// böylece hem oyuncunun [E]'si hem yoldaşın sensörü ek kod olmadan çalışır.
    /// Yalnızca toplandığını duyurur (Collected); sayımı CollectionQuest yapar (SRP).
    /// </summary>
    public sealed class Collectible : InteractableBase
    {
        [SerializeField] private string shardId = "shard";
        [Tooltip("Toplanınca gizlenecek görsel kısım (boşsa objenin tüm renderer'ları).")]
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private ParticleSystem pickupEffectPrefab;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField, Range(0f, 1f)] private float pickupVolume = 0.8f;

        private bool _collected;

        public string ShardId => shardId;
        public bool IsCollected => _collected;

        public event Action<Collectible> Collected;

        protected override bool IsAvailable => !_collected;

        private void Reset()
        {
            interactionPrompt = "Yankı Taşı parçasını al [E]";
        }

        protected override void OnInteract(GameObject interactor, InteractorType interactorType)
        {
            if (_collected) return;
            _collected = true;

            Vector3 position = transform.position;
            if (pickupEffectPrefab != null)
            {
                ParticleSystem fx = Instantiate(pickupEffectPrefab, position, Quaternion.identity);
                fx.Play();
                Destroy(fx.gameObject, 2f);
            }

            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, position, pickupVolume * AudioListener.volume);
            }

            Collected?.Invoke(this);

            // Hemen Destroy edilmez: yoldaş o karede hâlâ referans tutuyor olabilir.
            foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
            if (visualRoot != null)
            {
                visualRoot.SetActive(false);
            }
            else
            {
                foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            }

            FloatingSpin spin = GetComponentInChildren<FloatingSpin>();
            if (spin != null) spin.enabled = false;
        }
    }
}
