using UnityEngine;
using UnityEngine.Rendering;

namespace LudumDare.VoiceFog
{
    /// <summary>
    /// Snapshots <see cref="RenderSettings"/> fog after <see cref="DenseFogBootstrap"/> and animates it via <see cref="SetFogWeight"/>.
    /// </summary>
    [DefaultExecutionOrder(-99)]
    public sealed class LegacyRenderSettingsFogDriver : MonoBehaviour, IFogController
    {
        bool _captured;
        FogMode _mode;
        Color _color;
        float _density;
        float _linearStart;
        float _linearEnd;

        float _authoritativeWeight = 1f;

        Camera _camera;

        const float LinearClearFar = 7500f;

        [Header("Optional perceptual remap")]
        [SerializeField] bool useWeightRemapCurve;
        [SerializeField] AnimationCurve weightRemapCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("SmoothStep on fog strength before lerping distances/density. Off by default: when on, fade-out (1→0) tends to stay dense then snap clear at the end.")]
        [SerializeField] bool smoothStrengthEndpoints;

        [Header("Clear at weight 0")]
        [Tooltip("If on, weight 0 is almost no fog (far linear distances / minimal exp. density). If off, weight 0 only moves fog to the «relaxed» distances/density below — visibility grows, but haze stays.")]
        [SerializeField] bool useFullVisualClearAtWeightZero = true;

        [Tooltip("Linear fog at weight 0 when «full clear» is off; should be beyond baseline so the world opens up.")]
        [SerializeField] float relaxedLinearFogStart = 40f;

        [SerializeField] float relaxedLinearFogEnd = 280f;

        [Tooltip("Exponential / Exp² fog density at weight 0 when «full clear» is off (still some atmosphere).")]
        [SerializeField] [Range(0.0002f, 0.08f)] float relaxedFogDensity = 0.006f;

        void Awake()
        {
            _camera = GetComponent<Camera>();
            CaptureFromRenderSettings();
        }

        void OnEnable()
        {
            if (!_captured)
                CaptureFromRenderSettings();

            if (GraphicsSettings.defaultRenderPipeline != null)
                RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        void OnDisable()
        {
            if (GraphicsSettings.defaultRenderPipeline != null)
                RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        void OnValidate()
        {
            if (weightRemapCurve == null || weightRemapCurve.length < 2)
                weightRemapCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            relaxedLinearFogEnd = Mathf.Max(relaxedLinearFogEnd, relaxedLinearFogStart + 0.5f);
        }

        /// <summary>URP (incl. deferred) applies legacy fog late; re-push right before our camera renders so distance fog tracks animated <see cref="RenderSettings"/>.</summary>
        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!_captured || _camera == null || camera != _camera)
                return;
            PushToRenderSettings();
        }

        void LateUpdate()
        {
            if (GraphicsSettings.defaultRenderPipeline != null)
                return;
            if (!_captured)
                return;
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
            var weight01 = _authoritativeWeight;

            var w = weight01;
            if (useWeightRemapCurve && weightRemapCurve != null && weightRemapCurve.length >= 2)
                w = Mathf.Clamp01(weightRemapCurve.Evaluate(weight01));

            if (smoothStrengthEndpoints)
                w = Mathf.SmoothStep(0f, 1f, w);

            // A1: keep fog enabled at weight 0 with parametric «clear» state — avoids a pop from toggling fog off mid-lerp.
            RenderSettings.fog = true;
            RenderSettings.fogMode = _mode;
            RenderSettings.fogColor = _color;

            if (_mode == FogMode.Linear)
            {
                float clearStart;
                float clearEndDist;
                if (useFullVisualClearAtWeightZero)
                {
                    clearStart = Mathf.Max(0f, LinearClearFar - 400f);
                    clearEndDist = LinearClearFar;
                }
                else
                {
                    clearStart = Mathf.Max(0f, relaxedLinearFogStart);
                    clearEndDist = relaxedLinearFogEnd;
                }

                RenderSettings.fogStartDistance = Mathf.Lerp(clearStart, _linearStart, w);
                RenderSettings.fogEndDistance = Mathf.Lerp(clearEndDist, _linearEnd, w);
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
