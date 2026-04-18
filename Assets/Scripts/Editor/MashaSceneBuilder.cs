using UnityEngine;
using UnityEditor;

namespace LudumDare.Editor
{
    public static class MashaSceneBuilder
    {
        [MenuItem("Tools/Build Masha Scene")]
        public static void BuildScene()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Build Masha Scene");

            CreateMaterials();
            CreateGroundBase();
            SetDarkSky();
            BuildForkLayout();
            CreateBodyBag();
            CreateItems();
            CreateLockedGate();
            CreateBurialArea();

            EditorUtility.SetDirty(RenderSettings.skybox);
            Debug.Log("[MashaSceneBuilder] Scene built successfully!");
        }

        static Material GetOrCreateMat(string path, Color color, string shaderName = "Universal Render Pipeline/Lit")
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            var shader = Shader.Find(shaderName);
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void CreateMaterials()
        {
            GetOrCreateMat("Assets/Materials/SH_BlackSky.mat", Color.black, "Skybox/Procedural");
            GetOrCreateMat("Assets/Materials/SH_Dirt.mat", new Color(0.25f, 0.15f, 0.08f));
            GetOrCreateMat("Assets/Materials/SH_DarkCloth.mat", new Color(0.08f, 0.06f, 0.05f));
            GetOrCreateMat("Assets/Materials/SH_Wood.mat", new Color(0.35f, 0.22f, 0.1f));
            GetOrCreateMat("Assets/Materials/SH_Metal.mat", new Color(0.4f, 0.4f, 0.42f));
            GetOrCreateMat("Assets/Materials/SH_Rope.mat", new Color(0.55f, 0.45f, 0.3f));
            GetOrCreateMat("Assets/Materials/SH_GateMetal.mat", new Color(0.2f, 0.2f, 0.22f));
            GetOrCreateMat("Assets/Materials/SH_Ground.mat", new Color(0.12f, 0.1f, 0.08f));
            AssetDatabase.SaveAssets();
        }

