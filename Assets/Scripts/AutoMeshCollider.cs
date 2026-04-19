using UnityEngine;

namespace LudumDare
{
    /// <summary>
    /// Place on a single GameObject in the scene.
    /// On Awake, scans every MeshFilter in the scene and adds a MeshCollider
    /// wherever a Collider is missing, so imported models become solid.
    /// </summary>
    public sealed class AutoMeshCollider : MonoBehaviour
    {
        void Awake()
        {
            foreach (var filter in FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if (filter.GetComponent<Collider>() != null)
                    continue;

                var col = filter.gameObject.AddComponent<MeshCollider>();
                col.sharedMesh = filter.sharedMesh;
            }
        }
    }
}
