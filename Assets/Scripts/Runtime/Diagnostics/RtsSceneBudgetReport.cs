using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuestCommandRTS
{
    public readonly struct RtsSceneBudgetItem
    {
        public readonly string Label;
        public readonly bool Passed;
        public readonly string Detail;

        public RtsSceneBudgetItem(string label, bool passed, string detail)
        {
            Label = label;
            Passed = passed;
            Detail = detail;
        }
    }

    [Serializable]
    public sealed class RtsSceneBudgetSnapshot
    {
        public string capturedAtUtc = string.Empty;
        public string runtimeMode = string.Empty;
        public int entityCount;
        public int resourceNodeCount;
        public int totalGameObjects;
        public int activeGameObjects;
        public int rendererCount;
        public int enabledRendererCount;
        public int lineRendererCount;
        public int fogOverlayObjectCount;
        public int fogOverlayRendererCount;
        public int fogCellObjectCount;
        public int uniqueSharedMaterialCount;
        public int colliderCount;
        public int enabledColliderCount;
        public int visualSetDressingObjectCount;
        public int visualSetDressingColliderCount;
        public int cameraCount;
        public int enabledCameraCount;
        public int lightCount;
        public int enabledLightCount;
        public int canvasCount;
        public int worldSpaceCanvasCount;
        public int screenSpaceOverlayCanvasCount;
        public int tacticalMapCanvasCount;

        public static RtsSceneBudgetSnapshot Capture(RtsGame game)
        {
            RtsSceneBudgetSnapshot snapshot = new RtsSceneBudgetSnapshot
            {
                capturedAtUtc = DateTime.UtcNow.ToString("o"),
                runtimeMode = game != null ? game.RuntimeMode.ToString() : "None",
                entityCount = game != null && game.Entities != null ? game.Entities.Count : 0,
                resourceNodeCount = game != null && game.ResourceNodes != null ? game.ResourceNodes.Count : 0
            };

            Transform root = game != null ? game.transform : null;
            CaptureGameObjects(snapshot, root);
            CaptureRenderers(snapshot, root);
            CaptureColliders(snapshot, root);
            CaptureCameras(snapshot, root);
            CaptureLights(snapshot);
            CaptureCanvases(snapshot, root);
            return snapshot;
        }

        public string ToJson(bool prettyPrint)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }

        private static void CaptureGameObjects(RtsSceneBudgetSnapshot snapshot, Transform root)
        {
            Transform[] transforms = root != null ? root.GetComponentsInChildren<Transform>(true) : UnityEngine.Object.FindObjectsOfType<Transform>(true);
            snapshot.totalGameObjects = transforms.Length;
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                GameObject sceneObject = transform != null ? transform.gameObject : null;
                if (sceneObject == null)
                {
                    continue;
                }

                if (sceneObject.activeInHierarchy)
                {
                    snapshot.activeGameObjects++;
                }

                if (IsVisualSetDressing(sceneObject.name))
                {
                    snapshot.visualSetDressingObjectCount++;
                    if (sceneObject.GetComponent<Collider>() != null)
                    {
                        snapshot.visualSetDressingColliderCount++;
                    }
                }

                if (sceneObject.name == "Fog Overlay")
                {
                    snapshot.fogOverlayObjectCount++;
                }

                if (sceneObject.name.StartsWith("Fog Cell", StringComparison.Ordinal))
                {
                    snapshot.fogCellObjectCount++;
                }
            }
        }

        private static void CaptureRenderers(RtsSceneBudgetSnapshot snapshot, Transform root)
        {
            Renderer[] renderers = root != null ? root.GetComponentsInChildren<Renderer>(true) : UnityEngine.Object.FindObjectsOfType<Renderer>(true);
            List<Material> uniqueMaterials = new List<Material>();
            snapshot.rendererCount = renderers.Length;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (renderer.enabled && renderer.gameObject.activeInHierarchy)
                {
                    snapshot.enabledRendererCount++;
                }

                if (renderer is LineRenderer)
                {
                    snapshot.lineRendererCount++;
                }

                if (renderer.gameObject.name == "Fog Overlay")
                {
                    snapshot.fogOverlayRendererCount++;
                }

                Material[] materials = renderer.sharedMaterials;
                for (int j = 0; j < materials.Length; j++)
                {
                    Material material = materials[j];
                    if (material != null && !uniqueMaterials.Contains(material))
                    {
                        uniqueMaterials.Add(material);
                    }
                }
            }

            snapshot.uniqueSharedMaterialCount = uniqueMaterials.Count;
        }

        private static void CaptureColliders(RtsSceneBudgetSnapshot snapshot, Transform root)
        {
            Collider[] colliders = root != null ? root.GetComponentsInChildren<Collider>(true) : UnityEngine.Object.FindObjectsOfType<Collider>(true);
            snapshot.colliderCount = colliders.Length;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy)
                {
                    snapshot.enabledColliderCount++;
                }
            }
        }

        private static void CaptureCameras(RtsSceneBudgetSnapshot snapshot, Transform root)
        {
            Camera[] cameras = root != null ? root.GetComponentsInChildren<Camera>(true) : UnityEngine.Object.FindObjectsOfType<Camera>(true);
            snapshot.cameraCount = cameras.Length;
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera != null && camera.enabled && camera.gameObject.activeInHierarchy)
                {
                    snapshot.enabledCameraCount++;
                }
            }
        }

        private static void CaptureLights(RtsSceneBudgetSnapshot snapshot)
        {
            Light[] lights = UnityEngine.Object.FindObjectsOfType<Light>(true);
            snapshot.lightCount = lights.Length;
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light != null && light.enabled && light.gameObject.activeInHierarchy)
                {
                    snapshot.enabledLightCount++;
                }
            }
        }

        private static void CaptureCanvases(RtsSceneBudgetSnapshot snapshot, Transform root)
        {
            Canvas[] canvases = root != null ? root.GetComponentsInChildren<Canvas>(true) : UnityEngine.Object.FindObjectsOfType<Canvas>(true);
            snapshot.canvasCount = canvases.Length;
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null)
                {
                    continue;
                }

                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    snapshot.worldSpaceCanvasCount++;
                }

                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    snapshot.screenSpaceOverlayCanvasCount++;
                }

                if (canvas.gameObject.name == "Quest Tactical Map")
                {
                    snapshot.tacticalMapCanvasCount++;
                }
            }
        }

        private static bool IsVisualSetDressing(string objectName)
        {
            return objectName.StartsWith("Projected Water", StringComparison.Ordinal) ||
                objectName.Contains("Dune Shelf") ||
                objectName.Contains("Mesa Ridge") ||
                objectName.Contains("Cliff") ||
                objectName.Contains("Mountain") ||
                objectName.Contains("Talus") ||
                objectName.Contains("Dry Wash") ||
                objectName.Contains("Blast Scorch") ||
                objectName.Contains(" Rock ") ||
                objectName.Contains("Table Rail") ||
                objectName.Contains("Table Pylon") ||
                objectName.Contains("Board Glow");
        }
    }

    public static class RtsSceneBudgetReport
    {
        public const int MaxQuestGameObjects = 1700;
        public const int MaxQuestRenderers = 1000;
        public const int MaxQuestUniqueSharedMaterials = 180;
        public const int MaxQuestEnabledColliders = 130;
        public const int MaxQuestEnabledLights = 1;
        public const int MaxQuestEnabledCameras = 1;

        public static List<RtsSceneBudgetItem> BuildQuestBudget(RtsGame game)
        {
            RtsSceneBudgetSnapshot snapshot = RtsSceneBudgetSnapshot.Capture(game);
            List<RtsSceneBudgetItem> results = new List<RtsSceneBudgetItem>();

            Add(results, "Runtime mode", snapshot.runtimeMode == RtsRuntimeMode.QuestVr.ToString(), snapshot.runtimeMode);
            Add(results, "GameObject budget", snapshot.totalGameObjects <= MaxQuestGameObjects, snapshot.totalGameObjects + "/" + MaxQuestGameObjects);
            Add(results, "Renderer budget", snapshot.rendererCount <= MaxQuestRenderers, snapshot.rendererCount + "/" + MaxQuestRenderers);
            Add(results, "Shared material budget", snapshot.uniqueSharedMaterialCount <= MaxQuestUniqueSharedMaterials, snapshot.uniqueSharedMaterialCount + "/" + MaxQuestUniqueSharedMaterials);
            Add(results, "Collider budget", snapshot.enabledColliderCount <= MaxQuestEnabledColliders, snapshot.enabledColliderCount + "/" + MaxQuestEnabledColliders);
            Add(results, "Light budget", snapshot.enabledLightCount <= MaxQuestEnabledLights, snapshot.enabledLightCount + "/" + MaxQuestEnabledLights);
            Add(results, "Camera budget", snapshot.enabledCameraCount == MaxQuestEnabledCameras, snapshot.enabledCameraCount + "/" + MaxQuestEnabledCameras);
            Add(results, "World-space Quest UI", snapshot.worldSpaceCanvasCount >= 3 && snapshot.screenSpaceOverlayCanvasCount == 0, "world=" + snapshot.worldSpaceCanvasCount + ", overlay=" + snapshot.screenSpaceOverlayCanvasCount);
            Add(results, "Quest tactical map UI", snapshot.tacticalMapCanvasCount == 1, "tacticalMapCanvases=" + snapshot.tacticalMapCanvasCount);
            Add(results, "Fog overlay budget", snapshot.fogOverlayObjectCount == 1 && snapshot.fogOverlayRendererCount == 1 && snapshot.fogCellObjectCount == 0, "overlays=" + snapshot.fogOverlayObjectCount + ", renderers=" + snapshot.fogOverlayRendererCount + ", cells=" + snapshot.fogCellObjectCount);
            Add(results, "Visual set dressing colliders", snapshot.visualSetDressingObjectCount > 0 && snapshot.visualSetDressingColliderCount == 0, "objects=" + snapshot.visualSetDressingObjectCount + ", colliders=" + snapshot.visualSetDressingColliderCount);
            return results;
        }

        private static void Add(List<RtsSceneBudgetItem> results, string label, bool passed, string detail)
        {
            results.Add(new RtsSceneBudgetItem(label, passed, detail));
        }
    }
}
