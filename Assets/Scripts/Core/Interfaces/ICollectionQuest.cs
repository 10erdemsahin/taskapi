using System;

namespace AdventureGame
{
    /// <summary>
    /// Haritadaki parçaları toplama görevi (Yankı Taşı). Ömer uygular (CollectionQuest),
    /// GameManager yalnızca bu arayüzü dinler: ilerlemeyi görev metnine yansıtır ve
    /// görev tamamlanmadan kapının açılmasına izin vermez.
    /// </summary>
    public interface ICollectionQuest
    {
        int CollectedCount { get; }
        int RequiredCount { get; }
        bool IsCompleted { get; }

        /// <summary>Görev tamamlanınca oyuncuya gösterilecek ipucu.</summary>
        string HintText { get; }

        /// <summary>(toplanan, gereken)</summary>
        event Action<int, int> ProgressChanged;

        /// <summary>İpucu metniyle birlikte, yalnızca bir kez tetiklenir.</summary>
        event Action<string> Completed;
    }
}
