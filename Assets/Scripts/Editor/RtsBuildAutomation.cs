#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace QuestCommandRTS.Editor
{
    public static class RtsBuildAutomation
    {
        private const string BattlefieldScene = "Assets/Scenes/Battlefield.unity";
        private const string WindowsBuildPath = "Builds/Windows/QuestCommandRTS.exe";

        [MenuItem("Command RTS/Build/Desktop Development Build")]
        public static void BuildDesktopDevelopment()
        {
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { BattlefieldScene },
                locationPathName = WindowsBuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            Directory.CreateDirectory(Path.GetDirectoryName(WindowsBuildPath));
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("Desktop build failed: " + report.summary.result);
            }

            if (!IsValidBuildArtifact(WindowsBuildPath))
            {
                throw new InvalidOperationException("Desktop build reported success but did not create " + Path.GetFullPath(WindowsBuildPath) + ". Confirm the Windows Standalone module is installed and enabled for this Unity editor.");
            }

            Debug.Log("Desktop build created at " + Path.GetFullPath(WindowsBuildPath));
        }

        internal static bool IsValidBuildArtifact(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            return new FileInfo(path).Length > 0;
        }
    }
}
#endif
