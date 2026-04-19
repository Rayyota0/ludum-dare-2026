using UnityEngine;
using UnityEngine.Rendering;

namespace LudumDare.VoiceFog
{
    /// <summary>
    /// Drives VoiceFog via <see cref="IFogController"/>: same weight remapping as the former legacy driver, pushes distance fog to
    /// <see cref="RenderSettings"/> (URP 17.4 deferred still uses environment fog here). Optional <see cref="useLocalClearBandAtWeightZero"/>
    /// adds meters to baseline linear fog distances at fog weight 0 so the near view opens slightly on «сигнал» without global clear.
    /// A high-priority global <see cref="Volume"/> is reserved for future URP <c>Fog</c> volume overrides when the package exposes them;
    /// the profile stays empty so scene defaults apply.
    /// </summary>
    [DefaultExecutionOrder(-99)]
    public sealed class UniversalVolumeFogDriver : MonoBehaviour, IFogController
    {
        bool _captured;
        FogMode _mode;
        Color _color;
        float _density;
        float _linearStart;
        float _linearEnd;

        float _authoritativeWeight = 1f;

        public float FogWeight => _authoritativeWeight;

        Camera _camera;
        Volume _voiceFogVolume;

        const float LinearClearFar = 7500f;

        [Header("Optional perceptual remap")]
        [SerializeField] bool useWeightRemapCurve;
        [SerializeField] AnimationCurve weightRemapCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Partial fog (clip weight range)")]
        [Tooltip("Gameplay weight 0 is remapped here (0 = as open as the rest of the driver allows, 1 = baseline dense). E.g. 0.3 = always keep ~30 % of the «dense» look even on signal.")]
        [SerializeField] [Range(0f, 1f)] float fogWeightWhenInputZero = 0.3f;

        [Tooltip("Gameplay weight 1 maps here (usually 1 = full baseline). Lower to never reach full wall fog.")]
        [SerializeField] [Range(0f, 1f)] float fogWeightWhenInputOne = 1f;

        [Tooltip("SmoothStep on fog strength before lerping distances/density. Off by default: when on, fade-out (1→0) tends to stay dense then snap clear at the end.")]
        [SerializeField] bool smoothStrengthEndpoints;

        [Header("Clear at weight 0 (linear fog)")]
        [Tooltip("If on, at gameplay fog weight 0 linear fog pushes fog start/end outward vs baseline (see extras). Unity: less fog closer than fogStart; fog ramps to full by fogEnd — larger values = better near visibility. Takes precedence over «full clear». Ignored for exponential fog.")]
        [SerializeField] bool useLocalClearBandAtWeightZero = true;

        [Tooltip("At fog weight 0 (linear): added to baseline fogStartDistance so the no-fog region extends this many meters farther from the camera.")]
        [SerializeField] float localClearFogStartExtraAtWeightZero = 8f;

        [Tooltip("At fog weight 0 (linear): added to baseline fogEndDistance so the full-fog distance moves outward with the ramp (keep ≥ 0).")]
        [SerializeField] float localClearFogEndExtraAtWeightZero = 6f;

        [Tooltip("If on, weight 0 pushes linear fog to very far distances (almost no fog). If off, only the start of the fog ramp moves outward; ramp width (end−start) stays as in the scene baseline, so haze does not vanish.")]
        [SerializeField] bool useFullVisualClearAtWeightZero;

        [Tooltip("When «full clear» and «local clear» are off: at weight 0 the linear ramp begins at this distance (m). End = this + baseline (end−start). Must be ≥ baseline start.")]
        [SerializeField] float outerFogRampStartAtWeightZero = 90f;

        [Header("Clear at weight 0 (exponential fog)")]
        [Tooltip("When «full clear» is off: density at weight 0 (still visible haze). Local clear band applies only to linear fog — exponential fog cannot carve a fixed-radius near-camera hole; use linear fog on DenseFogBootstrap for that.")]
        [SerializeField] [Range(0.0002f, 0.08f)] float relaxedFogDensity = 0.03f;

        [Header("Volume (placeholder for future URP Fog override)")]
        [Tooltip("Keeps a global Volume above scene defaults without adding overrides, so a Fog volume component can be wired later without reordering.")]
        [SerializeField] bool createReservedGlobalVolume = true;

        [SerializeField] float reservedVolumePriority = 100f;

        void Awake()
        {
            _camera = GetComponent<Camera>();
            if (!_captured)
                CaptureFromRenderSettings();
            if (_captured)
                PushToRenderSettings();
        }

