using UnityEngine;
using UnityEditor;

namespace LudumDare.Editor
{
    public static class FogParticleBuilder
    {
        [MenuItem("Tools/Create Ground Fog")]
        public static void CreateGroundFog()
        {
            var old = GameObject.Find("GroundFog");
            if (old != null) Undo.DestroyObjectImmediate(old);

            // Create material with URP Particles shader
            var matPath = "Assets/Materials/SH_FogParticle.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.SetColor("_BaseColor", new Color(0.6f, 0.6f, 0.65f, 0.08f));
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetFloat("_SoftParticlesEnabled", 1);
            mat.SetFloat("_SoftParticleFadeParams", 1);
            mat.renderQueue = 3000;
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();

            // Parent object
            var fogParent = new GameObject("GroundFog");
            fogParent.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(fogParent, "Create Ground Fog");

            // Create fog zones along the map
            var zones = new[] {
                new Vector3(0, 0.5f, 0),    // Start area
                new Vector3(0, 0.5f, 15),   // Left alley area
                new Vector3(0, 0.5f, 30),   // Right alley area
                new Vector3(0, 0.5f, 45),   // Gate area
                new Vector3(0, 0.5f, 55),   // Burial area
            };

            foreach (var pos in zones)
            {
                CreateFogZone(fogParent.transform, pos, mat);
            }

            // Add controller
            var controller = fogParent.AddComponent<LudumDare.Intro.ParticleFogController>();

            Debug.Log("[FogParticleBuilder] Ground fog zones created!");
        }

        static void CreateFogZone(Transform parent, Vector3 position, Material mat)
        {
            var zoneGO = new GameObject($"FogZone_z{position.z:F0}");
            zoneGO.transform.position = position;
            zoneGO.transform.SetParent(parent, true);

            var ps = zoneGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 10f;
            main.startSpeed = 0.15f;
            main.startSize = new ParticleSystem.MinMaxCurve(6f, 14f);
            main.startColor = new Color(0.5f, 0.5f, 0.55f, 0.12f);
            main.maxParticles = 100;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);

            var emission = ps.emission;
            emission.rateOverTime = 12;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(14, 2.5f, 14);

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.5f, 0.5f, 0.55f), 0f),
                    new GradientColorKey(new Color(0.5f, 0.5f, 0.55f), 1f)
                },
                new[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.15f, 0.2f),
                    new GradientAlphaKey(0.15f, 0.8f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLife.color = gradient;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.7f, 1, 1.3f));

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);

            var renderer = zoneGO.GetComponent<ParticleSystemRenderer>();
            renderer.material = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }
    }
}
