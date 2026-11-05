using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// Ana menü arka planı: kamera bir noktanın etrafında yavaşça döner.
    /// Time.timeScale'den etkilenmez (unscaled).
    /// </summary>
    public sealed class MenuCameraOrbit : MonoBehaviour
    {
        [SerializeField] private Vector3 target = new Vector3(0f, 1.5f, 0f);
        [SerializeField, Min(1f)] private float radius = 14f;
        [SerializeField] private float height = 4.5f;
        [SerializeField] private float degreesPerSecond = 4f;

        private float _angle;

        private void Update()
        {
            _angle = (_angle + degreesPerSecond * Time.unscaledDeltaTime) % 360f;
            float rad = _angle * Mathf.Deg2Rad;
            transform.position = target + new Vector3(Mathf.Sin(rad) * radius, height, Mathf.Cos(rad) * radius);
            transform.LookAt(target);
        }
    }
}
