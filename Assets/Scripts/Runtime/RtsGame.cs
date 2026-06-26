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
        public IReadOnlyList<RtsEntity> Entities => entities;
        public IReadOnlyList<RtsEntity> Selection => selection;

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

        public bool TryQueueUnit(UnitKind kind)
        {
            ProductionStructure producer = FindProducer(kind, true);
            if (producer == null)
            {
                producer = FindProducer(kind, false);
            }

            if (producer == null)
            {
                SpawnFloatingText("Need producer", GetPlayerBaseCenter() + Vector3.up * 2f, Color.yellow);
                return false;
            }

            return producer.QueueUnit(kind);
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
            RtsEntity commandCenter = null;

            for (int i = 0; i < entities.Count; i++)
            {
                RtsStructure structure = entities[i] as RtsStructure;
                if (structure == null || structure.Team != RtsTeam.Player || !structure.IsAlive)
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

        public Vector3 GetPlayerBaseCenter()
        {
            RtsEntity target = FindPlayerPrimaryTarget();
            return target != null ? target.transform.position : new Vector3(-16f, 0f, -10f);
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

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            Resources = new ResourceBank(3400);
            CreateMaterials();
            CreateRoots();
            SetupCameraAndLight();
            CreateGround();
            CreateResourceFields();
            SpawnStartingForces();

            BuildManager = gameObject.AddComponent<BuildManager>();
            BuildManager.Initialize(this);
            gameObject.AddComponent<RtsInputController>().Initialize(this);
            gameObject.AddComponent<RtsHud>().Initialize(this);
            gameObject.AddComponent<EnemyDirector>().Initialize(this);

            RecalculatePower();
        }

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

        private void SetupCameraAndLight()
        {
            CommandCamera = Camera.main;
            if (CommandCamera == null)
            {
                GameObject cameraObject = new GameObject("Command Camera");
                CommandCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            CommandCamera.transform.position = new Vector3(0f, 29f, -29f);
            CommandCamera.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            CommandCamera.clearFlags = CameraClearFlags.SolidColor;
            CommandCamera.backgroundColor = new Color(0.035f, 0.04f, 0.05f);
            CommandCamera.nearClipPlane = 0.05f;
            CommandCamera.farClipPlane = 250f;
            CommandCamera.fieldOfView = 58f;

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

            for (int i = -3; i <= 3; i++)
            {
                CreateGridLine(new Vector3(i * 10f, 0.025f, -RtsBalance.MapHalfSize), new Vector3(i * 10f, 0.025f, RtsBalance.MapHalfSize));
                CreateGridLine(new Vector3(-RtsBalance.MapHalfSize, 0.026f, i * 10f), new Vector3(RtsBalance.MapHalfSize, 0.026f, i * 10f));
            }
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
            CreateResourceField(new Vector3(-2f, 0f, -19f), 6);
            CreateResourceField(new Vector3(16f, 0f, -3f), 5);
            CreateResourceField(new Vector3(4f, 0f, 20f), 7);
        }

        private void CreateResourceField(Vector3 center, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (Mathf.PI * 2f * i) / count;
                float radius = 2f + (i % 3) * 1.45f;
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
            RtsStructure command = CreateStructure(RtsTeam.Player, StructureKind.CommandCenter, new Vector3(-20f, 0f, -14f));
            RtsStructure refinery = CreateStructure(RtsTeam.Player, StructureKind.Refinery, new Vector3(-11f, 0f, -14f));
            CreateStructure(RtsTeam.Player, StructureKind.PowerPlant, new Vector3(-24f, 0f, -5f));
            CreateStructure(RtsTeam.Player, StructureKind.Barracks, new Vector3(-16f, 0f, -5f));

            RtsUnit rifleOne = CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-13f, 0f, -2f));
            CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-15f, 0f, -1f));
            HarvesterUnit harvester = CreateUnit(RtsTeam.Player, UnitKind.Harvester, new Vector3(-8f, 0f, -17f)) as HarvesterUnit;

            if (harvester != null)
            {
                harvester.IssueHarvest(FindNearestResource(harvester.transform.position), refinery as RefineryStructure);
            }

            CreateStructure(RtsTeam.Enemy, StructureKind.CommandCenter, new Vector3(22f, 0f, 17f));
            CreateStructure(RtsTeam.Enemy, StructureKind.PowerPlant, new Vector3(26f, 0f, 9f));
            CreateStructure(RtsTeam.Enemy, StructureKind.Barracks, new Vector3(16f, 0f, 12f));
            CreateStructure(RtsTeam.Enemy, StructureKind.Turret, new Vector3(13f, 0f, 20f));
            CreateUnit(RtsTeam.Enemy, UnitKind.Rifleman, new Vector3(12f, 0f, 13f));
            CreateUnit(RtsTeam.Enemy, UnitKind.Rifleman, new Vector3(14f, 0f, 15f));
            CreateUnit(RtsTeam.Enemy, UnitKind.Tank, new Vector3(19f, 0f, 10f));

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
                Destroy(collider);
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
