using UnityEngine;
using UnityEditor;

namespace LudumDare.Editor
{
    public static class TextureGenerator
    {
        [MenuItem("Tools/Generate Textures/Asphalt")]
        public static void GenerateAsphalt()
        {
            int size = 512;
            var tex = new Texture2D(size, size, TextureFormat.RGB24, true);

            // Seed for reproducibility
            Random.InitState(42);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Base dark grey
                    float baseVal = 0.18f;

                    // Large-scale variation (patches)
                    float large = Mathf.PerlinNoise(x * 0.008f, y * 0.008f) * 0.06f - 0.03f;

                    // Medium noise (aggregate texture)
                    float med = Mathf.PerlinNoise(x * 0.05f + 100, y * 0.05f + 100) * 0.04f - 0.02f;

                    // Fine grain noise (individual stones)
                    float fine = Mathf.PerlinNoise(x * 0.3f + 200, y * 0.3f + 200) * 0.03f - 0.015f;

                    // Random speckles (gravel)
                    float speckle = Random.value < 0.08f ? (Random.value * 0.06f - 0.03f) : 0f;

                    // Cracks (dark lines)
                    float crack = 0f;
                    float crackNoise = Mathf.PerlinNoise(x * 0.015f + 500, y * 0.015f + 500);
                    if (crackNoise > 0.48f && crackNoise < 0.52f)
                        crack = -0.08f;

                    // Another crack direction
                    float crack2 = Mathf.PerlinNoise(x * 0.012f + 300, y * 0.02f + 300);
                    if (crack2 > 0.49f && crack2 < 0.51f)
                        crack = -0.06f;

                    float val = Mathf.Clamp01(baseVal + large + med + fine + speckle + crack);

                    // Slight warm tint variation
                    float r = val;
                    float g = val * 0.97f;
                    float b = val * 0.93f;

                    tex.SetPixel(x, y, new Color(r, g, b));
                }
            }

            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            // Save texture
            byte[] bytes = tex.EncodeToPNG();
            string texPath = "Assets/Textures/TX_Asphalt.png";
            System.IO.Directory.CreateDirectory("Assets/Textures");
            System.IO.File.WriteAllBytes(texPath, bytes);
            AssetDatabase.Refresh();

            // Configure import settings
            var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            // Update asphalt material — ensure URP Lit shader and texture
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SH_Asphalt.mat");
            var loadedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (mat != null && loadedTex != null)
            {
                // Force URP Lit shader
                var urpLit = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLit != null) mat.shader = urpLit;

