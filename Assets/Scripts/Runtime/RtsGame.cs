using System.Collections.Generic;
using UnityEngine;

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
        public RtsRuntimeMode RuntimeMode { get; private set; }
        public QuestTabletopRig QuestRig { get; private set; }
        public IReadOnlyList<RtsEntity> Entities => entities;
        public IReadOnlyList<RtsEntity> Selection => selection;
        public IReadOnlyList<ResourceNode> ResourceNodes => resourceNodes;
        public RtsMatchState MatchState { get; private set; } = RtsMatchState.Running;
        public string StatusMessage { get; private set; } = "Destroy the enemy base.";
        public float MatchTime { get; private set; }
        public bool IsMatchOver => MatchState != RtsMatchState.Running;

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
        private Material resourceMaterial;
        private Material depletedResourceMaterial;
        private Material darkMaterial;
        private bool initialized;
        private float nextObjectiveCheckTime;

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
            if (!initialized || IsMatchOver)
            {
                return;
            }

            MatchTime += Time.deltaTime;

            if (Time.time >= nextObjectiveCheckTime)
            {
                nextObjectiveCheckTime = Time.time + 0.5f;
                EvaluateMatchState();
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

        public void RegisterEntity(RtsEntity entity)
        {
            if (entity == null || entities.Contains(entity))
            {
                return;
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
                    Destroy(structure.gameObject);
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
            UnitStats stats = RtsBalance.GetUnit(kind);
            GameObject root = new GameObject(team + " " + stats.Name);
            root.transform.SetParent(unitsRoot, true);
            root.transform.position = ClampToGround(position);
            root.transform.rotation = Quaternion.Euler(0f, team == RtsTeam.Enemy ? 210f : 35f, 0f);

            AddUnitCollider(root, kind);
            RtsUnit unit = kind == UnitKind.Harvester ? root.AddComponent<HarvesterUnit>() : root.AddComponent<RtsUnit>();
            BuildUnitVisual(root.transform, kind, team);
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
            else if (kind == StructureKind.Turret)
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
            RuntimeMode = RtsRuntimeModeResolver.Resolve();
            PlayerCommands = new RtsPlayerCommandService();
            PlayerCommands.Initialize(this);
            CommandDispatcher = new RtsCommandDispatcher();
            CommandDispatcher.Initialize(this);
            Resources = new ResourceBank(3400);
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

            gameObject.AddComponent<EnemyDirector>().Initialize(this);

            RecalculatePower();
            EvaluateMatchState();
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
            playerMaterial = CreateMaterial(RtsBalance.TeamColor(RtsTeam.Player));
            enemyMaterial = CreateMaterial(RtsBalance.TeamColor(RtsTeam.Enemy));
            neutralMaterial = CreateMaterial(new Color(0.42f, 0.4f, 0.34f));
            groundMaterial = CreateMaterial(new Color(0.18f, 0.27f, 0.21f));
            resourceMaterial = CreateMaterial(new Color(0.2f, 0.95f, 0.62f));
            depletedResourceMaterial = CreateMaterial(new Color(0.11f, 0.18f, 0.14f));
            darkMaterial = CreateMaterial(new Color(0.075f, 0.08f, 0.09f));
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
            if (Object.FindObjectOfType<Light>() == null)
            {
                GameObject lightObject = new GameObject("Sun");
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.15f;
                lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
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
                GameObject nodeObject = new GameObject("Flux Crystals");
                nodeObject.transform.SetParent(resourcesRoot, true);
                nodeObject.transform.position = position;
                ResourceNode node = nodeObject.AddComponent<ResourceNode>();
                node.Initialize(2600 + i * 220, resourceMaterial, depletedResourceMaterial);
                resourceNodes.Add(node);
            }
        }

        private void SpawnStartingForces()
        {
            RtsStructure command = CreateStructure(RtsTeam.Player, StructureKind.CommandCenter, new Vector3(-84f, 0f, -78f));
            RtsStructure refinery = CreateStructure(RtsTeam.Player, StructureKind.Refinery, new Vector3(-70f, 0f, -80f));
            CreateStructure(RtsTeam.Player, StructureKind.PowerPlant, new Vector3(-92f, 0f, -62f));
            CreateStructure(RtsTeam.Player, StructureKind.Barracks, new Vector3(-76f, 0f, -62f));

            RtsUnit rifleOne = CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-70f, 0f, -56f));
            CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-73f, 0f, -54f));
            HarvesterUnit harvester = CreateUnit(RtsTeam.Player, UnitKind.Harvester, new Vector3(-64f, 0f, -84f)) as HarvesterUnit;

            if (harvester != null)
            {
                harvester.IssueHarvest(FindNearestResource(harvester.transform.position), refinery as RefineryStructure);
            }

            CreateStructure(RtsTeam.Enemy, StructureKind.CommandCenter, new Vector3(86f, 0f, 78f));
            CreateStructure(RtsTeam.Enemy, StructureKind.PowerPlant, new Vector3(96f, 0f, 62f));
            CreateStructure(RtsTeam.Enemy, StructureKind.Barracks, new Vector3(74f, 0f, 64f));
            CreateStructure(RtsTeam.Enemy, StructureKind.Turret, new Vector3(66f, 0f, 84f));
            CreateUnit(RtsTeam.Enemy, UnitKind.Rifleman, new Vector3(66f, 0f, 62f));
            CreateUnit(RtsTeam.Enemy, UnitKind.Rifleman, new Vector3(70f, 0f, 66f));
            CreateUnit(RtsTeam.Enemy, UnitKind.Tank, new Vector3(84f, 0f, 58f));

            SelectEntity(command, false);
            SelectEntity(rifleOne, true);
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
            if (kind == UnitKind.Tank || kind == UnitKind.Harvester)
            {
                BoxCollider box = root.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.55f, 0f);
                box.size = kind == UnitKind.Tank ? new Vector3(1.7f, 1.1f, 2.1f) : new Vector3(1.5f, 1.2f, 2.0f);
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
        }

        private void BuildUnitVisual(Transform root, UnitKind kind, RtsTeam team)
        {
            Material teamMaterial = GetTeamMaterial(team);

            if (kind == UnitKind.Tank)
            {
                CreatePrimitive(PrimitiveType.Cube, root, "Hull", new Vector3(0f, 0.45f, 0f), new Vector3(1.7f, 0.55f, 2.1f), teamMaterial);
                CreatePrimitive(PrimitiveType.Cube, root, "Turret", new Vector3(0f, 0.88f, 0.1f), new Vector3(1f, 0.36f, 0.9f), teamMaterial);
                CreatePrimitive(PrimitiveType.Cylinder, root, "Barrel", new Vector3(0f, 0.9f, 0.85f), new Vector3(0.16f, 0.65f, 0.16f), darkMaterial).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                return;
            }

            if (kind == UnitKind.Harvester)
            {
                CreatePrimitive(PrimitiveType.Cube, root, "Cab", new Vector3(0f, 0.75f, -0.35f), new Vector3(1.2f, 0.8f, 0.9f), teamMaterial);
                CreatePrimitive(PrimitiveType.Cube, root, "Cargo", new Vector3(0f, 0.58f, 0.55f), new Vector3(1.35f, 0.55f, 1.3f), neutralMaterial);
                CreatePrimitive(PrimitiveType.Cylinder, root, "Collector", new Vector3(0f, 0.3f, 1.35f), new Vector3(0.65f, 0.2f, 0.65f), resourceMaterial).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                return;
            }

            CreatePrimitive(PrimitiveType.Capsule, root, "Body", new Vector3(0f, 0.8f, 0f), new Vector3(0.58f, 0.78f, 0.58f), teamMaterial);
            CreatePrimitive(PrimitiveType.Sphere, root, "Helmet", new Vector3(0f, 1.55f, 0.03f), new Vector3(0.46f, 0.36f, 0.46f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Rifle", new Vector3(0.35f, 1.05f, 0.38f), new Vector3(0.12f, 0.12f, 0.95f), darkMaterial);
        }

        private Transform BuildStructureVisual(Transform root, StructureKind kind, RtsTeam team)
        {
            Material teamMaterial = GetTeamMaterial(team);
            StructureStats stats = RtsBalance.GetStructure(kind);

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
                default:
                    CreatePrimitive(PrimitiveType.Cube, root, "Command Center", new Vector3(0f, 0.85f, 0f), new Vector3(stats.FootprintRadius * 1.65f, 1.7f, stats.FootprintRadius * 1.55f), teamMaterial);
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Radar", new Vector3(0f, 2.25f, -0.2f), new Vector3(0.95f, 0.12f, 0.95f), neutralMaterial);
                    break;
            }

            return null;
        }

        private GameObject CreatePrimitive(PrimitiveType type, Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localScale = localScale;
            primitive.GetComponent<Renderer>().sharedMaterial = material;

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
    }
}
