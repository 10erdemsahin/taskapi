using System;
using UnityEngine;

namespace AdventureGame.Tests
{
    /// <summary>
    /// Ömer'in CollectionQuest'i gelmeden GameManager'ın Yankı Taşı kapı kuralını test etmek için sahte görev.
    /// Sadece ICollectionQuest sözleşmesine dayanır.
    /// </summary>
    public sealed class FakeQuest : MonoBehaviour, ICollectionQuest
    {
        public int CollectedCount { get; private set; }
        public int RequiredCount => 5;
        public bool IsCompleted => CollectedCount >= RequiredCount;
        public string HintText => "Test ipucu";

        public event Action<int, int> ProgressChanged;
        public event Action<string> Completed;

        public void CompleteAll()
        {
            CollectedCount = RequiredCount;
            ProgressChanged?.Invoke(CollectedCount, RequiredCount);
            Completed?.Invoke(HintText);
        }
    }
}
