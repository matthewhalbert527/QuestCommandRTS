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
        private const string WindowsPlayerTemplateRelativePath = "Data/PlaybackEngines/windowsstandalonesupport/Variations/win64_player_development_mono/WindowsPlayer.exe";

        [MenuItem("Command RTS/Build/Desktop Development Build")]
        public static void BuildDesktopDevelopment()
        {
            if (!TryValidateDesktopBuildSupport(out string validationError))
            {
                throw new InvalidOperationException(validationError);
            }

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

        [MenuItem("Command RTS/Build/Validate Desktop Build Support")]
        public static void ValidateDesktopBuildSupport()
        {
            if (!TryValidateDesktopBuildSupport(out string validationError))
            {
                throw new InvalidOperationException(validationError);
            }

            Debug.Log("Desktop build support validated for StandaloneWindows64 at " + Path.GetFullPath(WindowsBuildPath));
        }

        internal static bool TryValidateDesktopBuildSupport(out string validationError)
        {
            return TryValidateDesktopBuildSupport(IsWindowsStandaloneBuildSupported(), EditorApplication.applicationPath, out validationError);
        }

        internal static bool TryValidateDesktopBuildSupport(bool buildTargetSupported, string editorApplicationPath, out string validationError)
        {
            if (!buildTargetSupported)
            {
                validationError = GetUnsupportedDesktopBuildTargetMessage();
                return false;
            }

            if (!HasWindowsStandalonePlayerTemplate(editorApplicationPath))
            {
                validationError = GetMissingDesktopPlayerTemplateMessage(editorApplicationPath);
                return false;
            }

            validationError = string.Empty;
            return true;
        }

        internal static bool IsValidBuildArtifact(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            return new FileInfo(path).Length > 0;
        }

        internal static bool IsWindowsStandaloneBuildSupported()
        {
            return BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        }

        internal static bool HasWindowsStandalonePlayerTemplate(string editorApplicationPath)
        {
            return IsValidBuildArtifact(GetWindowsStandalonePlayerTemplatePath(editorApplicationPath));
        }

        internal static string GetUnsupportedDesktopBuildTargetMessage()
        {
            return "Desktop build target StandaloneWindows64 is not supported by this Unity editor install. Install or repair Windows Build Support for Unity 2022.3.62f3 in Unity Hub, then rerun Command RTS > Build > Desktop Development Build.";
        }

        internal static string GetMissingDesktopPlayerTemplateMessage(string editorApplicationPath)
        {
            return "Desktop build target StandaloneWindows64 is missing its WindowsPlayer.exe template at " + GetWindowsStandalonePlayerTemplatePath(editorApplicationPath) + ". Repair Unity 2022.3.62f3 in Unity Hub or reinstall Windows Build Support, then rerun Command RTS > Build > Desktop Development Build.";
        }

        private static string GetWindowsStandalonePlayerTemplatePath(string editorApplicationPath)
        {
            if (string.IsNullOrEmpty(editorApplicationPath))
            {
                return WindowsPlayerTemplateRelativePath;
            }

            string editorFolder = Path.GetDirectoryName(editorApplicationPath);
            return Path.Combine(editorFolder, WindowsPlayerTemplateRelativePath);
        }
    }
}
#endif
