using System.Collections;
using UnityEngine;

namespace LudumDare.VoiceFog
{
    /// <summary>
    /// On «сигнал» from <see cref="KeywordSourceBehaviour"/> on the same GameObject: fog weight 1 → 0 → 1 within a fixed total time (default 5 s).
    /// </summary>
    public sealed class SignalFogBurst : MonoBehaviour
    {
        const float MaxCycleSeconds = 8f;

        [SerializeField] Transform cameraTransform;
        [SerializeField] [Range(0.05f, MaxCycleSeconds)] float fadeClearSeconds = 0.7f;
        [SerializeField] [Range(0f, MaxCycleSeconds)] float holdClearSeconds = 3f;
        [SerializeField] [Range(0.05f, MaxCycleSeconds)] float fadeRestoreSeconds = 1.3f;
        [SerializeField] float cooldownAfterCycleSeconds = 5f;

        [Header("Fade clear")]
        [Tooltip("If enabled, fade-out uses Fade Clear Ease curve. If off (default), a built-in cubic in-out over time is used — avoids broken curves that jump fog to 0 on the first frame (common in URP deferred).")]
        [SerializeField] bool fadeClearUseInspectorCurve;

        [SerializeField] AnimationCurve fadeClearEase;

        [Header("Fade restore")]
        [SerializeField] AnimationCurve fadeRestoreEase;

        [Tooltip("Smooth normalized time before evaluating the restore curve.")]
        [SerializeField] bool smoothTimeline = true;

        [Header("Light pulse (fog bubble)")]
        [SerializeField] bool enableSpotPulse = true;
        [SerializeField] float spotPeakIntensity = 12f;
        [SerializeField] float spotRange = 20f;
        [SerializeField] float spotSpotAngle = 55f;
        [SerializeField] Color spotColor = new Color(0.75f, 0.85f, 1f);

        KeywordSourceBehaviour[] _sources;
        IFogController _fog;
        Light _spot;
        float _cooldownUntil;
        Coroutine _cycleRoutine;
        bool _cycleRunning;

        void Awake()
        {
            EnsureFadeCurves();

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            ResolveFogController();
            EnsureSpotLight();
        }

        void OnValidate()
        {
            EnsureFadeCurves();
        }

        void Start()
        {
            ResolveFogController();
            EnsureSpotLight();
        }

        /// <summary>Symmetric ease for 1→0 fog weight: avoids «ease-out on time» which clears most of the fog in the first part of the clip.</summary>
        static AnimationCurve CreateFadeClearDefaultCurve() =>
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        static AnimationCurve CreateEaseInCurve()
        {
            var c = new AnimationCurve();
            c.preWrapMode = WrapMode.Clamp;
            c.postWrapMode = WrapMode.Clamp;
            c.AddKey(new Keyframe(0f, 0f, 0.35f, 0f));
            c.AddKey(new Keyframe(1f, 1f, 0f, 3f));
            return c;
        }

        void EnsureFadeCurves()
        {
            if (fadeClearUseInspectorCurve && (fadeClearEase == null || fadeClearEase.length < 2))
                fadeClearEase = CreateFadeClearDefaultCurve();
            if (fadeRestoreEase == null || fadeRestoreEase.length < 2)
                fadeRestoreEase = CreateEaseInCurve();
        }

        static float CubicInOut01(float x)
        {
            x = Mathf.Clamp01(x);
            return x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) / 2f;
        }

        void ResolveFogController()
        {
            _fog = GetComponent<IFogController>();
        }

        void EnsureSpotLight()
        {
            if (!enableSpotPulse || _spot != null)
                return;

            var lightGo = new GameObject("SignalBurstLight");
            lightGo.transform.SetParent(cameraTransform != null ? cameraTransform : transform, false);
            lightGo.transform.localPosition = Vector3.zero;
            lightGo.transform.localRotation = Quaternion.identity;
            _spot = lightGo.AddComponent<Light>();
            _spot.type = LightType.Point;
            _spot.enabled = false;
            _spot.shadows = LightShadows.Soft;
            _spot.range = spotRange;
            _spot.color = spotColor;
        }

        void OnEnable()
        {
            ResolveFogController();
            _sources = GetComponents<KeywordSourceBehaviour>();

            foreach (var s in _sources)
                s.OnKeywordSignal += HandleKeyword;

            if (_sources.Length == 0)
                Debug.LogWarning("[SignalFogBurst] No KeywordSourceBehaviour found; add Vosk and/or keyboard fallback.");
            if (_fog == null)
                Debug.LogWarning("[SignalFogBurst] No IFogController on this GameObject; add UniversalVolumeFogDriver.");
        }

