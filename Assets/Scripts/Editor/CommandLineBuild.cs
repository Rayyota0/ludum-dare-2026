#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LudumDare.Editor
{
    /// <summary>
    /// Headless Linux64 build for CI / CLI. Does not change Editor Build Settings.
    /// </summary>
    public static class CommandLineBuild
    {
        public static void BuildLinux64EgorInterface()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? ".";
            var outDir = Path.Combine(projectRoot, "Builds", "Linux64", "egor-interface");
            Directory.CreateDirectory(outDir);
            var exe = Path.Combine(outDir, "egor-interface.x86_64");

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/egor-interface.unity" },
                locationPathName = exe,
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result != BuildResult.Succeeded)
                EditorApplication.Exit(1);

            EditorApplication.Exit(0);
        }
    }
}
#endif
