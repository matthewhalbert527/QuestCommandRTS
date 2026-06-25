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

        [MenuItem("Command RTS/Export Sample Screenshot")]
        public static void ExportMenuScreenshot()
        {
            Export(DefaultOutputPath);
        }

        public static void ExportForCodex()
        {
            Export(DefaultOutputPath);
        }

        private static void Export(string outputPath)
        {
            EditorSceneManager.OpenScene(ScenePath);

            RtsRuntimeModeResolver.ForceModeForTests(RtsRuntimeMode.Desktop);
            GameObject root = new GameObject("Screenshot Runtime");
            RtsGame game = root.AddComponent<RtsGame>();
            try
            {
                game.Initialize();
                Physics.SyncTransforms();
                RtsSoakScenarioExporter.PopulateGeneratedMatchForSoak(game);
                Physics.SyncTransforms();
            }
            finally
            {
                RtsRuntimeModeResolver.ForceModeForTests(null);
            }

            Camera camera = game.CommandCamera;
            camera.transform.position = new Vector3(-14f, 112f, -146f);
            camera.transform.rotation = Quaternion.Euler(59f, 0f, 0f);
            camera.fieldOfView = 66f;

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
    }
}
#endif
