using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LudumDare.UI
{
    /// <summary>
    /// Shows a sequence of tutorial hints at the start of the game.
    /// Place on any GameObject in the scene.
    /// </summary>
    public sealed class TutorialHints : MonoBehaviour
    {
        [SerializeField] float delayBeforeStart = 1f;
        [SerializeField] float hintDuration = 4f;
        [SerializeField] float fadeDuration = 0.6f;
        [SerializeField] float pauseBetween = 0.5f;

        Canvas _canvas;
        Text _text;
        CanvasGroup _group;

        static readonly string[] Hints =
        {
            "Скажите «СИГНАЛ» чтобы разогнать туман",
            "WASD — движение   |   Мышь — осмотр",
            "E — взаимодействие с предметами",
        };

        void Start()
        {
            BuildUI();
            StartCoroutine(ShowHints());
        }

        void BuildUI()
        {
            var go = new GameObject("TutorialCanvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 80;
            UiOverlayLayout.ConfigureOverlayScaler(go.AddComponent<CanvasScaler>());

            _group = go.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            var textGo = new GameObject("HintText");
            textGo.transform.SetParent(go.transform, false);

            _text = textGo.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 34;
            _text.color = new Color(0.9f, 0.9f, 0.85f, 1f);
            _text.alignment = TextAnchor.MiddleCenter;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;

            var outline = textGo.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.7f);
            outline.effectDistance = new Vector2(2f, -2f);

            var rt = _text.rectTransform;
            UiOverlayLayout.SetNormalizedBand(rt, 0.05f, 0.95f, 0.78f, 0.92f);
        }

        IEnumerator ShowHints()
        {
            yield return new WaitForSeconds(delayBeforeStart);

            foreach (var hint in Hints)
            {
                _text.text = hint;

                // Fade in
                float t = 0f;
                while (t < fadeDuration)
                {
                    t += Time.deltaTime;
                    _group.alpha = Mathf.Clamp01(t / fadeDuration);
                    yield return null;
                }
                _group.alpha = 1f;

                yield return new WaitForSeconds(hintDuration);

                // Fade out
                t = 0f;
                while (t < fadeDuration)
                {
                    t += Time.deltaTime;
                    _group.alpha = 1f - Mathf.Clamp01(t / fadeDuration);
                    yield return null;
                }
                _group.alpha = 0f;

                yield return new WaitForSeconds(pauseBetween);
            }

            Destroy(_canvas.gameObject);
        }
    }
}
