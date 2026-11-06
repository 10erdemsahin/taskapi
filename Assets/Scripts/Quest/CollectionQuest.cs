using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// "Yankı Taşı Parçaları" görevi: parçaların Collected olaylarını sayar ve ICollectionQuest olaylarını yayınlar.
    /// GameManager'ı doğrudan çağırmaz; GameManager ve HUD bu arayüzü dinler (gevşek bağlılık).
    /// </summary>
    public sealed class CollectionQuest : MonoBehaviour, ICollectionQuest
    {
        [Tooltip("Boşsa sahnedeki tüm Collectible'lar kullanılır.")]
        [SerializeField] private List<Collectible> collectibles = new List<Collectible>();
        [SerializeField] private QuestHintData hintData;

        private readonly HashSet<Collectible> _collected = new HashSet<Collectible>();
        private readonly List<Collectible> _subscribed = new List<Collectible>();
        private bool _completedRaised;

        public int CollectedCount => _collected.Count;
        public int RequiredCount
        {
            get
            {
                int count = 0;
                foreach (Collectible c in collectibles) if (c != null) count++;
                return count;
            }
        }

        public bool IsCompleted => RequiredCount > 0 && CollectedCount >= RequiredCount;

        public string HintText
        {
            get
            {
                if (hintData != null) return hintData.hintText;
                Debug.LogError("CollectionQuest: hintData atanmamış; ipucu boş.", this);
                return string.Empty;
            }
        }

        public string QuestName => hintData != null && !string.IsNullOrWhiteSpace(hintData.questName) ? hintData.questName : "Yankı Taşı";

        public event Action<int, int> ProgressChanged;
        public event Action<string> Completed;

        private void Awake()
        {
            if (collectibles.Count == 0)
            {
                collectibles.AddRange(FindObjectsByType<Collectible>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID));
            }

            if (collectibles.Count == 0)
            {
                Debug.LogWarning("CollectionQuest: Hiç parça yok; görev devre dışı.", this);
            }
            else if (collectibles.Contains(null))
            {
                Debug.LogWarning("CollectionQuest: Listede boş (null) parça var; sayılmayacak.", this);
            }
        }

        private void OnEnable()
        {
            foreach (Collectible c in collectibles)
            {
                if (c == null || _subscribed.Contains(c)) continue;
                c.Collected += HandleCollected;
                _subscribed.Add(c);
            }
        }

        private void OnDisable()
        {
            foreach (Collectible c in _subscribed)
            {
                if (c != null) c.Collected -= HandleCollected;
            }

            _subscribed.Clear();
        }

        /// <summary>Parçayı sayar (aynı parça iki kez sayılmaz). Testler ve olaylar buradan geçer.</summary>
        private void HandleCollected(Collectible collectible)
        {
            if (collectible == null || !collectibles.Contains(collectible) || !_collected.Add(collectible))
            {
                return;
            }

            ProgressChanged?.Invoke(CollectedCount, RequiredCount);

            if (!_completedRaised && IsCompleted)
            {
                _completedRaised = true;
                Completed?.Invoke(HintText);
            }
        }
    }
}
