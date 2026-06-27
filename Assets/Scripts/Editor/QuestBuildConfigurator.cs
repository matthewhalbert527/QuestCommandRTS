#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace QuestCommandRTS.Editor
{
    public static class QuestBuildConfigurator
    {
        private const string ScenePath = "Assets/Scenes/Battlefield.unity";

        [MenuItem("Quest RTS/Open Battlefield Scene")]
        public static void OpenBattlefieldScene()
        {
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Quest RTS/Apply Quest Android Settings")]
        public static void ApplyQuestAndroidSettings()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            PlayerSettings.companyName = "Codex Prototype";
            PlayerSettings.productName = "Quest Command RTS";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.codex.questcommandrts");
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            AssetDatabase.SaveAssets();
            Debug.Log("Quest RTS Android settings applied. Enable OpenXR for Android in Project Settings > XR Plug-in Management before building to a Quest headset.");
        }

        [MenuItem("Quest RTS/Build Windows Standalone")]
        public static void BuildWindowsStandalone()
        {
            const string outputDirectory = "Builds/WindowsStandalone";
            const string outputPath = outputDirectory + "/QuestCommandRTS.exe";

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

            PlayerSettings.companyName = "Codex Prototype";
            PlayerSettings.productName = "Quest Command RTS";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, "com.codex.questcommandrts");
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.colorSpace = ColorSpace.Linear;

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }

            Directory.CreateDirectory(outputDirectory);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException("Windows standalone build failed: " + report.summary.result);
            }

            Debug.Log("Quest RTS Windows standalone build written to " + outputPath);
        }
    }
}
#endif
