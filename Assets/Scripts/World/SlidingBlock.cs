using System.Collections;
using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// İki nokta arasında gidip gelen taş blok. Üzerindeki NavMeshObstacle (Carve) sayesinde
    /// yoldaş NavMesh üzerinden otomatik olarak yeniden rota bulur; yoldaş koduna dokunulmaz.
    /// Noktalar sahnedeki objelerdir (koordinat koda gömülü değil).
    /// </summary>
    public sealed class SlidingBlock : MonoBehaviour
    {
        [SerializeField] private Transform pointA;
        [SerializeField] private Transform pointB;
        [SerializeField, Min(0.1f)] private float speed = 1.5f;
        [SerializeField, Min(0f)] private float waitAtEnds = 2f;

        private void OnEnable()
        {
            if (pointA == null || pointB == null)
            {
                Debug.LogWarning("SlidingBlock: pointA / pointB atanmamış, blok sabit kalacak.", this);
                return;
            }

            StartCoroutine(MoveRoutine());
        }

        private IEnumerator MoveRoutine()
        {
            transform.position = pointA.position;
            Transform target = pointB;

            while (true)
            {
                while ((transform.position - target.position).sqrMagnitude > 0.0001f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
                    yield return null;
                }

                yield return new WaitForSeconds(waitAtEnds);
                target = target == pointA ? pointB : pointA;
            }
        }
    }
}
