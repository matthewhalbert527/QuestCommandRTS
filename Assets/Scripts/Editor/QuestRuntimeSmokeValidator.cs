#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace QuestCommandRTS.Editor
{
    public static class QuestRuntimeSmokeValidator
    {
        private struct CameraState
        {
            public Camera Camera;
            public bool Enabled;
            public string Tag;
        }

        [MenuItem("Tools/Quest RTS/Validate Generated Quest Runtime")]
        public static void ValidateGeneratedQuestRuntime()
        {
            List<QuestRuntimeSmokeItem> results = BuildGeneratedQuestRuntimeReport();
            bool hasFailure = false;

            for (int i = 0; i < results.Count; i++)
            {
                QuestRuntimeSmokeItem item = results[i];
                string message = item.Label + ": " + item.Detail;
                if (item.Passed)
                {
                    Debug.Log("[Quest RTS Runtime] PASS - " + message);
                }
                else if (item.Manual)
                {
                    Debug.LogWarning("[Quest RTS Runtime] MANUAL - " + message);
                }
                else
                {
                    hasFailure = true;
                    Debug.LogError("[Quest RTS Runtime] FAIL - " + message);
                }
            }

            if (hasFailure)
            {
                Debug.LogError("[Quest RTS Runtime] Generated Quest runtime smoke validation found blocking setup issues.");
            }
            else
            {
                Debug.Log("[Quest RTS Runtime] Generated Quest runtime smoke validation completed. Physical headset checks remain manual.");
            }
        }

        internal static List<QuestRuntimeSmokeItem> BuildGeneratedQuestRuntimeReport()
        {
            CameraState[] cameraStates = CaptureCameraStates();
            GameObject existingSun = GameObject.Find("Sun");
            RtsRuntimeModeResolver.ForceModeForTests(RtsRuntimeMode.QuestVr);
            GameObject root = new GameObject("Quest Runtime Smoke Test");
            try
            {
                RtsGame game = root.AddComponent<RtsGame>();
                game.Initialize();
                Physics.SyncTransforms();
                return QuestRuntimeSmokeReport.Build(game);
            }
            finally
            {
                RtsRuntimeModeResolver.ForceModeForTests(null);
                RestoreCameraStates(cameraStates);
                DestroyGeneratedObjects(root, existingSun);
            }
        }

        private static CameraState[] CaptureCameraStates()
        {
            Camera[] cameras = Object.FindObjectsOfType<Camera>();
            CameraState[] states = new CameraState[cameras.Length];
            for (int i = 0; i < cameras.Length; i++)
            {
                states[i] = new CameraState
                {
                    Camera = cameras[i],
                    Enabled = cameras[i] != null && cameras[i].enabled,
                    Tag = cameras[i] != null ? cameras[i].gameObject.tag : "Untagged"
                };
            }

            return states;
        }

        private static void RestoreCameraStates(CameraState[] states)
        {
            for (int i = 0; i < states.Length; i++)
            {
                Camera camera = states[i].Camera;
                if (camera == null)
                {
                    continue;
                }

                camera.enabled = states[i].Enabled;
                camera.gameObject.tag = states[i].Tag;
            }
        }

        private static void DestroyGeneratedObjects(GameObject root, GameObject existingSun)
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            GameObject generatedSun = GameObject.Find("Sun");
            if (generatedSun != null && generatedSun != existingSun)
            {
                Object.DestroyImmediate(generatedSun);
            }
        }
    }
}
#endif
