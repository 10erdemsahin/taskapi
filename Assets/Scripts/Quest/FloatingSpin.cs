using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// Parçayı yavaşça döndürür ve yukarı aşağı sallar. Yalnızca bu objeyi (görsel) hareket ettirir;
    /// collider üst objede sabit kalır. Oyun zamanını kullanır, böylece duraklatınca durur.
    /// </summary>
    public sealed class FloatingSpin : MonoBehaviour
    {
        [SerializeField] private float degreesPerSecond = 60f;
        [SerializeField] private float bobHeight = 0.15f;
        [SerializeField] private float bobSpeed = 2f;

        private Vector3 _startLocalPosition;
        private float _phase;

        private void Awake()
        {
            _startLocalPosition = transform.localPosition;
            _phase = Random.value * Mathf.PI * 2f; // Parçalar aynı anda sallanmasın.
        }

        private void Update()
        {
            transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
            transform.localPosition = _startLocalPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + _phase) * bobHeight);
        }
    }
}
