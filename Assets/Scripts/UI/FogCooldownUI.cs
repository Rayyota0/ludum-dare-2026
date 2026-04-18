using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

namespace LudumDare.UI
{
    /// <summary>
    /// Cooldown bar synced directly with SignalFogBurst state.
    /// Reads _cycleRunning and _cooldownUntil via reflection.
    /// </summary>
    public class FogCooldownUI : MonoBehaviour
    {
        [Header("Settings")]
        public Color barBgColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        public Color barFillColor = new Color(0.8f, 0.8f, 1f, 0.8f);
        public Color barCooldownColor = new Color(1f, 0.3f, 0.2f, 0.7f);
        public Color barActiveColor = new Color(0.3f, 1f, 0.3f, 0.8f);

        Image _bgImage;
        Image _fillImage;

        MonoBehaviour _burst;
        FieldInfo _cycleRunningField;
        FieldInfo _cooldownUntilField;
        FieldInfo _fadeClearField;
        FieldInfo _holdClearField;
        FieldInfo _fadeRestoreField;
        FieldInfo _cooldownAfterField;

        bool _found;
        float _totalCycleDuration;

        void Start()
        {
            CreateUI();
            StartCoroutine(FindSignalFogBurst());
        }

        void CreateUI()
        {
            var canvasGO = new GameObject("FogCooldownCanvas");
            canvasGO.transform.SetParent(transform);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGO.AddComponent<CanvasScaler>();

            // Bar background
            var bgGO = new GameObject("BarBG");
            bgGO.transform.SetParent(canvasGO.transform);
            _bgImage = bgGO.AddComponent<Image>();
            _bgImage.color = barBgColor;
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0);
            bgRect.anchorMax = new Vector2(0.5f, 0);
            bgRect.pivot = new Vector2(0.5f, 0);
            bgRect.anchoredPosition = new Vector2(0, 30);
            bgRect.sizeDelta = new Vector2(200, 12);

            // Fill bar
            var fillGO = new GameObject("BarFill");
            fillGO.transform.SetParent(bgGO.transform);
            _fillImage = fillGO.AddComponent<Image>();
            _fillImage.color = barFillColor;
            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2, 2);
            fillRect.offsetMax = new Vector2(-2, -2);
            fillRect.pivot = new Vector2(0, 0.5f);
        }

        System.Collections.IEnumerator FindSignalFogBurst()
        {
            for (int i = 0; i < 30; i++)
            {
                yield return null;
                var cam = Camera.main;
                if (cam == null) continue;

                _burst = cam.GetComponent<LudumDare.VoiceFog.SignalFogBurst>();
                if (_burst == null) continue;

                var type = _burst.GetType();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;

                _cycleRunningField = type.GetField("_cycleRunning", flags);
                _cooldownUntilField = type.GetField("_cooldownUntil", flags);

                var sFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                _fadeClearField = type.GetField("fadeClearSeconds", sFlags);
                _holdClearField = type.GetField("holdClearSeconds", sFlags);
                _fadeRestoreField = type.GetField("fadeRestoreSeconds", sFlags);
                _cooldownAfterField = type.GetField("cooldownAfterCycleSeconds", sFlags);

                if (_cycleRunningField != null && _cooldownUntilField != null)
                {
                    float fadeClear = _fadeClearField != null ? (float)_fadeClearField.GetValue(_burst) : 0.7f;
                    float holdClear = _holdClearField != null ? (float)_holdClearField.GetValue(_burst) : 3f;
                    float fadeRestore = _fadeRestoreField != null ? (float)_fadeRestoreField.GetValue(_burst) : 1.3f;
                    float cooldownAfter = _cooldownAfterField != null ? (float)_cooldownAfterField.GetValue(_burst) : 5f;
                    _totalCycleDuration = fadeClear + holdClear + fadeRestore + cooldownAfter;
                    _found = true;
                    break;
                }
            }
        }

        void Update()
        {
            if (!_found || _burst == null) return;

            bool cycleRunning = (bool)_cycleRunningField.GetValue(_burst);
            float cooldownUntil = (float)_cooldownUntilField.GetValue(_burst);

            if (cycleRunning)
            {
                // Cycle active — show green, don't fill
                _fillImage.color = barActiveColor;
                _fillImage.rectTransform.anchorMax = new Vector2(1, 1);
            }
            else if (Time.unscaledTime < cooldownUntil)
            {
                // On cooldown — red bar filling up
                float remaining = cooldownUntil - Time.unscaledTime;
                float cooldownAfter = _cooldownAfterField != null ? (float)_cooldownAfterField.GetValue(_burst) : 5f;
                float ratio = 1f - (remaining / cooldownAfter);
                _fillImage.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1);
                _fillImage.color = barCooldownColor;
            }
            else
            {
                // Ready
                _fillImage.rectTransform.anchorMax = new Vector2(1, 1);
                _fillImage.color = barFillColor;
            }
        }
    }
}
