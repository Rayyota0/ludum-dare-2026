using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LudumDare.VoiceFog
{
    /// <summary>
    /// Ensures fog + keyword + burst behaviours exist on <see cref="Camera.main"/> for each loaded scene.
    /// </summary>
    public static class VoiceFogRuntimeBootstrap
    {
        static bool _hooksRegistered;
        static bool _loggedCamNull;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void HookScenesAndInstallFirst()
        {
            EnsurePlayModeInstall(fromDomainReload: true, logBanner: true);
        }

        /// <summary>
        /// Safe to call from Editor when entering Play Mode without domain reload (Fast Enter Play Mode).
        /// </summary>
        public static void EnsurePlayModeInstall(bool fromDomainReload = false, bool logBanner = true)
        {
            RegisterHooksOnce();

            if (logBanner)
            {
                if (fromDomainReload)
                    Debug.Log("[VoiceFog] Bootstrap: hook registered (domain reload); installing on Main Camera.", null);
                else
                    Debug.Log("[VoiceFog] Bootstrap: EnsurePlayModeInstall (editor / no domain reload).", null);
            }

            TryInstallNow();
            CreateRetryHelperIfNeeded();
        }

        static void RegisterHooksOnce()
        {
            if (_hooksRegistered)
                return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            _hooksRegistered = true;
        }

        static void CreateRetryHelperIfNeeded()
        {
            if (Object.FindObjectsByType<VoiceFogInstallRetry>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
                return;

            var helper = new GameObject(nameof(VoiceFogInstallRetry));
            helper.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(helper);
            helper.AddComponent<VoiceFogInstallRetry>();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryInstallNow();
        }

        /// <summary>Install on <see cref="Camera.main"/> if not already present.</summary>
        public static void TryInstallNow()
        {
            TryInstall(Camera.main);
        }

        static void TryInstall(Camera cam)
        {
            if (cam == null)
            {
                if (!_loggedCamNull)
                {
                    _loggedCamNull = true;
                    Debug.LogWarning(
                        "[VoiceFog] Camera.main is null — waiting for MainCamera tag (retry helper runs each frame).",
                        null);
                }

                return;
            }

            _loggedCamNull = false;

            if (cam.GetComponent<VoiceFogInstallMarker>() != null)
            {
                if (cam.GetComponent<SignalFogBurst>() != null && cam.GetComponent<IFogController>() == null)
                {
                    if (cam.GetComponent<UniversalVolumeFogDriver>() == null)
                        cam.gameObject.AddComponent<UniversalVolumeFogDriver>();
                    if (cam.GetComponent<DenseFogBootstrap>() == null)
                        cam.gameObject.AddComponent<DenseFogBootstrap>();
                }

                return;
            }

            if (cam.GetComponent<SignalFogBurst>() != null)
            {
                if (cam.GetComponent<IFogController>() == null)
                {
                    if (cam.GetComponent<UniversalVolumeFogDriver>() == null)
                        cam.gameObject.AddComponent<UniversalVolumeFogDriver>();
                    if (cam.GetComponent<DenseFogBootstrap>() == null)
                        cam.gameObject.AddComponent<DenseFogBootstrap>();
                }

                if (cam.GetComponent<VoiceFogInstallMarker>() == null)
                    cam.gameObject.AddComponent<VoiceFogInstallMarker>();
                return;
            }

            try
            {
                cam.gameObject.AddComponent<UniversalVolumeFogDriver>();
                cam.gameObject.AddComponent<DenseFogBootstrap>();
                cam.gameObject.AddComponent<VoskSignalKeywordSource>();
                cam.gameObject.AddComponent<SignalFogBurst>();
#if UNITY_EDITOR
                if (cam.GetComponent<KeyboardSignalFallback>() == null)
                    cam.gameObject.AddComponent<KeyboardSignalFallback>();
#endif
                cam.gameObject.AddComponent<VoiceFogInstallMarker>();
                Debug.Log($"[VoiceFog] Components added to \"{cam.name}\" (tag={cam.tag}).", cam);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    sealed class VoiceFogInstallRetry : MonoBehaviour
    {
        IEnumerator Start()
        {
            const int maxFrames = 120;
            for (var i = 0; i < maxFrames; i++)
            {
                VoiceFogRuntimeBootstrap.TryInstallNow();
                var cam = Camera.main;
                if (cam != null && cam.GetComponent<VoiceFogInstallMarker>() != null)
                {
                    if (i > 0)
                        Debug.Log($"[VoiceFog] Install confirmed on \"{cam.name}\" after {i} frame(s).", cam);
                    Destroy(gameObject);
                    yield break;
                }

                if (i == 0 && cam == null)
                    Debug.LogWarning(
                        "[VoiceFog] Camera.main was null — waiting for a camera with tag MainCamera (retrying).",
                        null);

                yield return null;
            }

            Debug.LogWarning(
                "[VoiceFog] Giving up: still no Main Camera / install marker after waiting. Check MainCamera tag.",
                null);
            Destroy(gameObject);
        }
    }
}
