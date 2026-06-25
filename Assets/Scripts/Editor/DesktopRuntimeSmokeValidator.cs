#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace QuestCommandRTS.Editor
{
    public static class DesktopRuntimeSmokeValidator
    {
        private struct CameraState
        {
            public Camera Camera;
            public bool Enabled;
            public string Tag;
            public Vector3 Position;
            public Quaternion Rotation;
            public CameraClearFlags ClearFlags;
            public Color BackgroundColor;
            public float NearClipPlane;
            public float FarClipPlane;
            public float FieldOfView;
        }

        private struct EventSystemState
        {
            public EventSystemState(GameObject gameObject)
            {
                GameObject = gameObject;
            }

            public GameObject GameObject;
        }

        [MenuItem("Command RTS/Validate Generated Desktop Runtime")]
        public static void ValidateGeneratedDesktopRuntime()
        {
            List<DesktopRuntimeSmokeItem> results = BuildGeneratedDesktopRuntimeReport();
            bool hasFailure = false;

            for (int i = 0; i < results.Count; i++)
            {
                DesktopRuntimeSmokeItem item = results[i];
                string message = item.Label + ": " + item.Detail;
                if (item.Passed)
                {
                    Debug.Log("[Command RTS Desktop] PASS - " + message);
                }
                else if (item.Manual)
                {
                    Debug.LogWarning("[Command RTS Desktop] MANUAL - " + message);
                }
                else
                {
                    hasFailure = true;
                    Debug.LogError("[Command RTS Desktop] FAIL - " + message);
                }
            }

            if (hasFailure)
            {
                Debug.LogError("[Command RTS Desktop] Generated desktop runtime smoke validation found blocking setup issues.");
            }
            else
            {
                Debug.Log("[Command RTS Desktop] Generated desktop runtime smoke validation completed. Manual desktop controls still need playthrough verification.");
            }
        }

        internal static List<DesktopRuntimeSmokeItem> BuildGeneratedDesktopRuntimeReport()
        {
            CameraState[] cameraStates = CaptureCameraStates();
            EventSystemState existingEventSystem = new EventSystemState(UnityEngine.EventSystems.EventSystem.current != null ? UnityEngine.EventSystems.EventSystem.current.gameObject : null);
            GameObject existingSun = GameObject.Find("Sun");
            GameObject existingCommandCamera = GameObject.Find("Command Camera");
            RtsRuntimeModeResolver.ForceModeForTests(RtsRuntimeMode.Desktop);
            GameObject root = new GameObject("Desktop Runtime Smoke Test");
            try
            {
                RtsGame game = root.AddComponent<RtsGame>();
                game.Initialize();
                Physics.SyncTransforms();
                return DesktopRuntimeSmokeReport.Build(game);
            }
            finally
            {
                RtsRuntimeModeResolver.ForceModeForTests(null);
                RestoreCameraStates(cameraStates);
                DestroyGeneratedObjects(root, existingSun, existingCommandCamera, existingEventSystem);
            }
        }

        private static CameraState[] CaptureCameraStates()
        {
            Camera[] cameras = Object.FindObjectsOfType<Camera>();
            CameraState[] states = new CameraState[cameras.Length];
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                states[i] = new CameraState
                {
                    Camera = camera,
                    Enabled = camera != null && camera.enabled,
                    Tag = camera != null ? camera.gameObject.tag : "Untagged",
                    Position = camera != null ? camera.transform.position : Vector3.zero,
                    Rotation = camera != null ? camera.transform.rotation : Quaternion.identity,
                    ClearFlags = camera != null ? camera.clearFlags : CameraClearFlags.Skybox,
                    BackgroundColor = camera != null ? camera.backgroundColor : Color.black,
                    NearClipPlane = camera != null ? camera.nearClipPlane : 0.3f,
                    FarClipPlane = camera != null ? camera.farClipPlane : 1000f,
                    FieldOfView = camera != null ? camera.fieldOfView : 60f
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
                camera.transform.position = states[i].Position;
                camera.transform.rotation = states[i].Rotation;
                camera.clearFlags = states[i].ClearFlags;
                camera.backgroundColor = states[i].BackgroundColor;
                camera.nearClipPlane = states[i].NearClipPlane;
                camera.farClipPlane = states[i].FarClipPlane;
                camera.fieldOfView = states[i].FieldOfView;
            }
        }

        private static void DestroyGeneratedObjects(GameObject root, GameObject existingSun, GameObject existingCommandCamera, EventSystemState existingEventSystem)
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

            GameObject generatedCommandCamera = GameObject.Find("Command Camera");
            if (generatedCommandCamera != null && generatedCommandCamera != existingCommandCamera)
            {
                Object.DestroyImmediate(generatedCommandCamera);
            }

            GameObject generatedEventSystem = GameObject.Find("EventSystem");
            if (generatedEventSystem != null && generatedEventSystem != existingEventSystem.GameObject)
            {
                Object.DestroyImmediate(generatedEventSystem);
            }
        }
    }
}
#endif
