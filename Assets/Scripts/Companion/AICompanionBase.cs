using System;
using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// Yoldaşlar için soyut taban sınıf: durum yönetimi ve raporlama ortak,
    /// hareket/karar mantığı alt sınıfa bırakılmıştır (NavMesh, A*, ML-Agents...).
    /// </summary>
    public abstract class AICompanionBase : MonoBehaviour, IAICompanion
    {
        [SerializeField] private string displayName = "Robo";

        public string DisplayName => displayName;
        public CompanionState State { get; private set; } = CompanionState.Idle;

        public virtual bool IsBusy =>
            State == CompanionState.Scanning ||
            State == CompanionState.MovingToTarget ||
            State == CompanionState.Interacting;

        public event Action<CompanionState> StateChanged;
        public event Action<string> Reported;

        protected void SetState(CompanionState newState)
        {
            if (State == newState)
            {
                return;
            }

            State = newState;
            StateChanged?.Invoke(newState);
        }

        protected void Report(string message)
        {
            Debug.Log($"[{displayName}] {message}", this);
            Reported?.Invoke(message);
        }

        public abstract void Follow(Transform target);
        public abstract bool TrySolveObstacle();
        public abstract void CancelTask();
    }
}
