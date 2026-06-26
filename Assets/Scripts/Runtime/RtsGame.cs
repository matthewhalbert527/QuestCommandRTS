using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuestCommandRTS
{
    public static class RtsBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateGame()
        {
            if (Object.FindObjectOfType<RtsGame>() != null)
            {
                return;
            }

            GameObject gameObject = new GameObject("Quest Command RTS");
            gameObject.AddComponent<RtsGame>();
        }
    }

    public sealed class RtsGame : MonoBehaviour
    {
        public static RtsGame Instance { get; private set; }
        public static bool HasInstance => Instance != null;

        public ResourceBank Resources { get; private set; }
        public Camera CommandCamera { get; private set; }
        public BuildManager BuildManager { get; private set; }
        public RtsFogOfWar FogOfWar { get; private set; }
        public RtsCommandDispatcher CommandDispatcher { get; private set; }
        public RtsPlayerCommandService PlayerCommands { get; private set; }
        public RtsSimulationClock Clock { get; private set; }
        public RtsLifecycleCoordinator Lifecycle { get; private set; }
        public RtsSaveService SaveService { get; private set; }
        public RtsProfileSettings ProfileSettings { get; private set; }
        public RtsRuntimeMode RuntimeMode { get; private set; }
        public QuestTabletopRig QuestRig { get; private set; }
        public EnemyDirector EnemyDirector { get; private set; }
        public RtsSkirmishOptions SkirmishOptions { get; private set; } = RtsSkirmishOptions.CreateDefault();
        public IReadOnlyList<RtsEntity> Entities => entities;
        public IReadOnlyList<RtsEntity> Selection => selection;
        public IReadOnlyList<ResourceNode> ResourceNodes => resourceNodes;
        public RtsMatchState MatchState { get; private set; } = RtsMatchState.Running;
        public string StatusMessage { get; private set; } = "Destroy the enemy base.";
        public float MatchTime { get; private set; }
        public bool IsMatchOver => MatchState != RtsMatchState.Running;
        public bool IsUserPaused => Clock != null && (Clock.PauseReasons & RtsPauseReason.User) != 0;
        public bool AcceptsSystemInput => initialized && Lifecycle != null && Lifecycle.AcceptsInput && (SaveService == null || !SaveService.IsBusy);
        public bool AcceptsPlayerInput => AcceptsSystemInput && Clock != null && !Clock.IsPaused;

        private readonly List<RtsEntity> entities = new List<RtsEntity>();
        private readonly List<RtsEntity> selection = new List<RtsEntity>();
        private readonly List<ResourceNode> resourceNodes = new List<ResourceNode>();

        private Transform unitsRoot;
        private Transform structuresRoot;
        private Transform resourcesRoot;
        private Transform effectsRoot;
        private Material playerMaterial;
        private Material enemyMaterial;
        private Material neutralMaterial;
        private Material groundMaterial;
        private Material terrainAccentMaterial;
        private Material waterMaterial;
        private Material ridgeMaterial;
        private Material cliffMaterial;
        private Material mountainMaterial;
        private Material talusMaterial;
        private Material dryWashMaterial;
        private Material craterMaterial;
        private Material resourceMaterial;
        private Material depletedResourceMaterial;
        private Material resourceGlowMaterial;
        private Material resourceMinerMaterial;
        private Material darkMaterial;
        private Material vehicleDetailMaterial;
        private Material structureDetailMaterial;
        private Material edgeHighlightMaterial;
        private Material shadowPanelMaterial;
        private Material cautionStripeMaterial;
        private Material weaponMetalMaterial;
        private Material trackRubberMaterial;
        private Material sensorGlassMaterial;
        private Material panelTrimMaterial;
        private Material warningLightMaterial;
        private bool initialized;
        private float nextObjectiveCheckTime;
        private int nextEntityId = 1;
        private int nextResourceNodeId = 1;

        private static readonly Vector3[] ResourceFieldCenters =
        {
            new Vector3(-58f, 0f, -82f),
            new Vector3(-18f, 0f, -64f),
            new Vector3(-70f, 0f, -18f),
            new Vector3(0f, 0f, -18f),
            new Vector3(34f, 0f, 18f),
            new Vector3(-16f, 0f, 54f),
            new Vector3(62f, 0f, 42f),
            new Vector3(74f, 0f, 84f)
        };

        private static readonly int[] ResourceFieldNodeCounts =
        {
            9,
            7,
            6,
            8,
            8,
            7,
            7,
            9
        };

        private const int BattlefieldMeshSegments = 72;
        private const int TerrainPatchSegments = 34;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (!initialized || Clock == null)
            {
                return;
            }

            using (RtsProfilerMarkers.GameUpdate.Auto())
            {
                Clock.Tick(Time.unscaledDeltaTime);
                MatchTime = Clock.SimulationTime;

                if (IsMatchOver || Clock.IsPaused)
                {
                    return;
                }

                if (Clock.SimulationTime >= nextObjectiveCheckTime)
                {
                    nextObjectiveCheckTime = Clock.SimulationTime + 0.5f;
                    EvaluateMatchState();
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                shader = Shader.Find("Diffuse");
            }

            Material material = new Material(shader);
            material.enableInstancing = true;
            material.color = color;
            SetMaterialColorIfPresent(material, "_Color", color);
            SetMaterialColorIfPresent(material, "_BaseColor", color);
            return material;
        }

        private static Material CreateTeamMaterial(Color color)
        {
            Material material = CreateMaterial(color);
            ConfigurePbr(material, 0.08f, 0.54f);
            Color emission = color * 0.42f;
            emission.a = 1f;
            ConfigureEmission(material, emission);
            return material;
        }

        private static Material CreateTexturedMaterial(Color color, Texture2D texture, Vector2 tiling)
        {
            Material material = CreateMaterial(color);
            ApplyTexture(material, texture, tiling);
            return material;
        }

        private static Material CreateTexturedPbrMaterial(Color color, Texture2D texture, Vector2 tiling, float metallic, float smoothness, Texture2D normalMap, float normalScale)
        {
            Material material = CreateTexturedMaterial(color, texture, tiling);
            ConfigurePbr(material, metallic, smoothness);
            ApplyNormalMap(material, normalMap, normalScale, tiling);
            return material;
        }

        private static Material CreateTexturedTransparentMaterial(Color color, Texture2D texture, Vector2 tiling)
        {
            Material material = CreateTransparentMaterial(color);
            ApplyTexture(material, texture, tiling);
            return material;
        }

        private static void ApplyTexture(Material material, Texture2D texture, Vector2 tiling)
        {
            if (material == null || texture == null)
            {
                return;
            }

            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 2;
            material.mainTexture = texture;
            material.mainTextureScale = tiling;

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
                material.SetTextureScale("_MainTex", tiling);
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", tiling);
            }
        }

        private static void ApplyNormalMap(Material material, Texture2D normalMap, float normalScale, Vector2 tiling)
        {
            if (material == null || normalMap == null)
            {
                return;
            }

            normalMap.wrapMode = TextureWrapMode.Repeat;
            normalMap.filterMode = FilterMode.Bilinear;
            normalMap.anisoLevel = 2;
            material.EnableKeyword("_NORMALMAP");
            SetMaterialTextureIfPresent(material, "_BumpMap", normalMap);
            if (material.HasProperty("_BumpMap"))
            {
                material.SetTextureScale("_BumpMap", tiling);
            }

            SetMaterialFloatIfPresent(material, "_BumpScale", normalScale);
        }

        private static void ConfigurePbr(Material material, float metallic, float smoothness)
        {
            if (material == null)
            {
                return;
            }

            SetMaterialFloatIfPresent(material, "_Metallic", metallic);
            SetMaterialFloatIfPresent(material, "_Glossiness", smoothness);
            SetMaterialFloatIfPresent(material, "_Smoothness", smoothness);
            SetMaterialFloatIfPresent(material, "_SpecularHighlights", 1f);
            SetMaterialFloatIfPresent(material, "_EnvironmentReflections", 1f);
        }

        private static void ConfigureEmission(Material material, Color emission)
        {
            if (material == null)
            {
                return;
            }

            material.EnableKeyword("_EMISSION");
            SetMaterialColorIfPresent(material, "_EmissionColor", emission);
        }

        private static void SetMaterialFloatIfPresent(Material material, string propertyName, float value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static void SetMaterialColorIfPresent(Material material, string propertyName, Color value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, value);
            }
        }

        private static void SetMaterialTextureIfPresent(Material material, string propertyName, Texture texture)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }

        private static Texture2D CreateTerrainTexture(string name, Color baseColor, Color lowColor, Color highColor, Color lineColor, int seed, float grainStrength, float crackStrength, float stripeStrength)
        {
            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            texture.name = name;
            texture.hideFlags = HideFlags.DontSave;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float v = y / (float)(size - 1);
                    float broadNoise = Mathf.PerlinNoise((x + seed * 29) * 0.011f, (y + seed * 17) * 0.011f);
                    float largeNoise = Mathf.PerlinNoise((x + seed * 13) * 0.028f, (y - seed * 7) * 0.028f);
                    float fineNoise = Mathf.PerlinNoise((x - seed * 5) * 0.19f, (y + seed * 11) * 0.19f);
                    float mineralNoise = Mathf.PerlinNoise((x + seed * 19) * 0.42f, (y - seed * 23) * 0.42f);
                    float grain = (Hash01(x, y, seed) - 0.5f) * grainStrength;
                    float shade = Mathf.Clamp01(broadNoise * 0.22f + largeNoise * 0.48f + fineNoise * 0.24f + mineralNoise * 0.06f + grain);
                    Color color = Color.Lerp(lowColor, highColor, shade);
                    color = Color.Lerp(color, baseColor, 0.32f);
                    color = Color.Lerp(color, lowColor, Mathf.Clamp01(1f - largeNoise) * 0.1f);

                    if (stripeStrength > 0f)
                    {
                        float stripeNoise = Mathf.PerlinNoise((x + seed) * 0.045f, (y - seed) * 0.045f);
                        float stripe = Mathf.Sin((u * 18f + v * 5.5f + stripeNoise * 2f) * Mathf.PI * 2f);
                        float stripeMask = Mathf.SmoothStep(0.42f, 0.96f, Mathf.Abs(stripe));
                        color = Color.Lerp(color, lineColor, stripeMask * stripeStrength);

                        float crossStripe = Mathf.Sin((v * 22f - u * 3.8f + stripeNoise * 1.35f) * Mathf.PI * 2f);
                        float crossMask = Mathf.SmoothStep(0.78f, 0.985f, Mathf.Abs(crossStripe));
                        color = Color.Lerp(color, highColor, crossMask * stripeStrength * 0.18f);
                    }

                    if (crackStrength > 0f)
                    {
                        float crackNoise = Mathf.PerlinNoise((x + seed * 3) * 0.115f, (y - seed * 2) * 0.115f);
                        float crackGuide = Mathf.PerlinNoise((x - seed) * 0.035f, (y + seed) * 0.035f);
                        float crack = Mathf.SmoothStep(0.72f, 0.9f, crackNoise) * Mathf.SmoothStep(0.42f, 0.88f, crackGuide);
                        float hairline = Mathf.SmoothStep(0.84f, 0.965f, crackNoise) * Mathf.SmoothStep(0.22f, 0.72f, crackGuide);
                        color = Color.Lerp(color, lineColor, crack * crackStrength);
                        color = Color.Lerp(color, highColor, hairline * crackStrength * 0.09f);
                    }

                    float speckle = Hash01(x * 17 + seed, y * 31 - seed, seed);
                    if (speckle > 0.985f)
                    {
                        color = Color.Lerp(color, highColor, 0.32f * grainStrength);
                    }
                    else if (speckle < 0.012f)
                    {
                        color = Color.Lerp(color, lineColor, 0.18f * grainStrength);
                    }

                    color.a = baseColor.a;
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true, false);
            return texture;
        }

        private static Texture2D CreateTerrainNormalTexture(string name, int seed, float heightStrength, float crackStrength, float stripeStrength)
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            texture.name = name;
            texture.hideFlags = HideFlags.DontSave;

            Color[] pixels = new Color[size * size];
            float texel = 1f / size;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float v = y / (float)(size - 1);
                    float hL = SampleTerrainHeight(u - texel, v, seed, crackStrength, stripeStrength);
                    float hR = SampleTerrainHeight(u + texel, v, seed, crackStrength, stripeStrength);
                    float hD = SampleTerrainHeight(u, v - texel, seed, crackStrength, stripeStrength);
                    float hU = SampleTerrainHeight(u, v + texel, seed, crackStrength, stripeStrength);
                    Vector3 normal = new Vector3((hL - hR) * heightStrength, (hD - hU) * heightStrength, 1f).normalized;
                    pixels[y * size + x] = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true, false);
            return texture;
        }

        private static float SampleTerrainHeight(float u, float v, int seed, float crackStrength, float stripeStrength)
        {
            u = Mathf.Repeat(u, 1f);
            v = Mathf.Repeat(v, 1f);
            float x = u * 128f;
            float y = v * 128f;
            float broad = Mathf.PerlinNoise((x + seed * 29) * 0.022f, (y + seed * 17) * 0.022f);
            float detail = Mathf.PerlinNoise((x - seed * 5) * 0.15f, (y + seed * 11) * 0.15f);
            float mineral = Mathf.PerlinNoise((x + seed * 19) * 0.36f, (y - seed * 23) * 0.36f);
            float stripeNoise = Mathf.PerlinNoise((x + seed) * 0.036f, (y - seed) * 0.036f);
            float stripe = Mathf.Abs(Mathf.Sin((u * 18f + v * 5.5f + stripeNoise * 1.7f) * Mathf.PI * 2f));
            float crackNoise = Mathf.PerlinNoise((x + seed * 3) * 0.095f, (y - seed * 2) * 0.095f);
            float crackGuide = Mathf.PerlinNoise((x - seed) * 0.031f, (y + seed) * 0.031f);
            float crack = Mathf.SmoothStep(0.76f, 0.94f, crackNoise) * Mathf.SmoothStep(0.38f, 0.88f, crackGuide);
            return broad * 0.42f + detail * 0.28f + mineral * 0.12f + Mathf.SmoothStep(0.7f, 0.985f, stripe) * stripeStrength - crack * crackStrength;
        }

        private static Texture2D CreatePanelTexture(string name, Color baseColor, Color lowColor, Color highColor, Color seamColor, Color accentColor, int seed, int columns, int rows, float grimeStrength, float scratchStrength, float rivetStrength)
        {
            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            texture.name = name;
            texture.hideFlags = HideFlags.DontSave;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float v = y / (float)(size - 1);
                    float panelU = Mathf.Repeat(u * columns, 1f);
                    float panelV = Mathf.Repeat(v * rows + Mathf.Floor(u * columns) * 0.17f, 1f);
                    float cellId = Mathf.Floor(u * columns) + Mathf.Floor(v * rows) * 19f + seed;
                    float panelVariation = Hash01((int)cellId, seed, (int)(cellId * 3f)) - 0.5f;
                    float broad = Mathf.PerlinNoise((x + seed * 5) * 0.026f, (y - seed * 7) * 0.026f);
                    float fine = Mathf.PerlinNoise((x - seed * 11) * 0.18f, (y + seed * 13) * 0.18f);
                    float shade = Mathf.Clamp01(0.58f + panelVariation * 0.16f + (broad - 0.5f) * grimeStrength + (fine - 0.5f) * 0.18f);
                    Color color = Color.Lerp(lowColor, highColor, shade);
                    color = Color.Lerp(color, baseColor, 0.42f);

                    float seamDistance = Mathf.Min(Mathf.Min(panelU, 1f - panelU), Mathf.Min(panelV, 1f - panelV));
                    float seam = 1f - Mathf.SmoothStep(0.008f, 0.04f, seamDistance);
                    color = Color.Lerp(color, seamColor, seam * 0.72f);

                    float scratchNoise = Mathf.PerlinNoise((x + seed * 17) * 0.09f, (y - seed * 19) * 0.09f);
                    float scratch = Mathf.SmoothStep(0.82f, 0.98f, Mathf.Abs(Mathf.Sin((u * 44f + v * 8f + scratchNoise * 1.8f) * Mathf.PI)));
                    color = Color.Lerp(color, highColor, scratch * scratchStrength * 0.35f);

                    float rivet = GetPanelRivetMask(panelU, panelV);
                    color = Color.Lerp(color, accentColor, rivet * rivetStrength);

                    float chip = Hash01(x * 31 + seed, y * 29 - seed, seed);
                    if (chip > 0.992f)
                    {
                        color = Color.Lerp(color, seamColor, 0.42f);
                    }
                    else if (chip < 0.01f)
                    {
                        color = Color.Lerp(color, highColor, 0.22f);
                    }

                    color.a = baseColor.a;
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true, false);
            return texture;
        }

        private static Texture2D CreatePanelNormalTexture(string name, int seed, int columns, int rows, float seamDepth, float rivetHeight, float scratchHeight)
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            texture.name = name;
            texture.hideFlags = HideFlags.DontSave;

            Color[] pixels = new Color[size * size];
            float texel = 1f / size;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float v = y / (float)(size - 1);
                    float hL = SamplePanelHeight(u - texel, v, seed, columns, rows, seamDepth, rivetHeight, scratchHeight);
                    float hR = SamplePanelHeight(u + texel, v, seed, columns, rows, seamDepth, rivetHeight, scratchHeight);
                    float hD = SamplePanelHeight(u, v - texel, seed, columns, rows, seamDepth, rivetHeight, scratchHeight);
                    float hU = SamplePanelHeight(u, v + texel, seed, columns, rows, seamDepth, rivetHeight, scratchHeight);
                    Vector3 normal = new Vector3((hL - hR) * 2.6f, (hD - hU) * 2.6f, 1f).normalized;
                    pixels[y * size + x] = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true, false);
            return texture;
        }

        private static float SamplePanelHeight(float u, float v, int seed, int columns, int rows, float seamDepth, float rivetHeight, float scratchHeight)
        {
            u = Mathf.Repeat(u, 1f);
            v = Mathf.Repeat(v, 1f);
            float panelU = Mathf.Repeat(u * columns, 1f);
            float panelV = Mathf.Repeat(v * rows + Mathf.Floor(u * columns) * 0.17f, 1f);
            float seamDistance = Mathf.Min(Mathf.Min(panelU, 1f - panelU), Mathf.Min(panelV, 1f - panelV));
            float seam = 1f - Mathf.SmoothStep(0.006f, 0.05f, seamDistance);
            float rivet = GetPanelRivetMask(panelU, panelV);
            float scratchNoise = Mathf.PerlinNoise((u * 128f + seed * 17) * 0.09f, (v * 128f - seed * 19) * 0.09f);
            float scratch = Mathf.SmoothStep(0.88f, 0.995f, Mathf.Abs(Mathf.Sin((u * 44f + v * 8f + scratchNoise * 1.8f) * Mathf.PI)));
            return 0.5f - seam * seamDepth + rivet * rivetHeight + scratch * scratchHeight;
        }

        private static float GetPanelRivetMask(float panelU, float panelV)
        {
            float cornerDistance = Mathf.Min(
                Mathf.Min(Vector2.Distance(new Vector2(panelU, panelV), new Vector2(0.14f, 0.14f)), Vector2.Distance(new Vector2(panelU, panelV), new Vector2(0.86f, 0.14f))),
                Mathf.Min(Vector2.Distance(new Vector2(panelU, panelV), new Vector2(0.14f, 0.86f)), Vector2.Distance(new Vector2(panelU, panelV), new Vector2(0.86f, 0.86f))));
            return 1f - Mathf.SmoothStep(0.018f, 0.052f, cornerDistance);
        }

        private static float Hash01(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)x * 374761393u + (uint)y * 668265263u + (uint)seed * 2246822519u;
                h = (h ^ (h >> 13)) * 1274126177u;
                return (h ^ (h >> 16)) / 4294967295f;
            }
        }

        public Transform GetViewCameraTransform()
        {
            if (RuntimeMode == RtsRuntimeMode.QuestVr && QuestRig != null && QuestRig.HeadCamera != null)
            {
                return QuestRig.HeadCamera.transform;
            }

            if (CommandCamera != null)
            {
                return CommandCamera.transform;
            }

            Camera mainCamera = Camera.main;
            return mainCamera != null ? mainCamera.transform : null;
        }

        public void RegisterEntity(RtsEntity entity)
        {
            if (entity == null || entities.Contains(entity))
            {
                return;
            }

            if (entity.PersistentId <= 0)
            {
                entity.AssignPersistentId(AllocateEntityId());
            }
            else
            {
                nextEntityId = Mathf.Max(nextEntityId, entity.PersistentId + 1);
            }

            entities.Add(entity);
        }

        public void UnregisterEntity(RtsEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            entities.Remove(entity);
            if (selection.Remove(entity))
            {
                entity.SetSelected(false);
            }
        }

        public int AllocateEntityId()
        {
            return nextEntityId++;
        }

        public int AllocateResourceNodeId()
        {
            return nextResourceNodeId++;
        }

        public void ClearSelection()
        {
            for (int i = 0; i < selection.Count; i++)
            {
                if (selection[i] != null)
                {
                    selection[i].SetSelected(false);
                }
            }

            selection.Clear();
        }

        public void SelectEntity(RtsEntity entity, bool additive)
        {
            if (entity == null || entity.Team != RtsTeam.Player)
            {
                if (!additive)
                {
                    ClearSelection();
                }

                return;
            }

            if (!additive)
            {
                ClearSelection();
            }

            if (!selection.Contains(entity))
            {
                selection.Add(entity);
                entity.SetSelected(true);
            }
        }

        public void SelectCombatUnits()
        {
            ClearSelection();
            for (int i = 0; i < entities.Count; i++)
            {
                RtsUnit unit = entities[i] as RtsUnit;
                if (unit != null && unit.Team == RtsTeam.Player && unit.UnitKind != UnitKind.Harvester)
                {
                    selection.Add(unit);
                    unit.SetSelected(true);
                }
            }
        }

        public bool HasSelectedControllableUnits()
        {
            for (int i = 0; i < selection.Count; i++)
            {
                RtsUnit unit = selection[i] as RtsUnit;
                if (unit != null && unit.Team == RtsTeam.Player && unit.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        public void ToggleUserPause()
        {
            SetUserPaused(!IsUserPaused);
        }

        public void SetUserPaused(bool paused)
        {
            if (Lifecycle == null || IsMatchOver)
            {
                return;
            }

            Lifecycle.SetUserPaused(paused);
            if (paused)
            {
                StatusMessage = "Paused - resume when ready.";
                return;
            }

            EvaluateMatchState();
        }

        public bool TryManualSave()
        {
            return TrySaveSlot("manual");
        }

        public bool TryManualLoad()
        {
            return TryLoadSlot("manual");
        }

        public bool TryRestartMatch()
        {
            if (!initialized || Lifecycle == null || (SaveService != null && SaveService.IsBusy) || Lifecycle.IsSavingOrLoading)
            {
                return false;
            }

            Lifecycle.ResetForNewMatch();
            ClearDynamicWorld();
            nextEntityId = 1;
            nextResourceNodeId = 1;
            nextObjectiveCheckTime = 0.5f;
            MatchState = RtsMatchState.Running;
            StatusMessage = "Destroy the enemy base.";
            Clock.SetSimulationTime(0f);
            ApplySkirmishClockSettings();
            MatchTime = 0f;
            Resources = new ResourceBank(SkirmishOptions.PlayerStartingCredits);

            CreateResourceFields();
            SpawnStartingForces();
            RecalculatePower();
            FogOfWar?.ResetExploration();
            FogOfWar?.SetFogEnabled(SkirmishOptions.FogOfWarEnabled);
            EnemyDirector?.ResetForNewMatch();
            Lifecycle.SetMatchEnded(false);
            Physics.SyncTransforms();
            EvaluateMatchState();
            SpawnFloatingText("New match", GetPlayerBaseCenter() + Vector3.up * 3f, new Color(0.55f, 0.9f, 1f));
            return true;
        }

        public void SetSkirmishOptions(RtsSkirmishOptions options)
        {
            SkirmishOptions = options != null ? options.Clone() : RtsSkirmishOptions.CreateDefault();
            SkirmishOptions.Normalize();
            ApplySkirmishClockSettings();
            FogOfWar?.SetFogEnabled(SkirmishOptions.FogOfWarEnabled);
        }

        private void ApplySkirmishClockSettings()
        {
            if (Clock != null && SkirmishOptions != null)
            {
                Clock.SetTimeScale(SkirmishOptions.GameSpeedMultiplier);
            }
        }

        public void RequestQuit()
        {
            StatusMessage = "Quitting...";
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public bool CanLoadManualSave()
        {
            return SaveService != null && SaveService.HasSlot("manual");
        }

        public string GetManualSaveSummary()
        {
            if (SaveService == null || !SaveService.HasSlot("manual"))
            {
                return "empty";
            }

            if (!SaveService.TryGetSlotMetadata("manual", out RtsSaveMetadata metadata, out _))
            {
                return "metadata unreadable";
            }

            string source = metadata.readFromBackup ? "backup" : "primary";
            return source + " " + FormatSaveTime(metadata.matchTime) + " " + metadata.matchState;
        }

        public bool TrySaveSlot(string slotId)
        {
            if (SaveService == null)
            {
                StatusMessage = "Save system unavailable.";
                return false;
            }

            bool success = SaveService.TryWriteSlot(slotId, out string error);
            StatusMessage = success ? "Saved slot " + slotId + "." : "Save failed: " + error;
            SpawnFloatingText(success ? "Saved" : "Save failed", GetPlayerBaseCenter() + Vector3.up * 3f, success ? new Color(0.5f, 0.95f, 1f) : Color.yellow);
            return success;
        }

        public bool TryLoadSlot(string slotId)
        {
            if (SaveService == null)
            {
                StatusMessage = "Save system unavailable.";
                return false;
            }

            if (!SaveService.HasSlot(slotId))
            {
                StatusMessage = "No save slot " + slotId + ".";
                SpawnFloatingText("No save found", GetPlayerBaseCenter() + Vector3.up * 3f, Color.yellow);
                return false;
            }

            bool success = SaveService.TryLoadSlot(slotId, out string error);
            StatusMessage = success ? "Loaded slot " + slotId + "." : "Load failed: " + error;
            SpawnFloatingText(success ? "Loaded" : "Load failed", GetPlayerBaseCenter() + Vector3.up * 3f, success ? new Color(0.55f, 1f, 0.72f) : Color.yellow);
            return success;
        }

#if UNITY_EDITOR
        public void SetProfileSettingsForTests(RtsProfileSettings settings)
        {
            ProfileSettings = settings;
        }

        public void SetSaveServiceForTests(RtsSaveService service)
        {
            SaveService = service;
        }

        public void SetMatchTimeForTests(float time)
        {
            if (Clock != null)
            {
                Clock.SetSimulationTime(time);
            }

            MatchTime = Mathf.Max(0f, time);
        }
#endif

        public void SelectPlayerEntitiesInScreenRect(Rect screenRect, bool additive)
        {
            if (!additive)
            {
                ClearSelection();
            }

            if (CommandCamera == null)
            {
                return;
            }

            List<RtsEntity> matches = new List<RtsEntity>();
            CollectScreenRectMatches(screenRect, matches, true);

            if (matches.Count == 0)
            {
                CollectScreenRectMatches(screenRect, matches, false);
            }

            for (int i = 0; i < matches.Count; i++)
            {
                SelectEntity(matches[i], true);
            }
        }

        public int SelectPlayerUnitsInRadius(Vector3 center, float radius, bool additive)
        {
            if (!additive)
            {
                ClearSelection();
            }

            float safeRadius = Mathf.Max(0f, radius);
            int added = 0;
            for (int i = 0; i < entities.Count; i++)
            {
                RtsUnit unit = entities[i] as RtsUnit;
                if (unit == null || unit.Team != RtsTeam.Player || !unit.IsAlive)
                {
                    continue;
                }

                float selectionRadius = safeRadius + Mathf.Max(0.45f, unit.SelectionRadius);
                Vector3 offset = unit.GroundPosition - center;
                offset.y = 0f;
                if (offset.sqrMagnitude > selectionRadius * selectionRadius)
                {
                    continue;
                }

                bool wasSelected = unit.IsSelected;
                SelectEntity(unit, true);
                if (!wasSelected && unit.IsSelected)
                {
                    added++;
                }
            }

            return added;
        }

        public bool TryQueueUnit(UnitKind kind)
        {
            return PlayerCommands != null && PlayerCommands.QueueProduction(kind);
        }

        public bool IsWorldVisible(Vector3 point)
        {
            return FogOfWar == null || FogOfWar.IsVisible(point);
        }

        public bool IsEntityVisible(RtsEntity entity)
        {
            return entity == null || entity.Team != RtsTeam.Enemy || IsWorldVisible(entity.GroundPosition);
        }

        public bool CanBuildStructure(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.CommandCenter:
                    return false;
                case StructureKind.PowerPlant:
                case StructureKind.Refinery:
                    return HasPlayerStructure(StructureKind.CommandCenter);
                case StructureKind.Barracks:
                    return HasPlayerStructure(StructureKind.PowerPlant);
                case StructureKind.WarFactory:
                    return HasPlayerStructure(StructureKind.Barracks) && HasPlayerStructure(StructureKind.Refinery);
                case StructureKind.Turret:
                    return HasPlayerStructure(StructureKind.Barracks) && HasPlayerStructure(StructureKind.PowerPlant);
                case StructureKind.GunTower:
                    return HasPlayerStructure(StructureKind.WarFactory) && HasPlayerStructure(StructureKind.PowerPlant);
                case StructureKind.AdvancedGunTower:
                    return HasPlayerStructure(StructureKind.WarFactory) && HasPlayerStructure(StructureKind.PowerPlant) && HasPlayerStructure(StructureKind.Refinery);
                default:
                    return false;
            }
        }

        public string GetStructureRequirement(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.PowerPlant:
                case StructureKind.Refinery:
                    return HasPlayerStructure(StructureKind.CommandCenter) ? string.Empty : "Needs Command Center";
                case StructureKind.Barracks:
                    return HasPlayerStructure(StructureKind.PowerPlant) ? string.Empty : "Needs Power Plant";
                case StructureKind.WarFactory:
                    if (!HasPlayerStructure(StructureKind.Barracks))
                    {
                        return "Needs Barracks";
                    }

                    return HasPlayerStructure(StructureKind.Refinery) ? string.Empty : "Needs Refinery";
                case StructureKind.Turret:
                    if (!HasPlayerStructure(StructureKind.Barracks))
                    {
                        return "Needs Barracks";
                    }

                    return HasPlayerStructure(StructureKind.PowerPlant) ? string.Empty : "Needs Power Plant";
                case StructureKind.GunTower:
                    if (!HasPlayerStructure(StructureKind.WarFactory))
                    {
                        return "Needs War Factory";
                    }

                    return HasPlayerStructure(StructureKind.PowerPlant) ? string.Empty : "Needs Power Plant";
                case StructureKind.AdvancedGunTower:
                    if (!HasPlayerStructure(StructureKind.WarFactory))
                    {
                        return "Needs War Factory";
                    }

                    if (!HasPlayerStructure(StructureKind.Refinery))
                    {
                        return "Needs Refinery";
                    }

                    return HasPlayerStructure(StructureKind.PowerPlant) ? string.Empty : "Needs Power Plant";
                default:
                    return "Unavailable";
            }
        }

        public bool TryRepairSelectedStructures()
        {
            RtsStructure target = FindMostDamagedSelectedStructure();
            if (target == null)
            {
                SpawnFloatingText("Select damaged building", GetPlayerBaseCenter() + Vector3.up * 2.2f, Color.yellow);
                return false;
            }

            float missingHealth = target.MaxHealth - target.Health;
            int cost = Mathf.Clamp(Mathf.CeilToInt(missingHealth * 0.22f), 25, 180);
            if (!Resources.TrySpend(cost))
            {
                SpawnFloatingText("Need credits", target.transform.position + Vector3.up * 2.2f, Color.yellow);
                return false;
            }

            float repairAmount = cost / 0.22f;
            target.Repair(repairAmount);
            SpawnFloatingText("Repair -" + cost, target.transform.position + Vector3.up * 2.2f, new Color(0.7f, 1f, 0.75f));
            return true;
        }

        public bool CanRepairSelectedStructures()
        {
            return FindMostDamagedSelectedStructure() != null && Resources != null && Resources.Credits >= 25;
        }

        public bool SellSelectedStructures()
        {
            List<RtsStructure> soldStructures = new List<RtsStructure>();
            int refund = 0;

            for (int i = 0; i < selection.Count; i++)
            {
                RtsStructure structure = selection[i] as RtsStructure;
                if (structure == null || structure.Team != RtsTeam.Player || !structure.IsAlive)
                {
                    continue;
                }

                StructureStats stats = RtsBalance.GetStructure(structure.StructureKind);
                int structureRefund = Mathf.RoundToInt(stats.Cost * 0.5f * structure.HealthPercent);
                refund += Mathf.Max(0, structureRefund);
                soldStructures.Add(structure);
            }

            if (soldStructures.Count == 0)
            {
                SpawnFloatingText("Select building", GetPlayerBaseCenter() + Vector3.up * 2.2f, Color.yellow);
                return false;
            }

            ClearSelection();
            if (refund > 0)
            {
                Resources.Add(refund);
            }

            for (int i = 0; i < soldStructures.Count; i++)
            {
                RtsStructure structure = soldStructures[i];
                if (structure != null)
                {
                    SpawnFloatingText("Sold +" + Mathf.RoundToInt(RtsBalance.GetStructure(structure.StructureKind).Cost * 0.5f * structure.HealthPercent), structure.transform.position + Vector3.up * 2.2f, new Color(0.55f, 1f, 0.65f));
                    DestroyRuntimeObject(structure.gameObject);
                }
            }

            RecalculatePower();
            return true;
        }

        public bool CanSellSelectedStructures()
        {
            for (int i = 0; i < selection.Count; i++)
            {
                RtsStructure structure = selection[i] as RtsStructure;
                if (structure != null && structure.Team == RtsTeam.Player && structure.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        public Vector3 ClampWorldPoint(Vector3 position)
        {
            return ClampToGround(position);
        }

        public RtsUnit CreateUnit(RtsTeam team, UnitKind kind, Vector3 position)
        {
            kind = RtsBalance.NormalizeUnitKind(kind);
            UnitStats stats = RtsBalance.GetUnit(kind);
            GameObject root = new GameObject(team + " " + stats.Name);
            root.transform.SetParent(unitsRoot, true);
            root.transform.position = ClampToGround(position);
            root.transform.rotation = Quaternion.Euler(0f, team == RtsTeam.Enemy ? 210f : 35f, 0f);

            AddUnitCollider(root, kind);
            RtsUnit unit;
            if (kind == UnitKind.Harvester)
            {
                unit = root.AddComponent<HarvesterUnit>();
            }
            else if (kind == UnitKind.Engineer)
            {
                unit = root.AddComponent<EngineerUnit>();
            }
            else if (kind == UnitKind.MediumTank)
            {
                unit = root.AddComponent<MediumTankUnit>();
            }
            else
            {
                unit = root.AddComponent<RtsUnit>();
            }

            BuildUnitVisual(root.transform, kind, team);
            BuildUnitMotionRig(root.transform, kind);
            RtsUnitVisualAnimator visualAnimator = root.AddComponent<RtsUnitVisualAnimator>();
            visualAnimator.Initialize(unit, kind);
            unit.Initialize(team, kind);
            RegisterEntity(unit);
            return unit;
        }

        public RtsStructure CreateStructure(RtsTeam team, StructureKind kind, Vector3 position)
        {
            StructureStats stats = RtsBalance.GetStructure(kind);
            GameObject root = new GameObject(team + " " + stats.Name);
            root.transform.SetParent(structuresRoot, true);
            root.transform.position = ClampToGround(position);
            root.transform.rotation = GetStructureFacingRotation();

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(stats.FootprintRadius * 2f, 2.1f, stats.FootprintRadius * 2f);
            collider.center = new Vector3(0f, 1.05f, 0f);

            RtsStructure structure;
            if (kind == StructureKind.Refinery)
            {
                structure = root.AddComponent<RefineryStructure>();
            }
            else if (IsDefenseStructure(kind))
            {
                structure = root.AddComponent<TurretStructure>();
            }
            else if (kind == StructureKind.CommandCenter || kind == StructureKind.Barracks || kind == StructureKind.WarFactory)
            {
                structure = root.AddComponent<ProductionStructure>();
            }
            else
            {
                structure = root.AddComponent<RtsStructure>();
            }

            Transform turretHead = BuildStructureVisual(root.transform, kind, team);
            TurretStructure turret = structure as TurretStructure;
            if (turret != null)
            {
                turret.SetHead(turretHead);
            }

            structure.Initialize(team, kind);
            RegisterEntity(structure);
            RecalculatePower();
            return structure;
        }

        public GameObject CreateStructurePreview(StructureKind kind)
        {
            StructureStats stats = RtsBalance.GetStructure(kind);
            GameObject root = new GameObject("Preview " + stats.Name);
            root.transform.localScale = Vector3.one;
            root.transform.rotation = GetStructureFacingRotation();
            BuildStructureVisual(root.transform, kind, RtsTeam.Player);

            foreach (Collider collider in root.GetComponentsInChildren<Collider>())
            {
                Destroy(collider);
            }

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = CreateTransparentMaterial(new Color(0.3f, 0.9f, 1f, 0.45f));
            }

            return root;
        }

        public void SetPreviewValid(GameObject preview, bool valid)
        {
            if (preview == null)
            {
                return;
            }

            Color color = valid ? new Color(0.25f, 1f, 0.45f, 0.45f) : new Color(1f, 0.2f, 0.2f, 0.45f);
            foreach (Renderer renderer in preview.GetComponentsInChildren<Renderer>())
            {
                if (renderer.sharedMaterial != null)
                {
                    renderer.sharedMaterial.color = color;
                    renderer.sharedMaterial.SetColor("_Color", color);
                    renderer.sharedMaterial.SetColor("_BaseColor", color);
                }
            }
        }

        public void RecalculatePower()
        {
            if (Resources == null)
            {
                return;
            }

            int provided = 0;
            int used = 0;

            for (int i = 0; i < entities.Count; i++)
            {
                RtsStructure structure = entities[i] as RtsStructure;
                if (structure == null || structure.Team != RtsTeam.Player || !structure.IsAlive)
                {
                    continue;
                }

                provided += structure.PowerProvided;
                used += structure.PowerUsed;
            }

            Resources.SetPower(provided, used);
        }

        public RefineryStructure FindNearestPlayerRefinery(Vector3 point)
        {
            return FindNearestRefinery(RtsTeam.Player, point);
        }

        public RefineryStructure FindNearestRefinery(RtsTeam team, Vector3 point)
        {
            RefineryStructure best = null;
            float bestDistance = float.PositiveInfinity;

            for (int i = 0; i < entities.Count; i++)
            {
                RefineryStructure refinery = entities[i] as RefineryStructure;
                if (refinery == null || refinery.Team != team || !refinery.IsAlive)
                {
                    continue;
                }

                float distance = PlanarDistance(point, refinery.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = refinery;
                }
            }

            return best;
        }

        public ResourceNode FindNearestResource(Vector3 point)
        {
            ResourceNode best = null;
            float bestDistance = float.PositiveInfinity;

            for (int i = 0; i < resourceNodes.Count; i++)
            {
                ResourceNode node = resourceNodes[i];
                if (node == null || node.IsDepleted)
                {
                    continue;
                }

                float distance = PlanarDistance(point, node.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = node;
                }
            }

            return best;
        }

        public RtsEntity FindClosestEnemy(RtsTeam team, Vector3 point, float maxRange)
        {
            RtsEntity best = null;
            float maxRangeSqr = maxRange * maxRange;
            float bestDistance = maxRangeSqr;

            for (int i = 0; i < entities.Count; i++)
            {
                RtsEntity entity = entities[i];
                if (entity == null || !entity.IsAlive || entity.Team == team || entity.Team == RtsTeam.Neutral)
                {
                    continue;
                }

                Vector3 delta = entity.transform.position - point;
                delta.y = 0f;
                float sqrDistance = delta.sqrMagnitude;
                if (sqrDistance <= bestDistance)
                {
                    bestDistance = sqrDistance;
                    best = entity;
                }
            }

            return best;
        }

        public int DamageEnemiesInRadius(RtsTeam attackerTeam, Vector3 center, float radius, float damage, RtsEntity attacker, RtsEntity excluded)
        {
            if (damage <= 0f || radius <= 0f)
            {
                return 0;
            }

            int damaged = 0;
            float radiusSqr = radius * radius;
            for (int i = 0; i < entities.Count; i++)
            {
                RtsEntity entity = entities[i];
                if (entity == null || entity == excluded || !entity.IsAlive || entity.Team == attackerTeam || entity.Team == RtsTeam.Neutral)
                {
                    continue;
                }

                Vector3 delta = entity.GroundPosition - center;
                delta.y = 0f;
                if (delta.sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                entity.TakeDamage(damage, attacker);
                damaged++;
            }

            return damaged;
        }

        public RtsEntity FindPlayerPrimaryTarget()
        {
            return FindPrimaryTarget(RtsTeam.Player);
        }

        public RtsEntity FindPrimaryTarget(RtsTeam team)
        {
            RtsEntity commandCenter = null;

            for (int i = 0; i < entities.Count; i++)
            {
                RtsStructure structure = entities[i] as RtsStructure;
                if (structure == null || structure.Team != team || !structure.IsAlive)
                {
                    continue;
                }

                if (structure.StructureKind == StructureKind.CommandCenter)
                {
                    return structure;
                }

                commandCenter = structure;
            }

            return commandCenter;
        }

        public int CountLivingStructures(RtsTeam team)
        {
            int count = 0;
            for (int i = 0; i < entities.Count; i++)
            {
                RtsStructure structure = entities[i] as RtsStructure;
                if (structure != null && structure.Team == team && structure.IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        public bool HasPlayerStructure(StructureKind kind)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                RtsStructure structure = entities[i] as RtsStructure;
                if (structure != null && structure.Team == RtsTeam.Player && structure.StructureKind == kind && structure.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        public Vector3 GetPlayerBaseCenter()
        {
            RtsEntity target = FindPlayerPrimaryTarget();
            return target != null ? target.transform.position : new Vector3(-78f, 0f, -70f);
        }

        public Vector3 GetEnemyBaseCenter()
        {
            RtsEntity target = FindPrimaryTarget(RtsTeam.Enemy);
            return target != null ? target.transform.position : new Vector3(82f, 0f, 72f);
        }

        public void SpawnTracer(Vector3 from, Vector3 to, RtsTeam team)
        {
            GameObject tracer = new GameObject("Weapon Tracer");
            tracer.transform.SetParent(effectsRoot, true);
            LineRenderer line = tracer.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.widthMultiplier = 0.06f;
            line.material = CreateMaterial(team == RtsTeam.Enemy ? new Color(1f, 0.38f, 0.22f) : new Color(0.45f, 0.8f, 1f));
            RtsTimedDestroy timedDestroy = tracer.AddComponent<RtsTimedDestroy>();
            timedDestroy.Lifetime = 0.09f;
        }

        public RtsProjectile SpawnProjectile(
            RtsProjectileKind kind,
            RtsTeam team,
            RtsEntity attacker,
            RtsEntity target,
            Vector3 from,
            float damage,
            float splashRadius,
            float splashDamage)
        {
            if (target == null || !target.IsAlive)
            {
                return null;
            }

            Vector3 impactPoint = target.GroundPosition + Vector3.up * GetProjectileTargetHeight(target);
            GameObject projectileObject = GameObject.CreatePrimitive(GetProjectilePrimitive(kind));
            projectileObject.name = kind + " Projectile";
            projectileObject.transform.SetParent(effectsRoot, true);
            projectileObject.transform.position = from;
            projectileObject.transform.localScale = GetProjectileScale(kind);

            Collider collider = projectileObject.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyRuntimeObject(collider);
            }

            Renderer renderer = projectileObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateMaterial(GetProjectileColor(kind, team));
            }

            RtsProjectile projectile = projectileObject.AddComponent<RtsProjectile>();
            projectile.Initialize(
                team,
                attacker,
                target,
                from,
                impactPoint,
                damage,
                GetProjectileSpeed(kind),
                splashRadius,
                splashDamage,
                GetProjectileArcHeight(kind));

            return projectile;
        }

        public void SpawnImpactPulse(Vector3 position, RtsTeam team, float radius)
        {
            GameObject pulse = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pulse.name = "Projectile Impact Pulse";
            pulse.transform.SetParent(effectsRoot, true);
            pulse.transform.position = position + Vector3.up * 0.035f;
            float safeRadius = Mathf.Max(0.35f, radius);
            pulse.transform.localScale = new Vector3(safeRadius, 0.025f, safeRadius);

            Collider collider = pulse.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyRuntimeObject(collider);
            }

            Renderer renderer = pulse.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = team == RtsTeam.Enemy ? new Color(1f, 0.28f, 0.16f, 0.62f) : new Color(0.42f, 0.9f, 1f, 0.62f);
                renderer.sharedMaterial = CreateTransparentMaterial(color);
            }

            RtsTimedDestroy timedDestroy = pulse.AddComponent<RtsTimedDestroy>();
            timedDestroy.Lifetime = 0.28f;
        }

        private static PrimitiveType GetProjectilePrimitive(RtsProjectileKind kind)
        {
            switch (kind)
            {
                case RtsProjectileKind.RifleRound:
                case RtsProjectileKind.Rocket:
                case RtsProjectileKind.FlameBolt:
                    return PrimitiveType.Capsule;
                default:
                    return PrimitiveType.Sphere;
            }
        }

        private static Vector3 GetProjectileScale(RtsProjectileKind kind)
        {
            switch (kind)
            {
                case RtsProjectileKind.RifleRound:
                    return new Vector3(0.08f, 0.2f, 0.08f);
                case RtsProjectileKind.Grenade:
                    return new Vector3(0.2f, 0.2f, 0.2f);
                case RtsProjectileKind.Rocket:
                    return new Vector3(0.13f, 0.34f, 0.13f);
                case RtsProjectileKind.FlameBolt:
                    return new Vector3(0.18f, 0.28f, 0.18f);
                case RtsProjectileKind.DefenseShell:
                    return new Vector3(0.2f, 0.2f, 0.2f);
                default:
                    return new Vector3(0.18f, 0.18f, 0.18f);
            }
        }

        private static float GetProjectileSpeed(RtsProjectileKind kind)
        {
            switch (kind)
            {
                case RtsProjectileKind.RifleRound:
                    return 34f;
                case RtsProjectileKind.Grenade:
                    return 13.5f;
                case RtsProjectileKind.Rocket:
                    return 22f;
                case RtsProjectileKind.FlameBolt:
                    return 15f;
                case RtsProjectileKind.DefenseShell:
                    return 26f;
                default:
                    return 24f;
            }
        }

        private static float GetProjectileArcHeight(RtsProjectileKind kind)
        {
            return kind == RtsProjectileKind.Grenade ? 2.6f : 0f;
        }

        private static Color GetProjectileColor(RtsProjectileKind kind, RtsTeam team)
        {
            switch (kind)
            {
                case RtsProjectileKind.Grenade:
                    return new Color(0.25f, 0.3f, 0.2f);
                case RtsProjectileKind.Rocket:
                    return new Color(1f, 0.32f, 0.18f);
                case RtsProjectileKind.FlameBolt:
                    return new Color(1f, 0.48f, 0.12f);
                case RtsProjectileKind.TankShell:
                case RtsProjectileKind.DefenseShell:
                    return team == RtsTeam.Enemy ? new Color(1f, 0.32f, 0.18f) : new Color(0.45f, 0.88f, 1f);
                default:
                    return team == RtsTeam.Enemy ? new Color(1f, 0.48f, 0.32f) : new Color(0.62f, 0.9f, 1f);
            }
        }

        private static float GetProjectileTargetHeight(RtsEntity entity)
        {
            RtsUnit unit = entity as RtsUnit;
            if (unit != null)
            {
                return RtsBalance.IsVehicle(unit.UnitKind) ? 0.85f : 0.8f;
            }

            RtsStructure structure = entity as RtsStructure;
            if (structure != null)
            {
                return Mathf.Clamp(structure.FootprintRadius * 0.45f, 0.8f, 2.4f);
            }

            return 0.8f;
        }

        public void SpawnFloatingText(string message, Vector3 position, Color color)
        {
            GameObject textObject = new GameObject("Floating Text");
            textObject.transform.SetParent(effectsRoot, true);
            textObject.transform.position = position;
            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = message;
            textMesh.fontSize = 42;
            textMesh.characterSize = 0.08f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;
            textObject.AddComponent<FloatingText>();
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            if (Instance == null)
            {
                Instance = this;
            }

            initialized = true;
            Clock = new RtsSimulationClock();
            SkirmishOptions.Normalize();
            ApplySkirmishClockSettings();
            if (ProfileSettings == null)
            {
                ProfileSettings = RtsProfileSettings.CreateDefault();
                ProfileSettings.TryLoad(out string profileError);
                if (!string.IsNullOrEmpty(profileError))
                {
                    Debug.LogWarning("Profile settings load failed: " + profileError);
                }
            }
            else
            {
                ProfileSettings.Data.Normalize();
            }

            SaveService = new RtsSaveService(this, RtsSaveFileStore.CreateDefault());
            Lifecycle = gameObject.AddComponent<RtsLifecycleCoordinator>();
            Lifecycle.Initialize(this);
            RuntimeMode = RtsRuntimeModeResolver.Resolve();
            PlayerCommands = new RtsPlayerCommandService();
            PlayerCommands.Initialize(this);
            CommandDispatcher = new RtsCommandDispatcher();
            CommandDispatcher.Initialize(this);
            Resources = new ResourceBank(SkirmishOptions.PlayerStartingCredits);
            CreateMaterials();
            CreateRoots();
            if (RuntimeMode == RtsRuntimeMode.Desktop)
            {
                SetupDesktopCamera();
            }

            SetupLight();
            CreateGround();
            CreateResourceFields();
            SpawnStartingForces();

            BuildManager = gameObject.AddComponent<BuildManager>();
            BuildManager.Initialize(this);
            FogOfWar = gameObject.AddComponent<RtsFogOfWar>();
            FogOfWar.Initialize(this);

            if (RuntimeMode == RtsRuntimeMode.QuestVr)
            {
                QuestRig = gameObject.AddComponent<QuestTabletopRig>();
                QuestRig.Initialize(this, CommandDispatcher);
            }
            else
            {
                gameObject.AddComponent<RtsInputController>().Initialize(this, CommandDispatcher);
                gameObject.AddComponent<RtsHud>().Initialize(this);
            }

            EnemyDirector = gameObject.AddComponent<EnemyDirector>();
            EnemyDirector.Initialize(this);

            RecalculatePower();
            EvaluateMatchState();
        }

        public RtsMatchSaveData CaptureSaveData()
        {
            using (RtsProfilerMarkers.SaveCapture.Auto())
            {
                RtsMatchSaveData data = new RtsMatchSaveData
                {
                    matchTime = MatchTime,
                    matchState = MatchState.ToString(),
                    statusMessage = StatusMessage,
                    skirmishConfigId = SkirmishOptions != null ? SkirmishOptions.ConfigId : "default_skirmish_v1",
                    difficultyId = SkirmishOptions != null ? SkirmishOptions.DifficultyId : "standard",
                    nextEntityId = nextEntityId,
                    nextResourceNodeId = nextResourceNodeId,
                    skirmishOptions = SkirmishOptions != null ? SkirmishOptions.Clone() : RtsSkirmishOptions.CreateDefault(),
                    resources = new RtsResourceBankSaveData
                    {
                        credits = Resources != null ? Resources.Credits : 0,
                        powerProvided = Resources != null ? Resources.PowerProvided : 0,
                        powerUsed = Resources != null ? Resources.PowerUsed : 0
                    },
                    fog = FogOfWar != null ? FogOfWar.CaptureState() : new RtsFogSaveData(),
                    buildPlacement = BuildManager != null ? BuildManager.CapturePlacementState() : new RtsBuildPlacementSaveData(),
                    enemyDirector = EnemyDirector != null ? EnemyDirector.CaptureState() : new RtsEnemyDirectorSaveData()
                };

                for (int i = 0; i < resourceNodes.Count; i++)
                {
                    ResourceNode node = resourceNodes[i];
                    if (node == null)
                    {
                        continue;
                    }

                    data.resourceNodes.Add(new RtsResourceNodeSaveData
                    {
                        id = node.PersistentId,
                        position = new Vector3Data(node.transform.position),
                        amount = node.Amount,
                        maxAmount = node.MaxAmount
                    });
                }

                for (int i = 0; i < entities.Count; i++)
                {
                    RtsEntity entity = entities[i];
                    if (entity == null)
                    {
                        continue;
                    }

                    data.entities.Add(CaptureEntity(entity));
                }

                return data;
            }
        }

        public bool RestoreSaveData(RtsMatchSaveData data, out string error)
        {
            error = string.Empty;
            if (data == null)
            {
                error = "Save data is empty.";
                return false;
            }

            using (RtsProfilerMarkers.SaveRestore.Auto())
            {
                ClearDynamicWorld();

                nextEntityId = 1;
                nextResourceNodeId = 1;
                SetSkirmishOptions(data.skirmishOptions);
                Resources.SetCreditsForRestore(data.resources != null ? data.resources.credits : 0);
                Clock.SetSimulationTime(data.matchTime);
                ApplySkirmishClockSettings();
                MatchTime = Clock.SimulationTime;

                Dictionary<int, ResourceNode> resourceById = new Dictionary<int, ResourceNode>();
                if (data.resourceNodes != null)
                {
                    for (int i = 0; i < data.resourceNodes.Count; i++)
                    {
                        RtsResourceNodeSaveData nodeData = data.resourceNodes[i];
                        ResourceNode node = CreateResourceNode(nodeData.position.ToVector3(), nodeData.maxAmount, nodeData.amount, nodeData.id);
                        resourceById[node.PersistentId] = node;
                    }
                }

                CreateResourceFieldMinersForExistingNodes();

                Dictionary<int, RtsEntity> entityById = new Dictionary<int, RtsEntity>();
                List<RtsEntitySaveData> unitSaves = new List<RtsEntitySaveData>();

                if (data.entities != null)
                {
                    for (int i = 0; i < data.entities.Count; i++)
                    {
                        RtsEntitySaveData entityData = data.entities[i];
                        if (entityData == null || entityData.entityType != "Structure")
                        {
                            continue;
                        }

                        if (!Enum.TryParse(entityData.team, out RtsTeam team) || !Enum.TryParse(entityData.structureKind, out StructureKind kind))
                        {
                            continue;
                        }

                        RtsStructure structure = CreateStructure(team, kind, entityData.position.ToVector3());
                        ApplyEntitySaveData(structure, entityData);
                        ProductionStructure production = structure as ProductionStructure;
                        if (production != null)
                        {
                            production.RestoreProductionState(entityData.production);
                        }

                        entityById[structure.PersistentId] = structure;
                    }

                    for (int i = 0; i < data.entities.Count; i++)
                    {
                        RtsEntitySaveData entityData = data.entities[i];
                        if (entityData == null || entityData.entityType != "Unit")
                        {
                            continue;
                        }

                        if (!Enum.TryParse(entityData.team, out RtsTeam team) || !Enum.TryParse(entityData.unitKind, out UnitKind kind))
                        {
                            continue;
                        }

                        RtsUnit unit = CreateUnit(team, kind, entityData.position.ToVector3());
                        ApplyEntitySaveData(unit, entityData);
                        entityById[unit.PersistentId] = unit;
                        unitSaves.Add(entityData);
                    }
                }

                for (int i = 0; i < unitSaves.Count; i++)
                {
                    RtsEntitySaveData entityData = unitSaves[i];
                    if (!entityById.TryGetValue(entityData.id, out RtsEntity entity))
                    {
                        continue;
                    }

                    RtsUnit unit = entity as RtsUnit;
                    if (unit == null)
                    {
                        continue;
                    }

                    unit.RestoreOrderState(entityData.order, entityById);
                    HarvesterUnit harvester = unit as HarvesterUnit;
                    if (harvester != null)
                    {
                        harvester.RestoreHarvesterState(entityData.harvester, resourceById, entityById);
                    }

                    MediumTankUnit mediumTank = unit as MediumTankUnit;
                    if (mediumTank != null)
                    {
                        UnitKind passengerKind = UnitKind.Rifleman;
                        if (!string.IsNullOrEmpty(entityData.carriedInfantryKind))
                        {
                            Enum.TryParse(entityData.carriedInfantryKind, out passengerKind);
                        }

                        mediumTank.RestorePassenger(passengerKind, entityData.carriedRiflemen);
                    }
                }

                nextEntityId = Mathf.Max(nextEntityId, data.nextEntityId);
                nextResourceNodeId = Mathf.Max(nextResourceNodeId, data.nextResourceNodeId);
                RecalculatePower();

                if (!Enum.TryParse(data.matchState, out RtsMatchState restoredState))
                {
                    restoredState = RtsMatchState.Running;
                }

                MatchState = restoredState;
                StatusMessage = string.IsNullOrEmpty(data.statusMessage) ? "Destroy the enemy base." : data.statusMessage;
                nextObjectiveCheckTime = Clock.SimulationTime + 0.5f;

                if (FogOfWar != null)
                {
                    FogOfWar.RestoreState(data.fog);
                    FogOfWar.SetFogEnabled(SkirmishOptions.FogOfWarEnabled);
                }

                if (BuildManager != null)
                {
                    BuildManager.RestorePlacementState(data.buildPlacement);
                }

                if (EnemyDirector != null)
                {
                    EnemyDirector.RestoreState(data.enemyDirector);
                }

                Lifecycle?.SetMatchEnded(IsMatchOver);
                Physics.SyncTransforms();
                return true;
            }
        }

        private RtsEntitySaveData CaptureEntity(RtsEntity entity)
        {
            RtsEntitySaveData data = new RtsEntitySaveData
            {
                id = entity.PersistentId,
                team = entity.Team.ToString(),
                position = new Vector3Data(entity.transform.position),
                rotationY = entity.transform.eulerAngles.y,
                health = entity.Health,
                maxHealth = entity.MaxHealth
            };

            RtsUnit unit = entity as RtsUnit;
            if (unit != null)
            {
                data.entityType = "Unit";
                data.unitKind = unit.UnitKind.ToString();
                data.order = unit.CaptureOrderState();

                HarvesterUnit harvester = unit as HarvesterUnit;
                if (harvester != null)
                {
                    data.harvester = harvester.CaptureHarvesterState();
                }

                MediumTankUnit mediumTank = unit as MediumTankUnit;
                if (mediumTank != null)
                {
                    data.carriedRiflemen = mediumTank.LoadedRiflemen;
                    data.carriedInfantryKind = mediumTank.LoadedPassengerKind.ToString();
                }

                return data;
            }

            RtsStructure structure = entity as RtsStructure;
            if (structure != null)
            {
                data.entityType = "Structure";
                data.structureKind = structure.StructureKind.ToString();

                ProductionStructure production = structure as ProductionStructure;
                if (production != null)
                {
                    data.production = production.CaptureProductionState();
                }

                return data;
            }

            data.entityType = "Entity";
            return data;
        }

        private void ApplyEntitySaveData(RtsEntity entity, RtsEntitySaveData data)
        {
            entity.AssignPersistentId(data.id > 0 ? data.id : AllocateEntityId());
            nextEntityId = Mathf.Max(nextEntityId, entity.PersistentId + 1);
            entity.transform.position = ClampToGround(data.position.ToVector3());
            entity.transform.rotation = Quaternion.Euler(0f, data.rotationY, 0f);
            if (entity is RtsStructure)
            {
                entity.transform.rotation = GetStructureFacingRotation();
            }

            entity.SetHealthForRestore(data.health);
        }

        private void ClearDynamicWorld()
        {
            ClearSelection();
            if (BuildManager != null)
            {
                BuildManager.CancelPlacement();
            }

            DestroyChildren(unitsRoot);
            DestroyChildren(structuresRoot);
            DestroyChildren(resourcesRoot);
            DestroyChildren(effectsRoot);
            entities.Clear();
            selection.Clear();
            resourceNodes.Clear();
        }

        private static void DestroyChildren(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                DestroyRuntimeObject(root.GetChild(i).gameObject);
            }
        }

        private static void DestroyRuntimeObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private void CollectScreenRectMatches(Rect screenRect, List<RtsEntity> matches, bool unitsOnly)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                RtsEntity entity = entities[i];
                if (entity == null || entity.Team != RtsTeam.Player || !entity.IsAlive)
                {
                    continue;
                }

                if (unitsOnly && !(entity is RtsUnit))
                {
                    continue;
                }

                if (!unitsOnly && !(entity is RtsStructure))
                {
                    continue;
                }

                Vector3 screenPoint = CommandCamera.WorldToScreenPoint(entity.GroundPosition + Vector3.up * 0.8f);
                if (screenPoint.z < 0f)
                {
                    continue;
                }

                screenPoint.y = Screen.height - screenPoint.y;
                if (screenRect.Contains(new Vector2(screenPoint.x, screenPoint.y)))
                {
                    matches.Add(entity);
                }
            }
        }

        private void EvaluateMatchState()
        {
            int playerStructures = CountLivingStructures(RtsTeam.Player);
            int enemyStructures = CountLivingStructures(RtsTeam.Enemy);

            if (playerStructures <= 0)
            {
                EndMatch(RtsMatchState.Defeat, "Defeat - your base has fallen.");
                return;
            }

            if (enemyStructures <= 0)
            {
                EndMatch(RtsMatchState.Victory, "Victory - enemy base destroyed.");
                return;
            }

            StatusMessage = "Destroy enemy base - enemy structures " + enemyStructures;
        }

        private RtsStructure FindMostDamagedSelectedStructure()
        {
            RtsStructure best = null;
            float bestMissingHealth = 0f;

            for (int i = 0; i < selection.Count; i++)
            {
                RtsStructure structure = selection[i] as RtsStructure;
                if (structure == null || structure.Team != RtsTeam.Player || !structure.IsAlive)
                {
                    continue;
                }

                float missingHealth = structure.MaxHealth - structure.Health;
                if (missingHealth > bestMissingHealth + 0.5f)
                {
                    bestMissingHealth = missingHealth;
                    best = structure;
                }
            }

            return best;
        }

        private void EndMatch(RtsMatchState state, string message)
        {
            if (MatchState != RtsMatchState.Running)
            {
                return;
            }

            MatchState = state;
            StatusMessage = message;
            Lifecycle?.SetMatchEnded(true);
            if (BuildManager != null)
            {
                BuildManager.CancelPlacement();
            }

            SpawnFloatingText(message, GetPlayerBaseCenter() + Vector3.up * 4f, state == RtsMatchState.Victory ? Color.cyan : Color.red);
        }

#if UNITY_EDITOR
        public void ForceEndMatchForTests(RtsMatchState state)
        {
            EndMatch(state, state == RtsMatchState.Victory ? "Victory" : "Defeat");
        }
#endif

        private void CreateMaterials()
        {
            playerMaterial = CreateTeamMaterial(RtsBalance.TeamColor(RtsTeam.Player));
            enemyMaterial = CreateTeamMaterial(RtsBalance.TeamColor(RtsTeam.Enemy));
            neutralMaterial = CreateMaterial(new Color(0.62f, 0.6f, 0.5f));

            Texture2D sandGroundTexture = CreateTerrainTexture(
                "Command RTS Sand Ground Texture",
                new Color(0.62f, 0.5f, 0.32f),
                new Color(0.42f, 0.33f, 0.21f),
                new Color(0.86f, 0.72f, 0.47f),
                new Color(0.24f, 0.18f, 0.12f),
                17,
                0.16f,
                0f,
                0f);
            Texture2D sandGroundNormal = CreateTerrainNormalTexture(
                "Command RTS Sand Ground Normal",
                17,
                2.4f,
                0f,
                0f);
            Texture2D duneAccentTexture = CreateTerrainTexture(
                "Command RTS Dune Accent Texture",
                new Color(0.48f, 0.38f, 0.25f),
                new Color(0.32f, 0.24f, 0.16f),
                new Color(0.72f, 0.57f, 0.36f),
                new Color(0.25f, 0.18f, 0.11f),
                31,
                0.18f,
                0.34f,
                0.28f);
            Texture2D duneAccentNormal = CreateTerrainNormalTexture(
                "Command RTS Dune Accent Normal",
                31,
                2.15f,
                0.3f,
                0.34f);
            Texture2D waterRippleTexture = CreateTerrainTexture(
                "Command RTS Water Ripple Texture",
                new Color(0.08f, 0.42f, 0.54f, 0.72f),
                new Color(0.035f, 0.2f, 0.32f, 0.72f),
                new Color(0.18f, 0.62f, 0.72f, 0.72f),
                new Color(0.6f, 0.95f, 1f, 0.72f),
                47,
                0.06f,
                0f,
                0.48f);
            Texture2D waterRippleNormal = CreateTerrainNormalTexture(
                "Command RTS Water Ripple Normal",
                47,
                1.55f,
                0f,
                0.55f);
            Texture2D ridgeRockTexture = CreateTerrainTexture(
                "Command RTS Ridge Rock Texture",
                new Color(0.5f, 0.41f, 0.3f),
                new Color(0.3f, 0.25f, 0.19f),
                new Color(0.72f, 0.62f, 0.47f),
                new Color(0.16f, 0.13f, 0.11f),
                59,
                0.22f,
                0.52f,
                0.18f);
            Texture2D ridgeRockNormal = CreateTerrainNormalTexture(
                "Command RTS Ridge Rock Normal",
                59,
                3.1f,
                0.54f,
                0.22f);
            Texture2D cliffFaceTexture = CreateTerrainTexture(
                "Command RTS Cliff Face Texture",
                new Color(0.56f, 0.45f, 0.33f),
                new Color(0.28f, 0.22f, 0.17f),
                new Color(0.78f, 0.64f, 0.48f),
                new Color(0.15f, 0.12f, 0.09f),
                61,
                0.26f,
                0.88f,
                0.42f);
            Texture2D cliffFaceNormal = CreateTerrainNormalTexture(
                "Command RTS Cliff Face Normal",
                61,
                3.8f,
                0.9f,
                0.48f);
            Texture2D mountainStoneTexture = CreateTerrainTexture(
                "Command RTS Mountain Stone Texture",
                new Color(0.44f, 0.41f, 0.35f),
                new Color(0.23f, 0.22f, 0.19f),
                new Color(0.72f, 0.67f, 0.55f),
                new Color(0.12f, 0.11f, 0.095f),
                67,
                0.24f,
                0.66f,
                0.28f);
            Texture2D mountainStoneNormal = CreateTerrainNormalTexture(
                "Command RTS Mountain Stone Normal",
                67,
                3.35f,
                0.64f,
                0.32f);
            Texture2D talusTexture = CreateTerrainTexture(
                "Command RTS Talus Texture",
                new Color(0.48f, 0.39f, 0.27f),
                new Color(0.28f, 0.22f, 0.15f),
                new Color(0.74f, 0.62f, 0.43f),
                new Color(0.17f, 0.13f, 0.09f),
                69,
                0.32f,
                0.38f,
                0.12f);
            Texture2D talusNormal = CreateTerrainNormalTexture(
                "Command RTS Talus Normal",
                69,
                2.8f,
                0.42f,
                0.16f);
            Texture2D dryWashTexture = CreateTerrainTexture(
                "Command RTS Dry Wash Texture",
                new Color(0.61f, 0.51f, 0.35f),
                new Color(0.33f, 0.27f, 0.18f),
                new Color(0.82f, 0.7f, 0.47f),
                new Color(0.18f, 0.14f, 0.09f),
                71,
                0.2f,
                0.5f,
                0.34f);
            Texture2D dryWashNormal = CreateTerrainNormalTexture(
                "Command RTS Dry Wash Normal",
                71,
                2.0f,
                0.46f,
                0.42f);
            Texture2D vehicleArmorTexture = CreatePanelTexture(
                "Command RTS Vehicle Armor Panel Texture",
                new Color(0.43f, 0.48f, 0.4f),
                new Color(0.22f, 0.27f, 0.24f),
                new Color(0.68f, 0.72f, 0.6f),
                new Color(0.08f, 0.1f, 0.09f),
                new Color(0.8f, 0.84f, 0.68f),
                97,
                5,
                4,
                0.38f,
                0.46f,
                0.55f);
            Texture2D vehicleArmorNormal = CreatePanelNormalTexture(
                "Command RTS Vehicle Armor Panel Normal",
                97,
                5,
                4,
                0.18f,
                0.12f,
                0.05f);
            Texture2D structurePanelTexture = CreatePanelTexture(
                "Command RTS Structure Panel Texture",
                new Color(0.4f, 0.45f, 0.37f),
                new Color(0.2f, 0.25f, 0.21f),
                new Color(0.64f, 0.68f, 0.55f),
                new Color(0.07f, 0.085f, 0.075f),
                new Color(0.75f, 0.8f, 0.62f),
                101,
                6,
                5,
                0.42f,
                0.36f,
                0.5f);
            Texture2D structurePanelNormal = CreatePanelNormalTexture(
                "Command RTS Structure Panel Normal",
                101,
                6,
                5,
                0.16f,
                0.11f,
                0.04f);
            Texture2D rubberTreadTexture = CreatePanelTexture(
                "Command RTS Rubber Tread Texture",
                new Color(0.075f, 0.082f, 0.075f),
                new Color(0.025f, 0.03f, 0.028f),
                new Color(0.19f, 0.2f, 0.17f),
                new Color(0.01f, 0.012f, 0.011f),
                new Color(0.24f, 0.25f, 0.21f),
                103,
                3,
                9,
                0.3f,
                0.24f,
                0.18f);
            Texture2D rubberTreadNormal = CreatePanelNormalTexture(
                "Command RTS Rubber Tread Normal",
                103,
                3,
                9,
                0.24f,
                0.04f,
                0.03f);
            Texture2D gunmetalTexture = CreatePanelTexture(
                "Command RTS Gunmetal Barrel Texture",
                new Color(0.17f, 0.19f, 0.18f),
                new Color(0.06f, 0.075f, 0.07f),
                new Color(0.36f, 0.39f, 0.34f),
                new Color(0.02f, 0.025f, 0.023f),
                new Color(0.52f, 0.55f, 0.48f),
                107,
                4,
                8,
                0.25f,
                0.58f,
                0.22f);
            Texture2D gunmetalNormal = CreatePanelNormalTexture(
                "Command RTS Gunmetal Barrel Normal",
                107,
                4,
                8,
                0.12f,
                0.05f,
                0.08f);
            Texture2D scorchTexture = CreateTerrainTexture(
                "Command RTS Scorch Texture",
                new Color(0.15f, 0.12f, 0.08f, 0.34f),
                new Color(0.06f, 0.048f, 0.038f, 0.34f),
                new Color(0.3f, 0.22f, 0.13f, 0.34f),
                new Color(0.035f, 0.028f, 0.024f, 0.34f),
                73,
                0.2f,
                0.85f,
                0.12f);

            groundMaterial = CreateTexturedPbrMaterial(
                new Color(0.62f, 0.5f, 0.32f),
                sandGroundTexture,
                new Vector2(1.15f, 1.15f),
                0.01f,
                0.32f,
                sandGroundNormal,
                0.68f);
            terrainAccentMaterial = CreateTexturedPbrMaterial(
                new Color(0.48f, 0.38f, 0.25f),
                duneAccentTexture,
                new Vector2(4f, 4f),
                0.01f,
                0.26f,
                duneAccentNormal,
                0.58f);
            waterMaterial = CreateTexturedTransparentMaterial(
                new Color(0.08f, 0.42f, 0.54f, 0.72f),
                waterRippleTexture,
                new Vector2(3f, 2f));
            ConfigurePbr(waterMaterial, 0f, 0.82f);
            ApplyNormalMap(waterMaterial, waterRippleNormal, 0.34f, new Vector2(3f, 2f));
            ridgeMaterial = CreateTexturedPbrMaterial(
                new Color(0.5f, 0.41f, 0.3f),
                ridgeRockTexture,
                new Vector2(2.4f, 2.4f),
                0.02f,
                0.28f,
                ridgeRockNormal,
                0.72f);
            cliffMaterial = CreateTexturedPbrMaterial(
                new Color(0.56f, 0.45f, 0.33f),
                cliffFaceTexture,
                new Vector2(2.8f, 3.6f),
                0.02f,
                0.24f,
                cliffFaceNormal,
                0.94f);
            mountainMaterial = CreateTexturedPbrMaterial(
                new Color(0.44f, 0.41f, 0.35f),
                mountainStoneTexture,
                new Vector2(2.2f, 2.2f),
                0.02f,
                0.2f,
                mountainStoneNormal,
                0.8f);
            talusMaterial = CreateTexturedPbrMaterial(
                new Color(0.48f, 0.39f, 0.27f),
                talusTexture,
                new Vector2(3.4f, 2.1f),
                0.01f,
                0.22f,
                talusNormal,
                0.62f);
            dryWashMaterial = CreateTexturedPbrMaterial(
                new Color(0.61f, 0.51f, 0.35f),
                dryWashTexture,
                new Vector2(4.5f, 1.8f),
                0f,
                0.28f,
                dryWashNormal,
                0.5f);
            craterMaterial = CreateTexturedTransparentMaterial(
                new Color(0.15f, 0.12f, 0.08f, 0.34f),
                scorchTexture,
                new Vector2(1.6f, 1.6f));
            resourceMaterial = CreateMaterial(new Color(0.2f, 0.95f, 0.62f));
            depletedResourceMaterial = CreateMaterial(new Color(0.11f, 0.18f, 0.14f));
            resourceGlowMaterial = CreateMaterial(new Color(0.55f, 1f, 0.78f));
            resourceGlowMaterial.EnableKeyword("_EMISSION");
            resourceGlowMaterial.SetColor("_EmissionColor", new Color(0.32f, 1f, 0.64f));
            resourceMinerMaterial = CreateMaterial(new Color(0.2f, 0.32f, 0.24f));
            darkMaterial = CreateTexturedPbrMaterial(
                new Color(0.12f, 0.14f, 0.13f),
                gunmetalTexture,
                new Vector2(1.6f, 1.6f),
                0.12f,
                0.34f,
                gunmetalNormal,
                0.28f);
            shadowPanelMaterial = CreateTexturedPbrMaterial(
                new Color(0.045f, 0.052f, 0.048f),
                rubberTreadTexture,
                new Vector2(1.2f, 1.2f),
                0.02f,
                0.22f,
                rubberTreadNormal,
                0.24f);
            edgeHighlightMaterial = CreateTexturedPbrMaterial(
                new Color(0.76f, 0.8f, 0.68f),
                vehicleArmorTexture,
                new Vector2(1.8f, 1.8f),
                0.16f,
                0.64f,
                vehicleArmorNormal,
                0.22f);
            cautionStripeMaterial = CreateTexturedPbrMaterial(
                new Color(0.95f, 0.67f, 0.18f),
                vehicleArmorTexture,
                new Vector2(2.1f, 1.1f),
                0.08f,
                0.46f,
                vehicleArmorNormal,
                0.18f);
            vehicleDetailMaterial = CreateTexturedPbrMaterial(
                new Color(0.42f, 0.47f, 0.39f),
                vehicleArmorTexture,
                new Vector2(2.8f, 2.8f),
                0.2f,
                0.56f,
                vehicleArmorNormal,
                0.58f);
            structureDetailMaterial = CreateTexturedPbrMaterial(
                new Color(0.38f, 0.43f, 0.36f),
                structurePanelTexture,
                new Vector2(3.4f, 3.4f),
                0.16f,
                0.5f,
                structurePanelNormal,
                0.52f);
            weaponMetalMaterial = CreateTexturedPbrMaterial(
                new Color(0.18f, 0.2f, 0.19f),
                gunmetalTexture,
                new Vector2(2.4f, 2.4f),
                0.48f,
                0.62f,
                gunmetalNormal,
                0.46f);
            trackRubberMaterial = CreateTexturedPbrMaterial(
                new Color(0.065f, 0.072f, 0.066f),
                rubberTreadTexture,
                new Vector2(1.6f, 4.2f),
                0.02f,
                0.36f,
                rubberTreadNormal,
                0.72f);
            sensorGlassMaterial = CreateMaterial(new Color(0.26f, 0.78f, 0.96f));
            ConfigurePbr(sensorGlassMaterial, 0.02f, 0.9f);
            ConfigureEmission(sensorGlassMaterial, new Color(0.09f, 0.48f, 0.68f));
            panelTrimMaterial = CreateTexturedPbrMaterial(
                new Color(0.58f, 0.62f, 0.5f),
                vehicleArmorTexture,
                new Vector2(3.1f, 1.7f),
                0.18f,
                0.58f,
                vehicleArmorNormal,
                0.34f);
            warningLightMaterial = CreateMaterial(new Color(1f, 0.38f, 0.12f));
            ConfigurePbr(warningLightMaterial, 0.02f, 0.86f);
            ConfigureEmission(warningLightMaterial, new Color(0.9f, 0.2f, 0.06f));
        }

        private void CreateRoots()
        {
            unitsRoot = new GameObject("Units").transform;
            unitsRoot.SetParent(transform, false);
            structuresRoot = new GameObject("Structures").transform;
            structuresRoot.SetParent(transform, false);
            resourcesRoot = new GameObject("Resources").transform;
            resourcesRoot.SetParent(transform, false);
            effectsRoot = new GameObject("Effects").transform;
            effectsRoot.SetParent(transform, false);
        }

        private void SetupDesktopCamera()
        {
            CommandCamera = Camera.main;
            if (CommandCamera == null)
            {
                GameObject cameraObject = new GameObject("Command Camera");
                cameraObject.transform.SetParent(transform, false);
                CommandCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            CommandCamera.transform.position = new Vector3(-72f, 48f, -104f);
            CommandCamera.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            CommandCamera.clearFlags = CameraClearFlags.SolidColor;
            CommandCamera.backgroundColor = new Color(0.035f, 0.04f, 0.05f);
            CommandCamera.nearClipPlane = 0.05f;
            CommandCamera.farClipPlane = 250f;
            CommandCamera.fieldOfView = 60f;
        }

        private void SetupLight()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.63f, 0.6f);
            RenderSettings.reflectionIntensity = 0.5f;

            if (Object.FindObjectOfType<Light>() == null)
            {
                GameObject lightObject = new GameObject("Sun");
                lightObject.transform.SetParent(transform, false);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.72f;
                lightObject.transform.rotation = Quaternion.Euler(58f, -32f, 0f);
            }
        }

        private void CreateGround()
        {
            GameObject ground = new GameObject("Battlefield");
            ground.name = "Battlefield";
            ground.transform.SetParent(transform, true);
            ground.transform.position = Vector3.zero;
            MeshFilter groundFilter = ground.AddComponent<MeshFilter>();
            groundFilter.sharedMesh = CreateBattlefieldArtMesh();
            MeshRenderer groundRenderer = ground.AddComponent<MeshRenderer>();
            groundRenderer.sharedMaterial = groundMaterial;
            BoxCollider groundCollider = ground.AddComponent<BoxCollider>();
            float battlefieldSize = RtsBalance.MapHalfSize * 2f;
            groundCollider.center = new Vector3(0f, -0.04f, 0f);
            groundCollider.size = new Vector3(battlefieldSize, 0.08f, battlefieldSize);

            CreateTerrainSetDressing();

            CreateSectorEdgeTicks();

            CreateBoardFrame();
        }

        private void CreateGridLine(Vector3 a, Vector3 b)
        {
            GameObject lineObject = new GameObject("Grid Line");
            lineObject.transform.SetParent(transform, true);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, a);
            line.SetPosition(1, b);
            line.widthMultiplier = 0.018f;
            line.material = CreateTransparentMaterial(new Color(0.44f, 0.56f, 0.48f, 0.28f));
        }

        private Mesh CreateBattlefieldArtMesh()
        {
            int segmentCount = BattlefieldMeshSegments;
            int vertexCount = (segmentCount + 1) * (segmentCount + 1);
            float size = RtsBalance.MapHalfSize * 2f;
            float origin = -RtsBalance.MapHalfSize;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uv = new Vector2[vertexCount];
            int[] triangles = new int[segmentCount * segmentCount * 6];

            int vertexIndex = 0;
            for (int z = 0; z <= segmentCount; z++)
            {
                for (int x = 0; x <= segmentCount; x++)
                {
                    float u = x / (float)segmentCount;
                    float v = z / (float)segmentCount;
                    float worldX = origin + u * size;
                    float worldZ = origin + v * size;
                    float edgeFade = Mathf.SmoothStep(0f, 0.16f, Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)));
                    float dunes = Mathf.PerlinNoise(worldX * 0.026f + 2.8f, worldZ * 0.026f - 4.1f);
                    float pebble = Mathf.PerlinNoise(worldX * 0.17f - 8.4f, worldZ * 0.17f + 9.2f);
                    float height = (dunes - 0.5f) * 0.034f + (pebble - 0.5f) * 0.008f;
                    vertices[vertexIndex] = new Vector3(worldX, height * edgeFade, worldZ);
                    uv[vertexIndex] = new Vector2(u, v);
                    vertexIndex++;
                }
            }

            int triangleIndex = 0;
            for (int z = 0; z < segmentCount; z++)
            {
                for (int x = 0; x < segmentCount; x++)
                {
                    int a = z * (segmentCount + 1) + x;
                    int b = a + 1;
                    int c = a + segmentCount + 1;
                    int d = c + 1;
                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = d;
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "Command RTS Subdivided Battlefield Mesh";
            mesh.hideFlags = HideFlags.DontSave;
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void CreateSectorEdgeTicks()
        {
            float edge = RtsBalance.MapHalfSize;
            const float tickLength = 4.2f;
            for (int i = -5; i <= 5; i++)
            {
                float coordinate = i * 20f;
                CreateGridLine(new Vector3(coordinate, 0.05f, -edge), new Vector3(coordinate, 0.05f, -edge + tickLength));
                CreateGridLine(new Vector3(coordinate, 0.05f, edge), new Vector3(coordinate, 0.05f, edge - tickLength));
                CreateGridLine(new Vector3(-edge, 0.051f, coordinate), new Vector3(-edge + tickLength, 0.051f, coordinate));
                CreateGridLine(new Vector3(edge, 0.051f, coordinate), new Vector3(edge - tickLength, 0.051f, coordinate));
            }
        }

        private void CreateTerrainSetDressing()
        {
            CreateTerrainDisk("Projected Water Channel A", new Vector3(8f, 0.034f, -6f), new Vector3(34f, 0.018f, 13f), -8f, waterMaterial);
            CreateTerrainDisk("Projected Water Channel B", new Vector3(34f, 0.033f, 16f), new Vector3(28f, 0.018f, 9f), 24f, waterMaterial);
            CreateTerrainDisk("Projected Water Inlet", new Vector3(70f, 0.032f, -14f), new Vector3(22f, 0.018f, 8f), -20f, waterMaterial);

            CreateTerrainDisk("West Dune Shelf", new Vector3(-76f, 0.031f, -28f), new Vector3(26f, 0.02f, 15f), 15f, terrainAccentMaterial);
            CreateTerrainDisk("North Dune Shelf", new Vector3(-24f, 0.031f, 76f), new Vector3(38f, 0.02f, 13f), -12f, terrainAccentMaterial);
            CreateTerrainDisk("East Dune Shelf", new Vector3(78f, 0.031f, 54f), new Vector3(24f, 0.02f, 18f), 34f, terrainAccentMaterial);

            CreateDryWashTrail("Central Dry Wash", new Vector3(-12f, 0.029f, 8f), new Vector3(16f, 0.014f, 5.6f), -18f, 5, 11.2f);
            CreateDryWashTrail("South Dry Wash Delta", new Vector3(-50f, 0.028f, -74f), new Vector3(12f, 0.014f, 7.8f), 12f, 4, 8.2f);
            CreateDryWashTrail("East Dry Wash Fork", new Vector3(54f, 0.028f, 26f), new Vector3(10.5f, 0.014f, 4.8f), 32f, 4, 7.6f);

            CreateTerrainBlock("West Mesa Ridge", new Vector3(-101f, 0.72f, 6f), new Vector3(9f, 1.45f, 34f), -12f, ridgeMaterial);
            CreateTerrainBlock("North Mesa Ridge", new Vector3(-12f, 0.64f, 102f), new Vector3(48f, 1.28f, 7f), 7f, ridgeMaterial);
            CreateTerrainBlock("East Mesa Ridge", new Vector3(101f, 0.68f, 28f), new Vector3(8f, 1.36f, 31f), 18f, ridgeMaterial);

            CreateCliffBand("South Canyon Cliff", new Vector3(-18f, 1.16f, -103f), new Vector3(78f, 2.3f, 5.6f), -3f, 4);
            CreateCliffBand("West Rim Cliff", new Vector3(-103f, 1.08f, -34f), new Vector3(42f, 2.15f, 5.2f), 82f, 3);
            CreateCliffBand("Northeast Escarpment Cliff", new Vector3(91f, 1.3f, 70f), new Vector3(36f, 2.6f, 6.2f), 48f, 3);

            CreateMountainCluster("Northwest", new Vector3(-92f, 0f, 91f), 3, 6.8f, 1.25f);
            CreateMountainCluster("Northeast", new Vector3(88f, 0f, 95f), 2, 6.2f, 1.1f);
            CreateMountainCluster("Southeast", new Vector3(96f, 0f, -78f), 2, 5.8f, 1f);

            CreateTerrainDisk("Southwest Blast Scorch", new Vector3(-48f, 0.036f, -38f), new Vector3(7f, 0.012f, 4.8f), 22f, craterMaterial);
            CreateTerrainDisk("Central Blast Scorch", new Vector3(20f, 0.036f, 34f), new Vector3(8f, 0.012f, 5.2f), -18f, craterMaterial);
            CreateRockCluster("Southwest", new Vector3(-92f, 0f, -6f), 7);
            CreateRockCluster("Northeast", new Vector3(88f, 0f, 70f), 6);
            CreateRockCluster("Midfield", new Vector3(36f, 0f, -42f), 5);
            CreateTerrainPebbleField("South Canyon", new Vector3(-18f, 0f, -96f), 6, 48f, 4.8f, -3f);
            CreateTerrainPebbleField("West Rim", new Vector3(-97f, 0f, -34f), 5, 30f, 4.4f, 82f);
            CreateTerrainPebbleField("Northeast Escarpment", new Vector3(86f, 0f, 65f), 5, 28f, 4.2f, 48f);
        }

        private void CreateDryWashTrail(string trailName, Vector3 center, Vector3 segmentScale, float yawDegrees, int segmentCount, float step)
        {
            Quaternion rotation = Quaternion.Euler(0f, yawDegrees, 0f);
            int namedSegment = Mathf.Max(0, segmentCount / 2);
            for (int i = 0; i < segmentCount; i++)
            {
                float signedIndex = i - namedSegment;
                float meander = Mathf.Sin((i + GetStableSeed(trailName) * 0.01f) * 1.37f) * segmentScale.z * 0.34f;
                Vector3 offset = rotation * new Vector3(signedIndex * step, 0f, meander);
                float width = segmentScale.x * (0.82f + Hash01(i * 17, segmentCount, GetStableSeed(trailName)) * 0.36f);
                float depth = segmentScale.z * (0.76f + Hash01(i * 23, segmentCount + 3, GetStableSeed(trailName)) * 0.34f);
                string segmentName = i == namedSegment ? trailName : trailName + " Braid " + (i + 1);
                CreateTerrainDisk(segmentName, center + offset + Vector3.up * (i * 0.0007f), new Vector3(width, segmentScale.y, depth), yawDegrees + Mathf.Sin(i * 1.9f) * 9f, dryWashMaterial);
            }
        }

        private void CreateCliffBand(string cliffName, Vector3 center, Vector3 scale, float yawDegrees, int strataCount)
        {
            Quaternion rotation = Quaternion.Euler(0f, yawDegrees, 0f);
            Vector3 forward = rotation * Vector3.forward;
            Vector3 right = rotation * Vector3.right;

            CreateTerrainBlock(cliffName + " Face", center, scale, yawDegrees, cliffMaterial);
            CreateTerrainBlock(cliffName + " Shadow Base", center + forward * (scale.z * 0.55f) + Vector3.down * (scale.y * 0.35f), new Vector3(scale.x * 0.96f, 0.16f, 0.2f), yawDegrees, shadowPanelMaterial);
            CreateTerrainBlock(cliffName + " Sunlit Rim", center - forward * (scale.z * 0.53f) + Vector3.up * (scale.y * 0.52f), new Vector3(scale.x * 0.94f, 0.12f, 0.36f), yawDegrees, ridgeMaterial);

            for (int i = 0; i < strataCount; i++)
            {
                float t = strataCount <= 1 ? 0.5f : i / (float)(strataCount - 1);
                float lateral = Mathf.Sin((i + 1) * 1.91f) * scale.x * 0.07f;
                float height = Mathf.Lerp(-scale.y * 0.38f, scale.y * 0.42f, t);
                float ledgeDepth = 0.12f + (i % 3) * 0.045f;
                Vector3 position = center + right * lateral + forward * (scale.z * (0.18f + t * 0.18f)) + Vector3.up * height;
                Vector3 ledgeScale = new Vector3(scale.x * (0.32f + (i % 2) * 0.08f), 0.045f, ledgeDepth);
                CreateTerrainBlock(cliffName + " Strata " + (i + 1), position, ledgeScale, yawDegrees + Mathf.Sin(i * 2.1f) * 3f, i % 2 == 0 ? ridgeMaterial : mountainMaterial);
            }

            for (int i = 0; i < 3; i++)
            {
                float offset = ((i - 1) * scale.x * 0.28f) + Mathf.Sin(i * 2.4f) * scale.x * 0.04f;
                Vector3 talusPosition = center + right * offset + forward * (scale.z * 0.92f + i * 0.18f);
                Vector3 talusScale = new Vector3(scale.x * 0.18f + i * 0.75f, 0.018f, scale.z * (0.78f + i * 0.18f));
                CreateTerrainDisk(cliffName + " Talus Fan " + (i + 1), new Vector3(talusPosition.x, 0.033f, talusPosition.z), talusScale, yawDegrees + i * 9f, talusMaterial);
            }
        }

        private void CreateMountainCluster(string clusterName, Vector3 center, int peakCount, float footprint, float heightBias)
        {
            for (int i = 0; i < peakCount; i++)
            {
                float angle = i * 2.07f + footprint * 0.13f;
                float radius = i == 0 ? 0f : footprint * (0.42f + (i % 3) * 0.16f);
                Vector3 basePosition = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                float peakHeight = (1.9f + (i % 3) * 0.38f) * heightBias;
                float peakRadius = footprint * (0.42f - Mathf.Min(i, 2) * 0.045f);
                float yaw = angle * Mathf.Rad2Deg + i * 19f;

                CreateTerrainDisk(clusterName + " Mountain Base " + (i + 1), new Vector3(basePosition.x, peakHeight * 0.52f, basePosition.z), new Vector3(peakRadius * 1.45f, peakHeight * 0.52f, peakRadius * 1.12f), yaw, talusMaterial);
                CreateTerrainDisk(clusterName + " Mountain Shoulder " + (i + 1), new Vector3(basePosition.x + Mathf.Sin(angle) * 0.8f, peakHeight * 1.04f, basePosition.z - Mathf.Cos(angle) * 0.55f), new Vector3(peakRadius * 1.02f, peakHeight * 0.5f, peakRadius * 0.84f), yaw + 17f, cliffMaterial);
                CreateTerrainDisk(clusterName + " Mountain Peak " + (i + 1), new Vector3(basePosition.x - Mathf.Sin(angle) * 0.45f, peakHeight * 1.58f, basePosition.z + Mathf.Cos(angle) * 0.35f), new Vector3(peakRadius * 0.6f, peakHeight * 0.48f, peakRadius * 0.5f), yaw - 11f, mountainMaterial);

                if (i > 0)
                {
                    Vector3 midPoint = Vector3.Lerp(center, basePosition, 0.55f);
                    CreateTerrainBlock(clusterName + " Mountain Ridge Spur " + i, new Vector3(midPoint.x, 0.68f + peakHeight * 0.14f, midPoint.z), new Vector3(radius * 0.62f + 1.6f, 0.42f + peakHeight * 0.16f, 1.2f), yaw + 48f, ridgeMaterial);
                }
            }

            CreateTerrainDisk(clusterName + " Mountain Talus Apron", new Vector3(center.x, 0.032f, center.z), new Vector3(footprint * 1.95f, 0.018f, footprint * 1.45f), footprint * 7f, talusMaterial);
        }

        private void CreateTerrainPebbleField(string fieldName, Vector3 center, int count, float width, float depth, float yawDegrees)
        {
            Quaternion rotation = Quaternion.Euler(0f, yawDegrees, 0f);
            for (int i = 0; i < count; i++)
            {
                float u = count <= 1 ? 0.5f : i / (float)(count - 1);
                float lateral = Mathf.Lerp(-width * 0.5f, width * 0.5f, u);
                float forwardOffset = Mathf.Sin(i * 1.73f) * depth * 0.45f;
                Vector3 position = center + rotation * new Vector3(lateral, 0f, forwardOffset);
                float size = 0.28f + (i % 4) * 0.13f;
                float height = 0.18f + (i % 3) * 0.08f;
                CreateTerrainDisk(fieldName + " Talus Pebble " + (i + 1), new Vector3(position.x, height, position.z), new Vector3(size, height, size * (0.75f + (i % 2) * 0.24f)), yawDegrees + i * 31f, i % 3 == 0 ? ridgeMaterial : talusMaterial);
            }
        }

        private void CreateTerrainDisk(string name, Vector3 position, Vector3 scale, float yawDegrees, Material material)
        {
            if (scale.y <= 0.05f)
            {
                CreateTerrainPatchMesh(name, position, scale, yawDegrees, material);
                return;
            }

            CreateTerrainMoundMesh(name, position, scale, yawDegrees, material);
        }

        private void CreateTerrainBlock(string name, Vector3 position, Vector3 scale, float yawDegrees, Material material)
        {
            CreateTerrainPrismMesh(name, position, scale, yawDegrees, material);
        }

        private GameObject CreateTerrainMeshObject(string name, Vector3 position, float yawDegrees, Material material, Mesh mesh)
        {
            GameObject terrainObject = new GameObject(name);
            terrainObject.transform.SetParent(transform, false);
            terrainObject.transform.localPosition = position;
            terrainObject.transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
            MeshFilter filter = terrainObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = terrainObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return terrainObject;
        }

        private GameObject CreateTerrainPatchMesh(string name, Vector3 position, Vector3 scale, float yawDegrees, Material material)
        {
            int segments = TerrainPatchSegments;
            int rings = 3;
            int vertexCount = 1 + rings * segments;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uv = new Vector2[vertexCount];
            int[] triangles = new int[segments * 3 + (rings - 1) * segments * 6];
            float halfX = Mathf.Max(0.01f, scale.x * 0.5f);
            float halfZ = Mathf.Max(0.01f, scale.z * 0.5f);
            int seed = GetStableSeed(name);

            vertices[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);

            int vertexIndex = 1;
            for (int ring = 1; ring <= rings; ring++)
            {
                float radius = ring / (float)rings;
                for (int i = 0; i < segments; i++)
                {
                    float angle = (Mathf.PI * 2f * i) / segments;
                    float wobble = GetPerimeterWobble(i, ring, seed, ring == rings ? 0.22f : 0.09f);
                    float x = Mathf.Cos(angle) * halfX * radius * wobble;
                    float z = Mathf.Sin(angle) * halfZ * radius * wobble;
                    float height = Mathf.Sin(angle * 2.1f + seed * 0.03f) * scale.y * 0.14f;
                    vertices[vertexIndex] = new Vector3(x, height, z);
                    uv[vertexIndex] = new Vector2(0.5f + x / Mathf.Max(halfX * 2f, 0.01f), 0.5f + z / Mathf.Max(halfZ * 2f, 0.01f));
                    vertexIndex++;
                }
            }

            int triangleIndex = 0;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles[triangleIndex++] = 0;
                triangles[triangleIndex++] = 1 + next;
                triangles[triangleIndex++] = 1 + i;
            }

            for (int ring = 1; ring < rings; ring++)
            {
                int innerStart = 1 + (ring - 1) * segments;
                int outerStart = 1 + ring * segments;
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    int a = innerStart + i;
                    int b = innerStart + next;
                    int c = outerStart + i;
                    int d = outerStart + next;
                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = d;
                    triangles[triangleIndex++] = c;
                }
            }

            Mesh mesh = CreateConfiguredMesh(name + " Mesh", vertices, uv, triangles);
            return CreateTerrainMeshObject(name, position, yawDegrees, material, mesh);
        }

        private GameObject CreateTerrainMoundMesh(string name, Vector3 position, Vector3 scale, float yawDegrees, Material material)
        {
            const int segments = 18;
            const int rings = 4;
            int vertexCount = rings * segments + 1;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uv = new Vector2[vertexCount];
            int[] triangles = new int[(rings - 1) * segments * 6 + segments * 3];
            float halfX = Mathf.Max(0.01f, scale.x * 0.5f);
            float halfY = Mathf.Max(0.01f, scale.y);
            float halfZ = Mathf.Max(0.01f, scale.z * 0.5f);
            int seed = GetStableSeed(name);

            int vertexIndex = 0;
            for (int ring = 0; ring < rings; ring++)
            {
                float t = ring / (float)(rings - 1);
                float radius = Mathf.Lerp(1f, 0.24f, Mathf.SmoothStep(0f, 1f, t));
                float y = Mathf.Lerp(-halfY, halfY * 0.78f, t);
                for (int i = 0; i < segments; i++)
                {
                    float angle = (Mathf.PI * 2f * i) / segments;
                    float wobble = GetPerimeterWobble(i, ring + 4, seed, 0.2f);
                    float x = Mathf.Cos(angle) * halfX * radius * wobble;
                    float z = Mathf.Sin(angle) * halfZ * radius * wobble;
                    float shoulder = Mathf.PerlinNoise(i * 0.23f + seed * 0.017f, ring * 0.37f + seed * 0.011f) - 0.5f;
                    vertices[vertexIndex] = new Vector3(x, y + shoulder * halfY * 0.18f, z);
                    uv[vertexIndex] = new Vector2(0.5f + x / Mathf.Max(halfX * 2f, 0.01f), t);
                    vertexIndex++;
                }
            }

            int topIndex = vertexIndex;
            vertices[topIndex] = new Vector3(0f, halfY * 1.12f, 0f);
            uv[topIndex] = new Vector2(0.5f, 1f);

            int triangleIndex = 0;
            for (int ring = 0; ring < rings - 1; ring++)
            {
                int innerStart = ring * segments;
                int outerStart = (ring + 1) * segments;
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    int a = innerStart + i;
                    int b = innerStart + next;
                    int c = outerStart + i;
                    int d = outerStart + next;
                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = d;
                }
            }

            int lastRingStart = (rings - 1) * segments;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles[triangleIndex++] = lastRingStart + i;
                triangles[triangleIndex++] = topIndex;
                triangles[triangleIndex++] = lastRingStart + next;
            }

            Mesh mesh = CreateConfiguredMesh(name + " Mesh", vertices, uv, triangles);
            return CreateTerrainMeshObject(name, position, yawDegrees, material, mesh);
        }

        private GameObject CreateTerrainPrismMesh(string name, Vector3 position, Vector3 scale, float yawDegrees, Material material)
        {
            int perimeterCount = Mathf.Clamp(Mathf.CeilToInt((scale.x + scale.z) * 0.55f), 12, 34);
            Vector2[] perimeter = CreateWobblyRectanglePerimeter(scale.x, scale.z, perimeterCount, GetStableSeed(name));
            Vector3[] vertices = new Vector3[perimeter.Length * 2 + 1];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[perimeter.Length * 6 + perimeter.Length * 3];
            float halfY = Mathf.Max(0.01f, scale.y * 0.5f);
            int seed = GetStableSeed(name);

            for (int i = 0; i < perimeter.Length; i++)
            {
                Vector2 point = perimeter[i];
                float topLift = (Hash01(i * 17, seed, i + seed) - 0.5f) * halfY * 0.26f;
                vertices[i * 2] = new Vector3(point.x, -halfY, point.y);
                vertices[i * 2 + 1] = new Vector3(point.x, halfY + topLift, point.y);
                uv[i * 2] = new Vector2(point.x / Mathf.Max(scale.x, 0.01f) + 0.5f, point.y / Mathf.Max(scale.z, 0.01f) + 0.5f);
                uv[i * 2 + 1] = uv[i * 2] + new Vector2(0f, 0.35f);
            }

            int topCenterIndex = perimeter.Length * 2;
            vertices[topCenterIndex] = new Vector3(0f, halfY * 1.08f, 0f);
            uv[topCenterIndex] = new Vector2(0.5f, 0.5f);

            int triangleIndex = 0;
            for (int i = 0; i < perimeter.Length; i++)
            {
                int next = (i + 1) % perimeter.Length;
                int bottom = i * 2;
                int top = bottom + 1;
                int nextBottom = next * 2;
                int nextTop = nextBottom + 1;
                triangles[triangleIndex++] = bottom;
                triangles[triangleIndex++] = top;
                triangles[triangleIndex++] = nextBottom;
                triangles[triangleIndex++] = nextBottom;
                triangles[triangleIndex++] = top;
                triangles[triangleIndex++] = nextTop;
            }

            for (int i = 0; i < perimeter.Length; i++)
            {
                int next = (i + 1) % perimeter.Length;
                triangles[triangleIndex++] = topCenterIndex;
                triangles[triangleIndex++] = i * 2 + 1;
                triangles[triangleIndex++] = next * 2 + 1;
            }

            Mesh mesh = CreateConfiguredMesh(name + " Mesh", vertices, uv, triangles);
            return CreateTerrainMeshObject(name, position, yawDegrees, material, mesh);
        }

        private static Mesh CreateConfiguredMesh(string name, Vector3[] vertices, Vector2[] uv, int[] triangles)
        {
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.hideFlags = HideFlags.DontSave;
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector2[] CreateWobblyRectanglePerimeter(float width, float depth, int count, int seed)
        {
            Vector2[] perimeter = new Vector2[count];
            float halfX = Mathf.Max(0.01f, width * 0.5f);
            float halfZ = Mathf.Max(0.01f, depth * 0.5f);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float side = t * 4f;
                float x;
                float z;
                if (side < 1f)
                {
                    x = Mathf.Lerp(-halfX, halfX, side);
                    z = halfZ;
                }
                else if (side < 2f)
                {
                    x = halfX;
                    z = Mathf.Lerp(halfZ, -halfZ, side - 1f);
                }
                else if (side < 3f)
                {
                    x = Mathf.Lerp(halfX, -halfX, side - 2f);
                    z = -halfZ;
                }
                else
                {
                    x = -halfX;
                    z = Mathf.Lerp(-halfZ, halfZ, side - 3f);
                }

                float edgeNoise = Hash01(i * 13 + seed, seed - i * 7, seed);
                float inset = Mathf.Lerp(-0.16f, 0.08f, edgeNoise);
                Vector2 point = new Vector2(x, z);
                Vector2 normal = point.sqrMagnitude > 0.001f ? point.normalized : Vector2.up;
                perimeter[i] = point + normal * Mathf.Min(halfX, halfZ) * inset;
            }

            return perimeter;
        }

        private static float GetPerimeterWobble(int index, int ring, int seed, float strength)
        {
            float broad = Mathf.PerlinNoise(index * 0.18f + seed * 0.011f, ring * 0.37f + seed * 0.017f);
            float sharp = Hash01(index * 19 + ring, seed - index * 11, seed);
            return 1f + (broad - 0.5f) * strength + (sharp - 0.5f) * strength * 0.45f;
        }

        private static int GetStableSeed(string text)
        {
            unchecked
            {
                int hash = 5381;
                for (int i = 0; i < text.Length; i++)
                {
                    hash = ((hash << 5) + hash) ^ text[i];
                }

                return hash;
            }
        }

        private void CreateRockCluster(string clusterName, Vector3 center, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i * 1.618f;
                float radius = 2.2f + (i % 3) * 1.45f;
                float height = 0.45f + (i % 4) * 0.18f;
                Vector3 position = center + new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
                Vector3 scale = new Vector3(0.75f + (i % 3) * 0.22f, height, 0.65f + (i % 2) * 0.28f);
                CreateTerrainDisk(clusterName + " Rock " + (i + 1), position, scale, i * 23f, ridgeMaterial);
            }
        }

        private void CreateResourceFields()
        {
            for (int i = 0; i < ResourceFieldCenters.Length; i++)
            {
                CreateResourceField(ResourceFieldCenters[i], ResourceFieldNodeCounts[i]);
            }
        }

        private void CreateResourceField(Vector3 center, int count)
        {
            List<ResourceNode> fieldNodes = new List<ResourceNode>(count);
            for (int i = 0; i < count; i++)
            {
                float angle = (Mathf.PI * 2f * i) / count;
                float radius = 3.2f + (i % 4) * 1.8f;
                Vector3 position = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                fieldNodes.Add(CreateResourceNode(position, 2600 + i * 220, 2600 + i * 220, 0));
            }

            CreateResourceFieldMiner(center, fieldNodes);
        }

        private ResourceNode CreateResourceNode(Vector3 position, int maxAmount, int amount, int persistentId)
        {
            GameObject nodeObject = new GameObject("Flux Crystals");
            nodeObject.transform.SetParent(resourcesRoot, true);
            nodeObject.transform.position = ClampToGround(position);
            ResourceNode node = nodeObject.AddComponent<ResourceNode>();
            node.InitializeForRestore(maxAmount, amount, resourceMaterial, depletedResourceMaterial);
            node.AssignPersistentId(persistentId > 0 ? persistentId : AllocateResourceNodeId());
            nextResourceNodeId = Mathf.Max(nextResourceNodeId, node.PersistentId + 1);
            resourceNodes.Add(node);
            return node;
        }

        private void CreateResourceFieldMinersForExistingNodes()
        {
            for (int i = 0; i < ResourceFieldCenters.Length; i++)
            {
                List<ResourceNode> fieldNodes = new List<ResourceNode>();
                Vector3 center = ResourceFieldCenters[i];
                for (int n = 0; n < resourceNodes.Count; n++)
                {
                    ResourceNode node = resourceNodes[n];
                    if (node != null && PlanarDistance(center, node.transform.position) <= 10.5f)
                    {
                        fieldNodes.Add(node);
                    }
                }

                if (fieldNodes.Count > 0)
                {
                    CreateResourceFieldMiner(center, fieldNodes);
                }
            }
        }

        private void CreateResourceFieldMiner(Vector3 center, List<ResourceNode> fieldNodes)
        {
            if (fieldNodes == null || fieldNodes.Count == 0)
            {
                return;
            }

            GameObject minerObject = new GameObject("Ore Field Miner");
            minerObject.transform.SetParent(resourcesRoot, true);
            minerObject.transform.position = ClampToGround(center);
            ResourceFieldRegenerator regenerator = minerObject.AddComponent<ResourceFieldRegenerator>();
            regenerator.Initialize(fieldNodes, resourceMaterial, depletedResourceMaterial, resourceGlowMaterial, resourceMinerMaterial);
        }

        private void SpawnStartingForces()
        {
            RtsStructure command = CreateStructure(RtsTeam.Player, StructureKind.CommandCenter, new Vector3(-84f, 0f, -78f));

            CreateStructure(RtsTeam.Enemy, StructureKind.CommandCenter, new Vector3(86f, 0f, 78f));

            SpawnPlayerStartingUnits();

            SelectEntity(command, false);
        }

        private void SpawnPlayerStartingUnits()
        {
            if (SkirmishOptions == null || SkirmishOptions.startingForces == RtsStartingForcesPreset.FabricationOnly)
            {
                return;
            }

            Vector3 rally = new Vector3(-76f, 0f, -70f);
            RtsUnit first = CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-80f, 0f, -72f));
            first.IssueMove(rally + new Vector3(-2f, 0f, 0f));
            RtsUnit second = CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-78f, 0f, -75f));
            second.IssueMove(rally + new Vector3(2f, 0f, 0f));

            if (SkirmishOptions.startingForces != RtsStartingForcesPreset.StrikeTeam)
            {
                return;
            }

            CreateUnit(RtsTeam.Player, UnitKind.Grenadier, new Vector3(-82f, 0f, -68f)).IssueMove(rally + new Vector3(-3f, 0f, 3f));
            CreateUnit(RtsTeam.Player, UnitKind.RocketSoldier, new Vector3(-76f, 0f, -76f)).IssueMove(rally + new Vector3(3f, 0f, 3f));
            CreateUnit(RtsTeam.Player, UnitKind.LightTank, new Vector3(-72f, 0f, -74f)).IssueMove(rally + new Vector3(0f, 0f, 5f));
        }

        private ProductionStructure FindProducer(UnitKind kind, bool selectedOnly)
        {
            List<RtsEntity> source = selectedOnly ? selection : entities;

            for (int i = 0; i < source.Count; i++)
            {
                ProductionStructure producer = source[i] as ProductionStructure;
                if (producer != null && producer.Team == RtsTeam.Player && producer.CanTrain(kind))
                {
                    return producer;
                }
            }

            return null;
        }

        private void AddUnitCollider(GameObject root, UnitKind kind)
        {
            UnitKind normalized = RtsBalance.NormalizeUnitKind(kind);
            if (RtsBalance.IsVehicle(normalized))
            {
                BoxCollider box = root.AddComponent<BoxCollider>();
                if (normalized == UnitKind.Harvester)
                {
                    box.center = new Vector3(0f, 0.55f, 0f);
                    box.size = new Vector3(1.5f, 1.2f, 2.0f);
                }
                else if (RtsBalance.IsWheeledCombatVehicle(normalized))
                {
                    box.center = GetWheeledVehicleColliderCenter(normalized);
                    box.size = GetWheeledVehicleColliderSize(normalized);
                }
                else
                {
                    box.center = GetTankColliderCenter(normalized);
                    box.size = GetTankColliderSize(normalized);
                }
            }
            else
            {
                CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
                capsule.center = new Vector3(0f, 0.75f, 0f);
                capsule.height = 1.5f;
                capsule.radius = 0.42f;
            }
        }

        private void CreateBoardFrame()
        {
            float edge = RtsBalance.MapHalfSize + 2.2f;
            float span = RtsBalance.MapHalfSize * 2f + 7f;
            Material frameMaterial = CreateMaterial(new Color(0.1f, 0.14f, 0.16f));
            Material glowMaterial = CreateMaterial(new Color(0.2f, 0.82f, 0.92f));

            CreatePrimitive(PrimitiveType.Cube, transform, "North Table Rail", new Vector3(0f, 0.42f, edge), new Vector3(span, 0.85f, 2.6f), frameMaterial);
            CreatePrimitive(PrimitiveType.Cube, transform, "South Table Rail", new Vector3(0f, 0.42f, -edge), new Vector3(span, 0.85f, 2.6f), frameMaterial);
            CreatePrimitive(PrimitiveType.Cube, transform, "East Table Rail", new Vector3(edge, 0.42f, 0f), new Vector3(2.6f, 0.85f, span), frameMaterial);
            CreatePrimitive(PrimitiveType.Cube, transform, "West Table Rail", new Vector3(-edge, 0.42f, 0f), new Vector3(2.6f, 0.85f, span), frameMaterial);

            float glowEdge = RtsBalance.MapHalfSize + 0.55f;
            CreatePrimitive(PrimitiveType.Cube, transform, "North Board Glow", new Vector3(0f, 0.08f, glowEdge), new Vector3(span - 5f, 0.04f, 0.28f), glowMaterial);
            CreatePrimitive(PrimitiveType.Cube, transform, "South Board Glow", new Vector3(0f, 0.08f, -glowEdge), new Vector3(span - 5f, 0.04f, 0.28f), glowMaterial);
            CreatePrimitive(PrimitiveType.Cube, transform, "East Board Glow", new Vector3(glowEdge, 0.08f, 0f), new Vector3(0.28f, 0.04f, span - 5f), glowMaterial);
            CreatePrimitive(PrimitiveType.Cube, transform, "West Board Glow", new Vector3(-glowEdge, 0.08f, 0f), new Vector3(0.28f, 0.04f, span - 5f), glowMaterial);

            float pylonEdge = RtsBalance.MapHalfSize + 3.4f;
            CreatePrimitive(PrimitiveType.Cube, transform, "Northwest Table Pylon", new Vector3(-pylonEdge, 1.35f, pylonEdge), new Vector3(3.4f, 2.7f, 3.4f), frameMaterial);
            CreatePrimitive(PrimitiveType.Cube, transform, "Northeast Table Pylon", new Vector3(pylonEdge, 1.35f, pylonEdge), new Vector3(3.4f, 2.7f, 3.4f), frameMaterial);
            CreatePrimitive(PrimitiveType.Cube, transform, "Southwest Table Pylon", new Vector3(-pylonEdge, 1.35f, -pylonEdge), new Vector3(3.4f, 2.7f, 3.4f), frameMaterial);
            CreatePrimitive(PrimitiveType.Cube, transform, "Southeast Table Pylon", new Vector3(pylonEdge, 1.35f, -pylonEdge), new Vector3(3.4f, 2.7f, 3.4f), frameMaterial);
        }

        private void BuildUnitVisual(Transform root, UnitKind kind, RtsTeam team)
        {
            kind = RtsBalance.NormalizeUnitKind(kind);
            Material teamMaterial = GetTeamMaterial(team);

            if (RtsBalance.IsTank(kind))
            {
                if (TryBuildImportedTankVisual(root, kind, teamMaterial))
                {
                    return;
                }

                BuildFallbackTankVisual(root, kind, teamMaterial);
                return;
            }

            if (RtsBalance.IsWheeledCombatVehicle(kind))
            {
                BuildFallbackWheeledVehicleVisual(root, kind, teamMaterial);
                return;
            }

            if (kind == UnitKind.Harvester)
            {
                if (TryBuildImportedHarvesterVisual(root, teamMaterial))
                {
                    return;
                }

                CreatePrimitive(PrimitiveType.Cube, root, "Cab", new Vector3(0f, 0.75f, -0.35f), new Vector3(1.2f, 0.8f, 0.9f), teamMaterial);
                CreatePrimitive(PrimitiveType.Cube, root, "Cargo", new Vector3(0f, 0.58f, 0.55f), new Vector3(1.35f, 0.55f, 1.3f), neutralMaterial);
                CreatePrimitive(PrimitiveType.Cylinder, root, "Collector", new Vector3(0f, 0.3f, 1.35f), new Vector3(0.65f, 0.2f, 0.65f), resourceMaterial).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                CreateHarvesterReadabilityPanels(root, teamMaterial);
                return;
            }

            if (RtsBalance.IsInfantry(kind))
            {
                if (TryBuildImportedInfantryVisual(root, kind, teamMaterial))
                {
                    return;
                }

                BuildFallbackInfantryVisual(root, kind, teamMaterial);
                return;
            }

            BuildFallbackInfantryVisual(root, kind, teamMaterial);
        }

        private bool TryBuildImportedInfantryVisual(Transform root, UnitKind kind, Material teamMaterial)
        {
            string modelPath = GetInfantryModelResourcePath(kind);
            if (string.IsNullOrEmpty(modelPath))
            {
                return false;
            }

            GameObject modelPrefab = UnityEngine.Resources.Load<GameObject>(modelPath);
            if (modelPrefab == null)
            {
                return false;
            }

            GameObject model = Instantiate(modelPrefab, root);
            model.name = RtsBalance.GetUnit(kind).Name + " Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * GetInfantryModelScale(kind);

            foreach (Collider collider in model.GetComponentsInChildren<Collider>())
            {
                DestroyRuntimeObject(collider);
            }

            ConfigureImportedRenderers(model, GetImportedUnitMaterialProfile(kind));

            CreateInfantryReadabilityPanels(root, teamMaterial);
            return true;
        }

        private bool TryBuildImportedHarvesterVisual(Transform root, Material teamMaterial)
        {
            GameObject modelPrefab = UnityEngine.Resources.Load<GameObject>("StructureModels/BastionStructures/Meshes/Bastion_Harvester_Static");
            if (modelPrefab == null)
            {
                return false;
            }

            GameObject model = Instantiate(modelPrefab, root);
            model.name = "Harvester Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * 0.34f;

            foreach (Collider collider in model.GetComponentsInChildren<Collider>())
            {
                DestroyRuntimeObject(collider);
            }

            ConfigureImportedRenderers(model, new ImportedRendererMaterialProfile(new Color(1.18f, 1.2f, 1.08f, 1f), 0.18f, 0.54f, 0.78f));

            CreateHarvesterReadabilityPanels(root, teamMaterial);
            return true;
        }

        private void BuildFallbackInfantryVisual(Transform root, UnitKind kind, Material teamMaterial)
        {
            CreatePrimitive(PrimitiveType.Capsule, root, "Body", new Vector3(0f, 0.8f, 0f), new Vector3(0.58f, 0.78f, 0.58f), teamMaterial);
            CreatePrimitive(PrimitiveType.Sphere, root, "Helmet", new Vector3(0f, 1.55f, 0.03f), new Vector3(0.46f, 0.36f, 0.46f), teamMaterial);
            if (kind == UnitKind.Engineer)
            {
                CreatePrimitive(PrimitiveType.Cube, root, "Repair Tool", new Vector3(0.35f, 1.02f, 0.34f), new Vector3(0.13f, 0.13f, 0.55f), weaponMetalMaterial);
            }
            else if (kind == UnitKind.RocketSoldier)
            {
                CreatePrimitive(PrimitiveType.Cube, root, "Rocket Launcher", new Vector3(0.38f, 1.15f, 0.36f), new Vector3(0.22f, 0.22f, 1.05f), weaponMetalMaterial);
            }
            else
            {
                CreatePrimitive(PrimitiveType.Cube, root, "Rifle", new Vector3(0.35f, 1.05f, 0.38f), new Vector3(0.12f, 0.12f, 0.95f), weaponMetalMaterial);
            }
        }

        private bool TryBuildImportedTankVisual(Transform root, UnitKind kind, Material teamMaterial)
        {
            string modelPath = GetTankModelResourcePath(kind);
            if (string.IsNullOrEmpty(modelPath))
            {
                return false;
            }

            GameObject modelPrefab = UnityEngine.Resources.Load<GameObject>(modelPath);
            if (modelPrefab == null)
            {
                return false;
            }

            GameObject model = Instantiate(modelPrefab, root);
            model.name = RtsBalance.GetUnit(kind).Name + " Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * GetTankModelScale(kind);

            foreach (Collider collider in model.GetComponentsInChildren<Collider>())
            {
                DestroyRuntimeObject(collider);
            }

            ConfigureImportedRenderers(model, GetImportedUnitMaterialProfile(kind));

            CreateTankReadabilityPanels(root, kind, teamMaterial);
            return true;
        }

        private void BuildFallbackTankVisual(Transform root, UnitKind kind, Material teamMaterial)
        {
            switch (kind)
            {
                case UnitKind.LightTank:
                    CreatePrimitive(PrimitiveType.Cube, root, "Light Hull", new Vector3(0f, 0.38f, 0f), new Vector3(1.45f, 0.46f, 1.85f), teamMaterial);
                    CreatePrimitive(PrimitiveType.Cube, root, "Light Turret", new Vector3(0f, 0.78f, 0.08f), new Vector3(0.8f, 0.28f, 0.72f), teamMaterial);
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Light Barrel", new Vector3(0f, 0.78f, 0.75f), new Vector3(0.12f, 0.58f, 0.12f), weaponMetalMaterial).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    break;
                case UnitKind.HeavyTank:
                    CreatePrimitive(PrimitiveType.Cube, root, "Heavy Hull", new Vector3(0f, 0.52f, 0f), new Vector3(2.2f, 0.72f, 2.95f), teamMaterial);
                    CreatePrimitive(PrimitiveType.Cube, root, "Heavy Turret", new Vector3(0f, 1.08f, 0.16f), new Vector3(1.35f, 0.46f, 1.15f), teamMaterial);
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Heavy Cannon A", new Vector3(-0.24f, 1.08f, 1.08f), new Vector3(0.15f, 0.86f, 0.15f), weaponMetalMaterial).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Heavy Cannon B", new Vector3(0.24f, 1.08f, 1.08f), new Vector3(0.15f, 0.86f, 0.15f), weaponMetalMaterial).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    break;
                default:
                    CreatePrimitive(PrimitiveType.Cube, root, "Medium Hull", new Vector3(0f, 0.45f, 0f), new Vector3(1.75f, 0.55f, 2.25f), teamMaterial);
                    CreatePrimitive(PrimitiveType.Cube, root, "Medium Turret", new Vector3(0f, 0.88f, 0.1f), new Vector3(1f, 0.36f, 0.9f), teamMaterial);
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Medium Barrel", new Vector3(0f, 0.9f, 0.9f), new Vector3(0.16f, 0.68f, 0.16f), weaponMetalMaterial).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    CreatePrimitive(PrimitiveType.Cube, root, "Passenger Platform", new Vector3(0.62f, 0.72f, -0.28f), new Vector3(0.38f, 0.08f, 0.48f), weaponMetalMaterial);
                    break;
            }
        }

        private void BuildFallbackWheeledVehicleVisual(Transform root, UnitKind kind, Material teamMaterial)
        {
            bool apc = RtsBalance.NormalizeUnitKind(kind) == UnitKind.Apc;
            Material hullMaterial = apc ? vehicleDetailMaterial : teamMaterial;
            Vector3 hullSize = apc ? new Vector3(1.75f, 0.56f, 2.55f) : new Vector3(1.48f, 0.42f, 2.05f);
            Vector3 cabinSize = apc ? new Vector3(1.25f, 0.48f, 1.2f) : new Vector3(1.02f, 0.42f, 0.92f);
            float roofY = apc ? 0.98f : 0.78f;
            float sideOffset = apc ? 0.82f : 0.68f;
            float frontZ = apc ? 0.92f : 0.74f;
            float rearZ = apc ? -0.98f : -0.74f;

            CreatePrimitive(PrimitiveType.Cube, root, apc ? "APC Armored Hull" : "Humvee Hull", new Vector3(0f, 0.42f, 0f), hullSize, hullMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, apc ? "APC Sloped Nose" : "Humvee Hood", new Vector3(0f, 0.58f, frontZ), new Vector3(hullSize.x * 0.78f, apc ? 0.3f : 0.24f, 0.62f), vehicleDetailMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, apc ? "APC Command Cabin" : "Humvee Cabin", new Vector3(0f, roofY, -0.12f), cabinSize, hullMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Wheeled Vehicle Windshield", new Vector3(0f, roofY + 0.03f, frontZ - 0.36f), new Vector3(cabinSize.x * 0.78f, 0.1f, 0.055f), sensorGlassMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Wheeled Vehicle Rear Window", new Vector3(0f, roofY + 0.02f, rearZ + 0.32f), new Vector3(cabinSize.x * 0.66f, 0.08f, 0.05f), sensorGlassMaterial);

            for (int side = -1; side <= 1; side += 2)
            {
                string label = side < 0 ? "Left" : "Right";
                float x = sideOffset * side;
                CreatePrimitive(PrimitiveType.Cube, root, label + " Door Armor", new Vector3(x, 0.68f, -0.05f), new Vector3(0.07f, 0.42f, apc ? 1.15f : 0.92f), teamMaterial);
                CreatePrimitive(PrimitiveType.Cube, root, label + " Door Glass", new Vector3(x, roofY + 0.04f, 0.08f), new Vector3(0.055f, 0.18f, 0.34f), sensorGlassMaterial);
                CreatePrimitive(PrimitiveType.Cube, root, label + " Rocker Shadow", new Vector3(x, 0.29f, -0.02f), new Vector3(0.08f, 0.14f, hullSize.z * 0.82f), shadowPanelMaterial);
                CreatePrimitive(PrimitiveType.Cube, root, label + " Front Wheel Arch", new Vector3(x, 0.42f, frontZ), new Vector3(0.08f, 0.34f, 0.52f), shadowPanelMaterial);
                CreatePrimitive(PrimitiveType.Cube, root, label + " Rear Wheel Arch", new Vector3(x, 0.42f, rearZ), new Vector3(0.08f, 0.34f, 0.52f), shadowPanelMaterial);
                CreatePrimitive(PrimitiveType.Cube, root, label + " Armor Rivet Rail", new Vector3(x, 0.9f, -0.06f), new Vector3(0.05f, 0.05f, hullSize.z * 0.66f), panelTrimMaterial);
            }

            CreatePrimitive(PrimitiveType.Cube, root, "Wheeled Vehicle Front Bumper", new Vector3(0f, 0.33f, hullSize.z * 0.54f), new Vector3(hullSize.x * 0.9f, 0.12f, 0.12f), weaponMetalMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Wheeled Vehicle Rear Bumper", new Vector3(0f, 0.33f, -hullSize.z * 0.54f), new Vector3(hullSize.x * 0.86f, 0.1f, 0.12f), weaponMetalMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Wheeled Vehicle Roof Team Plate", new Vector3(0f, roofY + cabinSize.y * 0.58f, -0.08f), new Vector3(cabinSize.x * 0.72f, 0.07f, cabinSize.z * 0.48f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Wheeled Vehicle Front Team Strip", new Vector3(0f, 0.72f, hullSize.z * 0.55f), new Vector3(hullSize.x * 0.52f, 0.07f, 0.08f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Wheeled Vehicle Hood Vent", new Vector3(-hullSize.x * 0.22f, 0.74f, frontZ + 0.1f), new Vector3(0.28f, 0.04f, 0.22f), darkMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Wheeled Vehicle Hood Vent Mirror", new Vector3(hullSize.x * 0.22f, 0.74f, frontZ + 0.1f), new Vector3(0.28f, 0.04f, 0.22f), darkMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Wheeled Vehicle Front Highlight", new Vector3(0f, 0.63f, hullSize.z * 0.56f), new Vector3(hullSize.x * 0.74f, 0.045f, 0.06f), edgeHighlightMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Wheeled Vehicle Rear Shadow", new Vector3(0f, 0.53f, -hullSize.z * 0.56f), new Vector3(hullSize.x * 0.68f, 0.07f, 0.06f), shadowPanelMaterial);
            CreatePrimitive(PrimitiveType.Sphere, root, "Wheeled Vehicle Left Headlamp", new Vector3(-hullSize.x * 0.32f, 0.5f, hullSize.z * 0.6f), new Vector3(0.07f, 0.04f, 0.07f), warningLightMaterial);
            CreatePrimitive(PrimitiveType.Sphere, root, "Wheeled Vehicle Right Headlamp", new Vector3(hullSize.x * 0.32f, 0.5f, hullSize.z * 0.6f), new Vector3(0.07f, 0.04f, 0.07f), warningLightMaterial);
            CreateBoltRow(root, "Wheeled Vehicle Front Armor Bolt", new Vector3(-hullSize.x * 0.34f, 0.58f, hullSize.z * 0.54f), new Vector3(hullSize.x * 0.11f, 0f, 0f), 7, new Vector3(0.032f, 0.024f, 0.026f), panelTrimMaterial);
            CreateVentSlats(root, "Wheeled Vehicle Engine Slat", new Vector3(-0.34f, 0.79f, frontZ + 0.31f), new Vector3(0.17f, 0f, 0f), 5, new Vector3(0.09f, 0.025f, 0.05f), panelTrimMaterial);

            if (apc)
            {
                CreatePrimitive(PrimitiveType.Cube, root, "APC Troop Hatch", new Vector3(0f, 1.29f, -0.48f), new Vector3(0.62f, 0.065f, 0.42f), panelTrimMaterial);
                CreatePrimitive(PrimitiveType.Cube, root, "APC Side Storage Box L", new Vector3(-0.9f, 0.63f, -0.56f), new Vector3(0.1f, 0.24f, 0.46f), vehicleDetailMaterial);
                CreatePrimitive(PrimitiveType.Cube, root, "APC Side Storage Box R", new Vector3(0.9f, 0.63f, -0.56f), new Vector3(0.1f, 0.24f, 0.46f), vehicleDetailMaterial);
                CreatePrimitive(PrimitiveType.Cube, root, "APC Rear Ramp Line", new Vector3(0f, 0.58f, -1.36f), new Vector3(1.18f, 0.05f, 0.045f), cautionStripeMaterial);
            }
            else
            {
                CreatePrimitive(PrimitiveType.Cube, root, "Humvee Spare Tire Mount", new Vector3(0f, 0.64f, -1.1f), new Vector3(0.48f, 0.12f, 0.12f), trackRubberMaterial);
                CreatePrimitive(PrimitiveType.Cube, root, "Humvee Antenna Base", new Vector3(-0.48f, 1.13f, -0.62f), new Vector3(0.05f, 0.48f, 0.05f), weaponMetalMaterial);
                CreatePrimitive(PrimitiveType.Sphere, root, "Humvee Antenna Tip", new Vector3(-0.48f, 1.42f, -0.62f), new Vector3(0.045f, 0.045f, 0.045f), sensorGlassMaterial);
            }
        }

        private void BuildUnitMotionRig(Transform root, UnitKind kind)
        {
            kind = RtsBalance.NormalizeUnitKind(kind);
            if (RtsBalance.IsInfantry(kind))
            {
                CreateInfantryMotionRig(root);
                return;
            }

            if (RtsBalance.IsTank(kind))
            {
                CreateVehicleTrackRig(root, kind);
                CreateTankTurretMotionRig(root, kind);
                return;
            }

            if (RtsBalance.IsWheeledCombatVehicle(kind))
            {
                CreateVehicleWheelRig(root, kind);
                CreateTankTurretMotionRig(root, kind);
                return;
            }

            if (kind == UnitKind.Harvester)
            {
                CreateVehicleTrackRig(root, kind);
                CreateHarvesterHarvestRig(root);
            }
        }

        private void CreateInfantryMotionRig(Transform root)
        {
            CreatePrimitive(PrimitiveType.Cube, root, "Walk Leg L", new Vector3(-0.13f, 0.34f, -0.03f), new Vector3(0.12f, 0.52f, 0.12f), darkMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Walk Leg R", new Vector3(0.13f, 0.34f, -0.03f), new Vector3(0.12f, 0.52f, 0.12f), darkMaterial);
        }

        private void CreateVehicleWheelRig(Transform root, UnitKind kind)
        {
            GetVehicleWheelLayout(kind, out float sideOffset, out float centerY, out float forwardZ, out float wheelRadius, out float wheelThickness);
            CreateWheel(root, "Roll Wheel LF", new Vector3(-sideOffset, centerY, forwardZ), wheelRadius, wheelThickness);
            CreateWheel(root, "Roll Wheel RF", new Vector3(sideOffset, centerY, forwardZ), wheelRadius, wheelThickness);
            CreateWheel(root, "Roll Wheel LR", new Vector3(-sideOffset, centerY, -forwardZ), wheelRadius, wheelThickness);
            CreateWheel(root, "Roll Wheel RR", new Vector3(sideOffset, centerY, -forwardZ), wheelRadius, wheelThickness);
        }

        private void CreateHarvesterHarvestRig(Transform root)
        {
            GameObject collector = CreatePrimitive(
                PrimitiveType.Cylinder,
                root,
                "Harvest Motion Collector",
                new Vector3(0f, 0.34f, 1.2f),
                new Vector3(0.46f, 0.08f, 0.46f),
                resourceMaterial);
            collector.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            CreatePrimitive(
                PrimitiveType.Cube,
                root,
                "Harvest Motion Intake Arm",
                new Vector3(0f, 0.46f, 0.95f),
                new Vector3(0.18f, 0.1f, 0.7f),
                vehicleDetailMaterial);

            CreatePrimitive(
                PrimitiveType.Cube,
                root,
                "Harvest Motion Glow Bar",
                new Vector3(0f, 0.42f, 1.36f),
                new Vector3(0.82f, 0.045f, 0.08f),
                resourceMaterial);
        }

        private void CreateVehicleTrackRig(Transform root, UnitKind kind)
        {
            GetVehicleTrackLayout(kind, out float sideOffset, out float centerY, out float trackLength, out float trackHeight, out float trackWidth);
            for (int side = -1; side <= 1; side += 2)
            {
                string suffix = side < 0 ? "L" : "R";
                float x = sideOffset * side;
                CreatePrimitive(
                    PrimitiveType.Cube,
                    root,
                    "Track Belt " + suffix,
                    new Vector3(x, centerY, 0f),
                    new Vector3(trackWidth, trackHeight, trackLength),
                    trackRubberMaterial);

                CreatePrimitive(
                    PrimitiveType.Cube,
                    root,
                    "Track Guard " + suffix,
                    new Vector3(x, centerY + trackHeight * 0.58f, 0f),
                    new Vector3(trackWidth * 1.08f, trackHeight * 0.18f, trackLength * 0.92f),
                    vehicleDetailMaterial);

                for (int i = 0; i < 8; i++)
                {
                    float z = Mathf.Lerp(-trackLength * 0.38f, trackLength * 0.38f, i / 7f);
                    CreatePrimitive(
                        PrimitiveType.Cube,
                        root,
                        "Track Tread " + suffix + " " + i,
                        new Vector3(x, centerY - trackHeight * 0.04f, z),
                        new Vector3(trackWidth * 1.18f, trackHeight * 0.22f, trackLength * 0.07f),
                        trackRubberMaterial);
                }
            }
        }

        private void CreateWheel(Transform root, string name, Vector3 localPosition, float radius, float thickness)
        {
            GameObject wheel = new GameObject(name);
            wheel.transform.SetParent(root, false);
            wheel.transform.localPosition = localPosition;
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            string suffix = name.Replace("Roll Wheel ", string.Empty);
            CreatePrimitive(PrimitiveType.Cylinder, wheel.transform, "Tire " + suffix, Vector3.zero, new Vector3(radius, thickness, radius), trackRubberMaterial);
            CreatePrimitive(PrimitiveType.Cylinder, wheel.transform, "Metal Hub " + suffix, new Vector3(0f, thickness * 0.04f, 0f), new Vector3(radius * 0.46f, thickness * 1.08f, radius * 0.46f), panelTrimMaterial);
            CreatePrimitive(PrimitiveType.Cylinder, wheel.transform, "Inner Rim " + suffix, new Vector3(0f, thickness * 0.08f, 0f), new Vector3(radius * 0.72f, thickness * 0.28f, radius * 0.72f), weaponMetalMaterial);
            CreatePrimitive(PrimitiveType.Cube, wheel.transform, "Tread Crossbar A " + suffix, new Vector3(0f, 0f, radius * 0.72f), new Vector3(radius * 1.36f, thickness * 1.1f, radius * 0.08f), shadowPanelMaterial);
            CreatePrimitive(PrimitiveType.Cube, wheel.transform, "Tread Crossbar B " + suffix, new Vector3(radius * 0.72f, 0f, 0f), new Vector3(radius * 0.08f, thickness * 1.1f, radius * 1.36f), shadowPanelMaterial);
        }

        private void CreateTankTurretMotionRig(Transform root, UnitKind kind)
        {
            GetTankTurretRigLayout(kind, out Vector3 pivotPosition, out Vector3 capSize, out Vector3 barrelPosition, out Vector3 barrelScale);
            GameObject pivot = new GameObject("Animated Turret Pivot");
            pivot.transform.SetParent(root, false);
            pivot.transform.localPosition = pivotPosition;
            pivot.transform.localRotation = Quaternion.identity;

            CreatePrimitive(PrimitiveType.Cube, pivot.transform, "Animated Turret Cap", Vector3.zero, capSize, vehicleDetailMaterial);
            GameObject barrel = CreatePrimitive(PrimitiveType.Cylinder, pivot.transform, "Animated Turret Barrel", barrelPosition, barrelScale, weaponMetalMaterial);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            if (RtsBalance.IsWheeledCombatVehicle(kind))
            {
                CreatePrimitive(PrimitiveType.Cube, pivot.transform, "Animated Turret Shield", new Vector3(0f, 0.02f, 0.16f), new Vector3(capSize.x * 0.82f, capSize.y * 0.78f, 0.08f), shadowPanelMaterial);
                GameObject secondBarrel = CreatePrimitive(PrimitiveType.Cylinder, pivot.transform, "Animated Turret Barrel Twin", barrelPosition + new Vector3(capSize.x * 0.22f, 0f, 0f), barrelScale * 0.78f, weaponMetalMaterial);
                secondBarrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                GameObject firstBarrel = barrel;
                firstBarrel.transform.localPosition = barrelPosition - new Vector3(capSize.x * 0.22f, 0f, 0f);
            }

            GameObject muzzle = new GameObject("Animated Turret Muzzle");
            muzzle.transform.SetParent(pivot.transform, false);
            muzzle.transform.localPosition = barrelPosition + new Vector3(0f, 0f, barrelScale.y * 0.96f);
        }

        private static void GetVehicleWheelLayout(UnitKind kind, out float sideOffset, out float centerY, out float forwardZ, out float wheelRadius, out float wheelThickness)
        {
            switch (RtsBalance.NormalizeUnitKind(kind))
            {
                case UnitKind.Humvee:
                    sideOffset = 0.72f;
                    centerY = 0.34f;
                    forwardZ = 0.82f;
                    wheelRadius = 0.23f;
                    wheelThickness = 0.16f;
                    return;
                case UnitKind.Apc:
                    sideOffset = 0.86f;
                    centerY = 0.36f;
                    forwardZ = 0.98f;
                    wheelRadius = 0.27f;
                    wheelThickness = 0.18f;
                    return;
                case UnitKind.LightTank:
                    sideOffset = 0.68f;
                    centerY = 0.32f;
                    forwardZ = 0.68f;
                    wheelRadius = 0.2f;
                    wheelThickness = 0.14f;
                    return;
                case UnitKind.HeavyTank:
                    sideOffset = 1.08f;
                    centerY = 0.42f;
                    forwardZ = 1.08f;
                    wheelRadius = 0.28f;
                    wheelThickness = 0.18f;
                    return;
                case UnitKind.Harvester:
                    sideOffset = 0.78f;
                    centerY = 0.34f;
                    forwardZ = 0.76f;
                    wheelRadius = 0.24f;
                    wheelThickness = 0.16f;
                    return;
                default:
                    sideOffset = 0.84f;
                    centerY = 0.36f;
                    forwardZ = 0.86f;
                    wheelRadius = 0.24f;
                    wheelThickness = 0.16f;
                    return;
            }
        }

        private static void GetVehicleTrackLayout(UnitKind kind, out float sideOffset, out float centerY, out float trackLength, out float trackHeight, out float trackWidth)
        {
            switch (RtsBalance.NormalizeUnitKind(kind))
            {
                case UnitKind.Harvester:
                    sideOffset = 0.72f;
                    centerY = 0.3f;
                    trackLength = 1.92f;
                    trackHeight = 0.32f;
                    trackWidth = 0.28f;
                    return;
                case UnitKind.LightTank:
                    sideOffset = 0.62f;
                    centerY = 0.31f;
                    trackLength = 1.65f;
                    trackHeight = 0.34f;
                    trackWidth = 0.28f;
                    return;
                case UnitKind.HeavyTank:
                    sideOffset = 1.02f;
                    centerY = 0.42f;
                    trackLength = 2.75f;
                    trackHeight = 0.48f;
                    trackWidth = 0.42f;
                    return;
                default:
                    sideOffset = 0.78f;
                    centerY = 0.35f;
                    trackLength = 2.12f;
                    trackHeight = 0.4f;
                    trackWidth = 0.34f;
                    return;
            }
        }

        private static void GetTankTurretRigLayout(UnitKind kind, out Vector3 pivotPosition, out Vector3 capSize, out Vector3 barrelPosition, out Vector3 barrelScale)
        {
            switch (RtsBalance.NormalizeUnitKind(kind))
            {
                case UnitKind.Humvee:
                    pivotPosition = new Vector3(0f, 1.08f, -0.14f);
                    capSize = new Vector3(0.44f, 0.13f, 0.36f);
                    barrelPosition = new Vector3(0f, 0f, 0.38f);
                    barrelScale = new Vector3(0.045f, 0.46f, 0.045f);
                    return;
                case UnitKind.Apc:
                    pivotPosition = new Vector3(0f, 1.32f, -0.1f);
                    capSize = new Vector3(0.58f, 0.16f, 0.42f);
                    barrelPosition = new Vector3(0f, 0f, 0.44f);
                    barrelScale = new Vector3(0.055f, 0.52f, 0.055f);
                    return;
                case UnitKind.LightTank:
                    pivotPosition = new Vector3(0f, 0.9f, 0.05f);
                    capSize = new Vector3(0.62f, 0.16f, 0.48f);
                    barrelPosition = new Vector3(0f, 0f, 0.42f);
                    barrelScale = new Vector3(0.075f, 0.48f, 0.075f);
                    return;
                case UnitKind.HeavyTank:
                    pivotPosition = new Vector3(0f, 1.24f, 0.08f);
                    capSize = new Vector3(1.05f, 0.22f, 0.78f);
                    barrelPosition = new Vector3(0f, 0f, 0.68f);
                    barrelScale = new Vector3(0.1f, 0.74f, 0.1f);
                    return;
                default:
                    pivotPosition = new Vector3(0f, 1.02f, 0.08f);
                    capSize = new Vector3(0.82f, 0.18f, 0.62f);
                    barrelPosition = new Vector3(0f, 0f, 0.54f);
                    barrelScale = new Vector3(0.088f, 0.6f, 0.088f);
                    return;
            }
        }

        private static string GetTankModelResourcePath(UnitKind kind)
        {
            switch (kind)
            {
                case UnitKind.LightTank:
                    return "UnitModels/BastionLightTank/Meshes/Bastion_Tank_Static";
                case UnitKind.MediumTank:
                    return "UnitModels/BastionMediumTank/Meshes/Bastion_MT12_Static";
                case UnitKind.HeavyTank:
                    return "UnitModels/BastionHeavyTank/Meshes/Bastion_HT77_Static";
                default:
                    return string.Empty;
            }
        }

        private static string GetInfantryModelResourcePath(UnitKind kind)
        {
            switch (kind)
            {
                case UnitKind.Grenadier:
                    return "UnitModels/BastionInfantry/Meshes/Bastion_Grenadier_Static";
                case UnitKind.RocketSoldier:
                    return "UnitModels/BastionInfantry/Meshes/Bastion_RocketSoldier_Static";
                case UnitKind.FlameTrooper:
                    return "UnitModels/BastionInfantry/Meshes/Bastion_FlameTrooper_Static";
                case UnitKind.Engineer:
                    return "UnitModels/BastionInfantry/Meshes/Bastion_Engineer_Static";
                default:
                    return "UnitModels/BastionInfantry/Meshes/Bastion_Gunner_Static";
            }
        }

        private static float GetInfantryModelScale(UnitKind kind)
        {
            return 0.86f;
        }

        private static float GetTankModelScale(UnitKind kind)
        {
            switch (kind)
            {
                case UnitKind.LightTank:
                    return 0.31f;
                case UnitKind.HeavyTank:
                    return 0.32f;
                default:
                    return 0.315f;
            }
        }

        private static Vector3 GetTankColliderCenter(UnitKind kind)
        {
            switch (kind)
            {
                case UnitKind.LightTank:
                    return new Vector3(0f, 0.48f, 0f);
                case UnitKind.HeavyTank:
                    return new Vector3(0f, 0.72f, 0f);
                default:
                    return new Vector3(0f, 0.6f, 0f);
            }
        }

        private static Vector3 GetTankColliderSize(UnitKind kind)
        {
            switch (kind)
            {
                case UnitKind.LightTank:
                    return new Vector3(1.45f, 0.95f, 2.1f);
                case UnitKind.HeavyTank:
                    return new Vector3(2.25f, 1.4f, 3.35f);
                default:
                    return new Vector3(1.75f, 1.2f, 2.55f);
            }
        }

        private static Vector3 GetWheeledVehicleColliderCenter(UnitKind kind)
        {
            switch (RtsBalance.NormalizeUnitKind(kind))
            {
                case UnitKind.Apc:
                    return new Vector3(0f, 0.68f, 0f);
                default:
                    return new Vector3(0f, 0.58f, 0f);
            }
        }

        private static Vector3 GetWheeledVehicleColliderSize(UnitKind kind)
        {
            switch (RtsBalance.NormalizeUnitKind(kind))
            {
                case UnitKind.Apc:
                    return new Vector3(1.85f, 1.28f, 2.75f);
                default:
                    return new Vector3(1.55f, 1.08f, 2.2f);
            }
        }

        private static Vector3 GetTankRecognitionStripSize(UnitKind kind)
        {
            switch (kind)
            {
                case UnitKind.LightTank:
                    return new Vector3(0.9f, 0.08f, 0.16f);
                case UnitKind.HeavyTank:
                    return new Vector3(1.35f, 0.1f, 0.22f);
                default:
                    return new Vector3(1.05f, 0.09f, 0.18f);
            }
        }

        private void CreateInfantryReadabilityPanels(Transform root, Material teamMaterial)
        {
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Team Band", new Vector3(0f, 1.14f, -0.17f), new Vector3(0.46f, 0.08f, 0.1f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Team Top Plate", new Vector3(0f, 1.52f, -0.02f), new Vector3(0.32f, 0.045f, 0.24f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Kit Detail Plate", new Vector3(0f, 0.78f, 0.2f), new Vector3(0.3f, 0.06f, 0.18f), vehicleDetailMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Visor Lens", new Vector3(0f, 1.54f, 0.23f), new Vector3(0.28f, 0.055f, 0.045f), sensorGlassMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Left Shoulder Highlight", new Vector3(-0.26f, 1.16f, 0.04f), new Vector3(0.16f, 0.055f, 0.18f), edgeHighlightMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Right Shoulder Highlight", new Vector3(0.26f, 1.16f, 0.04f), new Vector3(0.16f, 0.055f, 0.18f), edgeHighlightMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Chest Trim Bar", new Vector3(0f, 1.02f, 0.24f), new Vector3(0.28f, 0.035f, 0.045f), panelTrimMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Backpack Vent", new Vector3(0f, 1.08f, -0.26f), new Vector3(0.26f, 0.18f, 0.045f), darkMaterial);
            CreateBoltRow(root, "Infantry Chest Fastener", new Vector3(-0.13f, 0.94f, 0.245f), new Vector3(0.13f, 0f, 0f), 3, new Vector3(0.035f, 0.035f, 0.025f), panelTrimMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Left Boot Shadow", new Vector3(-0.13f, 0.08f, 0.02f), new Vector3(0.18f, 0.06f, 0.24f), shadowPanelMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Right Boot Shadow", new Vector3(0.13f, 0.08f, 0.02f), new Vector3(0.18f, 0.06f, 0.24f), shadowPanelMaterial);
        }

        private void CreateHarvesterReadabilityPanels(Transform root, Material teamMaterial)
        {
            CreatePrimitive(PrimitiveType.Cube, root, "Harvester Team Strip", new Vector3(0f, 0.86f, -0.78f), new Vector3(1.08f, 0.1f, 0.16f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Harvester Team Roof Plate", new Vector3(0f, 1.02f, -0.1f), new Vector3(0.92f, 0.08f, 0.62f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Harvester Team Left Plate", new Vector3(-0.72f, 0.58f, -0.04f), new Vector3(0.08f, 0.24f, 0.9f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Harvester Team Right Plate", new Vector3(0.72f, 0.58f, -0.04f), new Vector3(0.08f, 0.24f, 0.9f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Harvester Armor Detail Plate", new Vector3(0f, 0.48f, 0.72f), new Vector3(1.08f, 0.08f, 0.32f), vehicleDetailMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Harvester Cab Glass", new Vector3(0f, 0.9f, 0.92f), new Vector3(0.72f, 0.09f, 0.08f), sensorGlassMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Harvester Front Edge Highlight", new Vector3(0f, 0.84f, 1.0f), new Vector3(1.18f, 0.055f, 0.08f), edgeHighlightMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Harvester Rear Recess Shadow", new Vector3(0f, 0.46f, -1.05f), new Vector3(1.2f, 0.12f, 0.08f), shadowPanelMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Harvester Left Lower Shadow", new Vector3(-0.76f, 0.32f, 0.06f), new Vector3(0.06f, 0.16f, 1.28f), shadowPanelMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Harvester Right Lower Shadow", new Vector3(0.76f, 0.32f, 0.06f), new Vector3(0.06f, 0.16f, 1.28f), shadowPanelMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Harvester Caution Intake Strip", new Vector3(0f, 0.36f, 1.16f), new Vector3(0.82f, 0.045f, 0.07f), cautionStripeMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Harvester Cargo Dark Trough", new Vector3(0f, 0.9f, 0.44f), new Vector3(0.96f, 0.12f, 0.74f), shadowPanelMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Harvester Left Vent Stack", new Vector3(-0.74f, 0.78f, 0.36f), new Vector3(0.04f, 0.34f, 0.46f), darkMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Harvester Right Vent Stack", new Vector3(0.74f, 0.78f, 0.36f), new Vector3(0.04f, 0.34f, 0.46f), darkMaterial);
            CreateVentSlats(root, "Harvester Left Vent Slat", new Vector3(-0.765f, 0.68f, 0.18f), new Vector3(0f, 0.08f, 0.09f), 4, new Vector3(0.045f, 0.025f, 0.12f), panelTrimMaterial);
            CreateVentSlats(root, "Harvester Right Vent Slat", new Vector3(0.765f, 0.68f, 0.18f), new Vector3(0f, 0.08f, 0.09f), 4, new Vector3(0.045f, 0.025f, 0.12f), panelTrimMaterial);
            CreatePrimitive(PrimitiveType.Sphere, root, "Harvester Amber Status Light", new Vector3(0.42f, 0.92f, 1.0f), new Vector3(0.09f, 0.05f, 0.09f), warningLightMaterial);
            CreateBoltRow(root, "Harvester Cargo Rail Bolt", new Vector3(-0.42f, 1.0f, 0.02f), new Vector3(0.14f, 0f, 0f), 7, new Vector3(0.035f, 0.025f, 0.035f), panelTrimMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Cargo Fill Ore Mass", new Vector3(0f, 1.02f, 0.44f), new Vector3(0.82f, 0.26f, 0.62f), resourceMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Cargo Fill Ore Crest", new Vector3(0f, 1.18f, 0.44f), new Vector3(0.58f, 0.1f, 0.38f), resourceGlowMaterial);
            CreatePrimitive(PrimitiveType.Sphere, root, "Cargo Fill Ore Glint L", new Vector3(-0.28f, 1.2f, 0.27f), new Vector3(0.12f, 0.06f, 0.12f), resourceGlowMaterial);
            CreatePrimitive(PrimitiveType.Sphere, root, "Cargo Fill Ore Glint R", new Vector3(0.28f, 1.2f, 0.58f), new Vector3(0.1f, 0.05f, 0.1f), resourceGlowMaterial);
        }

        private void CreateTankReadabilityPanels(Transform root, UnitKind kind, Material teamMaterial)
        {
            Vector3 roofPosition;
            Vector3 roofSize;
            Vector3 sideSize;
            float sideOffset;

            switch (kind)
            {
                case UnitKind.LightTank:
                    roofPosition = new Vector3(0f, 0.74f, -0.02f);
                    roofSize = new Vector3(0.74f, 0.075f, 0.5f);
                    sideSize = new Vector3(0.07f, 0.18f, 0.76f);
                    sideOffset = 0.62f;
                    break;
                case UnitKind.HeavyTank:
                    roofPosition = new Vector3(0f, 1.08f, 0.02f);
                    roofSize = new Vector3(1.08f, 0.095f, 0.76f);
                    sideSize = new Vector3(0.09f, 0.26f, 1.18f);
                    sideOffset = 0.98f;
                    break;
                default:
                    roofPosition = new Vector3(0f, 0.9f, 0f);
                    roofSize = new Vector3(0.9f, 0.085f, 0.62f);
                    sideSize = new Vector3(0.08f, 0.22f, 0.96f);
                    sideOffset = 0.78f;
                    break;
            }

            CreatePrimitive(PrimitiveType.Cube, root, "Team Recognition Strip", new Vector3(0f, 0.82f, -0.55f), GetTankRecognitionStripSize(kind), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Tank Team Roof Plate", roofPosition, roofSize, teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Tank Team Left Plate", new Vector3(-sideOffset, 0.52f, -0.05f), sideSize, teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Tank Team Right Plate", new Vector3(sideOffset, 0.52f, -0.05f), sideSize, teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Tank Armor Detail Plate", new Vector3(0f, 0.46f, 0.56f), new Vector3(roofSize.x * 0.82f, 0.07f, 0.22f), vehicleDetailMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Tank Sensor Optic", roofPosition + new Vector3(roofSize.x * 0.28f, 0.12f, 0.33f), new Vector3(0.16f, 0.06f, 0.05f), sensorGlassMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Tank Front Edge Highlight", new Vector3(0f, roofPosition.y - 0.18f, 1.03f), new Vector3(roofSize.x * 1.2f, 0.055f, 0.08f), edgeHighlightMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Tank Rear Recess Shadow", new Vector3(0f, 0.42f, -1.04f), new Vector3(roofSize.x * 1.08f, 0.09f, 0.08f), shadowPanelMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Tank Left Track Shadow", new Vector3(-sideOffset, 0.26f, 0f), new Vector3(0.08f, 0.2f, sideSize.z * 1.28f), shadowPanelMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Tank Right Track Shadow", new Vector3(sideOffset, 0.26f, 0f), new Vector3(0.08f, 0.2f, sideSize.z * 1.28f), shadowPanelMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Tank Turret Edge Highlight", roofPosition + new Vector3(0f, 0.12f, 0.26f), new Vector3(roofSize.x * 0.72f, 0.045f, 0.07f), edgeHighlightMaterial);
            CreatePrimitive(PrimitiveType.Cylinder, root, "Tank Turret Hatch Detail", roofPosition + new Vector3(-roofSize.x * 0.23f, 0.13f, -0.04f), new Vector3(0.16f, 0.035f, 0.16f), panelTrimMaterial);
            CreatePrimitive(PrimitiveType.Sphere, root, "Tank Amber Hull Light", new Vector3(-roofSize.x * 0.45f, roofPosition.y - 0.2f, 0.94f), new Vector3(0.07f, 0.04f, 0.07f), warningLightMaterial);
            CreateBoltRow(root, "Tank Front Armor Bolt", new Vector3(-roofSize.x * 0.36f, 0.51f, 0.69f), new Vector3(roofSize.x * 0.12f, 0f, 0f), 7, new Vector3(0.035f, 0.025f, 0.035f), panelTrimMaterial);
            CreateVentSlats(root, "Tank Left Engine Vent", new Vector3(-sideOffset - 0.015f, 0.56f, -0.48f), new Vector3(0f, 0f, 0.13f), 4, new Vector3(0.045f, 0.035f, 0.085f), darkMaterial);
            CreateVentSlats(root, "Tank Right Engine Vent", new Vector3(sideOffset + 0.015f, 0.56f, -0.48f), new Vector3(0f, 0f, 0.13f), 4, new Vector3(0.045f, 0.035f, 0.085f), darkMaterial);
        }

        private Transform BuildStructureVisual(Transform root, StructureKind kind, RtsTeam team)
        {
            Material teamMaterial = GetTeamMaterial(team);
            StructureStats stats = RtsBalance.GetStructure(kind);

            Transform importedHead;
            if (TryBuildImportedStructureVisual(root, kind, teamMaterial, out importedHead))
            {
                if (IsDefenseStructure(kind))
                {
                    return CreateDefenseTurretRig(root, kind, teamMaterial);
                }

                return importedHead;
            }

            switch (kind)
            {
                case StructureKind.Refinery:
                    CreatePrimitive(PrimitiveType.Cube, root, "Refinery Base", new Vector3(0f, 0.55f, 0f), new Vector3(4.8f, 1.1f, 4.2f), teamMaterial);
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Silo A", new Vector3(-1.25f, 1.45f, -0.7f), new Vector3(0.75f, 1.45f, 0.75f), neutralMaterial);
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Silo B", new Vector3(1.1f, 1.25f, -0.6f), new Vector3(0.65f, 1.2f, 0.65f), neutralMaterial);
                    break;
                case StructureKind.Barracks:
                    CreatePrimitive(PrimitiveType.Cube, root, "Barracks", new Vector3(0f, 0.5f, 0f), new Vector3(3.6f, 1f, 3f), teamMaterial);
                    CreatePrimitive(PrimitiveType.Cube, root, "Roof", new Vector3(0f, 1.12f, 0f), new Vector3(3.9f, 0.28f, 3.3f), neutralMaterial);
                    break;
                case StructureKind.WarFactory:
                    CreatePrimitive(PrimitiveType.Cube, root, "Factory", new Vector3(0f, 0.75f, 0f), new Vector3(5.8f, 1.5f, 4.8f), teamMaterial);
                    CreatePrimitive(PrimitiveType.Cube, root, "Door", new Vector3(0f, 0.55f, 2.45f), new Vector3(2.2f, 0.9f, 0.12f), darkMaterial);
                    break;
                case StructureKind.PowerPlant:
                    CreatePrimitive(PrimitiveType.Cube, root, "Generator", new Vector3(0f, 0.6f, 0f), new Vector3(3f, 1.2f, 2.6f), teamMaterial);
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Stack", new Vector3(0.8f, 1.7f, 0f), new Vector3(0.48f, 1.2f, 0.48f), neutralMaterial);
                    break;
                case StructureKind.Turret:
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Turret Base", new Vector3(0f, 0.4f, 0f), new Vector3(1.5f, 0.42f, 1.5f), teamMaterial);
                    GameObject head = CreatePrimitive(PrimitiveType.Cube, root, "Turret Head", new Vector3(0f, 1.03f, 0f), new Vector3(1.25f, 0.55f, 1.25f), teamMaterial);
                    CreatePrimitive(PrimitiveType.Cylinder, head.transform, "Turret Barrel", new Vector3(0f, 0f, 0.9f), new Vector3(0.18f, 0.75f, 0.18f), weaponMetalMaterial).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    return head.transform;
                case StructureKind.GunTower:
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Gun Tower Base", new Vector3(0f, 1.2f, 0f), new Vector3(1.45f, 1.2f, 1.45f), teamMaterial);
                    GameObject gunHead = CreatePrimitive(PrimitiveType.Cube, root, "Gun Tower Head", new Vector3(0f, 2.65f, 0f), new Vector3(1.45f, 0.7f, 1.25f), teamMaterial);
                    CreatePrimitive(PrimitiveType.Cylinder, gunHead.transform, "Gun Tower Barrel A", new Vector3(-0.22f, 0f, 0.95f), new Vector3(0.13f, 0.82f, 0.13f), weaponMetalMaterial).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    CreatePrimitive(PrimitiveType.Cylinder, gunHead.transform, "Gun Tower Barrel B", new Vector3(0.22f, 0f, 0.95f), new Vector3(0.13f, 0.82f, 0.13f), weaponMetalMaterial).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    return gunHead.transform;
                case StructureKind.AdvancedGunTower:
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Advanced Gun Tower Base", new Vector3(0f, 1.6f, 0f), new Vector3(1.7f, 1.6f, 1.7f), teamMaterial);
                    GameObject advancedHead = CreatePrimitive(PrimitiveType.Cube, root, "Advanced Gun Tower Head", new Vector3(0f, 3.5f, 0f), new Vector3(1.9f, 0.95f, 1.55f), teamMaterial);
                    CreatePrimitive(PrimitiveType.Cube, advancedHead.transform, "Missile Pod A", new Vector3(-0.46f, 0f, 0.72f), new Vector3(0.42f, 0.42f, 0.52f), weaponMetalMaterial);
                    CreatePrimitive(PrimitiveType.Cube, advancedHead.transform, "Missile Pod B", new Vector3(0.46f, 0f, 0.72f), new Vector3(0.42f, 0.42f, 0.52f), weaponMetalMaterial);
                    return advancedHead.transform;
                default:
                    CreatePrimitive(PrimitiveType.Cube, root, "Command Center", new Vector3(0f, 0.85f, 0f), new Vector3(stats.FootprintRadius * 1.65f, 1.7f, stats.FootprintRadius * 1.55f), teamMaterial);
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Radar", new Vector3(0f, 2.25f, -0.2f), new Vector3(0.95f, 0.12f, 0.95f), neutralMaterial);
                    break;
            }

            return null;
        }

        private Transform CreateDefenseTurretRig(Transform root, StructureKind kind, Material teamMaterial)
        {
            GetDefenseTurretRigLayout(kind, out Vector3 pivotPosition, out Vector3 headSize, out Vector3 barrelPosition, out Vector3 barrelScale);
            GameObject pivot = new GameObject("Animated Defense Head");
            pivot.transform.SetParent(root, false);
            pivot.transform.localPosition = pivotPosition;
            pivot.transform.localRotation = Quaternion.identity;

            CreatePrimitive(PrimitiveType.Cube, pivot.transform, "Animated Defense Turret Cap", Vector3.zero, headSize, teamMaterial);
            if (kind == StructureKind.AdvancedGunTower)
            {
                CreatePrimitive(PrimitiveType.Cube, pivot.transform, "Animated Defense Missile Pod L", barrelPosition + new Vector3(-0.28f, 0f, 0f), new Vector3(0.28f, 0.24f, 0.55f), weaponMetalMaterial);
                CreatePrimitive(PrimitiveType.Cube, pivot.transform, "Animated Defense Missile Pod R", barrelPosition + new Vector3(0.28f, 0f, 0f), new Vector3(0.28f, 0.24f, 0.55f), weaponMetalMaterial);
            }
            else
            {
                GameObject barrel = CreatePrimitive(PrimitiveType.Cylinder, pivot.transform, "Animated Defense Barrel", barrelPosition, barrelScale, weaponMetalMaterial);
                barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            return pivot.transform;
        }

        private static void GetDefenseTurretRigLayout(StructureKind kind, out Vector3 pivotPosition, out Vector3 headSize, out Vector3 barrelPosition, out Vector3 barrelScale)
        {
            switch (kind)
            {
                case StructureKind.GunTower:
                    pivotPosition = new Vector3(0f, 2.85f, 0f);
                    headSize = new Vector3(1.25f, 0.38f, 0.95f);
                    barrelPosition = new Vector3(0f, 0f, 0.82f);
                    barrelScale = new Vector3(0.1f, 0.74f, 0.1f);
                    return;
                case StructureKind.AdvancedGunTower:
                    pivotPosition = new Vector3(0f, 3.65f, 0f);
                    headSize = new Vector3(1.45f, 0.46f, 1.05f);
                    barrelPosition = new Vector3(0f, 0f, 0.72f);
                    barrelScale = new Vector3(0.12f, 0.7f, 0.12f);
                    return;
                default:
                    pivotPosition = new Vector3(0f, 1.16f, 0f);
                    headSize = new Vector3(1.1f, 0.32f, 0.9f);
                    barrelPosition = new Vector3(0f, 0f, 0.78f);
                    barrelScale = new Vector3(0.12f, 0.68f, 0.12f);
                    return;
            }
        }

        private bool TryBuildImportedStructureVisual(Transform root, StructureKind kind, Material teamMaterial, out Transform head)
        {
            head = null;
            string modelPath = GetStructureModelResourcePath(kind);
            if (string.IsNullOrEmpty(modelPath))
            {
                return false;
            }

            GameObject modelPrefab = UnityEngine.Resources.Load<GameObject>(modelPath);
            if (modelPrefab == null)
            {
                return false;
            }

            GameObject model = Instantiate(modelPrefab, root);
            model.name = RtsBalance.GetStructure(kind).Name + " Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * GetStructureModelScale(kind);

            foreach (Collider collider in model.GetComponentsInChildren<Collider>())
            {
                DestroyRuntimeObject(collider);
            }

            ConfigureImportedRenderers(model, GetImportedStructureMaterialProfile(kind));

            CreateStructureReadabilityPanels(root, kind, teamMaterial);
            return true;
        }

        private struct ImportedRendererMaterialProfile
        {
            public ImportedRendererMaterialProfile(Color materialBoost, float metallic, float smoothness, float occlusion)
            {
                MaterialBoost = materialBoost;
                Metallic = metallic;
                Smoothness = smoothness;
                Occlusion = occlusion;
            }

            public Color MaterialBoost;
            public float Metallic;
            public float Smoothness;
            public float Occlusion;
        }

        private static void ConfigureImportedRenderers(GameObject model, ImportedRendererMaterialProfile profile)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.GetPropertyBlock(block);
                Color tint = ResolveImportedRendererTint(renderer, profile.MaterialBoost);
                block.SetColor("_Color", tint);
                block.SetColor("_BaseColor", tint);
                block.SetFloat("_Metallic", profile.Metallic);
                block.SetFloat("_Glossiness", profile.Smoothness);
                block.SetFloat("_Smoothness", profile.Smoothness);
                block.SetFloat("_OcclusionStrength", profile.Occlusion);
                renderer.SetPropertyBlock(block);
                block.Clear();
            }
        }

        private static Color ResolveImportedRendererTint(Renderer renderer, Color materialBoost)
        {
            Material material = renderer != null ? renderer.sharedMaterial : null;
            bool hasPaletteTexture = material != null && material.mainTexture != null;
            if (hasPaletteTexture)
            {
                return new Color(
                    Mathf.Clamp(1f + (materialBoost.r - 1f) * 0.38f, 0.92f, 1.16f),
                    Mathf.Clamp(1f + (materialBoost.g - 1f) * 0.38f, 0.92f, 1.16f),
                    Mathf.Clamp(1f + (materialBoost.b - 1f) * 0.38f, 0.92f, 1.12f),
                    1f);
            }

            Color baseColor = GetMaterialColor(material);
            Color boosted = new Color(
                Mathf.Clamp01(baseColor.r * materialBoost.r + 0.035f),
                Mathf.Clamp01(baseColor.g * materialBoost.g + 0.035f),
                Mathf.Clamp01(baseColor.b * materialBoost.b + 0.025f),
                baseColor.a);
            return IncreaseColorContrast(boosted, 1.16f);
        }

        private static Color GetMaterialColor(Material material)
        {
            if (material == null)
            {
                return new Color(0.58f, 0.62f, 0.52f, 1f);
            }

            if (material.HasProperty("_BaseColor"))
            {
                return material.GetColor("_BaseColor");
            }

            if (material.HasProperty("_Color"))
            {
                return material.GetColor("_Color");
            }

            return material.color;
        }

        private static Color IncreaseColorContrast(Color color, float contrast)
        {
            return new Color(
                Mathf.Clamp01((color.r - 0.5f) * contrast + 0.5f),
                Mathf.Clamp01((color.g - 0.5f) * contrast + 0.5f),
                Mathf.Clamp01((color.b - 0.5f) * contrast + 0.5f),
                color.a);
        }

        private static ImportedRendererMaterialProfile GetImportedUnitMaterialProfile(UnitKind kind)
        {
            if (RtsBalance.IsInfantry(kind))
            {
                return new ImportedRendererMaterialProfile(new Color(1.24f, 1.25f, 1.12f, 1f), 0.12f, 0.46f, 0.86f);
            }

            switch (RtsBalance.NormalizeUnitKind(kind))
            {
                case UnitKind.HeavyTank:
                    return new ImportedRendererMaterialProfile(new Color(1.28f, 1.28f, 1.14f, 1f), 0.24f, 0.58f, 0.82f);
                case UnitKind.LightTank:
                    return new ImportedRendererMaterialProfile(new Color(1.22f, 1.24f, 1.12f, 1f), 0.2f, 0.54f, 0.84f);
                default:
                    return new ImportedRendererMaterialProfile(new Color(1.25f, 1.26f, 1.13f, 1f), 0.22f, 0.56f, 0.83f);
            }
        }

        private static ImportedRendererMaterialProfile GetImportedStructureMaterialProfile(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.CommandCenter:
                case StructureKind.WarFactory:
                    return new ImportedRendererMaterialProfile(new Color(1.34f, 1.34f, 1.16f, 1f), 0.18f, 0.52f, 0.82f);
                case StructureKind.Turret:
                case StructureKind.GunTower:
                case StructureKind.AdvancedGunTower:
                    return new ImportedRendererMaterialProfile(new Color(1.3f, 1.3f, 1.14f, 1f), 0.24f, 0.58f, 0.8f);
                default:
                    return new ImportedRendererMaterialProfile(new Color(1.26f, 1.28f, 1.12f, 1f), 0.16f, 0.48f, 0.84f);
            }
        }

        private void CreateStructureReadabilityPanels(Transform root, StructureKind kind, Material teamMaterial)
        {
            Vector3 roofPosition = GetStructureRoofTeamPosition(kind);
            Vector3 roofSize = GetStructureRoofTeamSize(kind);
            float sideOffset = GetStructureSideTeamOffset(kind);
            Vector3 sideSize = GetStructureSideTeamSize(kind);
            Vector3 detailPosition = GetStructureDetailPanelPosition(kind);
            Vector3 detailSize = GetStructureDetailPanelSize(kind);

            CreatePrimitive(PrimitiveType.Cube, root, "Structure Team Strip", GetStructureRecognitionStripPosition(kind), GetStructureRecognitionStripSize(kind), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Structure Team Roof Plate", roofPosition, roofSize, teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Structure Team Left Plate", new Vector3(-sideOffset, sideSize.y + 0.22f, 0f), sideSize, teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Structure Team Right Plate", new Vector3(sideOffset, sideSize.y + 0.22f, 0f), sideSize, teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Structure Armor Detail Plate", detailPosition, detailSize, structureDetailMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Structure Dark Panel Line", detailPosition + new Vector3(0f, 0.07f, -detailSize.z * 0.65f), new Vector3(detailSize.x * 0.76f, 0.035f, 0.045f), darkMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Structure Sensor Glass", detailPosition + new Vector3(detailSize.x * 0.31f, 0.15f, detailSize.z * 0.48f), new Vector3(detailSize.x * 0.16f, 0.055f, 0.05f), sensorGlassMaterial);
            Vector3 contrastSize = GetStructureContrastBaseSize(kind);
            CreatePrimitive(PrimitiveType.Cube, root, "Structure Base Shadow Plinth", new Vector3(0f, 0.075f, 0f), new Vector3(contrastSize.x, 0.075f, contrastSize.z), shadowPanelMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Structure Front Edge Highlight", new Vector3(0f, detailPosition.y + 0.28f, -contrastSize.z * 0.48f), new Vector3(contrastSize.x * 0.56f, 0.045f, 0.07f), edgeHighlightMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Structure Rear Recess Shadow", new Vector3(0f, detailPosition.y + 0.08f, contrastSize.z * 0.48f), new Vector3(contrastSize.x * 0.58f, 0.07f, 0.08f), shadowPanelMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Structure Left Edge Highlight", new Vector3(-contrastSize.x * 0.49f, detailPosition.y + 0.42f, 0f), new Vector3(0.055f, 0.42f, contrastSize.z * 0.38f), edgeHighlightMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Structure Right Edge Highlight", new Vector3(contrastSize.x * 0.49f, detailPosition.y + 0.42f, 0f), new Vector3(0.055f, 0.42f, contrastSize.z * 0.38f), edgeHighlightMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Structure Roof Lip Highlight", roofPosition + new Vector3(0f, 0.09f, roofSize.z * 0.5f), new Vector3(roofSize.x * 0.82f, 0.04f, 0.055f), edgeHighlightMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Structure Service Vent", detailPosition + new Vector3(-detailSize.x * 0.28f, 0.2f, detailSize.z * 0.5f), new Vector3(detailSize.x * 0.22f, 0.18f, 0.045f), darkMaterial);
            CreateVentSlats(root, "Structure Service Vent Slat", detailPosition + new Vector3(-detailSize.x * 0.34f, 0.15f, detailSize.z * 0.53f), new Vector3(detailSize.x * 0.055f, 0.04f, 0f), 4, new Vector3(detailSize.x * 0.04f, 0.02f, 0.045f), panelTrimMaterial);
            CreatePrimitive(PrimitiveType.Sphere, root, "Structure Warning Beacon", roofPosition + new Vector3(roofSize.x * 0.38f, 0.16f, -roofSize.z * 0.36f), new Vector3(0.12f, 0.055f, 0.12f), warningLightMaterial);
            CreateBoltRow(root, "Structure Face Bolt", detailPosition + new Vector3(-detailSize.x * 0.39f, 0.05f, detailSize.z * 0.53f), new Vector3(detailSize.x * 0.13f, 0f, 0f), 7, new Vector3(0.04f, 0.026f, 0.035f), panelTrimMaterial);
            if (kind == StructureKind.CommandCenter || kind == StructureKind.WarFactory || kind == StructureKind.Refinery)
            {
                CreatePrimitive(PrimitiveType.Cube, root, "Structure Caution Threshold", detailPosition + new Vector3(0f, 0.13f, detailSize.z * 0.82f), new Vector3(detailSize.x * 0.72f, 0.035f, 0.055f), cautionStripeMaterial);
            }
        }

        private void CreateBoltRow(Transform root, string prefix, Vector3 start, Vector3 step, int count, Vector3 scale, Material material)
        {
            for (int i = 0; i < count; i++)
            {
                CreatePrimitive(PrimitiveType.Cube, root, prefix + " " + (i + 1), start + step * i, scale, material);
            }
        }

        private void CreateVentSlats(Transform root, string prefix, Vector3 start, Vector3 step, int count, Vector3 scale, Material material)
        {
            for (int i = 0; i < count; i++)
            {
                CreatePrimitive(PrimitiveType.Cube, root, prefix + " " + (i + 1), start + step * i, scale, material);
            }
        }

        private static string GetStructureModelResourcePath(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.CommandCenter:
                    return "StructureModels/BastionFabricationHub/Meshes/Bastion_FabricationHub_Static";
                case StructureKind.Refinery:
                    return "StructureModels/BastionStructures/Meshes/Bastion_Refinery_Static";
                case StructureKind.Barracks:
                    return "StructureModels/BastionStructures/Meshes/Bastion_Barracks_Static";
                case StructureKind.WarFactory:
                    return "StructureModels/BastionStructures/Meshes/Bastion_WarFactory_Static";
                case StructureKind.PowerPlant:
                    return "StructureModels/BastionStructures/Meshes/Bastion_PowerPlant_Static";
                case StructureKind.Turret:
                    return "StructureModels/BastionStructures/Meshes/Bastion_Turret_Static";
                case StructureKind.GunTower:
                    return "StructureModels/BastionStructures/Meshes/Bastion_GunTower_Static";
                case StructureKind.AdvancedGunTower:
                    return "StructureModels/BastionStructures/Meshes/Bastion_AdvancedGunTower_Static";
                default:
                    return "StructureModels/BastionStructures/Meshes/Bastion_CommunicationsCenter_Static";
            }
        }

        private static float GetStructureModelScale(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.CommandCenter:
                    return 0.62f;
                case StructureKind.Barracks:
                    return 0.7f;
                case StructureKind.Refinery:
                    return 0.68f;
                case StructureKind.WarFactory:
                    return 0.68f;
                case StructureKind.PowerPlant:
                    return 0.72f;
                case StructureKind.Turret:
                    return 0.58f;
                case StructureKind.GunTower:
                    return 0.7f;
                case StructureKind.AdvancedGunTower:
                    return 0.95f;
                default:
                    return 0.95f;
            }
        }

        private static Vector3 GetStructureRoofTeamPosition(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.CommandCenter:
                    return new Vector3(0f, 3.32f, -0.18f);
                case StructureKind.WarFactory:
                    return new Vector3(0f, 1.45f, -0.1f);
                case StructureKind.Refinery:
                    return new Vector3(0f, 1.45f, -0.05f);
                case StructureKind.PowerPlant:
                    return new Vector3(0f, 1.28f, -0.02f);
                case StructureKind.Turret:
                    return new Vector3(0f, 1.08f, -0.05f);
                case StructureKind.GunTower:
                    return new Vector3(0f, 2.75f, -0.08f);
                case StructureKind.AdvancedGunTower:
                    return new Vector3(0f, 3.55f, -0.08f);
                default:
                    return new Vector3(0f, 0.95f, -0.05f);
            }
        }

        private static Vector3 GetStructureRoofTeamSize(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.CommandCenter:
                    return new Vector3(1.5f, 0.09f, 1.0f);
                case StructureKind.WarFactory:
                    return new Vector3(1.35f, 0.08f, 0.82f);
                case StructureKind.Refinery:
                    return new Vector3(1.1f, 0.08f, 0.72f);
                case StructureKind.PowerPlant:
                    return new Vector3(0.92f, 0.08f, 0.62f);
                case StructureKind.Turret:
                    return new Vector3(0.68f, 0.07f, 0.46f);
                case StructureKind.GunTower:
                    return new Vector3(0.82f, 0.08f, 0.5f);
                case StructureKind.AdvancedGunTower:
                    return new Vector3(1.0f, 0.09f, 0.58f);
                default:
                    return new Vector3(0.92f, 0.08f, 0.58f);
            }
        }

        private static float GetStructureSideTeamOffset(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.CommandCenter:
                    return 3.0f;
                case StructureKind.WarFactory:
                    return 2.4f;
                case StructureKind.Refinery:
                    return 2.05f;
                case StructureKind.PowerPlant:
                    return 1.55f;
                case StructureKind.Turret:
                    return 0.82f;
                case StructureKind.GunTower:
                    return 0.9f;
                case StructureKind.AdvancedGunTower:
                    return 1.05f;
                default:
                    return 1.45f;
            }
        }

        private static Vector3 GetStructureSideTeamSize(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.CommandCenter:
                    return new Vector3(0.09f, 0.7f, 1.35f);
                case StructureKind.WarFactory:
                    return new Vector3(0.08f, 0.42f, 1.05f);
                case StructureKind.Refinery:
                    return new Vector3(0.08f, 0.38f, 0.9f);
                case StructureKind.PowerPlant:
                    return new Vector3(0.07f, 0.34f, 0.74f);
                case StructureKind.Turret:
                    return new Vector3(0.06f, 0.24f, 0.42f);
                case StructureKind.GunTower:
                    return new Vector3(0.06f, 0.46f, 0.5f);
                case StructureKind.AdvancedGunTower:
                    return new Vector3(0.07f, 0.58f, 0.58f);
                default:
                    return new Vector3(0.07f, 0.32f, 0.64f);
            }
        }

        private static Vector3 GetStructureDetailPanelPosition(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.CommandCenter:
                    return new Vector3(0f, 0.44f, -2.05f);
                case StructureKind.WarFactory:
                    return new Vector3(0f, 0.42f, -1.55f);
                case StructureKind.Refinery:
                    return new Vector3(0f, 0.4f, -1.25f);
                case StructureKind.Turret:
                    return new Vector3(0f, 0.62f, -0.54f);
                case StructureKind.GunTower:
                    return new Vector3(0f, 1.18f, -0.58f);
                case StructureKind.AdvancedGunTower:
                    return new Vector3(0f, 1.52f, -0.68f);
                default:
                    return new Vector3(0f, 0.38f, -1.0f);
            }
        }

        private static Vector3 GetStructureDetailPanelSize(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.CommandCenter:
                    return new Vector3(2.0f, 0.08f, 0.48f);
                case StructureKind.WarFactory:
                    return new Vector3(1.65f, 0.07f, 0.42f);
                case StructureKind.Refinery:
                    return new Vector3(1.35f, 0.07f, 0.38f);
                case StructureKind.Turret:
                    return new Vector3(0.62f, 0.06f, 0.22f);
                case StructureKind.GunTower:
                    return new Vector3(0.74f, 0.06f, 0.26f);
                case StructureKind.AdvancedGunTower:
                    return new Vector3(0.86f, 0.06f, 0.3f);
                default:
                    return new Vector3(1.05f, 0.07f, 0.34f);
            }
        }

        private static Vector3 GetStructureRecognitionStripPosition(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.GunTower:
                    return new Vector3(0f, 2.5f, -0.75f);
                case StructureKind.AdvancedGunTower:
                    return new Vector3(0f, 3.2f, -0.9f);
                case StructureKind.Turret:
                    return new Vector3(0f, 0.72f, -0.65f);
                default:
                    return new Vector3(0f, 0.16f, -1.25f);
            }
        }

        private static Vector3 GetStructureRecognitionStripSize(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.GunTower:
                case StructureKind.AdvancedGunTower:
                    return new Vector3(1.15f, 0.1f, 0.16f);
                case StructureKind.Turret:
                    return new Vector3(0.75f, 0.08f, 0.12f);
                default:
                    return new Vector3(1.6f, 0.08f, 0.16f);
            }
        }

        private static Vector3 GetStructureContrastBaseSize(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.CommandCenter:
                    return new Vector3(6.8f, 0.08f, 5.6f);
                case StructureKind.WarFactory:
                    return new Vector3(5.5f, 0.08f, 4.7f);
                case StructureKind.Refinery:
                    return new Vector3(4.6f, 0.08f, 4.0f);
                case StructureKind.PowerPlant:
                    return new Vector3(3.4f, 0.08f, 3.0f);
                case StructureKind.GunTower:
                    return new Vector3(2.0f, 0.08f, 2.0f);
                case StructureKind.AdvancedGunTower:
                    return new Vector3(2.4f, 0.08f, 2.4f);
                case StructureKind.Turret:
                    return new Vector3(1.8f, 0.08f, 1.8f);
                default:
                    return new Vector3(3.0f, 0.08f, 2.8f);
            }
        }

        private static bool IsDefenseStructure(StructureKind kind)
        {
            return kind == StructureKind.Turret || kind == StructureKind.GunTower || kind == StructureKind.AdvancedGunTower;
        }

        private GameObject CreatePrimitive(PrimitiveType type, Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localScale = localScale;
            primitive.GetComponent<Renderer>().sharedMaterial = material;
            if (material == playerMaterial || material == enemyMaterial)
            {
                primitive.AddComponent<RtsTeamTintTarget>();
            }

            Collider collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(collider);
                }
                else
                {
                    DestroyImmediate(collider);
                }
            }

            return primitive;
        }

        private Material GetTeamMaterial(RtsTeam team)
        {
            if (team == RtsTeam.Enemy)
            {
                return enemyMaterial;
            }

            if (team == RtsTeam.Neutral)
            {
                return neutralMaterial;
            }

            return playerMaterial;
        }

        private static Material CreateTransparentMaterial(Color color)
        {
            Material material = CreateMaterial(color);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            return material;
        }

        private static Quaternion GetStructureFacingRotation()
        {
            return Quaternion.Euler(0f, 180f, 0f);
        }

        private static Vector3 ClampToGround(Vector3 position)
        {
            float limit = RtsBalance.MapHalfSize - 1f;
            return new Vector3(Mathf.Clamp(position.x, -limit, limit), 0f, Mathf.Clamp(position.z, -limit, limit));
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static string FormatSaveTime(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(seconds));
            int minutes = totalSeconds / 60;
            int remainingSeconds = totalSeconds % 60;
            return minutes + ":" + remainingSeconds.ToString("00");
        }
    }
}
