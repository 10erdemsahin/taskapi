using System;
using UnityEngine;

namespace AdventureGame
{
    public enum CompanionState
    {
        Idle,
        Following,
        Scanning,
        MovingToTarget,
        Interacting
    }

    /// <summary>
    /// Otonom yapay zekâ yoldaşının sözleşmesi. GameManager yalnızca bu arayüzü tanır;
    /// NavMesh tabanlı, A* tabanlı veya ML-Agents tabanlı başka bir uygulama
    /// koda dokunmadan takılabilir (LSP / OCP).
    /// </summary>
    public interface IAICompanion
    {
        string DisplayName { get; }
        CompanionState State { get; }
        bool IsBusy { get; }

        event Action<CompanionState> StateChanged;

        /// <summary>Yoldaşın oyuncuya ilettiği durum mesajları ("Şalteri görüyorum!" vb.).</summary>
        event Action<string> Reported;

        void Follow(Transform target);

        /// <summary>
        /// Çevreyi sensörlerle tarar, ulaşılabilir bir mekanizma bulursa göreve başlar.
        /// Görev başlatılamazsa false döner (oyun çökmez).
        /// </summary>
        bool TrySolveObstacle();

        void CancelTask();
    }
}
