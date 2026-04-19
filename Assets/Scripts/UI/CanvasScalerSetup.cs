using UnityEngine;
using UnityEngine.UI;

namespace LudumDare.UI
{
    public static class CanvasScalerSetup
    {
        public static void ApplyScreenSpaceScale(CanvasScaler scaler)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
        }
    }
}
