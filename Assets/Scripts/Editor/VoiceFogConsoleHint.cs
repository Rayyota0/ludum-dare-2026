#if UNITY_EDITOR
using LudumDare.VoiceFog;
using UnityEditor;
using UnityEngine;

namespace LudumDare.VoiceFog.Editor
{
    /// <summary>
    /// Fast Enter Play Mode (no domain reload) skips RuntimeInitialize — force VoiceFog bootstrap from Editor.
    /// </summary>
    [InitializeOnLoad]
    static class VoiceFogConsoleHint
    {
        static VoiceFogConsoleHint()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
                return;

            Debug.Log(
                "[VoiceFog] Entered Play — scheduling bootstrap (needed when Domain Reload is off).",
                null);

            ScheduleBootstrapRetries(attemptsLeft: 8, firstAttempt: true);
        }

        static void ScheduleBootstrapRetries(int attemptsLeft, bool firstAttempt)
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlaying)
                    return;

                VoiceFogRuntimeBootstrap.EnsurePlayModeInstall(fromDomainReload: false, logBanner: firstAttempt);

                var cam = Camera.main;
                var ok = cam != null && cam.GetComponent<VoiceFogInstallMarker>() != null;
                if (ok || attemptsLeft <= 1)
                    return;

                ScheduleBootstrapRetries(attemptsLeft - 1, firstAttempt: false);
            };
        }
    }
}
#endif