                mat.SetTexture("_BaseMap", loadedTex);
                mat.SetTexture("_MainTex", loadedTex);
                mat.SetTextureScale("_BaseMap", new Vector2(8, 8));
                mat.SetTextureOffset("_BaseMap", Vector2.zero);
                mat.SetColor("_BaseColor", Color.white); // don't tint over texture
                mat.SetFloat("_Smoothness", 0.15f); // rough asphalt
                EditorUtility.SetDirty(mat);
                Debug.Log($"[TextureGenerator] Material shader: {mat.shader.name}, texture: {loadedTex.name}");
            }
            else
            {
                Debug.LogError($"[TextureGenerator] mat={mat}, tex={loadedTex}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[TextureGenerator] Asphalt texture generated and applied!");
        }

        [MenuItem("Tools/Generate Textures/Fix BodyBag Materials")]
        public static void FixBodyBagMaterials()
        {
            // Create bag material with diffuse texture
            var bagTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/BodyBag/textures/Bag_PBR_diffuse.jpeg");
            var bagNorm = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/BodyBag/textures/Bag_PBR_normal.png");
            var bagMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SH_CorpseBag.mat");
            if (bagMat != null)
            {
                var urp = Shader.Find("Universal Render Pipeline/Lit");
                if (urp != null) bagMat.shader = urp;
                if (bagTex != null) { bagMat.SetTexture("_BaseMap", bagTex); bagMat.SetTexture("_MainTex", bagTex); }
                if (bagNorm != null) { bagMat.SetTexture("_BumpMap", bagNorm); bagMat.SetFloat("_BumpScale", 1f); }
                bagMat.SetColor("_BaseColor", Color.white);
                bagMat.SetFloat("_Smoothness", 0.1f);
                EditorUtility.SetDirty(bagMat);
            }

            // Create rope material
            var ropeTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/BodyBag/textures/Rope_Head_PBR_diffuse.jpeg");
            var ropeNorm = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/BodyBag/textures/Rope_Head_PBR_normal.png");
            var ropeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SH_Rope.mat");
            if (ropeMat != null)
            {
                var urp = Shader.Find("Universal Render Pipeline/Lit");
                if (urp != null) ropeMat.shader = urp;
                if (ropeTex != null) { ropeMat.SetTexture("_BaseMap", ropeTex); ropeMat.SetTexture("_MainTex", ropeTex); }
                if (ropeNorm != null) { ropeMat.SetTexture("_BumpMap", ropeNorm); ropeMat.SetFloat("_BumpScale", 1f); }
                ropeMat.SetColor("_BaseColor", Color.white);
                ropeMat.SetFloat("_Smoothness", 0.2f);
                EditorUtility.SetDirty(ropeMat);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[TextureGenerator] BodyBag materials fixed!");
        }

        [MenuItem("Tools/Generate Textures/Fix CratesBarrels Materials")]
        public static void FixCratesBarrelsMaterials()
        {
            var woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SH_Wood.mat");
            var rustMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SH_Rust.mat");

            var names = new[] { "CratesBarrels_Left", "CratesBarrels_Right" };
            foreach (var n in names)
            {
                var go = GameObject.Find(n);
                if (go == null) continue;
                var renderers = go.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    // Barrels get rust, everything else gets wood
                    bool isBarrel = r.gameObject.name.ToLower().Contains("barrel");
                    var mat = isBarrel ? rustMat : woodMat;
                    if (mat != null)
                    {
                        var mats = new Material[r.sharedMaterials.Length];
                        for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                        r.sharedMaterials = mats;
                    }
                }
            }
            Debug.Log("[TextureGenerator] CratesBarrels materials replaced with matte Wood/Rust!");
        }

        [MenuItem("Tools/Generate Textures/Apply Fence Texture")]
        public static void ApplyFenceTexture()
        {
            var matPath = "Assets/Materials/SH_Fence.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            var urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp != null) mat.shader = urp;

            var baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/TX_Fence_Base.jpg");
            var normTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/TX_Fence_Normal.jpg");
            if (baseTex != null) { mat.SetTexture("_BaseMap", baseTex); mat.SetTexture("_MainTex", baseTex); }
            if (normTex != null) { mat.SetTexture("_BumpMap", normTex); mat.SetFloat("_BumpScale", 1f); }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.3f);
            mat.SetFloat("_Metallic", 0.7f);
            mat.SetTextureScale("_BaseMap", new Vector2(4, 2));
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();

            // Apply to all fences
            var fences = GameObject.Find("--- BUILDING FENCES ---");
            var brickMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SH_Building_Dark.mat");
            if (fences != null && brickMat != null)
            {
                foreach (var r in fences.GetComponentsInChildren<Renderer>())
                {
                    r.sharedMaterial = brickMat;
                }
            }
            Debug.Log("[TextureGenerator] Fence texture applied!");
        }

        [MenuItem("Tools/Generate Textures/Fix Axe Material")]
        public static void FixAxeMaterial()
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Axe/Axe-Diffuse.tif");
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SH_Axe.mat");
            if (mat != null)
            {
                var urp = Shader.Find("Universal Render Pipeline/Lit");
                if (urp != null) mat.shader = urp;
                if (tex != null) { mat.SetTexture("_BaseMap", tex); mat.SetTexture("_MainTex", tex); }
                mat.SetColor("_BaseColor", Color.white);
                mat.SetFloat("_Smoothness", 0.2f);
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[TextureGenerator] Axe material fixed!");
        }

        [MenuItem("Tools/Generate Textures/Apply Real Asphalt")]
        public static void ApplyRealAsphalt()
        {
            var texPath = "Assets/Textures/TX_Asphalt_Real.jpg";
            var imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (imp != null) { imp.wrapMode = TextureWrapMode.Repeat; imp.SaveAndReimport(); }
            ApplyTexToMat("Assets/Materials/SH_Asphalt.mat", texPath, new Vector2(8, 80), 0.15f);
            AssetDatabase.SaveAssets();
            Debug.Log("[TextureGenerator] Real asphalt texture applied!");
        }

        [MenuItem("Tools/Generate Textures/BodyBag")]
        public static void GenerateBodyBag()
        {
            int size = 512;
            var tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            Random.InitState(77);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Burlap/canvas base color (lighter so visible)
                    float baseR = 0.32f;
                    float baseG = 0.26f;
                    float baseB = 0.2f;

                    // Weave pattern (horizontal + vertical threads)
                    float threadH = Mathf.Sin(y * 3.14159f * 0.8f) * 0.5f + 0.5f;
                    float threadV = Mathf.Sin(x * 3.14159f * 0.8f) * 0.5f + 0.5f;
                    float weave = (threadH * 0.5f + threadV * 0.5f) * 0.04f - 0.02f;

                    // Fine fabric texture
                    float fabric = Mathf.PerlinNoise(x * 0.15f + 50, y * 0.15f + 50) * 0.03f - 0.015f;

                    // Dirt stains (darker patches)
                    float dirt = Mathf.PerlinNoise(x * 0.006f + 400, y * 0.006f + 400);
                    float dirtVal = dirt > 0.55f ? -(dirt - 0.55f) * 0.15f : 0f;

                    // Blood/dark stains (reddish-brown spots)
                    float stain = Mathf.PerlinNoise(x * 0.01f + 700, y * 0.01f + 700);
                    float stainR = 0f, stainG = 0f, stainB = 0f;
                    if (stain > 0.62f)
                    {
                        float intensity = (stain - 0.62f) * 2f;
                        stainR = intensity * 0.08f;
                        stainG = -intensity * 0.02f;
                        stainB = -intensity * 0.03f;
                    }

                    // Wrinkle creases (dark lines)
                    float wrinkle1 = Mathf.PerlinNoise(x * 0.02f + 900, y * 0.005f + 900);
                    float wrinkleVal = (wrinkle1 > 0.49f && wrinkle1 < 0.51f) ? -0.04f : 0f;

                    // Rope marks (horizontal bands)
                    float ropeY1 = Mathf.Abs(y - size * 0.25f);
                    float ropeY2 = Mathf.Abs(y - size * 0.55f);
                    float ropeY3 = Mathf.Abs(y - size * 0.8f);
                    float ropeMark = 0f;
                    if (ropeY1 < 8) ropeMark = -0.03f * (1f - ropeY1 / 8f);
                    if (ropeY2 < 8) ropeMark = -0.03f * (1f - ropeY2 / 8f);
                    if (ropeY3 < 8) ropeMark = -0.03f * (1f - ropeY3 / 8f);

                    float r = Mathf.Clamp01(baseR + weave + fabric + dirtVal + stainR + wrinkleVal + ropeMark);
                    float g = Mathf.Clamp01(baseG + weave + fabric + dirtVal + stainG + wrinkleVal + ropeMark);
                    float b = Mathf.Clamp01(baseB + weave + fabric + dirtVal + stainB + wrinkleVal + ropeMark);

                    tex.SetPixel(x, y, new Color(r, g, b));
                }
            }

            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;

            byte[] bytes = tex.EncodeToPNG();
            string texPath = "Assets/Textures/TX_BodyBag.png";
            System.IO.File.WriteAllBytes(texPath, bytes);
            AssetDatabase.Refresh();

            var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }

            // Apply to DarkCloth material
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SH_DarkCloth.mat");
            var loadedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (mat != null && loadedTex != null)
            {
                var urpLit = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLit != null) mat.shader = urpLit;
                mat.SetTexture("_BaseMap", loadedTex);
                mat.SetTexture("_MainTex", loadedTex);
                mat.SetTextureScale("_BaseMap", new Vector2(2, 2));
                mat.SetColor("_BaseColor", Color.white);
                mat.SetFloat("_Smoothness", 0.05f); // rough fabric
                EditorUtility.SetDirty(mat);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[TextureGenerator] BodyBag texture generated and applied!");
        }

        [MenuItem("Tools/Generate Textures/All Scene")]
        public static void GenerateAllScene()
        {
            GenerateBuildingDark();
            GenerateBuildingGray();
            GenerateDirt();
            GenerateWood();
            GenerateMetal();
            GenerateConcrete();
            Debug.Log("[TextureGenerator] All scene textures generated!");
        }

        static void ApplyTexToMat(string matPath, string texPath, Vector2 tiling, float smoothness = 0.2f)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (mat != null && tex != null)
            {
                var urpLit = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLit != null) mat.shader = urpLit;
                mat.SetTexture("_BaseMap", tex);
                mat.SetTexture("_MainTex", tex);
                mat.SetTextureScale("_BaseMap", tiling);
                mat.SetColor("_BaseColor", Color.white);
                mat.SetFloat("_Smoothness", smoothness);
                EditorUtility.SetDirty(mat);
            }
        }

        static void SaveTex(Texture2D tex, string path)
        {
            System.IO.Directory.CreateDirectory("Assets/Textures");
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.Refresh();
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null) { imp.wrapMode = TextureWrapMode.Repeat; imp.SaveAndReimport(); }
        }

        static void GenerateBuildingDark()
        {
            int s = 512; var tex = new Texture2D(s, s, TextureFormat.RGB24, true);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float b = 0.13f;
                b += Mathf.PerlinNoise(x*0.01f, y*0.01f) * 0.04f;
                b += Mathf.PerlinNoise(x*0.05f+50, y*0.05f+50) * 0.02f;
                // Brick-like horizontal lines
                float brickH = Mathf.Abs(Mathf.Sin(y * 0.12f)) < 0.05f ? -0.03f : 0f;
                // Vertical mortar every ~40px, offset per row
                float offset = (int)(y * 0.12f / 3.14159f) % 2 == 0 ? 20 : 0;
                float brickV = Mathf.Abs(Mathf.Sin((x + offset) * 0.08f)) < 0.04f ? -0.02f : 0f;
                float v = Mathf.Clamp01(b + brickH + brickV);
                tex.SetPixel(x, y, new Color(v*0.95f, v*0.9f, v*0.88f));
            }
            tex.Apply();
            string p = "Assets/Textures/TX_BuildingDark.png";
            SaveTex(tex, p);
            ApplyTexToMat("Assets/Materials/SH_Building_Dark.mat", p, new Vector2(4,4), 0.1f);
        }

        static void GenerateBuildingGray()
        {
            int s = 512; var tex = new Texture2D(s, s, TextureFormat.RGB24, true);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float b = 0.35f;
                b += Mathf.PerlinNoise(x*0.008f+100, y*0.008f+100) * 0.06f;
                b += Mathf.PerlinNoise(x*0.04f+200, y*0.04f+200) * 0.03f;
                // Concrete panel lines
                float panelH = Mathf.Abs(y % 128) < 2 ? -0.06f : 0f;
                float panelV = Mathf.Abs(x % 96) < 2 ? -0.04f : 0f;
                // Water stains (dripping down)
                float stain = Mathf.PerlinNoise(x*0.02f+300, y*0.005f+300);
                float stainVal = stain > 0.6f ? -(stain-0.6f)*0.12f : 0f;
                float v = Mathf.Clamp01(b + panelH + panelV + stainVal);
                tex.SetPixel(x, y, new Color(v, v*0.98f, v*0.96f));
            }
            tex.Apply();
            string p = "Assets/Textures/TX_BuildingGray.png";
            SaveTex(tex, p);
            ApplyTexToMat("Assets/Materials/SH_Building_Gray.mat", p, new Vector2(3,3), 0.15f);
        }

        static void GenerateDirt()
        {
            int s = 512; var tex = new Texture2D(s, s, TextureFormat.RGB24, true);
            Random.InitState(99);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float br = 0.22f, bg = 0.16f, bb = 0.1f;
                float n1 = Mathf.PerlinNoise(x*0.01f, y*0.01f) * 0.08f;
                float n2 = Mathf.PerlinNoise(x*0.04f+150, y*0.04f+150) * 0.04f;
                float n3 = Mathf.PerlinNoise(x*0.15f+250, y*0.15f+250) * 0.03f;
                float speck = Random.value < 0.05f ? Random.value * 0.04f : 0f;
                float r = Mathf.Clamp01(br + n1 + n2 + n3 + speck);
                float g = Mathf.Clamp01(bg + n1*0.8f + n2*0.7f + n3 + speck*0.5f);
                float b2 = Mathf.Clamp01(bb + n1*0.5f + n2*0.5f + n3*0.6f);
                tex.SetPixel(x, y, new Color(r, g, b2));
            }
            tex.Apply();
            string p = "Assets/Textures/TX_Dirt.png";
            SaveTex(tex, p);
            ApplyTexToMat("Assets/Materials/SH_Dirt.mat", p, new Vector2(4,4), 0.05f);
        }

        static void GenerateWood()
        {
            int s = 512; var tex = new Texture2D(s, s, TextureFormat.RGB24, true);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                // Wood grain — elongated along Y
                float grain = Mathf.PerlinNoise(x*0.03f, y*0.005f) * 0.15f;
                float fine = Mathf.PerlinNoise(x*0.1f+400, y*0.02f+400) * 0.05f;
                float ring = Mathf.Sin(Mathf.PerlinNoise(x*0.01f, y*0.01f)*20) * 0.03f;
                float r = Mathf.Clamp01(0.3f + grain + fine + ring);
                float g = Mathf.Clamp01(0.2f + grain*0.8f + fine*0.7f + ring);
                float b = Mathf.Clamp01(0.1f + grain*0.4f + fine*0.4f);
                tex.SetPixel(x, y, new Color(r, g, b));
            }
            tex.Apply();
            string p = "Assets/Textures/TX_Wood.png";
            SaveTex(tex, p);
            ApplyTexToMat("Assets/Materials/SH_Wood.mat", p, new Vector2(2,2), 0.25f);
        }

        static void GenerateMetal()
        {
            int s = 512; var tex = new Texture2D(s, s, TextureFormat.RGB24, true);
            Random.InitState(55);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float b = 0.45f;
                b += Mathf.PerlinNoise(x*0.02f+500, y*0.02f+500) * 0.06f;
                float scratch = Mathf.PerlinNoise(x*0.005f+600, y*0.3f+600);
                float scratchVal = (scratch > 0.49f && scratch < 0.51f) ? 0.08f : 0f;
                // Rust spots
                float rust = Mathf.PerlinNoise(x*0.015f+700, y*0.015f+700);
                float rustR = 0f, rustG = 0f;
                if (rust > 0.6f) { rustR = (rust-0.6f)*0.3f; rustG = -(rust-0.6f)*0.1f; }
                float v = Mathf.Clamp01(b + scratchVal);
                tex.SetPixel(x, y, new Color(
                    Mathf.Clamp01(v + rustR),
                    Mathf.Clamp01(v*0.97f + rustG),
                    v*0.95f));
            }
            tex.Apply();
            string p = "Assets/Textures/TX_Metal.png";
            SaveTex(tex, p);
            ApplyTexToMat("Assets/Materials/SH_Metal.mat", p, new Vector2(2,2), 0.6f);
            ApplyTexToMat("Assets/Materials/SH_GateMetal.mat", p, new Vector2(1,3), 0.5f);
        }

        static void GenerateConcrete()
        {
            int s = 512; var tex = new Texture2D(s, s, TextureFormat.RGB24, true);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float b = 0.3f;
                b += Mathf.PerlinNoise(x*0.01f+800, y*0.01f+800) * 0.05f;
                b += Mathf.PerlinNoise(x*0.06f+900, y*0.06f+900) * 0.03f;
                float crack = Mathf.PerlinNoise(x*0.02f+1000, y*0.02f+1000);
                float crackVal = (crack > 0.49f && crack < 0.51f) ? -0.06f : 0f;
                float v = Mathf.Clamp01(b + crackVal);
                tex.SetPixel(x, y, new Color(v, v*0.98f, v*0.95f));
            }
            tex.Apply();
            string p = "Assets/Textures/TX_Concrete.png";
            SaveTex(tex, p);
            ApplyTexToMat("Assets/Materials/SH_Concrete.mat", p, new Vector2(6,6), 0.15f);
        }
    }
}
