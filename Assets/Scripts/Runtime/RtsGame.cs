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
        private Material craterMaterial;
        private Material resourceMaterial;
        private Material depletedResourceMaterial;
        private Material darkMaterial;
        private Material vehicleDetailMaterial;
        private Material structureDetailMaterial;
        private bool initialized;
        private float nextObjectiveCheckTime;
        private int nextEntityId = 1;
        private int nextResourceNodeId = 1;

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
            material.color = color;
            material.SetColor("_Color", color);
            material.SetColor("_BaseColor", color);
            return material;
        }

        private static Material CreateTeamMaterial(Color color)
        {
            Material material = CreateMaterial(color);
            Color emission = color * 0.26f;
            emission.a = 1f;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
            return material;
        }

        private static Material CreateTexturedMaterial(Color color, Texture2D texture, Vector2 tiling)
        {
            Material material = CreateMaterial(color);
            ApplyTexture(material, texture, tiling);
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

        private static Texture2D CreateTerrainTexture(string name, Color baseColor, Color lowColor, Color highColor, Color lineColor, int seed, float grainStrength, float crackStrength, float stripeStrength)
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.hideFlags = HideFlags.DontSave;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float v = y / (float)(size - 1);
                    float largeNoise = Mathf.PerlinNoise((x + seed * 13) * 0.028f, (y - seed * 7) * 0.028f);
                    float fineNoise = Mathf.PerlinNoise((x - seed * 5) * 0.19f, (y + seed * 11) * 0.19f);
                    float grain = (Hash01(x, y, seed) - 0.5f) * grainStrength;
                    float shade = Mathf.Clamp01(largeNoise * 0.72f + fineNoise * 0.28f + grain);
                    Color color = Color.Lerp(lowColor, highColor, shade);
                    color = Color.Lerp(color, baseColor, 0.42f);

                    if (stripeStrength > 0f)
                    {
                        float stripeNoise = Mathf.PerlinNoise((x + seed) * 0.045f, (y - seed) * 0.045f);
                        float stripe = Mathf.Sin((u * 18f + v * 5.5f + stripeNoise * 2f) * Mathf.PI * 2f);
                        float stripeMask = Mathf.SmoothStep(0.42f, 0.96f, Mathf.Abs(stripe));
                        color = Color.Lerp(color, lineColor, stripeMask * stripeStrength);
                    }

                    if (crackStrength > 0f)
                    {
                        float crackNoise = Mathf.PerlinNoise((x + seed * 3) * 0.115f, (y - seed * 2) * 0.115f);
                        float crackGuide = Mathf.PerlinNoise((x - seed) * 0.035f, (y + seed) * 0.035f);
                        float crack = Mathf.SmoothStep(0.72f, 0.9f, crackNoise) * Mathf.SmoothStep(0.42f, 0.88f, crackGuide);
                        color = Color.Lerp(color, lineColor, crack * crackStrength);
                    }

                    color.a = baseColor.a;
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
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
            MatchTime = 0f;
            Resources = new ResourceBank(6200);

            CreateResourceFields();
            SpawnStartingForces();
            RecalculatePower();
            FogOfWar?.ResetExploration();
            EnemyDirector?.ResetForNewMatch();
            Lifecycle.SetMatchEnded(false);
            Physics.SyncTransforms();
            EvaluateMatchState();
            SpawnFloatingText("New match", GetPlayerBaseCenter() + Vector3.up * 3f, new Color(0.55f, 0.9f, 1f));
            return true;
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
            root.transform.rotation = Quaternion.Euler(0f, team == RtsTeam.Enemy ? 180f : 0f, 0f);

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
            RefineryStructure best = null;
            float bestDistance = float.PositiveInfinity;

            for (int i = 0; i < entities.Count; i++)
            {
                RefineryStructure refinery = entities[i] as RefineryStructure;
                if (refinery == null || refinery.Team != RtsTeam.Player || !refinery.IsAlive)
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
                return RtsBalance.IsTank(unit.UnitKind) || unit.UnitKind == UnitKind.Harvester ? 0.85f : 0.8f;
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
            Resources = new ResourceBank(6200);
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
                    nextEntityId = nextEntityId,
                    nextResourceNodeId = nextResourceNodeId,
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
                Resources.SetCreditsForRestore(data.resources != null ? data.resources.credits : 0);
                Clock.SetSimulationTime(data.matchTime);
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
                new Color(0.54f, 0.43f, 0.27f),
                new Color(0.37f, 0.29f, 0.18f),
                new Color(0.74f, 0.61f, 0.39f),
                new Color(0.18f, 0.14f, 0.1f),
                17,
                0.16f,
                0.74f,
                0.08f);
            Texture2D duneAccentTexture = CreateTerrainTexture(
                "Command RTS Dune Accent Texture",
                new Color(0.34f, 0.27f, 0.19f),
                new Color(0.24f, 0.18f, 0.12f),
                new Color(0.58f, 0.46f, 0.29f),
                new Color(0.2f, 0.15f, 0.09f),
                31,
                0.18f,
                0.34f,
                0.28f);
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
            Texture2D ridgeRockTexture = CreateTerrainTexture(
                "Command RTS Ridge Rock Texture",
                new Color(0.44f, 0.36f, 0.27f),
                new Color(0.25f, 0.21f, 0.17f),
                new Color(0.61f, 0.52f, 0.41f),
                new Color(0.16f, 0.13f, 0.11f),
                59,
                0.22f,
                0.52f,
                0.18f);
            Texture2D scorchTexture = CreateTerrainTexture(
                "Command RTS Scorch Texture",
                new Color(0.055f, 0.047f, 0.04f, 0.58f),
                new Color(0.018f, 0.016f, 0.014f, 0.58f),
                new Color(0.19f, 0.14f, 0.09f, 0.58f),
                new Color(0.01f, 0.009f, 0.008f, 0.58f),
                73,
                0.2f,
                0.85f,
                0.12f);
            Texture2D armorPlateTexture = CreateTerrainTexture(
                "Command RTS Armor Plate Texture",
                new Color(0.25f, 0.29f, 0.25f),
                new Color(0.12f, 0.15f, 0.13f),
                new Color(0.46f, 0.49f, 0.42f),
                new Color(0.05f, 0.06f, 0.055f),
                89,
                0.2f,
                0.38f,
                0.22f);

            groundMaterial = CreateTexturedMaterial(
                new Color(0.54f, 0.43f, 0.27f),
                sandGroundTexture,
                new Vector2(13f, 13f));
            terrainAccentMaterial = CreateTexturedMaterial(
                new Color(0.34f, 0.27f, 0.19f),
                duneAccentTexture,
                new Vector2(4f, 4f));
            waterMaterial = CreateTexturedTransparentMaterial(
                new Color(0.08f, 0.42f, 0.54f, 0.72f),
                waterRippleTexture,
                new Vector2(3f, 2f));
            ridgeMaterial = CreateTexturedMaterial(
                new Color(0.44f, 0.36f, 0.27f),
                ridgeRockTexture,
                new Vector2(2.4f, 2.4f));
            craterMaterial = CreateTexturedTransparentMaterial(
                new Color(0.055f, 0.047f, 0.04f, 0.58f),
                scorchTexture,
                new Vector2(1.6f, 1.6f));
            resourceMaterial = CreateMaterial(new Color(0.2f, 0.95f, 0.62f));
            depletedResourceMaterial = CreateMaterial(new Color(0.11f, 0.18f, 0.14f));
            darkMaterial = CreateMaterial(new Color(0.16f, 0.18f, 0.17f));
            vehicleDetailMaterial = CreateTexturedMaterial(
                new Color(0.42f, 0.47f, 0.39f),
                armorPlateTexture,
                new Vector2(2.2f, 2.2f));
            structureDetailMaterial = CreateTexturedMaterial(
                new Color(0.38f, 0.43f, 0.36f),
                armorPlateTexture,
                new Vector2(3.2f, 3.2f));
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
            RenderSettings.ambientLight = new Color(0.54f, 0.56f, 0.58f);
            RenderSettings.reflectionIntensity = 0.42f;

            if (Object.FindObjectOfType<Light>() == null)
            {
                GameObject lightObject = new GameObject("Sun");
                lightObject.transform.SetParent(transform, false);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.58f;
                lightObject.transform.rotation = Quaternion.Euler(58f, -32f, 0f);
            }
        }

        private void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Battlefield";
            ground.transform.SetParent(transform, true);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = Vector3.one * (RtsBalance.MapHalfSize * 2f / 10f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            CreateTerrainSetDressing();

            for (int i = -5; i <= 5; i++)
            {
                CreateGridLine(new Vector3(i * 20f, 0.025f, -RtsBalance.MapHalfSize), new Vector3(i * 20f, 0.025f, RtsBalance.MapHalfSize));
                CreateGridLine(new Vector3(-RtsBalance.MapHalfSize, 0.026f, i * 20f), new Vector3(RtsBalance.MapHalfSize, 0.026f, i * 20f));
            }

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
            line.widthMultiplier = 0.025f;
            line.material = CreateMaterial(new Color(0.35f, 0.42f, 0.36f, 0.55f));
        }

        private void CreateTerrainSetDressing()
        {
            CreateTerrainDisk("Projected Water Channel A", new Vector3(8f, 0.034f, -6f), new Vector3(34f, 0.018f, 13f), -8f, waterMaterial);
            CreateTerrainDisk("Projected Water Channel B", new Vector3(34f, 0.033f, 16f), new Vector3(28f, 0.018f, 9f), 24f, waterMaterial);
            CreateTerrainDisk("Projected Water Inlet", new Vector3(70f, 0.032f, -14f), new Vector3(22f, 0.018f, 8f), -20f, waterMaterial);

            CreateTerrainDisk("West Dune Shelf", new Vector3(-76f, 0.031f, -28f), new Vector3(26f, 0.02f, 15f), 15f, terrainAccentMaterial);
            CreateTerrainDisk("North Dune Shelf", new Vector3(-24f, 0.031f, 76f), new Vector3(38f, 0.02f, 13f), -12f, terrainAccentMaterial);
            CreateTerrainDisk("East Dune Shelf", new Vector3(78f, 0.031f, 54f), new Vector3(24f, 0.02f, 18f), 34f, terrainAccentMaterial);

            CreateTerrainBlock("West Mesa Ridge", new Vector3(-101f, 0.72f, 6f), new Vector3(9f, 1.45f, 34f), -12f, ridgeMaterial);
            CreateTerrainBlock("North Mesa Ridge", new Vector3(-12f, 0.64f, 102f), new Vector3(48f, 1.28f, 7f), 7f, ridgeMaterial);
            CreateTerrainBlock("East Mesa Ridge", new Vector3(101f, 0.68f, 28f), new Vector3(8f, 1.36f, 31f), 18f, ridgeMaterial);

            CreateTerrainDisk("Southwest Blast Scorch", new Vector3(-48f, 0.036f, -38f), new Vector3(7f, 0.012f, 4.8f), 22f, craterMaterial);
            CreateTerrainDisk("Central Blast Scorch", new Vector3(20f, 0.036f, 34f), new Vector3(8f, 0.012f, 5.2f), -18f, craterMaterial);
            CreateRockCluster("Southwest", new Vector3(-92f, 0f, -6f), 7);
            CreateRockCluster("Northeast", new Vector3(88f, 0f, 70f), 6);
            CreateRockCluster("Midfield", new Vector3(36f, 0f, -42f), 5);
        }

        private void CreateTerrainDisk(string name, Vector3 position, Vector3 scale, float yawDegrees, Material material)
        {
            GameObject disk = CreatePrimitive(PrimitiveType.Cylinder, transform, name, position, scale, material);
            disk.transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
        }

        private void CreateTerrainBlock(string name, Vector3 position, Vector3 scale, float yawDegrees, Material material)
        {
            GameObject block = CreatePrimitive(PrimitiveType.Cube, transform, name, position, scale, material);
            block.transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
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
            CreateResourceField(new Vector3(-58f, 0f, -82f), 9);
            CreateResourceField(new Vector3(-18f, 0f, -64f), 7);
            CreateResourceField(new Vector3(-70f, 0f, -18f), 6);
            CreateResourceField(new Vector3(0f, 0f, -18f), 8);
            CreateResourceField(new Vector3(34f, 0f, 18f), 8);
            CreateResourceField(new Vector3(-16f, 0f, 54f), 7);
            CreateResourceField(new Vector3(62f, 0f, 42f), 7);
            CreateResourceField(new Vector3(74f, 0f, 84f), 9);
        }

        private void CreateResourceField(Vector3 center, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (Mathf.PI * 2f * i) / count;
                float radius = 3.2f + (i % 4) * 1.8f;
                Vector3 position = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                CreateResourceNode(position, 2600 + i * 220, 2600 + i * 220, 0);
            }
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

        private void SpawnStartingForces()
        {
            RtsStructure command = CreateStructure(RtsTeam.Player, StructureKind.CommandCenter, new Vector3(-84f, 0f, -78f));

            CreateStructure(RtsTeam.Enemy, StructureKind.CommandCenter, new Vector3(86f, 0f, 78f));

            SelectEntity(command, false);
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
            if (RtsBalance.IsTank(normalized) || normalized == UnitKind.Harvester)
            {
                BoxCollider box = root.AddComponent<BoxCollider>();
                box.center = normalized == UnitKind.Harvester ? new Vector3(0f, 0.55f, 0f) : GetTankColliderCenter(normalized);
                box.size = normalized == UnitKind.Harvester ? new Vector3(1.5f, 1.2f, 2.0f) : GetTankColliderSize(normalized);
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

            if (kind == UnitKind.Harvester)
            {
                if (TryBuildImportedHarvesterVisual(root, teamMaterial))
                {
                    return;
                }

                CreatePrimitive(PrimitiveType.Cube, root, "Cab", new Vector3(0f, 0.75f, -0.35f), new Vector3(1.2f, 0.8f, 0.9f), teamMaterial);
                CreatePrimitive(PrimitiveType.Cube, root, "Cargo", new Vector3(0f, 0.58f, 0.55f), new Vector3(1.35f, 0.55f, 1.3f), neutralMaterial);
                CreatePrimitive(PrimitiveType.Cylinder, root, "Collector", new Vector3(0f, 0.3f, 1.35f), new Vector3(0.65f, 0.2f, 0.65f), resourceMaterial).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
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

            ConfigureImportedRenderers(model, GetImportedUnitMaterialBoost(kind));

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

            ConfigureImportedRenderers(model, new Color(1.18f, 1.2f, 1.08f, 1f));

            CreateHarvesterReadabilityPanels(root, teamMaterial);
            return true;
        }

        private void BuildFallbackInfantryVisual(Transform root, UnitKind kind, Material teamMaterial)
        {
            CreatePrimitive(PrimitiveType.Capsule, root, "Body", new Vector3(0f, 0.8f, 0f), new Vector3(0.58f, 0.78f, 0.58f), teamMaterial);
            CreatePrimitive(PrimitiveType.Sphere, root, "Helmet", new Vector3(0f, 1.55f, 0.03f), new Vector3(0.46f, 0.36f, 0.46f), teamMaterial);
            if (kind == UnitKind.Engineer)
            {
                CreatePrimitive(PrimitiveType.Cube, root, "Repair Tool", new Vector3(0.35f, 1.02f, 0.34f), new Vector3(0.13f, 0.13f, 0.55f), darkMaterial);
            }
            else if (kind == UnitKind.RocketSoldier)
            {
                CreatePrimitive(PrimitiveType.Cube, root, "Rocket Launcher", new Vector3(0.38f, 1.15f, 0.36f), new Vector3(0.22f, 0.22f, 1.05f), darkMaterial);
            }
            else
            {
                CreatePrimitive(PrimitiveType.Cube, root, "Rifle", new Vector3(0.35f, 1.05f, 0.38f), new Vector3(0.12f, 0.12f, 0.95f), darkMaterial);
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

            ConfigureImportedRenderers(model, GetImportedUnitMaterialBoost(kind));

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
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Light Barrel", new Vector3(0f, 0.78f, 0.75f), new Vector3(0.12f, 0.58f, 0.12f), darkMaterial).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    break;
                case UnitKind.HeavyTank:
                    CreatePrimitive(PrimitiveType.Cube, root, "Heavy Hull", new Vector3(0f, 0.52f, 0f), new Vector3(2.2f, 0.72f, 2.95f), teamMaterial);
                    CreatePrimitive(PrimitiveType.Cube, root, "Heavy Turret", new Vector3(0f, 1.08f, 0.16f), new Vector3(1.35f, 0.46f, 1.15f), teamMaterial);
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Heavy Cannon A", new Vector3(-0.24f, 1.08f, 1.08f), new Vector3(0.15f, 0.86f, 0.15f), darkMaterial).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Heavy Cannon B", new Vector3(0.24f, 1.08f, 1.08f), new Vector3(0.15f, 0.86f, 0.15f), darkMaterial).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    break;
                default:
                    CreatePrimitive(PrimitiveType.Cube, root, "Medium Hull", new Vector3(0f, 0.45f, 0f), new Vector3(1.75f, 0.55f, 2.25f), teamMaterial);
                    CreatePrimitive(PrimitiveType.Cube, root, "Medium Turret", new Vector3(0f, 0.88f, 0.1f), new Vector3(1f, 0.36f, 0.9f), teamMaterial);
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Medium Barrel", new Vector3(0f, 0.9f, 0.9f), new Vector3(0.16f, 0.68f, 0.16f), darkMaterial).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    CreatePrimitive(PrimitiveType.Cube, root, "Passenger Platform", new Vector3(0.62f, 0.72f, -0.28f), new Vector3(0.38f, 0.08f, 0.48f), darkMaterial);
                    break;
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

            if (kind == UnitKind.Harvester)
            {
                CreateVehicleWheelRig(root, kind);
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
                    darkMaterial);

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
                        structureDetailMaterial);
                }
            }
        }

        private void CreateWheel(Transform root, string name, Vector3 localPosition, float radius, float thickness)
        {
            GameObject wheel = CreatePrimitive(PrimitiveType.Cylinder, root, name, localPosition, new Vector3(radius, thickness, radius), darkMaterial);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        private void CreateTankTurretMotionRig(Transform root, UnitKind kind)
        {
            GetTankTurretRigLayout(kind, out Vector3 pivotPosition, out Vector3 capSize, out Vector3 barrelPosition, out Vector3 barrelScale);
            GameObject pivot = new GameObject("Animated Turret Pivot");
            pivot.transform.SetParent(root, false);
            pivot.transform.localPosition = pivotPosition;
            pivot.transform.localRotation = Quaternion.identity;

            CreatePrimitive(PrimitiveType.Cube, pivot.transform, "Animated Turret Cap", Vector3.zero, capSize, vehicleDetailMaterial);
            GameObject barrel = CreatePrimitive(PrimitiveType.Cylinder, pivot.transform, "Animated Turret Barrel", barrelPosition, barrelScale, darkMaterial);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject muzzle = new GameObject("Animated Turret Muzzle");
            muzzle.transform.SetParent(pivot.transform, false);
            muzzle.transform.localPosition = barrelPosition + new Vector3(0f, 0f, barrelScale.y * 0.96f);
        }

        private static void GetVehicleWheelLayout(UnitKind kind, out float sideOffset, out float centerY, out float forwardZ, out float wheelRadius, out float wheelThickness)
        {
            switch (RtsBalance.NormalizeUnitKind(kind))
            {
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
        }

        private void CreateHarvesterReadabilityPanels(Transform root, Material teamMaterial)
        {
            CreatePrimitive(PrimitiveType.Cube, root, "Harvester Team Strip", new Vector3(0f, 0.86f, -0.78f), new Vector3(1.08f, 0.1f, 0.16f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Harvester Team Roof Plate", new Vector3(0f, 1.02f, -0.1f), new Vector3(0.92f, 0.08f, 0.62f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Harvester Team Left Plate", new Vector3(-0.72f, 0.58f, -0.04f), new Vector3(0.08f, 0.24f, 0.9f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Harvester Team Right Plate", new Vector3(0.72f, 0.58f, -0.04f), new Vector3(0.08f, 0.24f, 0.9f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Harvester Armor Detail Plate", new Vector3(0f, 0.48f, 0.72f), new Vector3(1.08f, 0.08f, 0.32f), vehicleDetailMaterial);
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
                    CreatePrimitive(PrimitiveType.Cylinder, head.transform, "Turret Barrel", new Vector3(0f, 0f, 0.9f), new Vector3(0.18f, 0.75f, 0.18f), darkMaterial).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    return head.transform;
                case StructureKind.GunTower:
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Gun Tower Base", new Vector3(0f, 1.2f, 0f), new Vector3(1.45f, 1.2f, 1.45f), teamMaterial);
                    GameObject gunHead = CreatePrimitive(PrimitiveType.Cube, root, "Gun Tower Head", new Vector3(0f, 2.65f, 0f), new Vector3(1.45f, 0.7f, 1.25f), teamMaterial);
                    CreatePrimitive(PrimitiveType.Cylinder, gunHead.transform, "Gun Tower Barrel A", new Vector3(-0.22f, 0f, 0.95f), new Vector3(0.13f, 0.82f, 0.13f), darkMaterial).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    CreatePrimitive(PrimitiveType.Cylinder, gunHead.transform, "Gun Tower Barrel B", new Vector3(0.22f, 0f, 0.95f), new Vector3(0.13f, 0.82f, 0.13f), darkMaterial).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    return gunHead.transform;
                case StructureKind.AdvancedGunTower:
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Advanced Gun Tower Base", new Vector3(0f, 1.6f, 0f), new Vector3(1.7f, 1.6f, 1.7f), teamMaterial);
                    GameObject advancedHead = CreatePrimitive(PrimitiveType.Cube, root, "Advanced Gun Tower Head", new Vector3(0f, 3.5f, 0f), new Vector3(1.9f, 0.95f, 1.55f), teamMaterial);
                    CreatePrimitive(PrimitiveType.Cube, advancedHead.transform, "Missile Pod A", new Vector3(-0.46f, 0f, 0.72f), new Vector3(0.42f, 0.42f, 0.52f), darkMaterial);
                    CreatePrimitive(PrimitiveType.Cube, advancedHead.transform, "Missile Pod B", new Vector3(0.46f, 0f, 0.72f), new Vector3(0.42f, 0.42f, 0.52f), darkMaterial);
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
                CreatePrimitive(PrimitiveType.Cube, pivot.transform, "Animated Defense Missile Pod L", barrelPosition + new Vector3(-0.28f, 0f, 0f), new Vector3(0.28f, 0.24f, 0.55f), darkMaterial);
                CreatePrimitive(PrimitiveType.Cube, pivot.transform, "Animated Defense Missile Pod R", barrelPosition + new Vector3(0.28f, 0f, 0f), new Vector3(0.28f, 0.24f, 0.55f), darkMaterial);
            }
            else
            {
                GameObject barrel = CreatePrimitive(PrimitiveType.Cylinder, pivot.transform, "Animated Defense Barrel", barrelPosition, barrelScale, darkMaterial);
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

            ConfigureImportedRenderers(model, GetImportedStructureMaterialBoost(kind));

            CreateStructureReadabilityPanels(root, kind, teamMaterial);
            return true;
        }

        private static void ConfigureImportedRenderers(GameObject model, Color materialBoost)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.GetPropertyBlock(block);
                block.SetColor("_Color", materialBoost);
                block.SetColor("_BaseColor", materialBoost);
                renderer.SetPropertyBlock(block);
                block.Clear();
            }
        }

        private static Color GetImportedUnitMaterialBoost(UnitKind kind)
        {
            if (RtsBalance.IsInfantry(kind))
            {
                return new Color(1.24f, 1.25f, 1.12f, 1f);
            }

            switch (RtsBalance.NormalizeUnitKind(kind))
            {
                case UnitKind.HeavyTank:
                    return new Color(1.28f, 1.28f, 1.14f, 1f);
                case UnitKind.LightTank:
                    return new Color(1.22f, 1.24f, 1.12f, 1f);
                default:
                    return new Color(1.25f, 1.26f, 1.13f, 1f);
            }
        }

        private static Color GetImportedStructureMaterialBoost(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.CommandCenter:
                case StructureKind.WarFactory:
                    return new Color(1.34f, 1.34f, 1.16f, 1f);
                case StructureKind.Turret:
                case StructureKind.GunTower:
                case StructureKind.AdvancedGunTower:
                    return new Color(1.3f, 1.3f, 1.14f, 1f);
                default:
                    return new Color(1.26f, 1.28f, 1.12f, 1f);
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
