using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// Oyuncu üstünde durduğu sürece "açık" olan basınç plakası.
    /// Çift kilit bulmacasının oyuncu tarafı: yoldaş şalteri çekerken oyuncu burada durmalı.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PressurePlate : MonoBehaviour, IToggleSignal
    {
        [SerializeField] private string playerTag = "Player";
        [Tooltip("Basılınca aşağı inen görsel kısım (opsiyonel).")]
        [SerializeField] private Transform visual;
        [SerializeField] private float pressedDepth = 0.05f;

        private readonly HashSet<Collider> _occupants = new HashSet<Collider>();
        private Vector3 _visualRestLocalPosition;

        public bool IsOn { get; private set; }
        public event Action<bool> Changed;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void Awake()
        {
            if (visual != null)
            {
                _visualRestLocalPosition = visual.localPosition;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            _occupants.Add(other);
            Refresh();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_occupants.Remove(other)) return;
            Refresh();
        }

        private void OnDisable()
        {
            _occupants.Clear();
            Refresh();
        }

        private void Refresh()
        {
            // Yok edilmiş / devre dışı collider'ları temizle (oyuncu ışınlanırsa Exit gelmeyebilir).
            _occupants.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);

            bool pressed = _occupants.Count > 0;
            if (pressed == IsOn) return;

            IsOn = pressed;

            if (visual != null)
            {
                visual.localPosition = _visualRestLocalPosition + (pressed ? Vector3.down * pressedDepth : Vector3.zero);
            }

            Changed?.Invoke(pressed);
        }
    }
}