        static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 pos, Vector3 scale, string matPath, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            if (parent != null) go.transform.SetParent(parent, true);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            return go;
        }

        static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Quaternion rot, string matPath, Transform parent = null)
        {
            var go = CreatePrimitive(name, type, pos, scale, matPath, parent);
            go.transform.rotation = rot;
            return go;
        }

        static void CreateGroundBase()
        {
            // Large ground under entire map to prevent falling through
            CreatePrimitive("Ground_Base", PrimitiveType.Cube,
                new Vector3(0, -0.55f, 30), new Vector3(100, 1, 120),
                "Assets/Materials/SH_Ground.mat");
        }

        [MenuItem("Tools/Build Alley Gate")]
        public static void BuildAlleyGate()
        {
            var old = GameObject.Find("RightAlleyGate");
            if (old != null) Undo.DestroyObjectImmediate(old);

            var parent = new GameObject("RightAlleyGate");
            parent.transform.position = new Vector3(9, 0, 32);
            Undo.RegisterCreatedObjectUndo(parent, "Create Alley Gate");
            var p = parent.transform;

            string metalMat = "Assets/Materials/SH_Metal.mat";
            string gateMat = "Assets/Materials/SH_GateMetal.mat";

            // Posts at front and back of opening (along Z axis, gate spans X)
            float gateX = 9f; // in the gap between fence and buildings
            float z = 32f;

            // Left post (closer to street)
            CreatePrimitive("GatePost_L", PrimitiveType.Cylinder,
                new Vector3(gateX, 1.4f, z - 3.5f), new Vector3(0.14f, 1.4f, 0.14f),
                metalMat, p);
            // Right post (far side)
            CreatePrimitive("GatePost_R", PrimitiveType.Cylinder,
                new Vector3(gateX, 1.4f, z + 3.5f), new Vector3(0.14f, 1.4f, 0.14f),
                metalMat, p);

            // Top bar (horizontal along Z)
            CreatePrimitive("GateTopBar", PrimitiveType.Cylinder,
                new Vector3(gateX, 2.8f, z), new Vector3(0.08f, 3.5f, 0.08f),
                Quaternion.Euler(90, 0, 0), metalMat, p);

            // Vertical bars along Z
            for (int i = 0; i < 12; i++)
            {
                float bz = z - 3f + i * 0.55f;
                CreatePrimitive($"GateBar_{i}", PrimitiveType.Cylinder,
                    new Vector3(gateX, 1.3f, bz), new Vector3(0.04f, 1.3f, 0.04f),
                    gateMat, p);
            }

            // Horizontal crossbars (along Z)
            CreatePrimitive("GateCross_Low", PrimitiveType.Cylinder,
                new Vector3(gateX, 0.5f, z), new Vector3(0.05f, 3.2f, 0.05f),
                Quaternion.Euler(90, 0, 0), gateMat, p);
            CreatePrimitive("GateCross_Mid", PrimitiveType.Cylinder,
                new Vector3(gateX, 1.5f, z), new Vector3(0.05f, 3.2f, 0.05f),
                Quaternion.Euler(90, 0, 0), gateMat, p);

            // Spikes on top
            for (int i = 0; i < 12; i++)
            {
                float bz = z - 3f + i * 0.55f;
                CreatePrimitive($"GateSpike_{i}", PrimitiveType.Sphere,
                    new Vector3(gateX, 2.65f, bz), new Vector3(0.07f, 0.12f, 0.07f),
                    metalMat, p);
            }

            // Post caps
            CreatePrimitive("PostCap_L", PrimitiveType.Sphere,
                new Vector3(gateX, 2.85f, z - 3.5f), new Vector3(0.22f, 0.22f, 0.22f),
                metalMat, p);
            CreatePrimitive("PostCap_R", PrimitiveType.Sphere,
                new Vector3(gateX, 2.85f, z + 3.5f), new Vector3(0.22f, 0.22f, 0.22f),
                metalMat, p);

            // Padlock (facing street side)
            CreatePrimitive("GatePadlock_Body", PrimitiveType.Cube,
                new Vector3(gateX - 0.1f, 1.0f, z), new Vector3(0.1f, 0.15f, 0.12f),
                metalMat, p);
            CreatePrimitive("GatePadlock_Shackle", PrimitiveType.Cylinder,
                new Vector3(gateX - 0.1f, 1.13f, z), new Vector3(0.08f, 0.04f, 0.08f),
                metalMat, p);

            // Chain
            CreatePrimitive("GateChain_1", PrimitiveType.Cylinder,
                new Vector3(gateX - 0.08f, 1.0f, z - 0.15f), new Vector3(0.025f, 0.15f, 0.025f),
                Quaternion.Euler(30, 0, 0), metalMat, p);
            CreatePrimitive("GateChain_2", PrimitiveType.Cylinder,
                new Vector3(gateX - 0.08f, 1.0f, z + 0.15f), new Vector3(0.025f, 0.15f, 0.025f),
                Quaternion.Euler(-30, 0, 0), metalMat, p);

            Debug.Log("[MashaSceneBuilder] Alley gate built!");
        }

        [MenuItem("Tools/Add Red Lanterns")]
        public static void AddRedLanterns()
        {
            var old1 = GameObject.Find("RedLantern_Left");
            var old2 = GameObject.Find("RedLantern_Right");
            if (old1 != null) Undo.DestroyObjectImmediate(old1);
            if (old2 != null) Undo.DestroyObjectImmediate(old2);

            CreateLantern("RedLantern_Left", new Vector3(-15, 0, 18));
            CreateLantern("RedLantern_Right", new Vector3(16, 0, 32));

            Debug.Log("[MashaSceneBuilder] Red lanterns added!");
        }

        static void CreateLantern(string name, Vector3 basePos)
        {
            string metalMat = "Assets/Materials/SH_Metal.mat";

            var parent = new GameObject(name);
            parent.transform.position = basePos;
            Undo.RegisterCreatedObjectUndo(parent, "Create " + name);
            var p = parent.transform;

            // Pole
            CreatePrimitive(name + "_Pole", PrimitiveType.Cylinder,
                basePos + new Vector3(0, 2f, 0), new Vector3(0.08f, 2f, 0.08f),
                metalMat, p);

            // Arm (horizontal bar at top)
            CreatePrimitive(name + "_Arm", PrimitiveType.Cylinder,
                basePos + new Vector3(0.4f, 3.8f, 0), new Vector3(0.05f, 0.4f, 0.05f),
                Quaternion.Euler(0, 0, 90), metalMat, p);

            // Lamp housing (cube)
            CreatePrimitive(name + "_Housing", PrimitiveType.Cube,
                basePos + new Vector3(0.8f, 3.6f, 0), new Vector3(0.25f, 0.3f, 0.25f),
                metalMat, p);

            // Red bulb (sphere, emissive)
            var bulb = CreatePrimitive(name + "_Bulb", PrimitiveType.Sphere,
                basePos + new Vector3(0.8f, 3.4f, 0), new Vector3(0.15f, 0.15f, 0.15f),
                metalMat, p);

            // Create red emissive material for bulb
            var bulbMatPath = "Assets/Materials/SH_RedBulb.mat";
            var bulbMat = AssetDatabase.LoadAssetAtPath<Material>(bulbMatPath);
            if (bulbMat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                bulbMat = new Material(shader);
                AssetDatabase.CreateAsset(bulbMat, bulbMatPath);
            }
            bulbMat.SetColor("_BaseColor", new Color(1f, 0.1f, 0.05f));
            bulbMat.SetColor("_EmissionColor", new Color(2f, 0.2f, 0.1f));
            bulbMat.EnableKeyword("_EMISSION");
            bulbMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(bulbMat);
            bulb.GetComponent<Renderer>().sharedMaterial = bulbMat;

            // Point light
            var lightGO = new GameObject(name + "_Light");
            lightGO.transform.position = basePos + new Vector3(0.8f, 3.3f, 0);
            lightGO.transform.SetParent(p, true);
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.15f, 0.1f);
            light.intensity = 5f;
            light.range = 12f;
            Undo.RegisterCreatedObjectUndo(lightGO, "Create " + name + " Light");
        }

        [MenuItem("Tools/Add Building Fences")]
        public static void AddBuildingFences()
        {
            var old = GameObject.Find("--- BUILDING FENCES ---");
            if (old != null) Undo.DestroyObjectImmediate(old);

            var parent = new GameObject("--- BUILDING FENCES ---");
            parent.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(parent, "Create Building Fences");
            var p = parent.transform;

            string gateMat = "Assets/Materials/SH_GateMetal.mat";

            float fenceH = 2.5f; // lower fences
            float fenceY = fenceH / 2f;
            string darkMat = "Assets/Materials/SH_Building_Dark.mat";

            // LEFT SIDE (x=-7): gap for left alley z=13.5..22.5
            CreatePrimitive("Fence_L_A", PrimitiveType.Cube, new Vector3(-7, fenceY, 3.75f), new Vector3(0.3f, fenceH, 19.5f), darkMat, p);
            CreatePrimitive("Fence_L_B", PrimitiveType.Cube, new Vector3(-7, fenceY, 34.75f), new Vector3(0.3f, fenceH, 24.5f), darkMat, p);

            // RIGHT SIDE (x=7): gap for right alley z=27.5..36.5
            CreatePrimitive("Fence_R_A", PrimitiveType.Cube, new Vector3(7, fenceY, 10.75f), new Vector3(0.3f, fenceH, 33.5f), darkMat, p);
            CreatePrimitive("Fence_R_B", PrimitiveType.Cube, new Vector3(7, fenceY, 41.75f), new Vector3(0.3f, fenceH, 10.5f), darkMat, p);

            Debug.Log("[MashaSceneBuilder] Building fences added!");
        }

        [MenuItem("Tools/Add Boundary Walls")]
        public static void AddBoundaryWalls()
        {
            var old = GameObject.Find("--- BOUNDARIES ---");
            if (old != null) Undo.DestroyObjectImmediate(old);

            var parent = new GameObject("--- BOUNDARIES ---");
            parent.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(parent, "Create Boundaries");
            var p = parent.transform;

            // Invisible wall helper — cube with collider, no renderer
            System.Action<string, Vector3, Vector3> wall = (name, pos, scale) =>
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.position = pos;
                go.transform.localScale = scale;
                go.transform.SetParent(p, true);
                go.GetComponent<Renderer>().enabled = false; // invisible
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            };

            // === LEFT WALL — gap at z=13.5..22.5 for left alley ===
            wall("Wall_Left_A", new Vector3(-7, 2.5f, 4), new Vector3(0.5f, 6, 18));   // z=-5 to z=13
            wall("Wall_Left_B", new Vector3(-7, 2.5f, 40), new Vector3(0.5f, 6, 34));  // z=23 to z=57

            // === RIGHT WALL — gap at z=27.5..36.5 for right alley ===
            wall("Wall_Right_A", new Vector3(7, 2.5f, 11), new Vector3(0.5f, 6, 32));  // z=-5 to z=27
            wall("Wall_Right_B", new Vector3(7, 2.5f, 51), new Vector3(0.5f, 6, 28));  // z=37 to z=65

            // === BACK WALL ===
            wall("Wall_Back", new Vector3(0, 2.5f, -5), new Vector3(16, 6, 0.5f));

            // === END WALL ===
            wall("Wall_End", new Vector3(0, 2.5f, 64), new Vector3(16, 6, 0.5f));

            // === LEFT ALLEY (z=13.5..22.5) ===
            wall("Wall_LeftAlley_Far", new Vector3(-18.5f, 2.5f, 18), new Vector3(0.5f, 6, 10));
            wall("Wall_LeftAlley_Back", new Vector3(-12.5f, 2.5f, 13), new Vector3(12, 6, 0.5f));
            wall("Wall_LeftAlley_Front", new Vector3(-12.5f, 2.5f, 23), new Vector3(12, 6, 0.5f));

            // === RIGHT ALLEY (z=27.5..36.5) ===
            wall("Wall_RightAlley_Far", new Vector3(18.5f, 2.5f, 32), new Vector3(0.5f, 6, 10));
            wall("Wall_RightAlley_Back", new Vector3(12.5f, 2.5f, 27), new Vector3(12, 6, 0.5f));
            wall("Wall_RightAlley_Front", new Vector3(12.5f, 2.5f, 37), new Vector3(12, 6, 0.5f));

            Debug.Log("[MashaSceneBuilder] Boundary walls added!");
        }

        [MenuItem("Tools/Update Sky (Lighter)")]
        public static void UpdateSkyLighter()
        {
            SetDarkSky();
            Debug.Log("[MashaSceneBuilder] Sky updated (lighter)");
        }

        static void SetDarkSky()
        {
            // Solid dark grey sky — kill ALL blue sources
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.1f, 0.1f, 0.1f);
            RenderSettings.ambientSkyColor = new Color(0.1f, 0.1f, 0.1f);
            RenderSettings.ambientEquatorColor = new Color(0.1f, 0.1f, 0.1f);
            RenderSettings.ambientGroundColor = new Color(0.1f, 0.1f, 0.1f);
            RenderSettings.subtractiveShadowColor = new Color(0.1f, 0.1f, 0.1f);
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = null;
            RenderSettings.reflectionIntensity = 0f;

            // Camera solid grey
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            }

            // Grey fog
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.15f, 0.15f, 0.15f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.015f;

            // Dim grey light, no sun
            var dirLight = GameObject.Find("Directional Light");
            if (dirLight != null)
            {
                var light = dirLight.GetComponent<Light>();
                light.intensity = 0.3f;
                light.color = new Color(0.6f, 0.6f, 0.6f);
                dirLight.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            }
        }

        static void BuildForkLayout()
        {
            // Parent for fork geometry
            var forkParent = new GameObject("--- FORK AREA ---");
            forkParent.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(forkParent, "Create Fork Area");
            var fp = forkParent.transform;

            // === LEFT ALLEY (x negative, starts at z=28) ===
            // Left alley ground
            CreatePrimitive("Alley_Left_Ground", PrimitiveType.Cube,
                new Vector3(-12, -0.05f, 30), new Vector3(12, 0.1f, 10),
                "Assets/Materials/SH_Concrete.mat", fp);

            // Left alley walls (buildings blocking sides)
            CreatePrimitive("Alley_Left_Wall_Far", PrimitiveType.Cube,
                new Vector3(-18, 2, 30), new Vector3(1, 5, 12),
                "Assets/Materials/SH_Building_Dark.mat", fp);
            CreatePrimitive("Alley_Left_Wall_Near_L", PrimitiveType.Cube,
                new Vector3(-12, 2, 25.5f), new Vector3(12, 5, 1),
                "Assets/Materials/SH_Building_Dark.mat", fp);
            CreatePrimitive("Alley_Left_Wall_Near_R", PrimitiveType.Cube,
                new Vector3(-12, 2, 34.5f), new Vector3(12, 5, 1),
                "Assets/Materials/SH_Building_Dark.mat", fp);

            // Dim light in left alley
            var leftLight = new GameObject("AlleyLeft_Light");
            leftLight.transform.position = new Vector3(-14, 3.5f, 30);
            leftLight.transform.SetParent(fp, true);
            var ll = leftLight.AddComponent<Light>();
            ll.type = LightType.Point;
            ll.color = new Color(0.6f, 0.5f, 0.3f);
            ll.intensity = 0.6f;
            ll.range = 10;
            Undo.RegisterCreatedObjectUndo(leftLight, "Create Left Light");

            // === RIGHT ALLEY (x positive, starts at z=28) ===
            CreatePrimitive("Alley_Right_Ground", PrimitiveType.Cube,
                new Vector3(12, -0.05f, 30), new Vector3(12, 0.1f, 10),
                "Assets/Materials/SH_Concrete.mat", fp);

            CreatePrimitive("Alley_Right_Wall_Far", PrimitiveType.Cube,
                new Vector3(18, 2, 30), new Vector3(1, 5, 12),
                "Assets/Materials/SH_Building_Dark.mat", fp);
            CreatePrimitive("Alley_Right_Wall_Near_L", PrimitiveType.Cube,
                new Vector3(12, 2, 25.5f), new Vector3(12, 5, 1),
                "Assets/Materials/SH_Building_Dark.mat", fp);
            CreatePrimitive("Alley_Right_Wall_Near_R", PrimitiveType.Cube,
                new Vector3(12, 2, 34.5f), new Vector3(12, 5, 1),
                "Assets/Materials/SH_Building_Dark.mat", fp);

            // Dim light in right alley
            var rightLight = new GameObject("AlleyRight_Light");
            rightLight.transform.position = new Vector3(14, 3.5f, 30);
            rightLight.transform.SetParent(fp, true);
            var rl = rightLight.AddComponent<Light>();
            rl.type = LightType.Point;
            rl.color = new Color(0.5f, 0.4f, 0.3f);
            rl.intensity = 0.5f;
            rl.range = 10;
            Undo.RegisterCreatedObjectUndo(rightLight, "Create Right Light");

            // === OPENINGS in building walls at z=28 so player can enter alleys ===
            // The existing buildings on left go from x=-9. We need a gap.
            // We'll add guide walls that funnel the player to choose left or right
            CreatePrimitive("Fork_Divider_Center", PrimitiveType.Cube,
                new Vector3(0, 1.5f, 32), new Vector3(2, 3, 6),
                "Assets/Materials/SH_Building_Gray.mat", fp);
        }

        [MenuItem("Tools/Rebuild BodyBag")]
        public static void RebuildBodyBag()
        {
            // Delete old if exists
            var old = GameObject.Find("BodyBag");
            if (old != null) Undo.DestroyObjectImmediate(old);
            CreateBodyBag();
            Debug.Log("[MashaSceneBuilder] BodyBag rebuilt!");
        }

        static void CreateBodyBag()
        {
            var bodyParent = new GameObject("BodyBag");
            bodyParent.transform.position = new Vector3(0.5f, 0, 2);
            Undo.RegisterCreatedObjectUndo(bodyParent, "Create BodyBag");
            var bp = bodyParent.transform;

            string clothMat = "Assets/Materials/SH_DarkCloth.mat";
            string ropeMat = "Assets/Materials/SH_Rope.mat";

            // Torso — main body mass (capsule, lying on side)
            CreatePrimitive("Bag_Torso", PrimitiveType.Capsule,
                new Vector3(0.5f, 0.2f, 2.1f), new Vector3(0.38f, 0.2f, 0.55f),
                Quaternion.Euler(90, 0, 0), clothMat, bp);

            // Head (sphere, slightly raised)
            CreatePrimitive("Bag_Head", PrimitiveType.Sphere,
                new Vector3(0.5f, 0.18f, 2.7f), new Vector3(0.26f, 0.24f, 0.26f),
                clothMat, bp);

            // Neck connection (small capsule)
            CreatePrimitive("Bag_Neck", PrimitiveType.Capsule,
                new Vector3(0.5f, 0.17f, 2.5f), new Vector3(0.15f, 0.1f, 0.15f),
                Quaternion.Euler(90, 0, 0), clothMat, bp);

            // Hips (capsule, wider)
            CreatePrimitive("Bag_Hips", PrimitiveType.Capsule,
                new Vector3(0.5f, 0.18f, 1.65f), new Vector3(0.35f, 0.18f, 0.3f),
                Quaternion.Euler(90, 0, 0), clothMat, bp);

            // Left leg (capsule)
            CreatePrimitive("Bag_LegL", PrimitiveType.Capsule,
                new Vector3(0.38f, 0.13f, 1.15f), new Vector3(0.14f, 0.28f, 0.14f),
                Quaternion.Euler(90, 0, 5), clothMat, bp);

            // Right leg (capsule)
            CreatePrimitive("Bag_LegR", PrimitiveType.Capsule,
                new Vector3(0.62f, 0.13f, 1.15f), new Vector3(0.14f, 0.28f, 0.14f),
                Quaternion.Euler(90, 0, -5), clothMat, bp);

            // Left foot
            CreatePrimitive("Bag_FootL", PrimitiveType.Sphere,
                new Vector3(0.36f, 0.1f, 0.8f), new Vector3(0.12f, 0.1f, 0.16f),
                clothMat, bp);

            // Right foot
            CreatePrimitive("Bag_FootR", PrimitiveType.Sphere,
                new Vector3(0.64f, 0.1f, 0.8f), new Vector3(0.12f, 0.1f, 0.16f),
                clothMat, bp);

            // Rope ties
            CreatePrimitive("Rope_Chest", PrimitiveType.Cylinder,
                new Vector3(0.5f, 0.22f, 2.2f), new Vector3(0.44f, 0.015f, 0.44f),
                ropeMat, bp);
            CreatePrimitive("Rope_Waist", PrimitiveType.Cylinder,
                new Vector3(0.5f, 0.2f, 1.7f), new Vector3(0.42f, 0.015f, 0.42f),
                ropeMat, bp);
            CreatePrimitive("Rope_Knees", PrimitiveType.Cylinder,
                new Vector3(0.5f, 0.15f, 1.15f), new Vector3(0.35f, 0.015f, 0.35f),
                ropeMat, bp);

            // Subtle warm light
            var bodyLight = new GameObject("BodyBag_Light");
            bodyLight.transform.position = new Vector3(0.5f, 1.5f, 2);
            bodyLight.transform.SetParent(bp, true);
            var bl = bodyLight.AddComponent<Light>();
            bl.type = LightType.Point;
            bl.color = new Color(0.8f, 0.6f, 0.4f);
            bl.intensity = 0.8f;
            bl.range = 6;
            Undo.RegisterCreatedObjectUndo(bodyLight, "Create Body Light");
        }

        static void CreateItems()
        {
            string metalMat = "Assets/Materials/SH_Metal.mat";
            string woodMat = "Assets/Materials/SH_Wood.mat";

            // === RIGHT ALLEY: AXE + KEY ===
            var rightItems = new GameObject("--- RIGHT ALLEY ITEMS ---");
            rightItems.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(rightItems, "Create Right Items");
            var ri = rightItems.transform;

            // Table / crate to place items on
            CreatePrimitive("Crate_Right", PrimitiveType.Cube,
                new Vector3(15, 0.4f, 30), new Vector3(1.2f, 0.8f, 0.8f),
                woodMat, ri);

            // Axe: handle (cylinder) + head (cube)
            var axe = new GameObject("Axe");
            axe.transform.position = new Vector3(15.3f, 0.85f, 30);
            axe.transform.rotation = Quaternion.Euler(0, 0, 80);
            axe.transform.SetParent(ri, true);
            Undo.RegisterCreatedObjectUndo(axe, "Create Axe");

            CreatePrimitive("Axe_Handle", PrimitiveType.Cylinder,
                new Vector3(15.3f, 0.85f, 30), new Vector3(0.06f, 0.35f, 0.06f),
                woodMat, axe.transform);
            CreatePrimitive("Axe_Head", PrimitiveType.Cube,
                new Vector3(15.3f, 1.15f, 30), new Vector3(0.2f, 0.15f, 0.04f),
                metalMat, axe.transform);

            // Key (small cylinder + cube for key shape)
            var key = new GameObject("Key");
            key.transform.position = new Vector3(14.7f, 0.85f, 29.8f);
            key.transform.SetParent(ri, true);
            Undo.RegisterCreatedObjectUndo(key, "Create Key");

            CreatePrimitive("Key_Shaft", PrimitiveType.Cylinder,
                new Vector3(14.7f, 0.85f, 29.8f), new Vector3(0.03f, 0.12f, 0.03f),
                Quaternion.Euler(0, 0, 90), metalMat, key.transform);
            CreatePrimitive("Key_Head", PrimitiveType.Cylinder,
                new Vector3(14.55f, 0.85f, 29.8f), new Vector3(0.08f, 0.015f, 0.08f),
                metalMat, key.transform);

            // === LEFT ALLEY: SHOVEL ===
            var leftItems = new GameObject("--- LEFT ALLEY ITEMS ---");
            leftItems.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(leftItems, "Create Left Items");
            var li = leftItems.transform;

            // Shovel leaning against wall
            var shovel = new GameObject("Shovel");
            shovel.transform.position = new Vector3(-16, 0, 29);
            shovel.transform.rotation = Quaternion.Euler(0, 0, 10);
            shovel.transform.SetParent(li, true);
            Undo.RegisterCreatedObjectUndo(shovel, "Create Shovel");

            CreatePrimitive("Shovel_Handle", PrimitiveType.Cylinder,
                new Vector3(-16, 0.65f, 29), new Vector3(0.05f, 0.6f, 0.05f),
                Quaternion.Euler(0, 0, 10), woodMat, shovel.transform);
            CreatePrimitive("Shovel_Blade", PrimitiveType.Cube,
                new Vector3(-15.85f, 1.28f, 29), new Vector3(0.2f, 0.25f, 0.03f),
                Quaternion.Euler(0, 0, 10), metalMat, shovel.transform);

            // Extra item: Plastic tarp / sheet (flat quad on floor)
            CreatePrimitive("PlasticTarp", PrimitiveType.Quad,
                new Vector3(-14, 0.02f, 31), new Vector3(1.5f, 1.5f, 1),
                Quaternion.Euler(90, 0, 15), "Assets/Materials/SH_DarkCloth.mat", li);

            // Barrel (cylinder)
            CreatePrimitive("Barrel", PrimitiveType.Cylinder,
                new Vector3(-15, 0.5f, 32), new Vector3(0.6f, 0.5f, 0.6f),
                "Assets/Materials/SH_Rust.mat", li);
        }

        static void CreateLockedGate()
        {
            // Locked fence/gate across the main road at z=38
            var gateParent = new GameObject("--- LOCKED GATE ---");
            gateParent.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(gateParent, "Create Locked Gate");
            var gp = gateParent.transform;

            string gateMat = "Assets/Materials/SH_GateMetal.mat";
            string metalMat = "Assets/Materials/SH_Metal.mat";

            // Left fence section (from left building to gate)
            CreatePrimitive("GateFence_Left", PrimitiveType.Cube,
                new Vector3(-4.5f, 1.2f, 38), new Vector3(4, 2.4f, 0.15f),
                gateMat, gp);

            // Right fence section (from gate to right building)
            CreatePrimitive("GateFence_Right", PrimitiveType.Cube,
                new Vector3(4.5f, 1.2f, 38), new Vector3(4, 2.4f, 0.15f),
                gateMat, gp);

            // Gate door (the part that opens - centered, 2.5m wide)
            var gateDoor = CreatePrimitive("GateDoor", PrimitiveType.Cube,
                new Vector3(0, 1.2f, 38), new Vector3(2.5f, 2.4f, 0.1f),
                gateMat, gp);

            // Gate posts
            CreatePrimitive("GatePost_Left", PrimitiveType.Cylinder,
                new Vector3(-1.25f, 1.3f, 38), new Vector3(0.15f, 1.3f, 0.15f),
                metalMat, gp);
            CreatePrimitive("GatePost_Right", PrimitiveType.Cylinder,
                new Vector3(1.25f, 1.3f, 38), new Vector3(0.15f, 1.3f, 0.15f),
                metalMat, gp);

            // Lock (visible on gate)
            CreatePrimitive("GateLock", PrimitiveType.Cube,
                new Vector3(0, 1.0f, 37.9f), new Vector3(0.15f, 0.12f, 0.08f),
                metalMat, gp);

            // Fence bars on the gate (vertical lines to look like a real gate)
            for (int i = -4; i <= 4; i++)
            {
                CreatePrimitive($"GateBar_{i + 4}", PrimitiveType.Cylinder,
                    new Vector3(i * 0.25f, 1.2f, 38), new Vector3(0.04f, 1.2f, 0.04f),
                    metalMat, gp);
            }

            // Sign: keep out (small board)
            CreatePrimitive("GateSign", PrimitiveType.Cube,
                new Vector3(0, 2.2f, 37.9f), new Vector3(1.0f, 0.3f, 0.05f),
                "Assets/Materials/SH_Wood.mat", gp);

            // Dim red light on gate
            var gateLight = new GameObject("Gate_Light");
            gateLight.transform.position = new Vector3(0, 2.8f, 37.5f);
            gateLight.transform.SetParent(gp, true);
            var gl = gateLight.AddComponent<Light>();
            gl.type = LightType.Point;
            gl.color = new Color(0.8f, 0.2f, 0.1f);
            gl.intensity = 0.5f;
            gl.range = 6;
            Undo.RegisterCreatedObjectUndo(gateLight, "Create Gate Light");
        }

        static void CreateBurialArea()
        {
            // Area beyond the gate (z=40 to z=55) - dirt clearing
            var burialParent = new GameObject("--- BURIAL AREA ---");
            burialParent.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(burialParent, "Create Burial Area");
            var bp = burialParent.transform;

            string dirtMat = "Assets/Materials/SH_Dirt.mat";

            // Dirt ground
            CreatePrimitive("Burial_Ground", PrimitiveType.Cube,
                new Vector3(0, -0.04f, 48), new Vector3(14, 0.08f, 16),
                dirtMat, bp);

            // Surrounding walls/hedges (to create an enclosed clearing)
            CreatePrimitive("Burial_Wall_Left", PrimitiveType.Cube,
                new Vector3(-7.5f, 1.5f, 48), new Vector3(1, 3, 16),
                "Assets/Materials/SH_Building_Dark.mat", bp);
            CreatePrimitive("Burial_Wall_Right", PrimitiveType.Cube,
                new Vector3(7.5f, 1.5f, 48), new Vector3(1, 3, 16),
                "Assets/Materials/SH_Building_Dark.mat", bp);
            CreatePrimitive("Burial_Wall_Back", PrimitiveType.Cube,
                new Vector3(0, 1.5f, 56), new Vector3(16, 3, 1),
                "Assets/Materials/SH_Building_Dark.mat", bp);

            // Soft dirt mound (where the digging will happen)
            CreatePrimitive("DirtMound", PrimitiveType.Cube,
                new Vector3(0, 0.15f, 50), new Vector3(2.5f, 0.3f, 3),
                dirtMat, bp);

            // A few rocks
            CreatePrimitive("Rock_1", PrimitiveType.Sphere,
                new Vector3(3, 0.2f, 46), new Vector3(0.6f, 0.4f, 0.5f),
                "Assets/Materials/SH_Concrete.mat", bp);
            CreatePrimitive("Rock_2", PrimitiveType.Sphere,
                new Vector3(-4, 0.15f, 52), new Vector3(0.4f, 0.3f, 0.5f),
                "Assets/Materials/SH_Concrete.mat", bp);
            CreatePrimitive("Rock_3", PrimitiveType.Sphere,
                new Vector3(5, 0.25f, 49), new Vector3(0.8f, 0.5f, 0.7f),
                "Assets/Materials/SH_Concrete.mat", bp);

            // Dead tree stump
            CreatePrimitive("TreeStump", PrimitiveType.Cylinder,
                new Vector3(-3, 0.4f, 47), new Vector3(0.5f, 0.4f, 0.5f),
                "Assets/Materials/SH_Wood.mat", bp);

            // Eerie light in burial area
            var burialLight = new GameObject("Burial_Light");
            burialLight.transform.position = new Vector3(0, 4, 48);
            burialLight.transform.SetParent(bp, true);
            var bl = burialLight.AddComponent<Light>();
            bl.type = LightType.Point;
            bl.color = new Color(0.3f, 0.4f, 0.6f);
            bl.intensity = 0.4f;
            bl.range = 15;
            Undo.RegisterCreatedObjectUndo(burialLight, "Create Burial Light");
        }
    }
}
