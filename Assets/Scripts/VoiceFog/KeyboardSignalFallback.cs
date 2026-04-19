using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LudumDare.VoiceFog
{
    /// <summary>
    /// Press <see cref="fallbackKey"/> to simulate the «сигнал» keyword when speech is unavailable.
    /// <see cref="VoiceFogRuntimeBootstrap"/> adds this on the main camera when installing voice fog (Editor and Player).
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
