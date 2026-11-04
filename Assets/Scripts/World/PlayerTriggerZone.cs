using UnityEngine;
using UnityEngine.Events;

namespace AdventureGame
{
    /// <summary>
    /// Yeniden kullanılabilir tetik bölgesi. Oyuncu girdiğinde Inspector'dan bağlanan
    /// UnityEvent'i çalıştırır (örn. GameManager.NotifyObstacleReached / CompleteGame).
    /// Bölge, kimi çağırdığını bilmez → gevşek bağlılık.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PlayerTriggerZone : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool triggerOnce = true;
        [SerializeField] private UnityEvent onPlayerEnter = new UnityEvent();

        private bool _hasFired;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasFired && triggerOnce)
            {
                return;
            }

            if (!other.CompareTag(playerTag))
            {
                return;
            }

            _hasFired = true;
            onPlayerEnter.Invoke();
        }
    }
}
