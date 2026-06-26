#if UNITY_EDITOR
using UnityEditor;
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
    }
}
#endif
