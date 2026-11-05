using System;
using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// Çift kilit: şalter (Activate çağrısı) VE bir sinyal (basınç plakası) birlikteyken hedefi çalıştırır.
    /// Şalter tek seferlik olduğu için çekilme bilgisi saklanır; plakaya sonradan basılması da yeterlidir.
    /// LeverSwitch ve DrawBridge koduna dokunmadan araya girer (OCP): şalterin hedefi bu kapıdır.
    /// </summary>
    public sealed class AndGate : MonoBehaviour, IActivatable
    {
        [Tooltip("IToggleSignal uygulayan bileşen (PressurePlate)")]
        [SerializeField] private MonoBehaviour signalBehaviour;

        [Tooltip("İki koşul sağlanınca çalışacak IActivatable (DrawBridge)")]
        [SerializeField] private MonoBehaviour targetBehaviour;

        [SerializeField] private string blockedMessage = "Şalter çekildi ama köprü kilitli! Basınç plakasında biri durmalı.";

        private IToggleSignal _signal;
        private IActivatable _target;
        private bool _leverPulled;

        public bool IsActivated { get; private set; }
        public bool IsLeverPulled => _leverPulled;
        public event Action Activated;

        private void Awake()
        {
            _signal = InterfaceResolver.Resolve<IToggleSignal>(signalBehaviour, this, nameof(signalBehaviour));
            _target = InterfaceResolver.Resolve<IActivatable>(targetBehaviour, this, nameof(targetBehaviour));
        }

        private void OnEnable()
        {
            if (_signal != null) _signal.Changed += HandleSignalChanged;
        }

        private void OnDisable()
        {
            if (_signal != null) _signal.Changed -= HandleSignalChanged;
        }

        /// <summary>Şalter tarafından çağrılır.</summary>
        public void Activate()
        {
            if (IsActivated) return;

            _leverPulled = true;

            if (!TryFire())
            {
                GameManager manager = GameManager.Instance;
                if (manager != null) manager.PostNotification(blockedMessage);
            }
        }

        private void HandleSignalChanged(bool isOn)
        {
            if (isOn) TryFire();
        }

        private bool TryFire()
        {
            if (IsActivated || !_leverPulled) return false;
            if (_signal == null || !_signal.IsOn) return false;

            IsActivated = true;
            _target?.Activate();
            Activated?.Invoke();
            return true;
        }
    }
}