        void OnDisable()
        {
            foreach (var s in _sources)
                s.OnKeywordSignal -= HandleKeyword;

            if (_cycleRoutine != null)
            {
                StopCoroutine(_cycleRoutine);
                _cycleRoutine = null;
            }

            _cycleRunning = false;
            if (_fog != null)
                _fog.SetFogWeight(1f);
        }

        void HandleKeyword()
        {
            ResolveFogController();

            if (_cycleRunning)
                return;
            if (Time.unscaledTime < _cooldownUntil)
                return;
            if (_fog == null)
                return;

            if (_cycleRoutine != null)
                StopCoroutine(_cycleRoutine);

            _cycleRoutine = StartCoroutine(CycleRoutine());
        }

        IEnumerator CycleRoutine()
        {
            ResolveFogController();
            if (_fog == null)
                yield break;

            _cycleRunning = true;
            EnsureFadeCurves();

            var fadeClear = fadeClearSeconds;
            var holdClear = holdClearSeconds;
            var fadeRestore = fadeRestoreSeconds;
            var sum = fadeClear + holdClear + fadeRestore;
            if (sum > MaxCycleSeconds)
            {
                var scale = MaxCycleSeconds / sum;
                fadeClear *= scale;
                holdClear *= scale;
                fadeRestore *= scale;
            }

            if (fadeClearUseInspectorCurve)
                yield return AnimateFogWeight(1f, 0f, fadeClear, fadeClearEase);
            else
                yield return AnimateFogWeightClearAnalytic(1f, 0f, fadeClear);

            if (enableSpotPulse && _spot != null)
                PulseSpot(spotPeakIntensity);

            var holdEnd = Time.unscaledTime + holdClear;
            while (Time.unscaledTime < holdEnd)
            {
                _fog.SetFogWeight(0f);
                if (enableSpotPulse && _spot != null)
                {
                    var k = 1f - (holdEnd - Time.unscaledTime) / Mathf.Max(0.01f, holdClear);
                    PulseSpot(Mathf.Lerp(spotPeakIntensity * 0.3f, 0f, k));
                }

                yield return null;
            }

            yield return AnimateFogWeight(0f, 1f, fadeRestore, fadeRestoreEase);

            if (_spot != null)
            {
                _spot.enabled = false;
                _spot.intensity = 0f;
            }

            _fog.SetFogWeight(1f);
            _cooldownUntil = Time.unscaledTime + cooldownAfterCycleSeconds;
            _cycleRunning = false;
            _cycleRoutine = null;
        }

        IEnumerator AnimateFogWeightClearAnalytic(float from, float to, float duration)
        {
            if (_fog == null)
                yield break;

            var t = 0f;
            duration = Mathf.Max(0.01f, duration);
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / duration);
                var kt = smoothTimeline ? Mathf.SmoothStep(0f, 1f, k) : k;
                var blend = CubicInOut01(kt);
                _fog.SetFogWeight(Mathf.Lerp(from, to, blend));
                yield return null;
            }

            _fog.SetFogWeight(to);
        }

        IEnumerator AnimateFogWeight(float from, float to, float duration, AnimationCurve easeCurve)
        {
            if (_fog == null)
                yield break;

            EnsureFadeCurves();
            if (easeCurve == null || easeCurve.length < 2)
                easeCurve = fadeRestoreEase;

            var t = 0f;
            duration = Mathf.Max(0.01f, duration);
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / duration);
                var kt = smoothTimeline ? Mathf.SmoothStep(0f, 1f, k) : k;
                var y0 = Mathf.Clamp01(easeCurve.Evaluate(0f));
                var y1 = Mathf.Clamp01(easeCurve.Evaluate(1f));
                var y = Mathf.Clamp01(easeCurve.Evaluate(kt));
                var span = y1 - y0;
                var eased = Mathf.Abs(span) < 0.0001f ? kt : Mathf.Clamp01((y - y0) / span);
                _fog.SetFogWeight(Mathf.Lerp(from, to, eased));
                yield return null;
            }

            _fog.SetFogWeight(to);
        }

        void PulseSpot(float intensity)
        {
            if (_spot == null)
                return;

            _spot.enabled = intensity > 0.01f;
            _spot.intensity = Mathf.Max(0f, intensity);
        }
    }
}
