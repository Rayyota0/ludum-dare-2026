using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LudumDare.VoiceFog
{
    /// <summary>
    /// While fog weight is high (player hasn't signaled), accumulates exposure.
    /// Rising exposure drives screen artifacts (vignette, noise, red tint, camera shake).
    /// At max exposure → game over screen with scene reload.
    /// </summary>
    public sealed class FogDamageController : MonoBehaviour
    {
        [Header("Exposure")]
        [Tooltip("Fog weight above which exposure accumulates.")]
        [SerializeField] float fogWeightThreshold = 0.5f;

        [Tooltip("Exposure gain per second while in dense fog.")]
        [SerializeField] float exposureRate = 1f;

        [Tooltip("Exposure recovery per second when fog is clear.")]
        [SerializeField] float recoveryRate = 0.15f;

        [Tooltip("Seconds of dense fog until game over.")]
        [SerializeField] float maxExposureSeconds = 10f;

        [Header("Effects intensity")]
        [Tooltip("Exposure fraction at which effects start being visible.")]
        [SerializeField] float effectsOnsetFraction = 0.15f;

        [Header("Camera shake")]
        [SerializeField] float maxShakeAngle = 3f;
        [SerializeField] float shakeSpeed = 25f;

        [Header("Audio")]
        [SerializeField] float maxHissVolume = 0.8f;

        [Header("Game Over")]
        [SerializeField] float gameOverFadeSeconds = 1.5f;
        [SerializeField] float restartDelaySeconds = 3f;

        float _exposure;
        bool _gameOver;
        IFogController _fog;
        Transform _cameraTransform;
        Quaternion _originalCameraRotation;

        // UI
        Canvas _canvas;
        Image _vignetteImage;
        Image _noiseImage;
        Image _tintImage;
        CanvasGroup _gameOverGroup;
        Text _gameOverText;
        Texture2D _noiseTex;
        AudioSource _hissSource;

        const int NoiseSize = 128;
        const string HissClipPath = "Audio/SFX/white-noise-hiss";

        void Awake()
        {
            _fog = GetComponent<IFogController>();
            _cameraTransform = transform;
        }

        void Start()
        {
            if (_fog == null)
                _fog = GetComponent<IFogController>();

            BuildUI();
            GenerateNoiseTexture();
            BuildHissAudio();
        }

        void Update()
        {
            if (_gameOver)
                return;

            if (_fog == null)
            {
                _fog = GetComponent<IFogController>();
                if (_fog == null) return;
            }

            UpdateExposure();
            float t = EffectIntensity();
            ApplyVignette(t);
            ApplyNoise(t);
            ApplyTint(t);
            ApplyHiss(t);
        }

        void LateUpdate()
        {
            if (_gameOver || _cameraTransform == null)
                return;

            ApplyShake(EffectIntensity());
        }

        void OnDisable()
        {
            if (_noiseTex != null)
            {
                Destroy(_noiseTex);
                _noiseTex = null;
            }
        }

        void UpdateExposure()
        {
            bool inDenseFog = _fog.FogWeight > fogWeightThreshold;

            if (inDenseFog)
                _exposure += exposureRate * Time.deltaTime;
            else
                _exposure = 0f;

            _exposure = Mathf.Clamp(_exposure, 0f, maxExposureSeconds);

            if (_exposure >= maxExposureSeconds)
                TriggerGameOver();
        }

        float EffectIntensity()
        {
            float fraction = _exposure / maxExposureSeconds;
            if (fraction < effectsOnsetFraction)
                return 0f;
            return Mathf.InverseLerp(effectsOnsetFraction, 1f, fraction);
        }

        #region Visual Effects

        void ApplyVignette(float t)
        {
            if (_vignetteImage == null) return;

            // Alpha 0→0.85 as intensity rises
            var c = _vignetteImage.color;
            c.a = Mathf.Lerp(0f, 0.85f, t);
            _vignetteImage.color = c;
        }

        void ApplyNoise(float t)
        {
            if (_noiseImage == null || _noiseTex == null) return;

            // Refresh noise texture when visible
            if (t > 0.01f && Time.frameCount % 2 == 0)
                RefreshNoiseTexture();

            var c = _noiseImage.color;
            c.a = Mathf.Lerp(0f, 0.4f, t * t);
            _noiseImage.color = c;
        }

        void ApplyTint(float t)
        {
            if (_tintImage == null) return;

            // Dark red tint that intensifies
            float alpha = Mathf.Lerp(0f, 0.35f, t);
            _tintImage.color = new Color(0.3f, 0f, 0f, alpha);
        }

        void ApplyShake(float t)
        {
            if (_cameraTransform == null || t < 0.01f) return;

            float angle = maxShakeAngle * t;
            float time = Time.unscaledTime * shakeSpeed;
            float rx = Mathf.PerlinNoise(time, 0f) * 2f - 1f;
            float ry = Mathf.PerlinNoise(0f, time) * 2f - 1f;
            float rz = Mathf.PerlinNoise(time, time) * 2f - 1f;

            _cameraTransform.localRotation *= Quaternion.Euler(rx * angle, ry * angle * 0.5f, rz * angle * 0.3f);
        }

        void ApplyHiss(float t)
        {
            if (_hissSource == null) return;

            _hissSource.volume = Mathf.Lerp(0f, maxHissVolume, t);

            if (t > 0.01f && !_hissSource.isPlaying)
                _hissSource.Play();
            else if (t <= 0.01f && _hissSource.isPlaying)
                _hissSource.Stop();
        }

        void BuildHissAudio()
        {
            var clip = Resources.Load<AudioClip>(HissClipPath);
            if (clip == null)
            {
                Debug.LogWarning($"[FogDamage] Hiss clip not found at Resources/{HissClipPath}. Move white-noise-hiss.wav to Assets/Resources/Audio/SFX/.");
                return;
            }

            _hissSource = gameObject.AddComponent<AudioSource>();
            _hissSource.clip = clip;
            _hissSource.loop = true;
            _hissSource.playOnAwake = false;
            _hissSource.volume = 0f;
            _hissSource.spatialBlend = 0f;
        }

        #endregion

        #region Game Over

        void TriggerGameOver()
        {
            if (_gameOver) return;
            _gameOver = true;

            // Max out effects
            ApplyVignette(1f);
            ApplyTint(1f);

            StartCoroutine(GameOverSequence());
        }

        IEnumerator GameOverSequence()
        {
            if (_gameOverGroup == null) yield break;

            _gameOverGroup.gameObject.SetActive(true);
            _gameOverGroup.alpha = 0f;

            float t = 0f;
            while (t < gameOverFadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                _gameOverGroup.alpha = Mathf.Clamp01(t / gameOverFadeSeconds);
                yield return null;
            }

            _gameOverGroup.alpha = 1f;

            yield return new WaitForSecondsRealtime(restartDelaySeconds);

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        #endregion

        #region UI Construction

        void BuildUI()
        {
            var go = new GameObject("FogDamage_Canvas");
            go.transform.SetParent(transform, false);

            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 999;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            go.AddComponent<GraphicRaycaster>();

            // Vignette (radial gradient dark edges)
            _vignetteImage = CreateFullscreenImage(go.transform, "Vignette");
            _vignetteImage.sprite = CreateVignetteSprite();
            _vignetteImage.color = new Color(0f, 0f, 0f, 0f);
            _vignetteImage.raycastTarget = false;

            // Noise overlay
            _noiseImage = CreateFullscreenImage(go.transform, "Noise");
            _noiseImage.color = new Color(1f, 1f, 1f, 0f);
            _noiseImage.raycastTarget = false;

            // Red tint
            _tintImage = CreateFullscreenImage(go.transform, "Tint");
            _tintImage.color = new Color(0.3f, 0f, 0f, 0f);
            _tintImage.raycastTarget = false;

            // Game over panel
            BuildGameOverPanel(go.transform);
        }

        void BuildGameOverPanel(Transform parent)
        {
            var panel = new GameObject("GameOverPanel");
            panel.transform.SetParent(parent, false);

            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.95f);
            panelImage.raycastTarget = false;

            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;

            _gameOverGroup = panel.AddComponent<CanvasGroup>();
            _gameOverGroup.alpha = 0f;
            _gameOverGroup.interactable = false;
            _gameOverGroup.blocksRaycasts = false;

            // Title text
            var textGo = new GameObject("GameOverText");
            textGo.transform.SetParent(panel.transform, false);

            _gameOverText = textGo.AddComponent<Text>();
            _gameOverText.text = "ВЫ ПОГЛОЩЕНЫ ТУМАНОМ";
            _gameOverText.font = Font.CreateDynamicFontFromOSFont("Arial", 64);
            if (_gameOverText.font == null)
                _gameOverText.font = Resources.GetBuiltinResource<Font>("LegacySRuntime.ttf");
            _gameOverText.fontSize = 64;
            _gameOverText.alignment = TextAnchor.MiddleCenter;
            _gameOverText.color = new Color(0.7f, 0.1f, 0.1f, 1f);
            _gameOverText.raycastTarget = false;

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.3f);
            textRect.anchorMax = new Vector2(1f, 0.7f);
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            panel.SetActive(false);
        }

        Image CreateFullscreenImage(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            return image;
        }

        Sprite CreateVignetteSprite()
        {
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var center = new Vector2(size * 0.5f, size * 0.5f);
            float maxDist = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                    // Smooth vignette: transparent center, dark edges
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((dist - 0.3f) / 0.7f));
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        void GenerateNoiseTexture()
        {
            _noiseTex = new Texture2D(NoiseSize, NoiseSize, TextureFormat.RGBA32, false);
            _noiseTex.filterMode = FilterMode.Point;
            _noiseTex.wrapMode = TextureWrapMode.Repeat;

            RefreshNoiseTexture();

            if (_noiseImage != null)
            {
                _noiseImage.sprite = Sprite.Create(
                    _noiseTex,
                    new Rect(0, 0, NoiseSize, NoiseSize),
                    new Vector2(0.5f, 0.5f));
            }
        }

        void RefreshNoiseTexture()
        {
            if (_noiseTex == null) return;

            var pixels = _noiseTex.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                byte v = (byte)Random.Range(0, 256);
                pixels[i] = new Color32(v, v, v, v);
            }

            _noiseTex.SetPixels32(pixels);
            _noiseTex.Apply();
        }

        #endregion
    }
}
