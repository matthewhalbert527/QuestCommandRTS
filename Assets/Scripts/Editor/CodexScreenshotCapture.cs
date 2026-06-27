using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace QuestCommandRTS.Editor
{
    public static class CodexScreenshotCapture
    {
        public static void CaptureTankDetailPass()
        {
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                GameObject gameObject = new GameObject("Screenshot RTS Game");
                RtsGame game = gameObject.AddComponent<RtsGame>();

                InvokePrivate(game, "CreateMaterials");
                InvokePrivate(game, "CreateRoots");
                InvokePrivate(game, "SetupCameraAndLight");
                CreateDesertPreviewGround();

                RebuildBastionPrefabIfAvailable();
                BuildUnitVisual(game, "Detailed Tank Showcase", UnitKind.Tank, Vector3.zero);

                Camera camera = game.CommandCamera;
                if (camera == null)
                {
                    throw new InvalidOperationException("RtsGame did not create a command camera.");
                }

                camera.transform.position = new Vector3(-2.75f, 1.55f, 3.55f);
                camera.transform.LookAt(new Vector3(0f, 0.68f, 0.18f), Vector3.up);
                camera.orthographic = false;
                camera.fieldOfView = 33f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.46f, 0.51f, 0.52f);

                ConfigureScreenshotLighting();

                string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Screenshots");
                Directory.CreateDirectory(outputDirectory);
                string outputPath = Path.Combine(outputDirectory, "tank_detail_pass.png");

                RenderCamera(camera, outputPath, 1600, 1000);
                Debug.Log("Codex screenshot written to " + outputPath);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void CaptureWarFactoryDetailPass()
        {
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                GameObject gameObject = new GameObject("Screenshot RTS Game");
                RtsGame game = gameObject.AddComponent<RtsGame>();

                InvokePrivate(game, "CreateMaterials");
                InvokePrivate(game, "CreateRoots");
                InvokePrivate(game, "SetupCameraAndLight");
                InvokePrivate(game, "CreateGround");

                BuildStructureVisual(game, "War Factory Showcase", StructureKind.WarFactory, Vector3.zero);

                Camera camera = game.CommandCamera;
                if (camera == null)
                {
                    throw new InvalidOperationException("RtsGame did not create a command camera.");
                }

                camera.transform.position = new Vector3(6.8f, 8.4f, 9.2f);
                camera.transform.LookAt(new Vector3(0f, 0.8f, 0.4f), Vector3.up);
                camera.orthographic = true;
                camera.orthographicSize = 5.25f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.07f, 0.075f, 0.07f);

                ConfigureScreenshotLighting();

                string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Screenshots");
                Directory.CreateDirectory(outputDirectory);
                string outputPath = Path.Combine(outputDirectory, "war_factory_detail_pass.png");

                RenderCamera(camera, outputPath, 1600, 1000);
                Debug.Log("Codex screenshot written to " + outputPath);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void CaptureAllUnitsDetailPass()
        {
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                GameObject gameObject = new GameObject("Screenshot RTS Game");
                RtsGame game = gameObject.AddComponent<RtsGame>();

                InvokePrivate(game, "CreateMaterials");
                InvokePrivate(game, "CreateRoots");
                InvokePrivate(game, "SetupCameraAndLight");
                CreateDesertPreviewGround();

                RebuildBastionPrefabIfAvailable();
                BuildUnitVisual(game, "Rifleman Showcase", UnitKind.Rifleman, new Vector3(-5.2f, 0f, -0.75f), 90f);
                BuildUnitVisual(game, "Rocket Soldier Showcase", UnitKind.RocketSoldier, new Vector3(-4.45f, 0f, -0.75f), 90f);
                BuildUnitVisual(game, "Grenadier Showcase", UnitKind.Grenadier, new Vector3(-3.7f, 0f, -0.75f), 90f);
                BuildUnitVisual(game, "Flame Trooper Showcase", UnitKind.FlameTrooper, new Vector3(-2.95f, 0f, -0.75f), 90f);
                BuildUnitVisual(game, "Engineer Showcase", UnitKind.Engineer, new Vector3(-2.2f, 0f, -0.75f), 90f);
                BuildUnitVisual(game, "Harvester Showcase", UnitKind.Harvester, new Vector3(-1.2f, 0f, 0.35f));
                BuildUnitVisual(game, "Tank Showcase", UnitKind.Tank, new Vector3(1.05f, 0f, 0.05f));
                BuildUnitVisual(game, "Skyraider Showcase", UnitKind.Skyraider, new Vector3(3.2f, 0f, -0.25f));
                BuildUnitVisual(game, "Orca Lifter Showcase", UnitKind.OrcaLifter, new Vector3(5.65f, 0f, 0.15f));

                Camera camera = game.CommandCamera;
                if (camera == null)
                {
                    throw new InvalidOperationException("RtsGame did not create a command camera.");
                }

                camera.transform.position = new Vector3(1.2f, 4.8f, 8.6f);
                camera.transform.LookAt(new Vector3(0.2f, 0.8f, 0.0f), Vector3.up);
                camera.orthographic = true;
                camera.orthographicSize = 5.25f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.45f, 0.5f, 0.5f);

                ConfigureScreenshotLighting();

                string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Screenshots");
                Directory.CreateDirectory(outputDirectory);
                string outputPath = Path.Combine(outputDirectory, "units_detail_pass.png");

                RenderCamera(camera, outputPath, 1800, 1050);
                Debug.Log("Codex screenshot written to " + outputPath);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void CaptureAllStructuresDetailPass()
        {
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                GameObject gameObject = new GameObject("Screenshot RTS Game");
                RtsGame game = gameObject.AddComponent<RtsGame>();

                InvokePrivate(game, "CreateMaterials");
                InvokePrivate(game, "CreateRoots");
                InvokePrivate(game, "SetupCameraAndLight");
                InvokePrivate(game, "CreateGround");

                BuildStructureVisual(game, "Command Center Showcase", StructureKind.CommandCenter, new Vector3(-6.4f, 0f, -3.6f));
                BuildStructureVisual(game, "Refinery Showcase", StructureKind.Refinery, new Vector3(0f, 0f, -3.6f));
                BuildStructureVisual(game, "War Factory Showcase", StructureKind.WarFactory, new Vector3(7.0f, 0f, -3.5f));
                BuildStructureVisual(game, "Barracks Showcase", StructureKind.Barracks, new Vector3(-6.4f, 0f, 3.2f));
                BuildStructureVisual(game, "Power Plant Showcase", StructureKind.PowerPlant, new Vector3(-1.4f, 0f, 3.4f));
                BuildStructureVisual(game, "Turret Showcase", StructureKind.Turret, new Vector3(2.7f, 0f, 3.6f));
                BuildStructureVisual(game, "Dual Helipad Showcase", StructureKind.DualHelipad, new Vector3(7.7f, 0f, 3.4f));

                Camera camera = game.CommandCamera;
                if (camera == null)
                {
                    throw new InvalidOperationException("RtsGame did not create a command camera.");
                }

                camera.transform.position = new Vector3(7.5f, 12.4f, 12.0f);
                camera.transform.LookAt(new Vector3(1.0f, 0.9f, 0.1f), Vector3.up);
                camera.orthographic = true;
                camera.orthographicSize = 8.2f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.075f, 0.078f, 0.072f);

                ConfigureScreenshotLighting();

                string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Screenshots");
                Directory.CreateDirectory(outputDirectory);
                string outputPath = Path.Combine(outputDirectory, "structures_detail_pass.png");

                RenderCamera(camera, outputPath, 1800, 1100);
                Debug.Log("Codex screenshot written to " + outputPath);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void CaptureAllCardIcons()
        {
            try
            {
                string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Resources", "Cards");
                Directory.CreateDirectory(outputDirectory);

                RebuildBastionPrefabIfAvailable();

                CaptureCardIcon(outputDirectory, "Rifleman", game => BuildUnitVisual(game, "Rifleman Card", UnitKind.Rifleman, Vector3.zero, 90f), 1.05f);
                CaptureCardIcon(outputDirectory, "RocketSoldier", game => BuildUnitVisual(game, "Rocket Soldier Card", UnitKind.RocketSoldier, Vector3.zero, 90f), 1.05f);
                CaptureCardIcon(outputDirectory, "Grenadier", game => BuildUnitVisual(game, "Grenadier Card", UnitKind.Grenadier, Vector3.zero, 90f), 1.05f);
                CaptureCardIcon(outputDirectory, "FlameTrooper", game => BuildUnitVisual(game, "Flame Trooper Card", UnitKind.FlameTrooper, Vector3.zero, 90f), 1.05f);
                CaptureCardIcon(outputDirectory, "Engineer", game => BuildUnitVisual(game, "Engineer Card", UnitKind.Engineer, Vector3.zero, 90f), 1.05f);
                CaptureCardIcon(outputDirectory, "Harvester", game => BuildUnitVisual(game, "Harvester Card", UnitKind.Harvester, Vector3.zero), 1.85f);
                CaptureCardIcon(outputDirectory, "Tank", game => BuildUnitVisual(game, "Tank Card", UnitKind.Tank, Vector3.zero), 1.75f);
                CaptureCardIcon(outputDirectory, "Skyraider", game => BuildUnitVisual(game, "Skyraider Card", UnitKind.Skyraider, Vector3.zero), 1.75f);
                CaptureCardIcon(outputDirectory, "OrcaLifter", game => BuildUnitVisual(game, "Orca Lifter Card", UnitKind.OrcaLifter, Vector3.zero), 2.05f);

                CaptureCardIcon(outputDirectory, "CommandCenter", game => BuildStructureVisual(game, "Command Center Card", StructureKind.CommandCenter, Vector3.zero), 3.3f);
                CaptureCardIcon(outputDirectory, "PowerPlant", game => BuildStructureVisual(game, "Power Plant Card", StructureKind.PowerPlant, Vector3.zero), 2.45f);
                CaptureCardIcon(outputDirectory, "Barracks", game => BuildStructureVisual(game, "Barracks Card", StructureKind.Barracks, Vector3.zero), 2.6f);
                CaptureCardIcon(outputDirectory, "Refinery", game => BuildStructureVisual(game, "Refinery Card", StructureKind.Refinery, Vector3.zero), 3.15f);
                CaptureCardIcon(outputDirectory, "WarFactory", game => BuildStructureVisual(game, "War Factory Card", StructureKind.WarFactory, Vector3.zero), 3.35f);
                CaptureCardIcon(outputDirectory, "Turret", game => BuildStructureVisual(game, "Turret Card", StructureKind.Turret, Vector3.zero), 1.85f);
                CaptureCardIcon(outputDirectory, "DualHelipad", game => BuildStructureVisual(game, "Dual Helipad Card", StructureKind.DualHelipad, Vector3.zero), 3.55f);

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log("Codex card icons written to " + outputDirectory);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void InvokePrivate(RtsGame game, string methodName)
        {
            MethodInfo method = typeof(RtsGame).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(typeof(RtsGame).FullName, methodName);
            }

            method.Invoke(game, null);
        }

        private static GameObject BuildStructureVisual(RtsGame game, string name, StructureKind kind, Vector3 position)
        {
            GameObject root = new GameObject(name);
            root.transform.position = position;

            MethodInfo method = typeof(RtsGame).GetMethod("BuildStructureVisual", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(typeof(RtsGame).FullName, "BuildStructureVisual");
            }

            method.Invoke(game, new object[] { root.transform, kind, RtsTeam.Player });
            return root;
        }

        private static GameObject BuildUnitVisual(RtsGame game, string name, UnitKind kind, Vector3 position, float yawDegrees = 0f)
        {
            GameObject root = new GameObject(name);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);

            MethodInfo method = typeof(RtsGame).GetMethod("BuildUnitVisual", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(typeof(RtsGame).FullName, "BuildUnitVisual");
            }

            method.Invoke(game, new object[] { root.transform, kind, RtsTeam.Player });
            return root;
        }

        private static void CaptureCardIcon(string outputDirectory, string fileName, Func<RtsGame, GameObject> buildVisual, float minimumOrthographicSize)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject gameObject = new GameObject("Card Icon RTS Game");
            RtsGame game = gameObject.AddComponent<RtsGame>();

            InvokePrivate(game, "CreateMaterials");
            InvokePrivate(game, "CreateRoots");
            InvokePrivate(game, "SetupCameraAndLight");

            GameObject visualRoot = buildVisual(game);
            Bounds bounds = GetRendererBounds(visualRoot);
            CreateCardPreviewFloor(bounds);

            Camera camera = game.CommandCamera;
            if (camera == null)
            {
                throw new InvalidOperationException("RtsGame did not create a command camera.");
            }

            Vector3 target = bounds.center + Vector3.up * (bounds.extents.y * 0.04f);
            camera.transform.position = target + new Vector3(5.2f, 4.1f, 5.8f);
            camera.transform.LookAt(target, Vector3.up);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(minimumOrthographicSize, bounds.extents.magnitude * 0.82f);
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 80f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.082f, 0.082f);

            ConfigureScreenshotLighting();

            string outputPath = Path.Combine(outputDirectory, fileName + ".png");
            RenderCamera(camera, outputPath, 768, 768);
        }

        private static Bounds GetRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(false);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(root.name + " did not create any renderers.");
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void CreateCardPreviewFloor(Bounds bounds)
        {
            float diameter = Mathf.Max(2.4f, Mathf.Max(bounds.size.x, bounds.size.z) * 1.55f);
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Card Industrial Base";
            floor.transform.position = new Vector3(bounds.center.x, bounds.min.y - 0.035f, bounds.center.z);
            floor.transform.localScale = new Vector3(diameter, 0.035f, diameter);
            UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());

            MeshRenderer renderer = floor.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateCardFloorMaterial();
        }

        private static Material CreateCardFloorMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            Texture2D texture = new Texture2D(256, 256, TextureFormat.RGBA32, true);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;

            Color dark = new Color(0.13f, 0.14f, 0.135f);
            Color panel = new Color(0.29f, 0.30f, 0.285f);
            Color seam = new Color(0.045f, 0.05f, 0.05f);
            Color hazard = new Color(0.86f, 0.56f, 0.12f);

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    int gridX = x % 64;
                    int gridY = y % 64;
                    bool seamLine = gridX < 3 || gridY < 3;
                    bool hazardStripe = y > 204 && ((x + y) / 18) % 2 == 0;
                    float noise = Mathf.PerlinNoise(x * 0.07f, y * 0.08f) * 0.18f;
                    Color color = Color.Lerp(dark, panel, 0.55f + noise);
                    if (seamLine)
                    {
                        color = seam;
                    }
                    else if (hazardStripe)
                    {
                        color = hazard;
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(true, false);
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_MainTex", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_Color", Color.white);
            material.SetFloat("_Smoothness", 0.18f);
            material.SetFloat("_Glossiness", 0.18f);
            return material;
        }

        private static void ConfigureScreenshotLighting()
        {
            Light light = UnityEngine.Object.FindObjectOfType<Light>();
            if (light != null)
            {
                light.intensity = 1.45f;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.62f;
                light.transform.rotation = Quaternion.Euler(34f, -52f, 0f);
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.34f, 0.32f);
            GameObject fillLightObject = new GameObject("Screenshot Fill Light");
            Light fillLight = fillLightObject.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.42f;
            fillLightObject.transform.rotation = Quaternion.Euler(28f, 132f, 0f);
            QualitySettings.shadowDistance = 80f;
            QualitySettings.antiAliasing = 4;
        }

        private static void CreateDesertPreviewGround()
        {
            const int resolution = 80;
            const float size = 42f;
            Vector3[] vertices = new Vector3[(resolution + 1) * (resolution + 1)];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[resolution * resolution * 6];

            for (int z = 0; z <= resolution; z++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    float u = x / (float)resolution;
                    float v = z / (float)resolution;
                    float worldX = (u - 0.5f) * size;
                    float worldZ = (v - 0.5f) * size;
                    float ripple = Mathf.PerlinNoise(worldX * 0.42f + 3.1f, worldZ * 0.62f + 7.4f) * 0.08f;
                    float dune = Mathf.Sin(worldX * 0.58f + worldZ * 0.22f) * 0.035f;
                    int index = z * (resolution + 1) + x;
                    vertices[index] = new Vector3(worldX, ripple + dune - 0.035f, worldZ);
                    uvs[index] = new Vector2(u * 8f, v * 8f);
                }
            }

            int triangleIndex = 0;
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int a = z * (resolution + 1) + x;
                    int b = a + 1;
                    int c = a + resolution + 1;
                    int d = c + 1;
                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = d;
                }
            }

            Mesh mesh = new Mesh { name = "Screenshot Desert Ground Mesh" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject ground = new GameObject("Screenshot Desert Ground");
            MeshFilter filter = ground.AddComponent<MeshFilter>();
            MeshRenderer renderer = ground.AddComponent<MeshRenderer>();
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = CreateDesertMaterial();

            CreateRangeMarker(new Vector3(1.9f, 0.07f, 1.65f), 0f);
            CreateRangeMarker(new Vector3(2.55f, 0.07f, 1.05f), 0f);
        }

        private static Material CreateDesertMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            Texture2D texture = new Texture2D(256, 256, TextureFormat.RGBA32, true);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 4;
            Color sand = new Color(0.46f, 0.42f, 0.35f);
            Color dark = new Color(0.24f, 0.23f, 0.22f);

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float n = Mathf.PerlinNoise(x * 0.035f, y * 0.05f);
                    float fine = Mathf.PerlinNoise(x * 0.21f + 8f, y * 0.18f + 4f);
                    Color color = Color.Lerp(dark, sand, 0.62f + n * 0.28f + fine * 0.08f);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(true, false);
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_MainTex", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_Color", Color.white);
            material.SetFloat("_Smoothness", 0.18f);
            material.SetFloat("_Glossiness", 0.18f);
            return material;
        }

        private static void CreateRangeMarker(Vector3 position, float yaw)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Red White Range Marker";
            marker.transform.position = position;
            marker.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            marker.transform.localScale = new Vector3(0.9f, 0.035f, 0.16f);
            UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());

            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            Texture2D texture = new Texture2D(128, 16, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    bool red = (x / 16) % 2 == 0;
                    texture.SetPixel(x, y, red ? new Color(0.62f, 0.08f, 0.08f) : new Color(0.86f, 0.84f, 0.78f));
                }
            }
            texture.Apply();
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_MainTex", texture);
            marker.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void RebuildBastionPrefabIfAvailable()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            TryInvokePrefabBuilder("BastionTankR.Editor.BastionTankPrefabBuilder, Assembly-CSharp-Editor");
            TryInvokePrefabBuilder("BastionHeavyTank.Editor.BastionHT77PrefabBuilder, Assembly-CSharp-Editor");
            TryInvokePrefabBuilder("BastionMediumTank.Editor.BastionMT12PrefabBuilder, Assembly-CSharp-Editor");
            TryInvokePrefabBuilder("BastionInfantry.Editor.BastionInfantryPrefabBuilder, Assembly-CSharp-Editor");
            TryInvokePrefabBuilder("BastionStructures.Editor.BastionStructuresPrefabBuilder, Assembly-CSharp-Editor");
            TryInvokePrefabBuilder("BastionFabricationHub.Editor.BastionFabricationHubPrefabBuilder, Assembly-CSharp-Editor");
            TryInvokePrefabBuilder("BastionWarFactoryCMV2.Editor.BastionWarFactoryCMV2PrefabBuilder, Assembly-CSharp-Editor");
            MirrorBastionPrefabsToResources();
        }

        private static void TryInvokePrefabBuilder(string typeName)
        {
            Type builderType = Type.GetType(typeName);
            if (builderType == null)
            {
                return;
            }

            string[] methodNames = { "Build", "BuildAll", "BuildPrefab" };
            foreach (string methodName in methodNames)
            {
                MethodInfo build = builderType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (build == null)
                {
                    continue;
                }

                ParameterInfo[] parameters = build.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(bool))
                {
                    build.Invoke(null, new object[] { false });
                    return;
                }

                if (parameters.Length == 0)
                {
                    build.Invoke(null, null);
                    return;
                }
            }
        }

        private static void MirrorBastionPrefabsToResources()
        {
            EnsureAssetFolder("Assets/Resources");
            EnsureAssetFolder("Assets/Resources/UnitModels");
            EnsureAssetFolder("Assets/Resources/StructureModels");

            CopyAssetIfPresent("Assets/BastionInfantry/Prefabs/Bastion_Gunner.prefab", "Assets/Resources/UnitModels/Bastion_Gunner.prefab");
            CopyAssetIfPresent("Assets/BastionInfantry/Prefabs/Bastion_Grenadier.prefab", "Assets/Resources/UnitModels/Bastion_Grenadier.prefab");
            CopyAssetIfPresent("Assets/BastionInfantry/Prefabs/Bastion_RocketSoldier.prefab", "Assets/Resources/UnitModels/Bastion_RocketSoldier.prefab");
            CopyAssetIfPresent("Assets/BastionInfantry/Prefabs/Bastion_FlameTrooper.prefab", "Assets/Resources/UnitModels/Bastion_FlameTrooper.prefab");
            CopyAssetIfPresent("Assets/BastionInfantry/Prefabs/Bastion_Engineer.prefab", "Assets/Resources/UnitModels/Bastion_Engineer.prefab");
            CopyAssetIfPresent("Assets/BastionStructures/Prefabs/Bastion_Harvester.prefab", "Assets/Resources/UnitModels/Bastion_Harvester.prefab");

            CopyAssetIfPresent("Assets/BastionStructures/Prefabs/Bastion_Barracks.prefab", "Assets/Resources/StructureModels/Bastion_Barracks.prefab");
            CopyAssetIfPresent("Assets/BastionStructures/Prefabs/Bastion_Refinery.prefab", "Assets/Resources/StructureModels/Bastion_Refinery.prefab");
            CopyAssetIfPresent("Assets/BastionStructures/Prefabs/Bastion_PowerPlant.prefab", "Assets/Resources/StructureModels/Bastion_PowerPlant.prefab");
            CopyAssetIfPresent("Assets/BastionStructures/Prefabs/Bastion_LargePowerPlant.prefab", "Assets/Resources/StructureModels/Bastion_LargePowerPlant.prefab");
            CopyAssetIfPresent("Assets/BastionStructures/Prefabs/Bastion_CommunicationsCenter.prefab", "Assets/Resources/StructureModels/Bastion_CommunicationsCenter.prefab");
            CopyAssetIfPresent("Assets/BastionStructures/Prefabs/Bastion_Turret.prefab", "Assets/Resources/StructureModels/Bastion_Turret.prefab");
            CopyAssetIfPresent("Assets/BastionStructures/Prefabs/Bastion_GunTower.prefab", "Assets/Resources/StructureModels/Bastion_GunTower.prefab");
            CopyAssetIfPresent("Assets/BastionStructures/Prefabs/Bastion_AdvancedGunTower.prefab", "Assets/Resources/StructureModels/Bastion_AdvancedGunTower.prefab");
            CopyAssetIfPresent("Assets/BastionStructures/Prefabs/Bastion_WarFactory.prefab", "Assets/Resources/StructureModels/Bastion_WarFactory.prefab");
            CopyAssetIfPresent("Assets/BastionFabricationHub/Prefabs/Bastion_FabricationHub.prefab", "Assets/Resources/StructureModels/Bastion_FabricationHub.prefab");
            CopyAssetIfPresent("Assets/BastionWarFactoryCMV2/Prefabs/Bastion_WarFactoryCMV2.prefab", "Assets/Resources/StructureModels/Bastion_WarFactoryCMV2.prefab");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void CopyAssetIfPresent(string sourcePath, string destinationPath)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(sourcePath) == null)
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(destinationPath) != null)
            {
                AssetDatabase.DeleteAsset(destinationPath);
            }

            AssetDatabase.CopyAsset(sourcePath, destinationPath);
        }

        private static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folder = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureAssetFolder(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static void RenderCamera(Camera camera, string outputPath, int width, int height)
        {
            RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            renderTexture.antiAliasing = 8;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;

            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(texture);
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }
    }
}
