using UnityEngine;

namespace LudumDare.Environment
{
    /// <summary>
    /// Creates environment props as real scene children on first run.
    /// Once created, they are normal GameObjects — move/edit freely in the editor.
    /// To regenerate, delete all children of this object and re-enable.
    /// </summary>
    [ExecuteAlways]
    public sealed class EnvironmentProps : MonoBehaviour
    {
        [SerializeField] bool forceRegenerate;

        void OnEnable()
        {
            if (transform.childCount == 0 || forceRegenerate)
            {
                ClearChildren();
                Generate();
                forceRegenerate = false;
            }
        }

        void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        void Generate()
        {
            // --- Fire cabinet (left alley wall, x=-6) ---
            MakeCube("FireCabinet", new Vector3(-6.0f, 1.3f, 17f),
                new Vector3(0.12f, 0.7f, 0.5f), new Color(0.7f, 0.1f, 0.05f));
            MakeCube("FireCabinet_Glass", new Vector3(-5.92f, 1.3f, 17f),
                new Vector3(0.02f, 0.6f, 0.4f), new Color(0.5f, 0.7f, 0.8f, 0.4f));
            MakeCube("FireCabinet_Label", new Vector3(-6.0f, 1.75f, 17f),
                new Vector3(0.14f, 0.12f, 0.5f), new Color(0.8f, 0.1f, 0.05f));

            // --- Key rack (right building wall) ---
            MakeCube("KeyRack", new Vector3(6.5f, 1.3f, 6f),
                new Vector3(0.08f, 0.3f, 0.4f), new Color(0.35f, 0.22f, 0.1f));
            for (int i = 0; i < 3; i++)
                MakeCube($"KeyRack_Hook_{i}", new Vector3(6.44f, 1.35f, 5.85f + i * 0.15f),
                    new Vector3(0.06f, 0.03f, 0.03f), new Color(0.4f, 0.4f, 0.4f));

            // --- Benches ---
            MakeBench(new Vector3(4f, 0f, 10f));
            MakeBench(new Vector3(-4f, 0f, 22f));
            MakeBench(new Vector3(4f, 0f, 38f));

            // --- Dumpsters ---
            MakeCube("Dumpster_1", new Vector3(-5f, 0.45f, 25f),
                new Vector3(0.9f, 0.9f, 1.3f), new Color(0.2f, 0.3f, 0.2f));
            MakeCube("Dumpster_1_Lid", new Vector3(-5f, 0.92f, 25f),
                new Vector3(0.95f, 0.06f, 1.35f), new Color(0.22f, 0.32f, 0.22f));
            MakeCube("Dumpster_2", new Vector3(5.5f, 0.35f, 30f),
                new Vector3(0.7f, 0.7f, 0.7f), new Color(0.25f, 0.25f, 0.28f));

            // --- Grave crosses ---
            MakeCube("GraveCross_V", new Vector3(0f, 0.9f, 58f),
                new Vector3(0.12f, 1.8f, 0.12f), new Color(0.25f, 0.15f, 0.08f));
            MakeCube("GraveCross_H", new Vector3(0f, 1.4f, 58f),
                new Vector3(0.12f, 0.12f, 0.8f), new Color(0.25f, 0.15f, 0.08f));
            MakeCube("GraveCross2_V", new Vector3(3f, 0.7f, 56f),
                new Vector3(0.1f, 1.4f, 0.1f), new Color(0.3f, 0.3f, 0.3f));
            MakeCube("GraveCross2_H", new Vector3(3f, 1.1f, 56f),
                new Vector3(0.1f, 0.1f, 0.6f), new Color(0.3f, 0.3f, 0.3f));
            MakeCube("GraveMound", new Vector3(0f, 0.08f, 58.5f),
                new Vector3(0.8f, 0.15f, 1.6f), new Color(0.18f, 0.12f, 0.06f));

            // --- Tool shed ---
            MakeCube("Shed_Back", new Vector3(-5f, 1f, 48.5f),
                new Vector3(0.1f, 2f, 2f), new Color(0.28f, 0.18f, 0.1f));
            MakeCube("Shed_Side", new Vector3(-4.5f, 1f, 47.5f),
                new Vector3(1f, 2f, 0.1f), new Color(0.28f, 0.18f, 0.1f));
            MakeCube("Shed_Roof", new Vector3(-4.3f, 2f, 48.5f),
                new Vector3(1.5f, 0.08f, 2.2f), new Color(0.2f, 0.14f, 0.08f));

            // --- Bollards ---
            float[] zPositions = { 5f, 12f, 20f, 28f, 36f, 42f };
            foreach (var z in zPositions)
            {
                MakeCube($"Bollard_L_{z}", new Vector3(-3f, 0.2f, z),
                    new Vector3(0.08f, 0.4f, 0.08f), new Color(0.15f, 0.15f, 0.15f));
                MakeCube($"Bollard_R_{z}", new Vector3(3f, 0.2f, z),
                    new Vector3(0.08f, 0.4f, 0.08f), new Color(0.15f, 0.15f, 0.15f));
            }
        }

        void MakeBench(Vector3 pos)
        {
            MakeCube($"Bench_Seat_{pos.z}", pos + new Vector3(0, 0.4f, 0),
                new Vector3(0.4f, 0.06f, 1.2f), new Color(0.3f, 0.18f, 0.08f));
            MakeCube($"Bench_Leg1_{pos.z}", pos + new Vector3(0, 0.2f, -0.45f),
                new Vector3(0.35f, 0.4f, 0.06f), new Color(0.25f, 0.25f, 0.25f));
            MakeCube($"Bench_Leg2_{pos.z}", pos + new Vector3(0, 0.2f, 0.45f),
                new Vector3(0.35f, 0.4f, 0.06f), new Color(0.25f, 0.25f, 0.25f));
            MakeCube($"Bench_Back_{pos.z}", pos + new Vector3(-0.17f, 0.65f, 0),
                new Vector3(0.06f, 0.3f, 1.2f), new Color(0.3f, 0.18f, 0.08f));
        }

        void MakeCube(string name, Vector3 worldPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.position = worldPos;
            go.transform.localScale = scale;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");
                var mat = new Material(shader);
                mat.SetColor("_BaseColor", color);
                if (color.a < 1f)
                {
                    mat.SetFloat("_Surface", 1);
                    mat.SetFloat("_Blend", 0);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = 3000;
                }
                renderer.material = mat;
            }
        }
    }
}
