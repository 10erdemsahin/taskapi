using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// Ölçeklenmiş küplerde texture'ın uzamasını önler: mesh UV'lerini objenin GERÇEK boyutuna
    /// göre (metre cinsinden) yeniden üretir. 21 m'lik duvar da 1 m'lik sütun da aynı taş
    /// boyutunu gösterir. Her yüz kendi eksenine göre izdüşürülür (box projection).
    ///
    /// Materyal dosyalarına DOKUNMAZ: tekrar sıklığı burada "metersPerTile" ile belirlenir ve
    /// materyalin kendi tiling değeri bu obje için MaterialPropertyBlock ile (1,1)'e çekilir.
    /// Texture seçimi/kalitesi Ömer'de, sahnedeki yoğunluk Erdem'de kalır.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class WorldSpaceUV : MonoBehaviour
    {
        private static readonly int MainTexStId = Shader.PropertyToID("_MainTex_ST");

        [Tooltip("Bir texture tekrarının kapladığı mesafe (metre). Küçük = daha sık/küçük desen.")]
        [SerializeField, Min(0.1f)] private float metersPerTile = 2f;

        [Tooltip("Orijinal mesh (ilk çalışmada otomatik kaydedilir).")]
        [SerializeField, HideInInspector] private Mesh sourceMesh;

        private Mesh _generatedMesh;
        private Vector3 _appliedScale;
        private float _appliedMetersPerTile;

        public float MetersPerTile
        {
            get => metersPerTile;
            set
            {
                metersPerTile = Mathf.Max(0.1f, value);
                Apply();
            }
        }

        private void OnEnable() => Apply();

        private void Update()
        {
            // Sadece editörde ölçek/ayar değişirse yeniden üret (oyunda maliyet yok).
            if (!Application.isPlaying &&
                (transform.lossyScale != _appliedScale || !Mathf.Approximately(metersPerTile, _appliedMetersPerTile)))
            {
                Apply();
            }
        }

        private void OnDisable()
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter != null && sourceMesh != null && filter.sharedMesh == _generatedMesh)
            {
                filter.sharedMesh = sourceMesh;
            }

            DestroyGenerated();
        }

        private void Apply()
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (filter == null || meshRenderer == null) return;

            if (sourceMesh == null && filter.sharedMesh != null && filter.sharedMesh != _generatedMesh)
            {
                sourceMesh = filter.sharedMesh;
            }

            if (sourceMesh == null || !sourceMesh.isReadable) return;

            DestroyGenerated();

            _generatedMesh = Instantiate(sourceMesh);
            _generatedMesh.name = sourceMesh.name + " (WorldUV)";
            _generatedMesh.hideFlags = HideFlags.HideAndDontSave;

            Vector3 scale = transform.lossyScale;
            Vector3[] vertices = _generatedMesh.vertices;
            Vector3[] normals = _generatedMesh.normals;
            var uvs = new Vector2[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = Vector3.Scale(vertices[i], scale);
                Vector3 n = normals.Length == vertices.Length ? normals[i] : Vector3.up;
                float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);

                Vector2 uv;
                if (ax >= ay && ax >= az) uv = new Vector2(n.x > 0 ? -p.z : p.z, p.y);       // sağ/sol yüz
                else if (ay >= ax && ay >= az) uv = new Vector2(p.x, n.y > 0 ? p.z : -p.z);   // üst/alt yüz
                else uv = new Vector2(n.z > 0 ? p.x : -p.x, p.y);                              // ön/arka yüz

                uvs[i] = uv / metersPerTile;
            }

            _generatedMesh.uv = uvs;
            _generatedMesh.RecalculateTangents(); // normal map doğru yöne baksın
            filter.sharedMesh = _generatedMesh;

            // Materyalin kendi tiling'i bu objede (1,1) olsun; yoğunluğu UV belirliyor.
            var block = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(block);
            block.SetVector(MainTexStId, new Vector4(1f, 1f, 0f, 0f));
            meshRenderer.SetPropertyBlock(block);

            _appliedScale = scale;
            _appliedMetersPerTile = metersPerTile;
        }

        private void DestroyGenerated()
        {
            if (_generatedMesh == null) return;

            if (Application.isPlaying) Destroy(_generatedMesh);
            else DestroyImmediate(_generatedMesh);

            _generatedMesh = null;
        }
    }
}
