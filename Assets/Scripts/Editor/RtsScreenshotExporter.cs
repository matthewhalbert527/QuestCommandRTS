#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace QuestCommandRTS.Editor
{
    public static class RtsScreenshotExporter
    {
        private const string ScenePath = "Assets/Scenes/Battlefield.unity";
        private const string DefaultOutputPath = "C:/Users/matth/Documents/Codex/2026-06-24/i-s/outputs/quest-command-rts-sample.png";
        private const string DefaultQuestOutputPath = "C:/Users/matth/Documents/Codex/2026-06-24/i-s/outputs/quest-command-rts-quest-sample.png";
        private const string DefaultQuestRoomOutputPath = "C:/Users/matth/Documents/Codex/2026-06-24/i-s/outputs/quest-command-rts-quest-room-sample.png";

        [MenuItem("Command RTS/Export Sample Screenshot")]
        public static void ExportMenuScreenshot()
        {
            Export(DefaultOutputPath, RtsRuntimeMode.Desktop);
        }

        [MenuItem("Command RTS/Export Quest Sample Screenshot")]
        public static void ExportMenuQuestScreenshot()
        {
            Export(DefaultQuestOutputPath, RtsRuntimeMode.QuestVr);
        }

        [MenuItem("Command RTS/Export Room-Sized Quest Sample Screenshot")]
        public static void ExportMenuQuestRoomScreenshot()
        {
            Export(DefaultQuestRoomOutputPath, RtsRuntimeMode.QuestVr, CreateRoomSizedScreenshotProfile());
        }

        public static void ExportForCodex()
        {
            Export(DefaultOutputPath, RtsRuntimeMode.Desktop);
        }

        public static void ExportQuestForCodex()
        {
            Export(DefaultQuestOutputPath, RtsRuntimeMode.QuestVr);
        }

        public static void ExportQuestRoomForCodex()
        {
            Export(DefaultQuestRoomOutputPath, RtsRuntimeMode.QuestVr, CreateRoomSizedScreenshotProfile());
        }

        private static void Export(string outputPath, RtsRuntimeMode mode)
        {
            Export(outputPath, mode, null);
        }

        private static void Export(string outputPath, RtsRuntimeMode mode, RtsProfileSettings profileSettings)
        {
            EditorSceneManager.OpenScene(ScenePath);

            RtsRuntimeModeResolver.ForceModeForTests(mode);
            GameObject root = new GameObject("Screenshot Runtime");
            RtsGame game = root.AddComponent<RtsGame>();
            try
            {
                if (profileSettings != null)
                {
                    game.SetProfileSettingsForTests(profileSettings);
                }

                game.Initialize();
                Physics.SyncTransforms();
                RtsSoakScenarioExporter.PopulateGeneratedMatchForSoak(game);
                if (mode == RtsRuntimeMode.QuestVr && game.QuestRig != null && game.QuestRig.CommandConsole != null)
                {
                    game.QuestRig.CommandConsole.SetOpen(true);
                    ConfigureQuestScreenshotUi(game);
                }

                Physics.SyncTransforms();
            }
            finally
            {
                RtsRuntimeModeResolver.ForceModeForTests(null);
            }

            Camera camera = ConfigureCamera(game, mode);

            const int width = 1440;
            const int height = 900;
            RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            image.Apply();

            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllBytes(outputPath, image.EncodeToPNG());

            Object.DestroyImmediate(image);
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(root);

            AssetDatabase.Refresh();
            Debug.Log("Command RTS screenshot exported to " + outputPath);
        }

        private static RtsProfileSettings CreateRoomSizedScreenshotProfile()
        {
            RtsProfileSettings settings = new RtsProfileSettings(Path.Combine(Path.GetTempPath(), "QuestCommandRTS-ScreenshotProfile.json"));
            settings.Data.tabletopScale = RtsProfileSettingsData.RoomSizedTabletopScale;
            settings.Data.pointerLength = RtsProfileSettingsData.RoomSizedPointerLength;
            settings.Data.Normalize();
            return settings;
        }

        private static Camera ConfigureCamera(RtsGame game, RtsRuntimeMode mode)
        {
            if (mode == RtsRuntimeMode.QuestVr && game.QuestRig != null && game.QuestRig.HeadCamera != null)
            {
                Camera questCamera = game.QuestRig.HeadCamera;
                questCamera.transform.position = new Vector3(-78f, 86f, -152f);
                questCamera.transform.rotation = Quaternion.Euler(55f, 24f, 0f);
                questCamera.fieldOfView = 82f;
                return questCamera;
            }

            Camera camera = game.CommandCamera;
            camera.transform.position = new Vector3(-14f, 112f, -146f);
            camera.transform.rotation = Quaternion.Euler(59f, 0f, 0f);
            camera.fieldOfView = 66f;
            return camera;
        }

        private static void ConfigureQuestScreenshotUi(RtsGame game)
        {
            GameObject status = GameObject.Find("Quest World Status");
            if (status != null)
            {
                status.transform.localPosition = new Vector3(-0.42f, 1.18f, 0.64f);
                status.transform.localRotation = Quaternion.Euler(14f, 2f, 0f);
                status.transform.localScale = Vector3.one * 0.00062f;
            }

            if (game.QuestRig.CommandConsole.PanelRect != null)
            {
                Transform console = game.QuestRig.CommandConsole.PanelRect.transform;
                console.localPosition = new Vector3(-0.46f, 0.72f, 0.46f);
                console.localRotation = Quaternion.Euler(18f, 4f, 0f);
                console.localScale = Vector3.one * 0.00058f;
            }

            GameObject tacticalMap = GameObject.Find("Quest Tactical Map");
            if (tacticalMap != null)
            {
                tacticalMap.transform.localPosition = new Vector3(0.02f, 0.92f, 0.48f);
                tacticalMap.transform.localRotation = Quaternion.Euler(18f, 4f, 0f);
                tacticalMap.transform.localScale = Vector3.one * 0.00086f;
            }
        }
    }
}
#endif
