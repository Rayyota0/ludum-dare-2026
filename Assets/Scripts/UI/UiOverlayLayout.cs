using UnityEngine;
using UnityEngine.UI;

namespace LudumDare.UI
{
    /// <summary>
    /// Shared overlay <see cref="CanvasScaler"/> settings and anchor presets as fractions of the parent <see cref="RectTransform"/> (typically the full-screen canvas root).
    /// </summary>
    public static class UiOverlayLayout
    {
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;

        public static void ConfigureOverlayScaler(CanvasScaler scaler)
        {
            if (scaler == null)
                return;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        public static void StretchToParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Normalized band in parent space (0–1 on each axis).</summary>
        public static void SetNormalizedBand(RectTransform rt, float xMin, float xMax, float yMin, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Top-left panel: margins from left and top edge; width and height as fractions of parent.</summary>
        public static void SetTopLeftPanelFractions(RectTransform rt, float marginLeft, float marginTop, float width, float height)
        {
            rt.anchorMin = new Vector2(marginLeft, 1f - marginTop - height);
            rt.anchorMax = new Vector2(marginLeft + width, 1f - marginTop);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Bottom-centered bar: horizontal span as fraction of width, vertical span and bottom inset as fractions of height.</summary>
        public static void SetBottomCenterBar(RectTransform rt, float widthFraction, float heightFraction, float bottomMarginFraction)
        {
            float half = widthFraction * 0.5f;
            rt.anchorMin = new Vector2(0.5f - half, bottomMarginFraction);
            rt.anchorMax = new Vector2(0.5f + half, bottomMarginFraction + heightFraction);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static int PaddingFromRefWidth(float fractionOf1920 = 0.002083f)
        {
            return Mathf.Max(2, Mathf.RoundToInt(ReferenceWidth * fractionOf1920));
        }
    }
}
