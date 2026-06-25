#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace QuestCommandRTS.Editor
{
    public static class RtsDiagnosticsExporter
    {
        private const string ScenePath = "Assets/Scenes/Battlefield.unity";
        private const string DefaultOutputPath = "C:/Users/matth/Documents/Codex/2026-06-24/i-s/outputs/quest-command-rts-diagnostics.json";

        [MenuItem("Command RTS/Export Runtime Diagnostics Snapshot")]
        public static void ExportMenuSnapshot()
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
            GameObject root = new GameObject("Diagnostics Runtime");
            RtsGame game = root.AddComponent<RtsGame>();
            try
            {
                game.Initialize();
                Physics.SyncTransforms();
                RtsRuntimeDiagnosticsSnapshot snapshot = RtsRuntimeDiagnosticsSnapshot.Capture(game);

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                File.WriteAllText(outputPath, snapshot.ToJson(true));
                Debug.Log("Command RTS diagnostics snapshot exported to " + outputPath);
            }
            finally
            {
                RtsRuntimeModeResolver.ForceModeForTests(null);
                Object.DestroyImmediate(root);
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
