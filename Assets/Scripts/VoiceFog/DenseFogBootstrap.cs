using UnityEngine;

namespace LudumDare.VoiceFog
{
    /// <summary>
    /// Dense distance fog via <see cref="RenderSettings"/> — heavy white-grey haze with short visibility (reference look).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class DenseFogBootstrap : MonoBehaviour
    {
        [SerializeField] Color fogColor = new Color(0.28f, 0.30f, 0.34f);
        [Header("Exponential fog (when Use linear fog is off)")]
        [SerializeField] [Range(0.001f, 0.35f)] float fogDensity = 0.2f;

        [Header("Linear fog — sharp distance cutoff, good for «wall» of whiteout")]
        [SerializeField] bool useLinearFog = false;
        [SerializeField] float linearFogStart = 1f;
        [SerializeField] float linearFogEnd = 12f;

        public static float BaselineDensity { get; private set; }

        void Awake()
        {
            BaselineDensity = fogDensity;

            if (TryGetComponent<Camera>(out var cam))
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = fogColor;
            }

            var volumeDriver = GetComponent<UniversalVolumeFogDriver>();
            if (volumeDriver != null)
            {
                var linearEnd = Mathf.Max(linearFogStart + 0.5f, linearFogEnd);
                volumeDriver.IngestBaselineFromBootstrap(fogColor, useLinearFog, linearFogStart, linearEnd, fogDensity);
                return;
            }

            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColor;

            if (useLinearFog)
            {
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = linearFogStart;
                RenderSettings.fogEndDistance = Mathf.Max(linearFogStart + 0.5f, linearFogEnd);
            }
            else
            {
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = fogDensity;
            }
        }
    }
}
