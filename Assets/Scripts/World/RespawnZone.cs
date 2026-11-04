using UnityEngine;
using UnityEngine.AI;

namespace AdventureGame
{
    /// <summary>
    /// Uçuruma düşen oyuncuyu (veya bir şekilde düşen ajanı) güvenli noktaya geri taşır.
    /// Oyunun "takılı kalma" durumuna düşmesini engeller.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class RespawnZone : MonoBehaviour
    {
        [SerializeField] private Transform respawnPoint;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (respawnPoint == null)
            {
                Debug.LogError("RespawnZone: respawnPoint atanmamış.", this);
                return;
            }

            if (other.TryGetComponent(out PlayerController playerController))
            {
                playerController.Teleport(respawnPoint.position);

                GameManager manager = GameManager.Instance;
                if (manager != null)
                {
                    manager.PostNotification("Uçuruma düştün! Tek başına bu boşluğu aşamazsın.");
                }

                return;
            }

            if (other.TryGetComponent(out NavMeshAgent agent))
            {
                agent.Warp(respawnPoint.position);
            }
        }
    }
}
