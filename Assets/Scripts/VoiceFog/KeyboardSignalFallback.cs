using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LudumDare.VoiceFog
{
    /// <summary>
    /// Optional: press <see cref="fallbackKey"/> to fake the keyword (add manually if you need it without speech).
    /// Not added by <see cref="VoiceFogRuntimeBootstrap"/>.
    /// </summary>
    public sealed class KeyboardSignalFallback : KeywordSourceBehaviour
    {
#if ENABLE_INPUT_SYSTEM
        [SerializeField] Key fallbackKey = Key.F;
#endif
        [SerializeField] bool logTrigger = true;

        void Update()
        {
            if (!WasFallbackPressed())
                return;

            if (logTrigger)
                Debug.Log("[KeyboardSignalFallback] Simulated keyword — press F (not voice).", this);

            RaiseKeywordSignal();
        }

        bool WasFallbackPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb[fallbackKey].wasPressedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.GetKeyDown(KeyCode.F))
                return true;
#endif
            return false;
        }
    }
}