        void OnEnable()
        {
            if (!_captured)
                CaptureFromRenderSettings();

            if (createReservedGlobalVolume && _voiceFogVolume == null)
                CreateReservedVolume();

            if (GraphicsSettings.defaultRenderPipeline != null)
                RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        void OnDisable()
        {
            if (GraphicsSettings.defaultRenderPipeline != null)
                RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        void OnDestroy()
        {
            if (_voiceFogVolume == null)
                return;
            var profile = _voiceFogVolume.profile;
            if (_voiceFogVolume.gameObject != null)
                Destroy(_voiceFogVolume.gameObject);
            _voiceFogVolume = null;
            if (profile != null)
                Destroy(profile);
        }

        void OnValidate()
        {
            if (weightRemapCurve == null || weightRemapCurve.length < 2)
                weightRemapCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            outerFogRampStartAtWeightZero = Mathf.Max(0f, outerFogRampStartAtWeightZero);
            localClearFogStartExtraAtWeightZero = Mathf.Max(0f, localClearFogStartExtraAtWeightZero);
            localClearFogEndExtraAtWeightZero = Mathf.Max(0f, localClearFogEndExtraAtWeightZero);
            if (fogWeightWhenInputZero > fogWeightWhenInputOne)
                fogWeightWhenInputZero = fogWeightWhenInputOne;
        }

        void CreateReservedVolume()
        {
            var existing = transform.Find("VoiceFog_ReservedVolume");
            if (existing != null)
            {
                if (existing.TryGetComponent(out Volume v))
                {
                    _voiceFogVolume = v;
                    return;
                }

                Destroy(existing.gameObject);
            }

            var go = new GameObject("VoiceFog_ReservedVolume");
            go.transform.SetParent(transform, false);
            _voiceFogVolume = go.AddComponent<Volume>();
            _voiceFogVolume.isGlobal = true;
            _voiceFogVolume.priority = reservedVolumePriority;
            _voiceFogVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        /// <summary>Called from <see cref="DenseFogBootstrap"/> (execution order -100) before this component's Awake.</summary>
        public void IngestBaselineFromBootstrap(Color fogColor, bool useLinearFog, float linearFogStart, float linearFogEnd, float fogDensity)
        {
            _color = fogColor;
            if (useLinearFog)
            {
                _mode = FogMode.Linear;
                _linearStart = linearFogStart;
                _linearEnd = linearFogEnd;
                _density = fogDensity;
            }
            else
            {
                _mode = FogMode.ExponentialSquared;
                _density = fogDensity;
                _linearStart = RenderSettings.fogStartDistance;
                _linearEnd = RenderSettings.fogEndDistance;
            }

            _captured = true;
        }

        /// <summary>URP (incl. deferred) applies legacy fog late; re-push right before our camera renders so distance fog tracks animated settings.</summary>
        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!_captured || _camera == null || camera != _camera)
                return;
            PushToRenderSettings();
        }

        void LateUpdate()
        {
            if (!_captured)
                return;
            // URP: beginCameraRendering should push each frame, but in some player builds timing can skip a frame;
            // repeating here keeps distance fog reliable (matches editor look on Artem / egor-interface).
            PushToRenderSettings();
        }

        void CaptureFromRenderSettings()
        {
            _mode = RenderSettings.fogMode;
            _color = RenderSettings.fogColor;
            _density = RenderSettings.fogDensity;
            _linearStart = RenderSettings.fogStartDistance;
            _linearEnd = RenderSettings.fogEndDistance;
            _captured = true;
        }

        /// <inheritdoc />
        public void SetFogWeight(float weight01)
        {
            if (!_captured)
                CaptureFromRenderSettings();

            _authoritativeWeight = Mathf.Clamp01(weight01);
            PushToRenderSettings();
        }

        void PushToRenderSettings()
        {
            var weight01 = Mathf.Clamp01(_authoritativeWeight);
            var w = Mathf.Lerp(fogWeightWhenInputZero, fogWeightWhenInputOne, weight01);
            if (useWeightRemapCurve && weightRemapCurve != null && weightRemapCurve.length >= 2)
                w = Mathf.Clamp01(weightRemapCurve.Evaluate(weight01));

            if (smoothStrengthEndpoints)
                w = Mathf.SmoothStep(0f, 1f, w);

            RenderSettings.fog = true;
            RenderSettings.fogMode = _mode;
            RenderSettings.fogColor = _color;

            if (_mode == FogMode.Linear)
            {
                if (useLocalClearBandAtWeightZero)
                {
                    // Lerp on raw gameplay weight so remap curves / partial-fog clips cannot invert «signal opens view» vs dense baseline.
                    var g = weight01;
                    var clearStart = _linearStart + localClearFogStartExtraAtWeightZero;
                    var clearEnd = Mathf.Max(clearStart + 0.05f, _linearEnd + localClearFogEndExtraAtWeightZero);
                    RenderSettings.fogStartDistance = Mathf.Lerp(clearStart, _linearStart, g);
                    RenderSettings.fogEndDistance = Mathf.Lerp(clearEnd, _linearEnd, g);
                }
                else if (useFullVisualClearAtWeightZero)
                {
                    var clearStart = Mathf.Max(0f, LinearClearFar - 400f);
                    RenderSettings.fogStartDistance = Mathf.Lerp(clearStart, _linearStart, w);
                    RenderSettings.fogEndDistance = Mathf.Lerp(LinearClearFar, _linearEnd, w);
                }
                else
                {
                    var band = Mathf.Max(0.5f, _linearEnd - _linearStart);
                    var farStart = Mathf.Max(_linearStart + 0.05f, outerFogRampStartAtWeightZero);
                    var farEnd = farStart + band;
                    RenderSettings.fogStartDistance = Mathf.Lerp(farStart, _linearStart, w);
                    RenderSettings.fogEndDistance = Mathf.Lerp(farEnd, _linearEnd, w);
                }
            }
            else
            {
                var minDensity = useFullVisualClearAtWeightZero ? 0.00015f : relaxedFogDensity;
                minDensity = Mathf.Min(minDensity, Mathf.Max(0.00015f, _density - 1e-6f));
                RenderSettings.fogDensity = Mathf.Lerp(minDensity, _density, w);
            }
        }
    }
}
