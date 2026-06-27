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
        public RtsAudio Audio { get; private set; }
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
        private Material concreteMaterial;
        private Material roadLineMaterial;
        private Material armorMaterial;
        private Material armorHighlightMaterial;
        private Material armorShadowMaterial;
        private Material glassMaterial;
        private Material accentMaterial;
        private Material glowMaterial;
        private Material subtleGlowMaterial;
        private Material whiteMaterial;
        private Material hazardMaterial;
        private Material pipeMaterial;
        private Material trimMaterial;
        private Material tankDustMaterial;
        private Material tankBareMetalMaterial;
        private Material tankDeepGrimeMaterial;
        private Material infantryCanvasMaterial;
        private Material infantryPlateMaterial;
        private Material infantryWebbingMaterial;
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
            Shader shader = FindRuntimeLitShader();
            Material material = new Material(shader);
            material.color = color;
            SetColorIfPresent(material, "_Color", color);
            SetColorIfPresent(material, "_BaseColor", color);
            return material;
        }

        private static Shader FindRuntimeLitShader()
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

            return shader;
        }

        private static Material CreateRuntimeCompatibleMaterial(Material source, Color fallbackColor)
        {
            Material material = CreateMaterial(ReadMaterialColor(source, fallbackColor));
            material.name = source != null ? source.name + " Runtime Compatible" : "Runtime Compatible Material";

            Texture texture = ReadMainTexture(source);
            if (texture != null)
            {
                texture.filterMode = FilterMode.Bilinear;
                texture.anisoLevel = Mathf.Max(texture.anisoLevel, 4);
                SetTextureIfPresent(material, "_MainTex", texture);
                SetTextureIfPresent(material, "_BaseMap", texture);
            }

            if (source != null && source.HasProperty("_EmissionColor"))
            {
                Color emission = source.GetColor("_EmissionColor");
                if (emission.maxColorComponent > 0.02f)
                {
                    material.EnableKeyword("_EMISSION");
                    SetColorIfPresent(material, "_EmissionColor", emission);
                }
            }

            SetFloatIfPresent(material, "_Metallic", 0.08f);
            SetFloatIfPresent(material, "_Smoothness", 0.24f);
            SetFloatIfPresent(material, "_Glossiness", 0.24f);
            return material;
        }

        private static Color ReadMaterialColor(Material source, Color fallbackColor)
        {
            if (source == null)
            {
                return fallbackColor;
            }

            if (source.HasProperty("_BaseColor"))
            {
                return source.GetColor("_BaseColor");
            }

            if (source.HasProperty("_Color"))
            {
                return source.GetColor("_Color");
            }

            return fallbackColor;
        }

        private static Texture ReadMainTexture(Material source)
        {
            if (source == null)
            {
                return null;
            }

            Texture texture = null;
            if (source.HasProperty("_BaseMap"))
            {
                texture = source.GetTexture("_BaseMap");
            }

            if (texture == null && source.HasProperty("_MainTex"))
            {
                texture = source.GetTexture("_MainTex");
            }

            return texture != null ? texture : source.mainTexture;
        }

        private static void SetTextureIfPresent(Material material, string property, Texture texture)
        {
            if (material != null && material.HasProperty(property))
            {
                material.SetTexture(property, texture);
            }
        }

        private static void SetColorIfPresent(Material material, string property, Color color)
        {
            if (material != null && material.HasProperty(property))
            {
                material.SetColor(property, color);
            }
        }

        private static void SetFloatIfPresent(Material material, string property, float value)
        {
            if (material != null && material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private static Material CreateIndustrialMaterial(Color baseColor, Color grimeColor, int seed, float metallic, float smoothness)
        {
            Material material = CreateMaterial(baseColor);
            Texture2D texture = CreateIndustrialTexture(baseColor, grimeColor, seed);
            material.SetTexture("_MainTex", texture);
            material.SetTexture("_BaseMap", texture);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        private static Material CreateFineNoiseMaterial(Color baseColor, Color grimeColor, int seed, float metallic, float smoothness)
        {
            Material material = CreateMaterial(baseColor);
            Texture2D texture = CreateFineNoiseTexture(baseColor, grimeColor, seed);
            material.SetTexture("_MainTex", texture);
            material.SetTexture("_BaseMap", texture);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        private static Material CreateEmissiveMaterial(Color baseColor, Color emissionColor, float intensity)
        {
            Material material = CreateMaterial(baseColor);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emissionColor * intensity);
            material.SetFloat("_Smoothness", 0.55f);
            return material;
        }

        private static Material CreateHazardMaterial()
        {
            Material material = CreateMaterial(new Color(0.82f, 0.42f, 0.08f));
            Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, true);
            texture.name = "Runtime Hazard Trim";
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;

            Color orange = new Color(0.9f, 0.48f, 0.08f);
            Color black = new Color(0.06f, 0.06f, 0.055f);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    bool stripe = ((x + y * 2) / 18) % 2 == 0;
                    Color color = stripe ? orange : black;
                    float grime = Mathf.PerlinNoise(x * 0.13f, y * 0.17f) * 0.12f;
                    texture.SetPixel(x, y, Color.Lerp(color, black, grime));
                }
            }

            texture.Apply(true, false);
            material.SetTexture("_MainTex", texture);
            material.SetTexture("_BaseMap", texture);
            material.SetFloat("_Metallic", 0.08f);
            material.SetFloat("_Smoothness", 0.28f);
            return material;
        }

        private static Texture2D CreateIndustrialTexture(Color baseColor, Color grimeColor, int seed)
        {
            const int size = 128;
            const int panel = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            texture.name = "Runtime Industrial " + seed;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n1 = Mathf.PerlinNoise((x + seed * 13) * 0.08f, (y + seed * 7) * 0.08f);
                    float n2 = Mathf.PerlinNoise((x + seed * 3) * 0.23f, (y + seed * 5) * 0.21f);
                    float shade = 0.84f + n1 * 0.24f - n2 * 0.08f;
                    Color color = baseColor * shade;
                    color.a = 1f;

                    int localX = x % panel;
                    int localY = y % panel;
                    bool seam = localX <= 1 || localY <= 1 || localX >= panel - 2 || localY >= panel - 2;
                    if (seam)
                    {
                        color = Color.Lerp(color, grimeColor, 0.65f);
                    }

                    bool boltX = localX >= 4 && localX <= 6 || localX >= panel - 7 && localX <= panel - 5;
                    bool boltY = localY >= 4 && localY <= 6 || localY >= panel - 7 && localY <= panel - 5;
                    if (boltX && boltY)
                    {
                        color = Color.Lerp(color, Color.white, 0.22f);
                    }

                    bool scratch = ((x * 19 + y * 7 + seed * 31) % 101) < 2 && localY > 6 && localY < panel - 6;
                    if (scratch)
                    {
                        color = Color.Lerp(color, Color.white, 0.18f);
                    }

                    float stain = Mathf.PerlinNoise((x + seed * 11) * 0.035f, (y + seed * 17) * 0.05f);
                    if (stain > 0.72f)
                    {
                        color = Color.Lerp(color, grimeColor, (stain - 0.72f) * 0.75f);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(true, false);
            return texture;
        }

        private static Texture2D CreateFineNoiseTexture(Color baseColor, Color grimeColor, int seed)
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            texture.name = "Runtime Fine Noise " + seed;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n1 = Mathf.PerlinNoise((x + seed * 11) * 0.11f, (y + seed * 5) * 0.1f);
                    float n2 = Mathf.PerlinNoise((x + seed * 17) * 0.025f, (y + seed * 19) * 0.035f);
                    float shade = 0.82f + n1 * 0.24f - n2 * 0.08f;
                    Color color = baseColor * shade;
                    color.a = 1f;

                    bool scratch = ((x * 13 + y * 23 + seed * 29) % 139) < 2;
                    if (scratch)
                    {
                        color = Color.Lerp(color, Color.white, 0.1f);
                    }

                    if (n2 > 0.68f)
                    {
                        color = Color.Lerp(color, grimeColor, (n2 - 0.68f) * 0.55f);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(true, false);
            return texture;
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

        public int GetQueuedUnitCount(UnitKind kind)
        {
            int count = 0;
            for (int i = 0; i < entities.Count; i++)
            {
                ProductionStructure producer = entities[i] as ProductionStructure;
                if (producer != null && producer.Team == RtsTeam.Player && producer.IsAlive)
                {
                    count += producer.GetQueuedCount(kind);
                }
            }

            return count;
        }

        public bool TryGetUnitBuildProgress(UnitKind kind, out float progress)
        {
            progress = 0f;
            bool found = false;
            for (int i = 0; i < entities.Count; i++)
            {
                ProductionStructure producer = entities[i] as ProductionStructure;
                if (producer == null || producer.Team != RtsTeam.Player || !producer.IsAlive)
                {
                    continue;
                }

                if (producer.TryGetActiveProgress(kind, out float producerProgress))
                {
                    progress = Mathf.Max(progress, producerProgress);
                    found = true;
                }
            }

            return found;
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
            else if (kind == StructureKind.CommandCenter || kind == StructureKind.Barracks || kind == StructureKind.WarFactory || kind == StructureKind.DualHelipad)
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

            foreach (RtsSmokeEmitter emitter in root.GetComponentsInChildren<RtsSmokeEmitter>())
            {
                Destroy(emitter);
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

        public void SpawnTracer(Vector3 from, Vector3 to, RtsTeam team, UnitKind soundKind = UnitKind.Rifleman)
        {
            bool hasProjectileTrail = ShouldUseProjectileTrail(soundKind);
            if (hasProjectileTrail)
            {
                GameObject projectile = new GameObject("Smoke Trail Projectile");
                projectile.transform.SetParent(effectsRoot, true);
                RtsProjectileTrail projectileTrail = projectile.AddComponent<RtsProjectileTrail>();
                projectileTrail.Initialize(from, to, team, soundKind);
            }
            else
            {
                GameObject tracer = new GameObject("Weapon Tracer");
                tracer.transform.SetParent(effectsRoot, true);
                LineRenderer line = tracer.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.SetPosition(0, from);
                line.SetPosition(1, to);
                line.widthMultiplier = 0.06f;
                line.material = CreateMaterial(team == RtsTeam.Enemy ? new Color(1f, 0.38f, 0.22f) : new Color(0.45f, 0.8f, 1f));
                RtsTracerFade tracerFade = tracer.AddComponent<RtsTracerFade>();
                tracerFade.Lifetime = 0.12f;
                tracerFade.Width = 0.065f;
            }

            SpawnMuzzleFlash(from, team);
            if (!hasProjectileTrail)
            {
                SpawnImpactSpark(to, team);
            }

            if (Audio != null)
            {
                Audio.PlayWeapon(from, soundKind);
                if (!hasProjectileTrail)
                {
                    Audio.PlayImpact(to);
                }
            }
        }

        private static bool ShouldUseProjectileTrail(UnitKind kind)
        {
            return kind == UnitKind.Tank || kind == UnitKind.Skyraider || kind == UnitKind.OrcaLifter;
        }

        public void SpawnExplosion(Vector3 point, float radius)
        {
            GameObject explosion = new GameObject("Explosion");
            explosion.transform.SetParent(effectsRoot, true);
            explosion.transform.position = point;
            RtsExplosionEffect effect = explosion.AddComponent<RtsExplosionEffect>();
            effect.Radius = Mathf.Max(1f, radius);

            if (Audio != null)
            {
                Audio.PlayExplosion(point);
            }
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

            Audio = gameObject.AddComponent<RtsAudio>();
            Audio.Initialize();

            BuildManager = gameObject.AddComponent<BuildManager>();
            BuildManager.Initialize(this);
            gameObject.AddComponent<RtsInputController>().Initialize(this);
            gameObject.AddComponent<RtsHud>().Initialize(this);
            gameObject.AddComponent<EnemyDirector>().Initialize(this);

            RecalculatePower();
        }

        private void CreateMaterials()
        {
            playerMaterial = CreateIndustrialMaterial(RtsBalance.TeamColor(RtsTeam.Player), new Color(0.05f, 0.08f, 0.12f), 11, 0.18f, 0.32f);
            enemyMaterial = CreateIndustrialMaterial(RtsBalance.TeamColor(RtsTeam.Enemy), new Color(0.15f, 0.04f, 0.03f), 17, 0.18f, 0.32f);
            neutralMaterial = CreateIndustrialMaterial(new Color(0.42f, 0.4f, 0.34f), new Color(0.12f, 0.11f, 0.09f), 23, 0.14f, 0.28f);
            groundMaterial = CreateIndustrialMaterial(new Color(0.18f, 0.27f, 0.21f), new Color(0.06f, 0.08f, 0.06f), 31, 0.02f, 0.18f);
            ApplyProceduralBattlefieldTerrainTexture(groundMaterial);
            resourceMaterial = CreateMaterial(new Color(0.2f, 0.95f, 0.62f));
            depletedResourceMaterial = CreateMaterial(new Color(0.11f, 0.18f, 0.14f));
            darkMaterial = CreateIndustrialMaterial(new Color(0.075f, 0.08f, 0.09f), new Color(0.025f, 0.027f, 0.03f), 41, 0.1f, 0.34f);
            concreteMaterial = CreateIndustrialMaterial(new Color(0.56f, 0.56f, 0.5f), new Color(0.16f, 0.16f, 0.14f), 43, 0.04f, 0.18f);
            roadLineMaterial = CreateIndustrialMaterial(new Color(0.9f, 0.68f, 0.16f), new Color(0.18f, 0.12f, 0.04f), 47, 0.04f, 0.2f);
            armorMaterial = CreateIndustrialMaterial(new Color(0.28f, 0.32f, 0.22f), new Color(0.07f, 0.09f, 0.055f), 53, 0.12f, 0.27f);
            armorHighlightMaterial = CreateIndustrialMaterial(new Color(0.46f, 0.45f, 0.32f), new Color(0.15f, 0.14f, 0.09f), 59, 0.1f, 0.25f);
            armorShadowMaterial = CreateIndustrialMaterial(new Color(0.12f, 0.13f, 0.1f), new Color(0.035f, 0.04f, 0.035f), 61, 0.16f, 0.38f);
            glassMaterial = CreateEmissiveMaterial(new Color(0.06f, 0.2f, 0.25f), new Color(0.08f, 0.55f, 0.68f), 0.55f);
            accentMaterial = CreateEmissiveMaterial(new Color(1f, 0.48f, 0.08f), new Color(1f, 0.38f, 0.02f), 0.85f);
            glowMaterial = CreateEmissiveMaterial(new Color(0.18f, 0.9f, 1f), new Color(0.1f, 0.75f, 1f), 1.1f);
            subtleGlowMaterial = CreateEmissiveMaterial(new Color(0.08f, 0.34f, 0.38f), new Color(0.05f, 0.34f, 0.42f), 0.42f);
            whiteMaterial = CreateIndustrialMaterial(new Color(0.74f, 0.73f, 0.64f), new Color(0.18f, 0.18f, 0.14f), 67, 0.08f, 0.2f);
            hazardMaterial = CreateHazardMaterial();
            pipeMaterial = CreateIndustrialMaterial(new Color(0.18f, 0.19f, 0.17f), new Color(0.045f, 0.05f, 0.045f), 71, 0.22f, 0.42f);
            trimMaterial = CreateIndustrialMaterial(new Color(0.28f, 0.29f, 0.25f), new Color(0.06f, 0.065f, 0.055f), 79, 0.18f, 0.34f);
            tankDustMaterial = CreateIndustrialMaterial(new Color(0.68f, 0.61f, 0.46f), new Color(0.24f, 0.19f, 0.12f), 83, 0.02f, 0.18f);
            tankBareMetalMaterial = CreateIndustrialMaterial(new Color(0.42f, 0.42f, 0.37f), new Color(0.11f, 0.11f, 0.1f), 97, 0.38f, 0.36f);
            tankDeepGrimeMaterial = CreateIndustrialMaterial(new Color(0.075f, 0.071f, 0.058f), new Color(0.02f, 0.018f, 0.014f), 101, 0.04f, 0.2f);
            infantryCanvasMaterial = CreateFineNoiseMaterial(new Color(0.34f, 0.32f, 0.24f), new Color(0.095f, 0.075f, 0.045f), 109, 0.03f, 0.2f);
            infantryPlateMaterial = CreateFineNoiseMaterial(new Color(0.58f, 0.57f, 0.47f), new Color(0.17f, 0.16f, 0.12f), 113, 0.08f, 0.22f);
            infantryWebbingMaterial = CreateFineNoiseMaterial(new Color(0.09f, 0.095f, 0.075f), new Color(0.025f, 0.025f, 0.018f), 127, 0.06f, 0.25f);
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

            if (CommandCamera.GetComponent<AudioListener>() == null)
            {
                CommandCamera.gameObject.AddComponent<AudioListener>();
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

            CreateMilitaryTerrainDressing();
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

        private void ApplyProceduralBattlefieldTerrainTexture(Material material)
        {
            if (material == null)
            {
                return;
            }

            Texture2D terrainTexture = CreateBattlefieldTerrainTexture();
            SetTextureIfPresent(material, "_MainTex", terrainTexture);
            SetTextureIfPresent(material, "_BaseMap", terrainTexture);
            material.mainTextureScale = Vector2.one * 5.5f;
            material.color = Color.white;
            SetColorIfPresent(material, "_Color", material.color);
            SetColorIfPresent(material, "_BaseColor", material.color);
            SetFloatIfPresent(material, "_Metallic", 0.01f);
            SetFloatIfPresent(material, "_Smoothness", 0.18f);
        }

        private static Texture2D CreateBattlefieldTerrainTexture()
        {
            const int size = 512;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            texture.name = "Runtime Battlefield Dirt Concrete";
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 4;

            Color dirt = new Color(0.18f, 0.21f, 0.17f);
            Color packedDirt = new Color(0.29f, 0.28f, 0.22f);
            Color dust = new Color(0.46f, 0.43f, 0.33f);
            Color concrete = new Color(0.38f, 0.39f, 0.34f);
            Color grime = new Color(0.06f, 0.065f, 0.055f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float broad = Mathf.PerlinNoise(x * 0.018f + 14.7f, y * 0.018f + 61.2f);
                    float mid = Mathf.PerlinNoise(x * 0.058f + 3.1f, y * 0.052f + 9.7f);
                    float fine = Mathf.PerlinNoise(x * 0.19f + 21.6f, y * 0.17f + 5.4f);
                    Color color = Color.Lerp(dirt, packedDirt, broad * 0.72f);
                    color = Color.Lerp(color, dust, Mathf.Clamp01((mid - 0.58f) * 1.9f));

                    int slab = 128;
                    int localX = x % slab;
                    int localY = y % slab;
                    bool concretePad = ((x / slab + y / slab) % 3) == 0 && broad > 0.34f;
                    if (concretePad)
                    {
                        color = Color.Lerp(color, concrete, 0.42f);
                    }

                    bool seam = localX <= 2 || localY <= 2 || localX >= slab - 3 || localY >= slab - 3;
                    if (seam)
                    {
                        color = Color.Lerp(color, grime, 0.62f);
                    }

                    float rutA = Mathf.Abs(localX - 42f);
                    float rutB = Mathf.Abs(localX - 86f);
                    if ((rutA < 4f || rutB < 4f) && mid > 0.42f)
                    {
                        color = Color.Lerp(color, grime, 0.28f);
                    }

                    if (fine > 0.78f)
                    {
                        color = Color.Lerp(color, dust, 0.14f);
                    }
                    else if (fine < 0.22f)
                    {
                        color = Color.Lerp(color, grime, 0.12f);
                    }

                    color.a = 1f;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(true, false);
            return texture;
        }

        private void CreateMilitaryTerrainDressing()
        {
            CreatePrimitive(PrimitiveType.Cube, transform, "Asset Store Main Concrete Motor Pool", new Vector3(-16f, 0.032f, -8f), new Vector3(11f, 0.04f, 7f), concreteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, transform, "Asset Store Enemy Concrete Motor Pool", new Vector3(21f, 0.033f, 14f), new Vector3(10f, 0.04f, 6.8f), concreteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, transform, "Asset Store Center Service Road", new Vector3(0f, 0.034f, 0f), new Vector3(4.2f, 0.035f, RtsBalance.MapHalfSize * 1.55f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, transform, "Asset Store East West Service Road", new Vector3(0f, 0.035f, -6f), new Vector3(RtsBalance.MapHalfSize * 1.25f, 0.035f, 3.4f), trimMaterial, true);

            for (int i = 0; i < 7; i++)
            {
                float z = -24f + i * 7.5f;
                CreatePrimitive(PrimitiveType.Cube, transform, "Asset Store Road Centerline", new Vector3(0f, 0.065f, z), new Vector3(0.22f, 0.025f, 2.2f), roadLineMaterial, true);
            }

            InstantiateAssetStorePrefab(transform, "AssetStore/Props/ItHappy_Barrier_006", "Player Motor Pool Barrier A", new Vector3(-22.5f, 0.05f, -8.8f), new Vector3(0f, 18f, 0f), 0.52f);
            InstantiateAssetStorePrefab(transform, "AssetStore/Props/ItHappy_Barrier_006", "Player Motor Pool Barrier B", new Vector3(-10.7f, 0.05f, -4.3f), new Vector3(0f, -18f, 0f), 0.52f);
            InstantiateAssetStorePrefab(transform, "AssetStore/Props/ItHappy_Box_022", "Player Supply Crates", new Vector3(-21.5f, 0.06f, -12.2f), new Vector3(0f, 35f, 0f), 0.42f);
            InstantiateAssetStorePrefab(transform, "AssetStore/Props/ItHappy_Barrel_005", "Player Fuel Drums", new Vector3(-13.3f, 0.05f, -10.8f), new Vector3(0f, -12f, 0f), 0.46f);
            InstantiateAssetStorePrefab(transform, "AssetStore/Props/ItHappy_Hedgehog_001", "Forward Anti Vehicle Hedgehog A", new Vector3(-5.5f, 0.04f, -2.8f), new Vector3(0f, 20f, 0f), 0.48f);
            InstantiateAssetStorePrefab(transform, "AssetStore/Props/ItHappy_Hedgehog_001", "Forward Anti Vehicle Hedgehog B", new Vector3(6.3f, 0.04f, 4.1f), new Vector3(0f, -15f, 0f), 0.48f);
            InstantiateAssetStorePrefab(transform, "AssetStore/Props/ItHappy_Barrier_006", "Enemy Motor Pool Barrier A", new Vector3(16.1f, 0.05f, 18.2f), new Vector3(0f, 162f, 0f), 0.52f);
            InstantiateAssetStorePrefab(transform, "AssetStore/Props/ItHappy_Box_022", "Enemy Supply Crates", new Vector3(25.4f, 0.06f, 11.2f), new Vector3(0f, 70f, 0f), 0.42f);
        }

        private GameObject InstantiateAssetStorePrefab(Transform parent, string resourcePath, string name, Vector3 localPosition, Vector3 localEulerAngles, float scale)
        {
            GameObject prefab = UnityEngine.Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                return null;
            }

            GameObject visual = Instantiate(prefab, parent, false);
            visual.name = name;
            visual.transform.localPosition = localPosition;
            visual.transform.localRotation = Quaternion.Euler(localEulerAngles);
            visual.transform.localScale = Vector3.one * scale;
            visual.AddComponent<RtsFixedMaterial>();
            ImproveImportedAssetRenderers(visual);
            RemoveImportedColliders(visual);
            return visual;
        }

        private void SpawnMuzzleFlash(Vector3 point, RtsTeam team)
        {
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "Muzzle Flash";
            flash.transform.SetParent(effectsRoot, true);
            flash.transform.position = point;
            flash.transform.localScale = Vector3.one * 0.28f;
            flash.GetComponent<Renderer>().sharedMaterial = CreateMaterial(team == RtsTeam.Enemy ? new Color(1f, 0.45f, 0.15f) : new Color(0.55f, 0.9f, 1f));

            Collider collider = flash.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            RtsTimedDestroy timedDestroy = flash.AddComponent<RtsTimedDestroy>();
            timedDestroy.Lifetime = 0.08f;
        }

        private void SpawnImpactSpark(Vector3 point, RtsTeam team)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spark.name = "Impact Spark";
            spark.transform.SetParent(effectsRoot, true);
            spark.transform.position = point;
            spark.transform.localScale = Vector3.one * 0.22f;
            spark.GetComponent<Renderer>().sharedMaterial = CreateMaterial(team == RtsTeam.Enemy ? new Color(1f, 0.7f, 0.28f) : new Color(0.82f, 1f, 1f));

            Collider collider = spark.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            RtsTimedDestroy timedDestroy = spark.AddComponent<RtsTimedDestroy>();
            timedDestroy.Lifetime = 0.12f;
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
            CreateStructure(RtsTeam.Enemy, StructureKind.DualHelipad, new Vector3(27f, 0f, 19f));
            CreateStructure(RtsTeam.Enemy, StructureKind.Turret, new Vector3(13f, 0f, 20f));
            CreateUnit(RtsTeam.Enemy, UnitKind.Rifleman, new Vector3(12f, 0f, 13f));
            CreateUnit(RtsTeam.Enemy, UnitKind.Rifleman, new Vector3(14f, 0f, 15f));
            CreateUnit(RtsTeam.Enemy, UnitKind.Tank, new Vector3(19f, 0f, 10f));
            CreateUnit(RtsTeam.Enemy, UnitKind.Skyraider, new Vector3(24f, 0f, 21f));

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

        private void BuildCommandCenterVisual(Transform root, Material teamMaterial, StructureStats stats)
        {
            float width = stats.FootprintRadius * 1.62f;
            float depth = stats.FootprintRadius * 1.48f;
            CreatePrimitive(PrimitiveType.Cube, root, "Command Foundation", new Vector3(0f, 0.12f, 0f), new Vector3(width + 0.55f, 0.24f, depth + 0.45f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Command Armor Core", new Vector3(0f, 0.82f, 0f), new Vector3(width, 1.42f, depth), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Command Rear Machinery", new Vector3(0f, 1.12f, -1.65f), new Vector3(width * 0.82f, 0.92f, 1.55f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Command Roof Slab", new Vector3(0f, 1.68f, 0f), new Vector3(width + 0.2f, 0.32f, depth + 0.18f), armorHighlightMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Command Front Team Fascia", new Vector3(0f, 1.06f, depth * 0.51f), new Vector3(2.35f, 0.38f, 0.08f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Command Front White Plate", new Vector3(0f, 1.34f, depth * 0.515f), new Vector3(3.5f, 0.22f, 0.07f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Command Left White Armor", new Vector3(-width * 0.51f, 0.92f, 0.65f), new Vector3(0.08f, 0.7f, 1.65f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Command Right White Armor", new Vector3(width * 0.51f, 0.92f, 0.65f), new Vector3(0.08f, 0.7f, 1.65f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Command Observation Glass", new Vector3(0f, 1.1f, depth * 0.55f + 0.02f), new Vector3(2.1f, 0.24f, 0.08f), glassMaterial, true);
            BuildVent(root, "Command Roof Vent", new Vector3(-1.45f, 1.87f, -0.55f), new Vector3(1.45f, 0.06f, 0.78f), 5);
            BuildRoofFan(root, "Command Radar Fan", new Vector3(1.35f, 1.92f, -0.68f), 0.82f);
            CreatePrimitive(PrimitiveType.Cylinder, root, "Radar Mast", new Vector3(0.35f, 2.48f, -0.2f), new Vector3(0.08f, 0.76f, 0.08f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Radar Array", new Vector3(0.35f, 3.22f, -0.2f), new Vector3(1.15f, 0.16f, 0.58f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Sphere, root, "Radar Beacon", new Vector3(0.35f, 3.38f, -0.2f), new Vector3(0.16f, 0.16f, 0.16f), glowMaterial, true);
            AddBoltLine(root, "Command Fascia Bolts", new Vector3(-1.7f, 1.48f, depth * 0.555f), new Vector3(0.42f, 0f, 0f), 9, 0.055f);
        }

        private void BuildRefineryVisual(Transform root, Material teamMaterial)
        {
            CreatePrimitive(PrimitiveType.Cube, root, "Refinery Foundation", new Vector3(0f, 0.12f, 0f), new Vector3(5.45f, 0.24f, 4.85f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Refinery Processing Hall", new Vector3(0.05f, 0.68f, 0.35f), new Vector3(4.75f, 1.18f, 3.25f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Ore Intake Bay", new Vector3(-1.2f, 0.62f, 2.15f), new Vector3(2.25f, 0.88f, 0.85f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Ore Intake Mouth", new Vector3(-1.2f, 0.58f, 2.62f), new Vector3(1.65f, 0.5f, 0.08f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Refinery Team Plate", new Vector3(1.42f, 1.0f, 1.98f), new Vector3(1.2f, 0.28f, 0.08f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Refinery White Armor A", new Vector3(-2.35f, 0.82f, 0.45f), new Vector3(0.08f, 0.68f, 1.45f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Refinery White Armor B", new Vector3(2.42f, 0.86f, -0.3f), new Vector3(0.08f, 0.58f, 1.75f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, root, "Silo A", new Vector3(-1.35f, 1.48f, -0.95f), new Vector3(0.72f, 1.35f, 0.72f), neutralMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, root, "Silo B", new Vector3(0.75f, 1.32f, -0.92f), new Vector3(0.62f, 1.18f, 0.62f), neutralMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, root, "Silo Cap A", new Vector3(-1.35f, 2.86f, -0.95f), new Vector3(0.76f, 0.08f, 0.76f), armorHighlightMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, root, "Silo Cap B", new Vector3(0.75f, 2.54f, -0.92f), new Vector3(0.66f, 0.08f, 0.66f), armorHighlightMaterial, true);
            CreatePipe(root, "Refinery Cross Pipe", new Vector3(-0.3f, 1.65f, -0.95f), new Vector3(0.14f, 1.15f, 0.14f), new Vector3(0f, 0f, 90f));
            CreatePipe(root, "Refinery Front Pipe", new Vector3(2.25f, 1.12f, 0.9f), new Vector3(0.13f, 1.45f, 0.13f), new Vector3(90f, 0f, 0f));
            BuildVent(root, "Refinery Roof Vents", new Vector3(0.1f, 1.31f, 1.0f), new Vector3(2.1f, 0.06f, 0.58f), 6);
            BuildSmokeStack(root, "Refinery Stack", new Vector3(2.05f, 2.02f, -1.62f), 0.28f, 1.05f, true);
            AddBoltLine(root, "Refinery Intake Bolts", new Vector3(-2f, 1.08f, 2.68f), new Vector3(0.42f, 0f, 0f), 5, 0.05f);
        }

        private void BuildBarracksVisual(Transform root, Material teamMaterial)
        {
            CreatePrimitive(PrimitiveType.Cube, root, "Barracks Foundation", new Vector3(0f, 0.11f, 0f), new Vector3(4.15f, 0.22f, 3.55f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Barracks Main Armor", new Vector3(0f, 0.58f, 0f), new Vector3(3.55f, 1.02f, 2.85f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Barracks Roof Cap", new Vector3(0f, 1.18f, 0f), new Vector3(3.92f, 0.32f, 3.18f), armorHighlightMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Barracks Entry Recess", new Vector3(0f, 0.54f, 1.48f), new Vector3(1.14f, 0.68f, 0.08f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Barracks Team Door Plate", new Vector3(0f, 0.78f, 1.53f), new Vector3(1.35f, 0.18f, 0.06f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Barracks Left White Plate", new Vector3(-1.82f, 0.72f, 0.35f), new Vector3(0.08f, 0.58f, 1.12f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Barracks Right White Plate", new Vector3(1.82f, 0.72f, 0.35f), new Vector3(0.08f, 0.58f, 1.12f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Barracks Side Annex", new Vector3(-1.18f, 0.46f, -1.42f), new Vector3(1.42f, 0.68f, 0.76f), trimMaterial, true);
            BuildVent(root, "Barracks Roof Vent", new Vector3(0.78f, 1.38f, -0.55f), new Vector3(1.45f, 0.06f, 0.62f), 5);
            CreatePipe(root, "Barracks Wall Pipe", new Vector3(1.92f, 0.84f, -0.55f), new Vector3(0.08f, 0.92f, 0.08f), new Vector3(0f, 0f, 0f));
            CreatePrimitive(PrimitiveType.Cylinder, root, "Barracks Antenna", new Vector3(-1.32f, 1.82f, -1.28f), new Vector3(0.055f, 0.68f, 0.055f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Sphere, root, "Barracks Antenna Light", new Vector3(-1.32f, 2.52f, -1.28f), new Vector3(0.13f, 0.13f, 0.13f), accentMaterial, true);
            AddBoltLine(root, "Barracks Roof Bolts", new Vector3(-1.35f, 1.38f, 1.12f), new Vector3(0.45f, 0f, 0f), 7, 0.045f);
        }

        private void BuildWarFactoryVisual(Transform root, Material teamMaterial)
        {
            CreatePrimitive(PrimitiveType.Cube, root, "Factory Foundation", new Vector3(0f, 0.1f, 0f), new Vector3(7.25f, 0.2f, 5.85f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Factory Left Main Pier", new Vector3(-2.2f, 0.94f, 0.18f), new Vector3(1.25f, 1.78f, 4.55f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Factory Right Main Pier", new Vector3(2.2f, 0.94f, 0.18f), new Vector3(1.25f, 1.78f, 4.55f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Factory Rear Hall", new Vector3(0f, 0.98f, -1.45f), new Vector3(5.4f, 1.86f, 1.9f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Factory Roof Machinery Deck", new Vector3(0f, 1.92f, 0.02f), new Vector3(5.9f, 0.34f, 4.52f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Factory Front Header", new Vector3(0f, 1.62f, 2.35f), new Vector3(5.8f, 0.58f, 0.28f), armorHighlightMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Factory Bay Interior", new Vector3(0f, 0.8f, 1.85f), new Vector3(2.92f, 1.08f, 1.35f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Factory Bay Back Wall", new Vector3(0f, 0.9f, 1.12f), new Vector3(2.65f, 0.95f, 0.08f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Factory Bay Cyan Light", new Vector3(0f, 1.38f, 2.51f), new Vector3(1.82f, 0.08f, 0.07f), glowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Factory Front Team Plate", new Vector3(0f, 1.72f, 2.52f), new Vector3(1.38f, 0.22f, 0.06f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Factory Left White Armor", new Vector3(-2.83f, 1.15f, 1.6f), new Vector3(0.08f, 0.92f, 1.22f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Factory Right White Armor", new Vector3(2.83f, 1.15f, 1.6f), new Vector3(0.08f, 0.92f, 1.22f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Factory Roof White Plate A", new Vector3(-1.38f, 2.12f, 0.88f), new Vector3(1.34f, 0.06f, 0.58f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Factory Roof White Plate B", new Vector3(1.38f, 2.12f, 0.88f), new Vector3(1.34f, 0.06f, 0.58f), whiteMaterial, true);

            CreatePrimitive(PrimitiveType.Cube, root, "Factory Left Annex", new Vector3(-3.36f, 0.56f, 0.62f), new Vector3(1.45f, 1.02f, 3.18f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Factory Right Annex", new Vector3(3.36f, 0.64f, 0.24f), new Vector3(1.45f, 1.16f, 3.65f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Left Annex Door", new Vector3(-3.36f, 0.46f, 2.25f), new Vector3(0.92f, 0.56f, 0.08f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Right Annex Window Strip", new Vector3(3.36f, 0.86f, 2.1f), new Vector3(0.95f, 0.18f, 0.08f), glassMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Right Annex Team Plate", new Vector3(3.36f, 1.28f, 1.95f), new Vector3(0.95f, 0.18f, 0.08f), teamMaterial);

            CreatePrimitive(PrimitiveType.Cube, root, "Factory Ramp", new Vector3(0f, 0.16f, 3.34f), new Vector3(3.52f, 0.18f, 1.65f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Factory Ramp Center Plate", new Vector3(0f, 0.28f, 3.12f), new Vector3(2.28f, 0.06f, 1.18f), armorHighlightMaterial, true);
            AddHazardStripes(root, "Factory Ramp Left Hazard", -1.54f);
            AddHazardStripes(root, "Factory Ramp Right Hazard", 1.54f);

            CreatePrimitive(PrimitiveType.Cube, root, "Factory Conveyor Belt", new Vector3(0f, 0.48f, 1.82f), new Vector3(0.74f, 0.16f, 2.35f), darkMaterial, true);
            for (int i = 0; i < 7; i++)
            {
                float z = 0.82f + i * 0.32f;
                GameObject roller = CreatePrimitive(PrimitiveType.Cylinder, root, "Conveyor Roller", new Vector3(0f, 0.61f, z), new Vector3(0.09f, 0.48f, 0.09f), pipeMaterial, true);
                roller.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

            BuildRobotArm(root, "Left Robot Arm", new Vector3(-0.92f, 0.58f, 1.42f), -1f);
            BuildRobotArm(root, "Right Robot Arm", new Vector3(0.92f, 0.58f, 1.42f), 1f);
            BuildVent(root, "Factory Roof Vent A", new Vector3(-1.52f, 2.15f, -0.68f), new Vector3(1.52f, 0.06f, 0.74f), 6);
            BuildVent(root, "Factory Roof Vent B", new Vector3(1.52f, 2.15f, -0.68f), new Vector3(1.52f, 0.06f, 0.74f), 6);
            BuildRoofFan(root, "Factory Left Roof Fan", new Vector3(-1.45f, 2.28f, -1.56f), 0.72f);
            BuildRoofFan(root, "Factory Right Roof Fan", new Vector3(1.45f, 2.28f, -1.56f), 0.72f);
            BuildSmokeStack(root, "Factory Rear Stack A", new Vector3(2.64f, 2.68f, -2.24f), 0.28f, 1.22f, true);
            BuildSmokeStack(root, "Factory Rear Stack B", new Vector3(3.26f, 2.24f, -1.62f), 0.2f, 0.82f, true);
            BuildSmokeStack(root, "Factory Rear Stack C", new Vector3(2.12f, 2.18f, -1.82f), 0.18f, 0.74f, false);
            CreatePipe(root, "Factory Left Wall Pipe", new Vector3(-2.92f, 1.08f, -0.72f), new Vector3(0.12f, 1.55f, 0.12f), new Vector3(90f, 0f, 0f));
            CreatePipe(root, "Factory Right Wall Pipe", new Vector3(2.92f, 1.1f, -0.68f), new Vector3(0.12f, 1.58f, 0.12f), new Vector3(90f, 0f, 0f));
            BuildCatwalkRail(root, "Factory Right Catwalk", new Vector3(3.98f, 1.28f, -0.45f), 2.4f);
            CreatePrimitive(PrimitiveType.Cylinder, root, "Factory Antenna Left", new Vector3(-3.28f, 1.96f, -1.85f), new Vector3(0.055f, 0.84f, 0.055f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Sphere, root, "Factory Antenna Beacon", new Vector3(-3.28f, 2.82f, -1.85f), new Vector3(0.13f, 0.13f, 0.13f), accentMaterial, true);
            AddBoltLine(root, "Factory Header Bolts", new Vector3(-2.55f, 1.95f, 2.55f), new Vector3(0.46f, 0f, 0f), 12, 0.045f);
        }

        private void BuildPowerPlantVisual(Transform root, Material teamMaterial)
        {
            CreatePrimitive(PrimitiveType.Cube, root, "Power Foundation", new Vector3(0f, 0.1f, 0f), new Vector3(3.75f, 0.2f, 3.25f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Power Generator Block", new Vector3(-0.35f, 0.62f, 0.1f), new Vector3(2.35f, 1.1f, 2.45f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Power White Armor", new Vector3(-1.58f, 0.82f, 0.3f), new Vector3(0.08f, 0.62f, 1.45f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Power Team Panel", new Vector3(-0.35f, 1.02f, 1.36f), new Vector3(1.14f, 0.2f, 0.08f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Power Glow Core", new Vector3(0.92f, 0.82f, 0.12f), new Vector3(0.13f, 0.54f, 0.38f), accentMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Power Secondary Glow Cell A", new Vector3(0.925f, 0.82f, -0.46f), new Vector3(0.12f, 0.42f, 0.18f), accentMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Power Secondary Glow Cell B", new Vector3(0.925f, 0.82f, 0.68f), new Vector3(0.12f, 0.42f, 0.18f), accentMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Power Core Dark Guard Top", new Vector3(0.94f, 1.12f, 0.12f), new Vector3(0.05f, 0.075f, 0.58f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Power Core Dark Guard Bottom", new Vector3(0.94f, 0.52f, 0.12f), new Vector3(0.05f, 0.075f, 0.58f), armorShadowMaterial, true);
            for (int i = 0; i < 3; i++)
            {
                float z = -0.18f + i * 0.18f;
                CreatePrimitive(PrimitiveType.Cube, root, "Power Core Protective Slat", new Vector3(0.96f, 0.82f, z), new Vector3(0.045f, 0.62f, 0.03f), tankDeepGrimeMaterial, true);
            }
            CreatePrimitive(PrimitiveType.Cube, root, "Power Heat Sink", new Vector3(-0.35f, 1.28f, -0.65f), new Vector3(1.55f, 0.18f, 0.92f), trimMaterial, true);
            BuildVent(root, "Power Roof Vent", new Vector3(-0.4f, 1.48f, -0.62f), new Vector3(1.4f, 0.06f, 0.72f), 5);
            BuildSmokeStack(root, "Power Main Stack", new Vector3(1.1f, 1.82f, -0.9f), 0.42f, 1.3f, true);
            BuildSmokeStack(root, "Power Aux Stack", new Vector3(1.62f, 1.42f, 0.58f), 0.2f, 0.72f, false);
            CreatePipe(root, "Power Side Pipe A", new Vector3(1.18f, 0.7f, 0.95f), new Vector3(0.12f, 0.86f, 0.12f), new Vector3(90f, 0f, 0f));
            CreatePipe(root, "Power Side Pipe B", new Vector3(1.18f, 1.08f, 0.95f), new Vector3(0.09f, 0.86f, 0.09f), new Vector3(90f, 0f, 0f));
            AddPowerPlantProfessionalDetail(root, teamMaterial);
        }

        private void AddPowerPlantProfessionalDetail(Transform root, Material teamMaterial)
        {
            GameObject turbine = CreatePrimitive(PrimitiveType.Cylinder, root, "Power Raised Turbine Core", new Vector3(0.72f, 1.55f, -0.38f), new Vector3(0.56f, 0.55f, 0.56f), neutralMaterial, true);
            turbine.transform.localRotation = Quaternion.identity;
            CreatePrimitive(PrimitiveType.Cylinder, root, "Power Turbine Lower Soot Collar", new Vector3(0.72f, 1.18f, -0.38f), new Vector3(0.62f, 0.055f, 0.62f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, root, "Power Turbine Team Upper Collar", new Vector3(0.72f, 1.9f, -0.38f), new Vector3(0.6f, 0.055f, 0.6f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, root, "Power Turbine Top Armor Cap", new Vector3(0.72f, 2.15f, -0.38f), new Vector3(0.58f, 0.07f, 0.58f), armorHighlightMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Power Turbine Top White Hatch", new Vector3(0.48f, 2.24f, -0.48f), new Vector3(0.34f, 0.035f, 0.24f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Power Turbine Top Dirty Service Hatch", new Vector3(0.88f, 2.245f, -0.2f), new Vector3(0.3f, 0.035f, 0.22f), tankDustMaterial, true);
            BuildVent(root, "Power Turbine Cap Louver", new Vector3(0.72f, 2.255f, -0.68f), new Vector3(0.52f, 0.035f, 0.22f), 5);
            CreatePrimitive(PrimitiveType.Cube, root, "Power Turbine Cyan Gauge Face", new Vector3(1.24f, 1.62f, -0.08f), new Vector3(0.045f, 0.18f, 0.26f), glassMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Power Turbine Amber Breaker Strip", new Vector3(1.25f, 1.35f, -0.58f), new Vector3(0.04f, 0.22f, 0.34f), accentMaterial, true);

            CreatePrimitive(PrimitiveType.Cube, root, "Power Roof Transformer Deck", new Vector3(-0.64f, 1.46f, 0.62f), new Vector3(1.18f, 0.1f, 0.66f), armorShadowMaterial, true);
            for (int i = 0; i < 4; i++)
            {
                float x = -1.04f + i * 0.28f;
                CreatePrimitive(PrimitiveType.Cube, root, "Power Roof Transformer Coil", new Vector3(x, 1.62f, 0.62f), new Vector3(0.18f, 0.28f, 0.18f), pipeMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, root, "Power Transformer White Label", new Vector3(x, 1.79f, 0.75f), new Vector3(0.13f, 0.035f, 0.045f), whiteMaterial, true);
            }

            CreatePrimitive(PrimitiveType.Cube, root, "Power Front Control Cabinet A", new Vector3(-1.2f, 0.58f, 1.74f), new Vector3(0.36f, 0.48f, 0.18f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Power Front Control Cabinet B", new Vector3(-0.72f, 0.58f, 1.74f), new Vector3(0.34f, 0.48f, 0.18f), pipeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Power Control Cabinet Cyan Meters", new Vector3(-1.2f, 0.72f, 1.86f), new Vector3(0.22f, 0.08f, 0.045f), glowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Power Control Cabinet Amber Switches", new Vector3(-0.72f, 0.72f, 1.86f), new Vector3(0.2f, 0.08f, 0.045f), accentMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Power Front Lower Soot Kick Plate", new Vector3(0f, 0.42f, 1.76f), new Vector3(2.5f, 0.08f, 0.05f), tankDeepGrimeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Power Roof White Armor Plate A", new Vector3(-0.82f, 1.32f, -0.08f), new Vector3(0.48f, 0.035f, 0.36f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Power Roof Dirty Armor Plate B", new Vector3(-0.22f, 1.33f, 0.18f), new Vector3(0.42f, 0.035f, 0.34f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Power Roof Dark Center Seam", new Vector3(-0.52f, 1.36f, 0.08f), new Vector3(0.045f, 0.03f, 0.92f), armorShadowMaterial, true);
            CreatePipe(root, "Power Roof Heavy Conduit A", new Vector3(0.04f, 1.54f, 0.86f), new Vector3(0.05f, 1.25f, 0.05f), new Vector3(0f, 0f, 90f));
            CreatePipe(root, "Power Roof Heavy Conduit B", new Vector3(0.12f, 1.86f, -0.42f), new Vector3(0.045f, 0.92f, 0.045f), new Vector3(0f, 0f, 90f));
            BuildCatwalkRail(root, "Power Turbine Catwalk", new Vector3(1.58f, 1.42f, -0.34f), 1.45f);
            AddBoltLine(root, "Power Turbine Cap Bolt Run", new Vector3(0.28f, 2.25f, -0.82f), new Vector3(0.15f, 0f, 0f), 7, 0.018f);
        }

        private Transform BuildTurretVisual(Transform root, Material teamMaterial)
        {
            CreatePrimitive(PrimitiveType.Cylinder, root, "Turret Foundation", new Vector3(0f, 0.18f, 0f), new Vector3(1.72f, 0.18f, 1.72f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, root, "Turret Armor Base", new Vector3(0f, 0.48f, 0f), new Vector3(1.25f, 0.42f, 1.25f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, root, "Turret Bearing Ring", new Vector3(0f, 0.85f, 0f), new Vector3(0.92f, 0.12f, 0.92f), trimMaterial, true);
            GameObject head = CreatePrimitive(PrimitiveType.Cube, root, "Turret Armor Head", new Vector3(0f, 1.1f, 0f), new Vector3(1.28f, 0.52f, 1.18f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, head.transform, "Turret White Face Plate", new Vector3(0f, 0.03f, 0.62f), new Vector3(0.82f, 0.24f, 0.07f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, head.transform, "Turret Barrel", new Vector3(0f, 0f, 0.9f), new Vector3(0.16f, 0.78f, 0.16f), darkMaterial, true).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cylinder, head.transform, "Turret Barrel Sleeve", new Vector3(0f, 0f, 0.47f), new Vector3(0.24f, 0.18f, 0.24f), pipeMaterial, true).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cube, head.transform, "Turret Sight Glow", new Vector3(0f, 0.18f, 0.62f), new Vector3(0.34f, 0.08f, 0.08f), glowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Turret Raised Roof Equipment Block", new Vector3(-0.28f, 1.52f, -0.16f), new Vector3(0.42f, 0.16f, 0.34f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Turret Roof White Access Plate", new Vector3(-0.28f, 1.63f, -0.16f), new Vector3(0.32f, 0.035f, 0.24f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Turret Roof Dark Sensor Fin", new Vector3(0.32f, 1.58f, -0.22f), new Vector3(0.08f, 0.24f, 0.34f), armorShadowMaterial, true);
            BuildVent(root, "Turret Roof Cooling Louver", new Vector3(0.28f, 1.52f, 0.08f), new Vector3(0.38f, 0.035f, 0.24f), 4);
            CreatePrimitive(PrimitiveType.Cube, root, "Turret Front Upper White Armor Patch", new Vector3(-0.3f, 1.24f, 0.62f), new Vector3(0.34f, 0.18f, 0.055f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Turret Front Upper Dirty Armor Patch", new Vector3(0.31f, 1.23f, 0.62f), new Vector3(0.32f, 0.18f, 0.055f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Turret Left Wall Service Panel", new Vector3(-0.66f, 1.04f, 0.0f), new Vector3(0.045f, 0.32f, 0.46f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Turret Right Wall White Service Panel", new Vector3(0.66f, 1.04f, -0.02f), new Vector3(0.045f, 0.3f, 0.42f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Turret Mantlet Recoil Housing", new Vector3(0f, 0.98f, 0.82f), new Vector3(0.56f, 0.18f, 0.24f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Turret Mantlet Team Warning Strip", new Vector3(0f, 1.1f, 0.96f), new Vector3(0.44f, 0.055f, 0.045f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Turret Barrel Top Heat Shield Plate", new Vector3(0f, 1.08f, 1.42f), new Vector3(0.3f, 0.06f, 0.72f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Turret Barrel Underside Soot Guard", new Vector3(0f, 0.94f, 1.36f), new Vector3(0.24f, 0.05f, 0.58f), tankDeepGrimeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Turret Base Front Service Plate", new Vector3(0f, 0.62f, 1.0f), new Vector3(0.92f, 0.14f, 0.055f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Turret Base Left Power Cabinet", new Vector3(-1.0f, 0.38f, 0.24f), new Vector3(0.28f, 0.3f, 0.34f), pipeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Turret Base Right Ammo Cabinet", new Vector3(1.0f, 0.38f, 0.24f), new Vector3(0.28f, 0.3f, 0.34f), pipeMaterial, true);
            AddBoltLine(root, "Turret Direct Roof Rivets", new Vector3(-0.42f, 1.64f, 0.14f), new Vector3(0.16f, 0f, 0f), 6, 0.016f);
            AddTurretProfessionalFinish(root, teamMaterial);
            return head.transform;
        }

        private void AddTurretProfessionalFinish(Transform root, Material teamMaterial)
        {
            Transform kit = CreateVisualRoot(root, "Turret Professional Finish Kit", Vector3.zero);
            kit.gameObject.AddComponent<RtsFixedMaterial>();

            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Compact Concrete Pad", new Vector3(0f, 0.012f, 0f), new Vector3(4.05f, 0.045f, 3.85f), concreteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Pad Front Curb", new Vector3(0f, 0.075f, 1.93f), new Vector3(4.05f, 0.09f, 0.08f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Pad Rear Curb", new Vector3(0f, 0.075f, -1.93f), new Vector3(4.05f, 0.09f, 0.08f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Pad Left Curb", new Vector3(-2.02f, 0.075f, 0f), new Vector3(0.08f, 0.09f, 3.85f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Pad Right Curb", new Vector3(2.02f, 0.075f, 0f), new Vector3(0.08f, 0.09f, 3.85f), tankBareMetalMaterial, true);
            AddRoofPanelGrid(kit, "Turret Pad Tile Grid", new Vector3(0f, 0.105f, 0f), 3.6f, 3.4f, 4, 4);

            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Front White Lower Armor Shell", new Vector3(0f, 0.72f, 1.12f), new Vector3(0.98f, 0.19f, 0.045f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Front Dirty Lower Armor Shell", new Vector3(0.38f, 0.9f, 1.12f), new Vector3(0.36f, 0.13f, 0.04f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Front Team Warning Stripe", new Vector3(0f, 0.84f, 1.155f), new Vector3(0.72f, 0.05f, 0.035f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Left White Cheek Shell", new Vector3(-0.72f, 1.1f, 0.18f), new Vector3(0.045f, 0.4f, 0.58f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Right Scuffed Cheek Shell", new Vector3(0.72f, 1.08f, 0.14f), new Vector3(0.045f, 0.36f, 0.56f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Head Top Off White Armor Plate", new Vector3(-0.18f, 1.385f, -0.06f), new Vector3(0.54f, 0.048f, 0.58f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Head Top Dirty Service Plate", new Vector3(0.34f, 1.39f, 0.12f), new Vector3(0.38f, 0.046f, 0.42f), tankDustMaterial, true);
            AddRoofPanelGrid(kit, "Turret Head Fine Armor Seams", new Vector3(0f, 1.415f, -0.04f), 1.02f, 0.82f, 3, 3);

            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Mantlet Recoil Rail Left", new Vector3(-0.24f, 0.96f, 1.34f), new Vector3(0.055f, 0.065f, 0.82f), tankDeepGrimeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Mantlet Recoil Rail Right", new Vector3(0.24f, 0.96f, 1.34f), new Vector3(0.055f, 0.065f, 0.82f), tankDeepGrimeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Barrel Upper Polished Wear Strip", new Vector3(0f, 1.13f, 1.5f), new Vector3(0.24f, 0.045f, 0.86f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Barrel Lower Soot Strip", new Vector3(0f, 0.9f, 1.48f), new Vector3(0.22f, 0.04f, 0.78f), tankDeepGrimeMaterial, true);
            for (int i = 0; i < 3; i++)
            {
                float z = 1.08f + i * 0.34f;
                GameObject collar = CreatePrimitive(PrimitiveType.Cylinder, kit, "Turret Barrel Final Cooling Collar", new Vector3(0f, 1.0f, z), new Vector3(0.18f - i * 0.014f, 0.036f, 0.18f - i * 0.014f), i % 2 == 0 ? pipeMaterial : tankBareMetalMaterial, true);
                collar.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            BuildVent(kit, "Turret Roof Intake Array", new Vector3(0.28f, 1.445f, -0.34f), new Vector3(0.42f, 0.035f, 0.22f), 5);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Cyan Targeting Lens Left", new Vector3(-0.28f, 1.08f, 1.16f), new Vector3(0.13f, 0.06f, 0.035f), glowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Cyan Targeting Lens Right", new Vector3(0.28f, 1.08f, 1.16f), new Vector3(0.13f, 0.06f, 0.035f), glowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Amber Safety Marker Left", new Vector3(-0.48f, 0.88f, 1.16f), new Vector3(0.08f, 0.08f, 0.035f), accentMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Amber Safety Marker Right", new Vector3(0.48f, 0.88f, 1.16f), new Vector3(0.08f, 0.08f, 0.035f), accentMaterial, true);

            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Left Anchored Ammo Locker", new Vector3(-1.18f, 0.32f, -0.62f), new Vector3(0.42f, 0.32f, 0.48f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Right Anchored Power Locker", new Vector3(1.18f, 0.34f, -0.58f), new Vector3(0.42f, 0.34f, 0.48f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Left Locker White Label", new Vector3(-1.18f, 0.48f, -0.36f), new Vector3(0.28f, 0.055f, 0.04f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Right Locker Team Label", new Vector3(1.18f, 0.5f, -0.32f), new Vector3(0.28f, 0.055f, 0.04f), teamMaterial, true);
            CreatePipe(kit, "Turret Pad Left Cable Run", new Vector3(-1.42f, 0.14f, 0.38f), new Vector3(0.045f, 1.36f, 0.045f), new Vector3(0f, 0f, 90f));
            CreatePipe(kit, "Turret Pad Right Cable Run", new Vector3(1.42f, 0.14f, 0.38f), new Vector3(0.045f, 1.36f, 0.045f), new Vector3(0f, 0f, 90f));
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Front Anchored Maintenance Hatch", new Vector3(0f, 0.17f, 1.54f), new Vector3(0.78f, 0.055f, 0.22f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Front Hatch Team Stripe", new Vector3(0f, 0.215f, 1.54f), new Vector3(0.52f, 0.028f, 0.04f), teamMaterial, true);
            AddBoltLine(kit, "Turret Professional Mantlet Bolts", new Vector3(-0.34f, 1.18f, 1.16f), new Vector3(0.17f, 0f, 0f), 5, 0.014f);
            AddBoltLine(kit, "Turret Professional Base Rivets", new Vector3(-0.66f, 0.74f, 0.98f), new Vector3(0.22f, 0f, 0f), 7, 0.016f);
        }

        private void BuildVent(Transform root, string name, Vector3 center, Vector3 size, int slats)
        {
            CreatePrimitive(PrimitiveType.Cube, root, name + " Recess", center, size, darkMaterial, true);
            float start = -size.x * 0.38f;
            float step = slats <= 1 ? 0f : size.x * 0.76f / (slats - 1);
            for (int i = 0; i < slats; i++)
            {
                Vector3 slatPosition = center + new Vector3(start + step * i, size.y * 0.85f, 0f);
                CreatePrimitive(PrimitiveType.Cube, root, name + " Slat", slatPosition, new Vector3(size.x * 0.055f, size.y * 0.8f, size.z * 0.92f), trimMaterial, true);
            }
        }

        private void BuildRoofFan(Transform root, string name, Vector3 center, float radius)
        {
            Transform fan = CreateVisualRoot(root, name, center);
            CreatePrimitive(PrimitiveType.Cylinder, fan, "Fan Housing", Vector3.zero, new Vector3(radius, 0.08f, radius), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, fan, "Fan Inner Glow", new Vector3(0f, 0.045f, 0f), new Vector3(radius * 0.55f, 0.035f, radius * 0.55f), glassMaterial, true);

            Transform blades = CreateVisualRoot(fan, "Fan Blades", new Vector3(0f, 0.11f, 0f));
            RtsSpinner spinner = blades.gameObject.AddComponent<RtsSpinner>();
            spinner.DegreesPerSecond = 520f;
            CreatePrimitive(PrimitiveType.Cube, blades, "Fan Blade A", Vector3.zero, new Vector3(radius * 0.16f, 0.025f, radius * 1.35f), pipeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, blades, "Fan Blade B", Vector3.zero, new Vector3(radius * 1.35f, 0.025f, radius * 0.16f), pipeMaterial, true);
        }

        private void BuildSmokeStack(Transform root, string name, Vector3 center, float radius, float height, bool emitsSmoke)
        {
            CreatePrimitive(PrimitiveType.Cylinder, root, name + " Body", center, new Vector3(radius, height, radius), pipeMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, root, name + " Armor Ring A", center + new Vector3(0f, height * 0.56f, 0f), new Vector3(radius * 1.16f, 0.08f, radius * 1.16f), armorHighlightMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, root, name + " Armor Ring B", center - new Vector3(0f, height * 0.48f, 0f), new Vector3(radius * 1.12f, 0.07f, radius * 1.12f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, name + " Orange Heat Strip", center + new Vector3(radius * 1.05f, 0f, 0f), new Vector3(0.06f, height * 0.68f, 0.08f), accentMaterial, true);

            if (!emitsSmoke)
            {
                return;
            }

            Transform emitter = CreateVisualRoot(root, name + " Smoke Socket", center + new Vector3(0f, height * 1.05f, 0f));
            RtsSmokeEmitter smokeEmitter = emitter.gameObject.AddComponent<RtsSmokeEmitter>();
            smokeEmitter.Interval = 0.55f;
            smokeEmitter.Radius = radius * 0.42f;
            smokeEmitter.MaxScale = radius * 2.6f;
        }

        private void CreatePipe(Transform root, string name, Vector3 center, Vector3 scale, Vector3 euler)
        {
            GameObject pipe = CreatePrimitive(PrimitiveType.Cylinder, root, name, center, scale, pipeMaterial, true);
            pipe.transform.localRotation = Quaternion.Euler(euler);
        }

        private void BuildCatwalkRail(Transform root, string name, Vector3 center, float length)
        {
            CreatePrimitive(PrimitiveType.Cube, root, name + " Deck", center - new Vector3(0f, 0.16f, 0f), new Vector3(0.92f, 0.08f, length), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, name + " Top Rail", center + new Vector3(0f, 0.18f, 0f), new Vector3(0.08f, 0.06f, length), accentMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, name + " Mid Rail", center, new Vector3(0.07f, 0.05f, length), accentMaterial, true);
            for (int i = 0; i < 4; i++)
            {
                float z = -length * 0.42f + i * length * 0.28f;
                CreatePrimitive(PrimitiveType.Cube, root, name + " Rail Post", center + new Vector3(0f, 0.02f, z), new Vector3(0.08f, 0.42f, 0.06f), accentMaterial, true);
            }
        }

        private void BuildRobotArm(Transform root, string name, Vector3 basePosition, float side)
        {
            Transform arm = CreateVisualRoot(root, name, basePosition);
            CreatePrimitive(PrimitiveType.Cylinder, arm, "Robot Base", Vector3.zero, new Vector3(0.22f, 0.16f, 0.22f), accentMaterial, true);
            GameObject lower = CreatePrimitive(PrimitiveType.Cube, arm, "Robot Lower Arm", new Vector3(side * 0.18f, 0.38f, 0.06f), new Vector3(0.16f, 0.72f, 0.14f), trimMaterial, true);
            lower.transform.localRotation = Quaternion.Euler(0f, 0f, side * -24f);
            GameObject upper = CreatePrimitive(PrimitiveType.Cube, arm, "Robot Upper Arm", new Vector3(side * 0.45f, 0.82f, -0.08f), new Vector3(0.14f, 0.66f, 0.13f), trimMaterial, true);
            upper.transform.localRotation = Quaternion.Euler(side * 28f, 0f, side * 36f);
            CreatePrimitive(PrimitiveType.Sphere, arm, "Robot Shoulder Joint", new Vector3(side * 0.16f, 0.68f, 0.04f), new Vector3(0.19f, 0.19f, 0.19f), accentMaterial, true);
            CreatePrimitive(PrimitiveType.Sphere, arm, "Robot Elbow Joint", new Vector3(side * 0.46f, 1.05f, -0.08f), new Vector3(0.16f, 0.16f, 0.16f), accentMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, arm, "Robot Tool Glow", new Vector3(side * 0.56f, 1.22f, -0.2f), new Vector3(0.22f, 0.08f, 0.08f), glowMaterial, true);
        }

        private void AddHazardStripes(Transform root, string name, float x)
        {
            for (int i = 0; i < 5; i++)
            {
                float z = 2.58f + i * 0.28f;
                GameObject stripe = CreatePrimitive(PrimitiveType.Cube, root, name, new Vector3(x, 0.32f, z), new Vector3(0.18f, 0.035f, 0.52f), hazardMaterial, true);
                stripe.transform.localRotation = Quaternion.Euler(0f, 24f, 0f);
            }
        }

        private void AddBoltLine(Transform root, string name, Vector3 start, Vector3 step, int count, float size)
        {
            for (int i = 0; i < count; i++)
            {
                CreatePrimitive(PrimitiveType.Cube, root, name, start + step * i, new Vector3(size, size, size), darkMaterial, true);
            }
        }


        private void AddUnitCollider(GameObject root, UnitKind kind)
        {
            if (kind == UnitKind.Skyraider || kind == UnitKind.OrcaLifter)
            {
                BoxCollider airBox = root.AddComponent<BoxCollider>();
                airBox.center = new Vector3(0f, 1.55f, 0f);
                airBox.size = kind == UnitKind.Skyraider ? new Vector3(3.4f, 1.2f, 4.2f) : new Vector3(4.8f, 1.3f, 4.1f);
                return;
            }

            if (kind == UnitKind.Tank || kind == UnitKind.Harvester)
            {
                BoxCollider box = root.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.55f, 0f);
                box.size = kind == UnitKind.Tank ? new Vector3(2.15f, 1.25f, 3.15f) : new Vector3(2.2f, 1.35f, 3.3f);
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

            if (kind == UnitKind.Skyraider)
            {
                if (TryBuildAssetStoreVehicleVisual(root, kind, teamMaterial))
                {
                    return;
                }

                if (TryBuildKitbashedSkyraiderVisual(root, teamMaterial))
                {
                    return;
                }

                BuildSkyraiderVisual(root, teamMaterial);
                return;
            }

            if (kind == UnitKind.OrcaLifter)
            {
                if (TryBuildAssetStoreVehicleVisual(root, kind, teamMaterial))
                {
                    return;
                }

                if (TryBuildKitbashedOrcaLifterVisual(root, teamMaterial))
                {
                    return;
                }

                BuildOrcaLifterVisual(root, teamMaterial);
                return;
            }

            if (kind == UnitKind.Tank)
            {
                if (TryBuildAssetStoreVehicleVisual(root, kind, teamMaterial))
                {
                    return;
                }

                if (TryBuildImportedTankVisual(root, teamMaterial))
                {
                    return;
                }

                BuildBattleTankVisual(root, teamMaterial);
                return;
            }

            if (kind == UnitKind.Harvester)
            {
                if (TryBuildAssetStoreVehicleVisual(root, kind, teamMaterial))
                {
                    return;
                }

                if (TryBuildKitbashedHarvesterVisual(root, teamMaterial))
                {
                    return;
                }

                BuildHarvesterVisual(root, teamMaterial);
                return;
            }

            if (TryBuildImportedInfantryVisual(root, kind, teamMaterial))
            {
                AddImportedUnitTeamMarkers(root, kind, teamMaterial);
                return;
            }

            BuildRiflemanVisual(root, teamMaterial);
            AddInfantryProfessionalFinish(root, kind, teamMaterial);
        }

        private bool TryBuildAssetStoreVehicleVisual(Transform root, UnitKind kind, Material teamMaterial)
        {
            string resourcePath;
            float scale;
            Vector3 rotation;
            Vector3 offset = Vector3.zero;

            switch (kind)
            {
                case UnitKind.Tank:
                    resourcePath = "AssetStore/Vehicles/ItHappy_Tank_006";
                    scale = 0.58f;
                    rotation = Vector3.zero;
                    break;
                case UnitKind.Harvester:
                    resourcePath = UnityEngine.Resources.Load<GameObject>("AssetStore/Vehicles/ItHappy_Hummer_003") != null
                        ? "AssetStore/Vehicles/ItHappy_Hummer_003"
                        : "AssetStore/Vehicles/Hipernt_Truck";
                    scale = 0.62f;
                    rotation = Vector3.zero;
                    offset = new Vector3(0f, 0f, -0.08f);
                    break;
                case UnitKind.Skyraider:
                    resourcePath = "AssetStore/Vehicles/Hipernt_A10";
                    scale = 0.24f;
                    rotation = new Vector3(0f, 180f, 0f);
                    offset = new Vector3(0f, 0.62f, 0f);
                    break;
                case UnitKind.OrcaLifter:
                    resourcePath = UnityEngine.Resources.Load<GameObject>("AssetStore/Vehicles/ItHappy_Helicopter_001") != null
                        ? "AssetStore/Vehicles/ItHappy_Helicopter_001"
                        : "AssetStore/Vehicles/Hipernt_AH64";
                    scale = 0.32f;
                    rotation = new Vector3(0f, 180f, 0f);
                    offset = new Vector3(0f, 0.55f, 0f);
                    break;
                default:
                    return false;
            }

            GameObject visual = InstantiateAssetStorePrefab(root, resourcePath, kind + " Asset Store Visual", offset, rotation, scale);
            if (visual == null)
            {
                return false;
            }

            ApplyAssetStoreVehicleFinish(visual, kind, teamMaterial);
            NormalizeImportedVehicleVisual(visual, kind, offset);
            AddAssetStoreRotorSpin(visual);
            Material vehicleTeamMaterial = CreateVehicleTeamDetailMaterial(teamMaterial);

            switch (kind)
            {
                case UnitKind.Tank:
                    AddImportedTankLightDetailKit(root, vehicleTeamMaterial);
                    AddTankProfessionalFinish(root, vehicleTeamMaterial);
                    AddVehicleReadabilityKit(root, UnitKind.Tank, vehicleTeamMaterial);
                    break;
                case UnitKind.Harvester:
                    AddHarvesterConversionKit(root, vehicleTeamMaterial);
                    AddImportedUnitTeamMarkers(root, UnitKind.Harvester, vehicleTeamMaterial);
                    AddVehicleReadabilityKit(root, UnitKind.Harvester, vehicleTeamMaterial);
                    break;
                case UnitKind.Skyraider:
                    AddAssetStoreAircraftHardpoints(root, UnitKind.Skyraider, vehicleTeamMaterial);
                    AddVehicleReadabilityKit(root, UnitKind.Skyraider, vehicleTeamMaterial);
                    break;
                case UnitKind.OrcaLifter:
                    AddAssetStoreAircraftHardpoints(root, UnitKind.OrcaLifter, vehicleTeamMaterial);
                    AddVehicleReadabilityKit(root, UnitKind.OrcaLifter, vehicleTeamMaterial);
                    break;
            }

            return true;
        }

        private void ApplyAssetStoreVehicleFinish(GameObject visual, UnitKind kind, Material teamMaterial)
        {
            Color armorTint = kind == UnitKind.Harvester
                ? new Color(0.48f, 0.46f, 0.34f)
                : new Color(0.58f, 0.61f, 0.50f);
            Color teamColor = GetMaterialDisplayColor(teamMaterial);

            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    Color tint = Color.Lerp(Color.white, armorTint, kind == UnitKind.Tank ? 0.18f : 0.12f);
                    if (source != null && source.name.ToLowerInvariant().Contains("blue"))
                    {
                        tint = Color.Lerp(teamColor, Color.white, 0.18f);
                    }

                    Material material = CreateRuntimeCompatibleMaterial(source, tint);
                    if (material.HasProperty("_BaseColor"))
                    {
                        material.SetColor("_BaseColor", tint);
                    }

                    if (material.HasProperty("_Color"))
                    {
                        material.SetColor("_Color", tint);
                    }

                    if (material.HasProperty("_Metallic"))
                    {
                        material.SetFloat("_Metallic", Mathf.Max(material.GetFloat("_Metallic"), 0.08f));
                    }

                    if (material.HasProperty("_Smoothness"))
                    {
                        material.SetFloat("_Smoothness", Mathf.Max(material.GetFloat("_Smoothness"), 0.24f));
                    }

                    if (material.HasProperty("_Glossiness"))
                    {
                        material.SetFloat("_Glossiness", Mathf.Max(material.GetFloat("_Glossiness"), 0.24f));
                    }

                    Texture texture = material.mainTexture;
                    if (texture != null)
                    {
                        texture.filterMode = FilterMode.Bilinear;
                        texture.anisoLevel = 4;
                    }

                    materials[i] = material;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private void NormalizeImportedVehicleVisual(GameObject visual, UnitKind kind, Vector3 desiredCenter)
        {
            if (visual == null || visual.transform.parent == null || !TryGetRendererBounds(visual, out Bounds bounds))
            {
                return;
            }

            float targetHorizontalSize;
            float desiredBottomY;
            switch (kind)
            {
                case UnitKind.Skyraider:
                    targetHorizontalSize = 3.25f;
                    desiredBottomY = 0.72f;
                    break;
                case UnitKind.OrcaLifter:
                    targetHorizontalSize = 3.55f;
                    desiredBottomY = 0.68f;
                    break;
                case UnitKind.Harvester:
                    targetHorizontalSize = 3.15f;
                    desiredBottomY = 0.03f;
                    break;
                default:
                    targetHorizontalSize = 3.05f;
                    desiredBottomY = 0.03f;
                    break;
            }

            float horizontalSize = Mathf.Max(bounds.size.x, bounds.size.z);
            if (horizontalSize > 0.01f)
            {
                float scaleFactor = Mathf.Clamp(targetHorizontalSize / horizontalSize, 0.12f, 4f);
                visual.transform.localScale *= scaleFactor;
            }

            if (!TryGetRendererBounds(visual, out bounds))
            {
                return;
            }

            Transform parent = visual.transform.parent;
            Vector3 centerLocal = parent.InverseTransformPoint(bounds.center);
            Vector3 bottomLocal = parent.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            Vector3 localPosition = visual.transform.localPosition;
            localPosition.x += desiredCenter.x - centerLocal.x;
            localPosition.z += desiredCenter.z - centerLocal.z;
            localPosition.y += desiredBottomY - bottomLocal.y;
            visual.transform.localPosition = localPosition;
        }

        private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
        {
            bounds = new Bounds();
            if (root == null)
            {
                return false;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private void AddAssetStoreRotorSpin(GameObject visual)
        {
            foreach (Transform child in visual.GetComponentsInChildren<Transform>(true))
            {
                string childName = child.name.ToLowerInvariant();
                if (!childName.Contains("blade") && !childName.Contains("rotor") && !childName.Contains("propeller"))
                {
                    continue;
                }

                if (child.GetComponent<RtsSpinner>() != null)
                {
                    continue;
                }

                RtsSpinner spinner = child.gameObject.AddComponent<RtsSpinner>();
                spinner.Axis = Vector3.up;
                spinner.DegreesPerSecond = 980f;
            }
        }

        private void AddAssetStoreAircraftHardpoints(Transform root, UnitKind kind, Material teamMaterial)
        {
            float wingY = kind == UnitKind.Skyraider ? 0.76f : 0.64f;
            float wingZ = kind == UnitKind.Skyraider ? 0.08f : -0.12f;
            float sideDistance = kind == UnitKind.Skyraider ? 1.2f : 1.05f;

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                string prefix = side < 0f ? "Left" : "Right";
                CreatePrimitive(PrimitiveType.Cube, root, prefix + " Imported Aircraft Team Wing Stripe", new Vector3(side * sideDistance, wingY + 0.08f, wingZ), new Vector3(0.46f, 0.04f, 0.12f), teamMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, root, prefix + " Imported Aircraft Dirty Service Plate", new Vector3(side * (sideDistance * 0.82f), wingY + 0.05f, wingZ - 0.34f), new Vector3(0.36f, 0.035f, 0.24f), tankDustMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, root, prefix + " Imported Aircraft Underwing Rail", new Vector3(side * sideDistance, wingY - 0.18f, wingZ + 0.28f), new Vector3(0.12f, 0.06f, 0.72f), pipeMaterial, true);
                AddBoltLine(root, prefix + " Imported Aircraft Wing Rivets", new Vector3(side * (sideDistance * 0.72f), wingY + 0.12f, wingZ - 0.28f), new Vector3(side * 0.14f, 0f, 0f), 5, 0.014f);
            }

            CreatePrimitive(PrimitiveType.Cube, root, "Imported Aircraft Cyan Sensor", new Vector3(0f, wingY + 0.02f, 1.14f), new Vector3(0.2f, 0.055f, 0.045f), subtleGlowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Imported Aircraft Amber Service Beacon", new Vector3(0f, wingY + 0.18f, -1.05f), new Vector3(0.18f, 0.052f, 0.055f), accentMaterial, true);
        }

        private bool TryBuildImportedInfantryVisual(Transform root, UnitKind kind, Material teamMaterial)
        {
            switch (kind)
            {
                case UnitKind.RocketSoldier:
                    return TryBuildImportedUnitVisual(root, "UnitModels/Bastion_RocketSoldier", "Bastion Rocket Soldier Visual", teamMaterial, 0.72f, kind);
                case UnitKind.Grenadier:
                    return TryBuildImportedUnitVisual(root, "UnitModels/Bastion_Grenadier", "Bastion Grenadier Visual", teamMaterial, 0.72f, kind);
                case UnitKind.FlameTrooper:
                    return TryBuildImportedUnitVisual(root, "UnitModels/Bastion_FlameTrooper", "Bastion Flame Trooper Visual", teamMaterial, 0.72f, kind);
                case UnitKind.Engineer:
                    return TryBuildImportedUnitVisual(root, "UnitModels/Bastion_Engineer", "Bastion Engineer Visual", teamMaterial, 0.72f, kind);
                default:
                    return TryBuildImportedUnitVisual(root, "UnitModels/Bastion_Gunner", "Bastion Gunner Visual", teamMaterial, 0.72f, kind);
            }
        }

        private bool TryBuildImportedUnitVisual(Transform root, string resourcePath, string visualName, Material teamMaterial, float scale, UnitKind kind)
        {
            GameObject prefab = UnityEngine.Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                return false;
            }

            GameObject visual = Instantiate(prefab, root, false);
            visual.name = visualName;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * scale;
            visual.AddComponent<RtsFixedMaterial>();
            if (kind == UnitKind.Harvester)
            {
                ImproveImportedAssetRenderers(visual);
            }
            else
            {
                ImproveImportedInfantryRenderers(visual, kind, teamMaterial);
            }

            RemoveImportedColliders(visual);
            return true;
        }

        private bool TryBuildImportedTankVisual(Transform root, Material teamMaterial)
        {
            GameObject prefab = UnityEngine.Resources.Load<GameObject>("UnitModels/Bastion_TX9R");
            float scale = 0.42f;
            bool isReferenceStyleTank = prefab != null;

            if (prefab == null)
            {
                prefab = UnityEngine.Resources.Load<GameObject>("UnitModels/Bastion_HT77_Mammoth");
                scale = 0.28f;
            }

            if (prefab == null)
            {
                prefab = UnityEngine.Resources.Load<GameObject>("UnitModels/Bastion_MT12_Rider");
                scale = 0.28f;
            }

            if (prefab == null)
            {
                return false;
            }

            GameObject visual = Instantiate(prefab, root, false);
            visual.name = prefab.name + " Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * scale;
            visual.AddComponent<RtsFixedMaterial>();
            ImproveImportedTankRenderers(visual);

            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            {
                DestroyGeneratedObject(collider);
            }

            if (isReferenceStyleTank)
            {
                AddImportedTankLightDetailKit(root, teamMaterial);
                AddTankProfessionalFinish(root, teamMaterial);
                AddVehicleReadabilityKit(root, UnitKind.Tank, teamMaterial);
            }

            return true;
        }

        private bool TryBuildKitbashedHarvesterVisual(Transform root, Material teamMaterial)
        {
            GameObject prefab = UnityEngine.Resources.Load<GameObject>("UnitModels/Bastion_TX9R");
            if (prefab == null)
            {
                if (TryBuildImportedUnitVisual(root, "UnitModels/Bastion_Harvester", "Bastion Harvester Visual", teamMaterial, 0.53f, UnitKind.Harvester))
                {
                    ReplaceImportedHarvesterCollector(root);
                    AddImportedUnitTeamMarkers(root, UnitKind.Harvester, teamMaterial);
                    AddVehicleReadabilityKit(root, UnitKind.Harvester, CreateVehicleTeamDetailMaterial(teamMaterial));
                    return true;
                }

                return false;
            }

            GameObject chassis = Instantiate(prefab, root, false);
            chassis.name = "TX-9R Harvester Chassis";
            chassis.transform.localPosition = Vector3.zero;
            chassis.transform.localRotation = Quaternion.identity;
            chassis.transform.localScale = Vector3.one * 0.43f;
            chassis.AddComponent<RtsFixedMaterial>();
            ImproveImportedTankRenderers(chassis);
            RemoveImportedColliders(chassis);
            RemoveTankWeaponChildren(chassis.transform);

            AddHarvesterConversionKit(root, teamMaterial);
            return true;
        }

        private void AddHarvesterConversionKit(Transform root, Material teamMaterial)
        {
            teamMaterial = CreateVehicleTeamDetailMaterial(teamMaterial);
            Transform kit = CreateVisualRoot(root, "Harvester Conversion Kit", Vector3.zero);
            kit.gameObject.AddComponent<RtsFixedMaterial>();

            CreatePrimitive(PrimitiveType.Cube, kit, "Armored Ore Hopper", new Vector3(0f, 0.72f, -0.42f), new Vector3(1.22f, 0.44f, 0.9f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Dark Ore Hopper Interior", new Vector3(0f, 0.98f, -0.42f), new Vector3(1.02f, 0.05f, 0.68f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Rear Hopper Armor Lip", new Vector3(0f, 1.05f, -0.88f), new Vector3(1.18f, 0.16f, 0.12f), armorHighlightMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Front Hopper Armor Lip", new Vector3(0f, 1.02f, 0.02f), new Vector3(1.06f, 0.13f, 0.1f), armorHighlightMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Hopper Wear Plate", new Vector3(-0.63f, 0.78f, -0.42f), new Vector3(0.035f, 0.28f, 0.48f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Hopper Wear Plate", new Vector3(0.63f, 0.78f, -0.42f), new Vector3(0.035f, 0.28f, 0.48f), tankBareMetalMaterial, true);

            GameObject conveyor = CreatePrimitive(PrimitiveType.Cube, kit, "Ore Intake Conveyor", new Vector3(0f, 0.48f, 0.76f), new Vector3(0.62f, 0.12f, 1.1f), trimMaterial, true);
            conveyor.transform.localRotation = Quaternion.Euler(-9f, 0f, 0f);
            BuildVent(kit, "Conveyor Slatted Belt", new Vector3(0f, 0.56f, 0.78f), new Vector3(0.54f, 0.035f, 0.82f), 6);

            CreatePrimitive(PrimitiveType.Cube, kit, "Compact Crusher Intake Housing", new Vector3(0f, 0.36f, 1.31f), new Vector3(1.08f, 0.22f, 0.32f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Crusher Intake White Armor Face", new Vector3(0f, 0.44f, 1.47f), new Vector3(0.76f, 0.09f, 0.035f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Crusher Intake Amber Strip", new Vector3(0f, 0.53f, 1.48f), new Vector3(0.58f, 0.045f, 0.035f), accentMaterial, true);

            GameObject drum = CreatePrimitive(PrimitiveType.Cylinder, kit, "Short Rotary Collector Drum", new Vector3(0f, 0.27f, 1.46f), new Vector3(0.16f, 0.35f, 0.16f), tankBareMetalMaterial, true);
            drum.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            RtsSpinner spinner = drum.AddComponent<RtsSpinner>();
            spinner.DegreesPerSecond = 420f;

            for (int i = 0; i < 7; i++)
            {
                float x = -0.36f + i * 0.12f;
                GameObject tooth = CreatePrimitive(PrimitiveType.Cube, kit, "Collector Carbide Tooth", new Vector3(x, 0.24f, 1.62f), new Vector3(0.05f, 0.08f, 0.16f), tankBareMetalMaterial, true);
                tooth.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
            }

            CreatePrimitive(PrimitiveType.Cube, kit, "Left Collector Arm", new Vector3(-0.52f, 0.36f, 1.19f), new Vector3(0.14f, 0.18f, 0.46f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Collector Arm", new Vector3(0.52f, 0.36f, 1.19f), new Vector3(0.14f, 0.18f, 0.46f), armorShadowMaterial, true);

            CreatePipe(kit, "Left Side Slurry Conduit", new Vector3(-0.78f, 0.62f, 0.08f), new Vector3(0.055f, 0.58f, 0.055f), new Vector3(90f, 0f, 0f));
            CreatePipe(kit, "Right Side Slurry Conduit", new Vector3(0.78f, 0.62f, 0.08f), new Vector3(0.055f, 0.58f, 0.055f), new Vector3(90f, 0f, 0f));
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Conduit Armor Clamp", new Vector3(-0.78f, 0.65f, 0.42f), new Vector3(0.13f, 0.13f, 0.06f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Conduit Armor Clamp", new Vector3(0.78f, 0.65f, 0.42f), new Vector3(0.13f, 0.13f, 0.06f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, kit, "Left Ore Slurry Tank", new Vector3(-0.82f, 0.82f, -0.42f), new Vector3(0.12f, 0.28f, 0.12f), tankDustMaterial, true).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cylinder, kit, "Right Ore Slurry Tank", new Vector3(0.82f, 0.82f, -0.42f), new Vector3(0.12f, 0.28f, 0.12f), tankDustMaterial, true).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            BuildSmokeStack(kit, "Harvester Exhaust Stack", new Vector3(-0.46f, 1.05f, -0.86f), 0.065f, 0.46f, false);
            BuildVent(kit, "Hopper Engine Deck Vent", new Vector3(0.42f, 1.03f, -0.12f), new Vector3(0.34f, 0.035f, 0.38f), 4);

            CreatePrimitive(PrimitiveType.Cube, kit, "Left White Hopper Panel", new Vector3(-0.68f, 0.78f, -0.1f), new Vector3(0.05f, 0.22f, 0.34f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right White Hopper Panel", new Vector3(0.68f, 0.78f, -0.1f), new Vector3(0.05f, 0.22f, 0.34f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Amber Warning Bar", new Vector3(0f, 0.82f, 0.16f), new Vector3(0.42f, 0.04f, 0.045f), accentMaterial, true);
            AddBoltLine(kit, "Hopper Rim Bolts", new Vector3(-0.48f, 1.07f, -0.05f), new Vector3(0.16f, 0f, 0f), 7, 0.028f);
            AddHarvesterIndustrialDetail(kit, teamMaterial);
            AddVehicleReadabilityKit(kit, UnitKind.Harvester, teamMaterial);
        }

        private bool TryBuildKitbashedSkyraiderVisual(Transform root, Material teamMaterial)
        {
            teamMaterial = CreateVehicleTeamDetailMaterial(teamMaterial);
            Transform kit = CreateVisualRoot(root, "Skyraider Airframe Kit", Vector3.zero);
            kit.gameObject.AddComponent<RtsFixedMaterial>();

            BuildSkyraiderCoreAirframe(kit, teamMaterial);
            CreateWingSlab(kit, "Left Swept Armored Wing", -1f, new Vector3(-0.9f, 0.82f, -0.04f), 1.42f, 0.58f, 0.26f, 0.16f, concreteMaterial);
            CreateWingSlab(kit, "Right Swept Armored Wing", 1f, new Vector3(0.9f, 0.82f, -0.04f), 1.42f, 0.58f, 0.26f, 0.16f, concreteMaterial);
            CreateWingSlab(kit, "Left White Wing Armor Plate", -1f, new Vector3(-0.98f, 0.94f, -0.08f), 0.9f, 0.36f, 0.16f, 0.045f, whiteMaterial);
            CreateWingSlab(kit, "Right White Wing Armor Plate", 1f, new Vector3(0.98f, 0.94f, -0.08f), 0.9f, 0.36f, 0.16f, 0.045f, whiteMaterial);
            CreateWingSlab(kit, "Left Wing Dark Leading Seam", -1f, new Vector3(-0.96f, 0.965f, 0.18f), 1.08f, 0.08f, 0.045f, 0.03f, armorShadowMaterial);
            CreateWingSlab(kit, "Right Wing Dark Leading Seam", 1f, new Vector3(0.96f, 0.965f, 0.18f), 1.08f, 0.08f, 0.045f, 0.03f, armorShadowMaterial);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Hardpoint Pod", new Vector3(-1.65f, 0.62f, 0.12f), new Vector3(0.32f, 0.24f, 0.92f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Hardpoint Pod", new Vector3(1.65f, 0.62f, 0.12f), new Vector3(0.32f, 0.24f, 0.92f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Hardpoint Service Stripe", new Vector3(-1.65f, 0.76f, 0.12f), new Vector3(0.24f, 0.045f, 0.66f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Hardpoint Service Stripe", new Vector3(1.65f, 0.76f, 0.12f), new Vector3(0.24f, 0.045f, 0.66f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Missile Rail", new Vector3(-1.64f, 0.44f, 0.28f), new Vector3(0.12f, 0.07f, 0.86f), pipeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Missile Rail", new Vector3(1.64f, 0.44f, 0.28f), new Vector3(0.12f, 0.07f, 0.86f), pipeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Nose Sensor Glow", new Vector3(0f, 0.83f, 1.45f), new Vector3(0.16f, 0.052f, 0.042f), subtleGlowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Worn Wing Service Plate", new Vector3(-1.18f, 0.93f, -0.1f), new Vector3(0.42f, 0.04f, 0.3f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Worn Wing Service Plate", new Vector3(1.18f, 0.93f, -0.1f), new Vector3(0.42f, 0.04f, 0.3f), tankDustMaterial, true);
            CreateTaperedBox(kit, "Tail Boom Armor", new Vector3(0f, 0.78f, -1.18f), new Vector3(0.5f, 0.2f, 0.96f), new Vector3(0.28f, 0.14f, 0.5f), concreteMaterial);
            CreateFinSlab(kit, "Left Tail Fin", new Vector3(-0.34f, 1.05f, -1.48f), -1f, whiteMaterial);
            CreateFinSlab(kit, "Right Tail Fin", new Vector3(0.34f, 1.05f, -1.48f), 1f, whiteMaterial);
            CreatePrimitive(PrimitiveType.Cube, kit, "Tail Beacon Bar", new Vector3(0f, 1.13f, -1.66f), new Vector3(0.42f, 0.055f, 0.06f), accentMaterial, true);

            BuildVent(kit, "Skyraider Top Intake", new Vector3(0f, 1.1f, -0.38f), new Vector3(0.64f, 0.035f, 0.36f), 4);
            BuildVent(kit, "Left Wing Heat Vent", new Vector3(-0.72f, 0.97f, -0.38f), new Vector3(0.48f, 0.03f, 0.22f), 4);
            BuildVent(kit, "Right Wing Heat Vent", new Vector3(0.72f, 0.97f, -0.38f), new Vector3(0.48f, 0.03f, 0.22f), 4);
            BuildSkyraiderRotor(kit, new Vector3(0f, 1.34f, -0.12f), 2.75f);
            CreatePrimitive(PrimitiveType.Cube, kit, "Armored Nose Weapon Shroud", new Vector3(0f, 0.7f, 1.37f), new Vector3(0.62f, 0.14f, 0.3f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Nose Shroud White Service Plate", new Vector3(0f, 0.79f, 1.35f), new Vector3(0.42f, 0.045f, 0.18f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Nose Shroud Cyan Targeting Slit", new Vector3(0f, 0.72f, 1.54f), new Vector3(0.22f, 0.055f, 0.04f), subtleGlowMaterial, true);
            BuildWeaponBarrel(kit, "Left Short Nose Cannon", new Vector3(-0.18f, 0.66f, 1.55f), 0.028f, 0.32f);
            BuildWeaponBarrel(kit, "Right Short Nose Cannon", new Vector3(0.18f, 0.66f, 1.55f), 0.028f, 0.32f);
            AddSkyraiderSurfaceDetail(kit, teamMaterial);
            AddBoltLine(kit, "Skyraider Wing Root Bolts", new Vector3(-0.52f, 0.94f, 0.16f), new Vector3(0.17f, 0f, 0f), 7, 0.026f);
            AddBoltLine(kit, "Skyraider Right Wing Root Bolts", new Vector3(0.52f, 0.94f, 0.16f), new Vector3(0.17f, 0f, 0f), 7, 0.026f);
            AddSkyraiderAdvancedDetail(kit, teamMaterial);
            AddSkyraiderProfessionalFinish(kit, teamMaterial);
            AddVehicleReadabilityKit(kit, UnitKind.Skyraider, teamMaterial);
            return true;
        }

        private bool TryBuildKitbashedOrcaLifterVisual(Transform root, Material teamMaterial)
        {
            teamMaterial = CreateVehicleTeamDetailMaterial(teamMaterial);
            Transform kit = CreateVisualRoot(root, "Orca Lifter Airframe Kit", Vector3.zero);
            kit.gameObject.AddComponent<RtsFixedMaterial>();

            BuildOrcaCoreAirframe(kit, teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, kit, "Cargo Roof Center Seam", new Vector3(0f, 0.89f, -0.32f), new Vector3(0.055f, 0.035f, 1.1f), armorShadowMaterial, true);
            CreateWingSlab(kit, "Left Lift Boom", -1f, new Vector3(-1.34f, 0.72f, -0.12f), 1.16f, 0.34f, 0.22f, 0.16f, concreteMaterial);
            CreateWingSlab(kit, "Right Lift Boom", 1f, new Vector3(1.34f, 0.72f, -0.12f), 1.16f, 0.34f, 0.22f, 0.16f, concreteMaterial);
            CreateWingSlab(kit, "Left White Boom Fairing", -1f, new Vector3(-1.34f, 0.84f, -0.12f), 0.9f, 0.22f, 0.13f, 0.045f, whiteMaterial);
            CreateWingSlab(kit, "Right White Boom Fairing", 1f, new Vector3(1.34f, 0.84f, -0.12f), 0.9f, 0.22f, 0.13f, 0.045f, whiteMaterial);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Boom Service Stripe", new Vector3(-1.34f, 0.88f, 0.08f), new Vector3(0.62f, 0.04f, 0.055f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Boom Service Stripe", new Vector3(1.34f, 0.88f, 0.08f), new Vector3(0.62f, 0.04f, 0.055f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Rear Cargo Door", new Vector3(0f, 0.42f, -1.38f), new Vector3(0.78f, 0.12f, 0.42f), tankBareMetalMaterial, true).transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cube, kit, "Magnetic Cargo Clamp", new Vector3(0f, 0.18f, -0.2f), new Vector3(0.5f, 0.14f, 0.32f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Cargo Worn Service Plate", new Vector3(-0.62f, 0.86f, -0.52f), new Vector3(0.05f, 0.24f, 0.46f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Cargo Worn Service Plate", new Vector3(0.62f, 0.86f, -0.52f), new Vector3(0.05f, 0.24f, 0.46f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Cargo Bay White Panel", new Vector3(-0.52f, 0.68f, -0.12f), new Vector3(0.055f, 0.22f, 0.72f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Cargo Bay White Panel", new Vector3(0.52f, 0.68f, -0.12f), new Vector3(0.055f, 0.22f, 0.72f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Landing Skid", new Vector3(-0.74f, 0.2f, -0.36f), new Vector3(0.14f, 0.08f, 1.28f), tankDeepGrimeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Landing Skid", new Vector3(0.74f, 0.2f, -0.36f), new Vector3(0.14f, 0.08f, 1.28f), tankDeepGrimeMaterial, true);
            CreateFinSlab(kit, "Left Rear Stabilizer Fin", new Vector3(-0.42f, 0.96f, -1.28f), -1f, whiteMaterial);
            CreateFinSlab(kit, "Right Rear Stabilizer Fin", new Vector3(0.42f, 0.96f, -1.28f), 1f, whiteMaterial);

            BuildDuctedFan(kit, "Front Left Lift Fan", new Vector3(-1.9f, 0.74f, 0.82f));
            BuildDuctedFan(kit, "Front Right Lift Fan", new Vector3(1.9f, 0.74f, 0.82f));
            BuildDuctedFan(kit, "Rear Left Lift Fan", new Vector3(-1.9f, 0.74f, -1.05f));
            BuildDuctedFan(kit, "Rear Right Lift Fan", new Vector3(1.9f, 0.74f, -1.05f));
            BuildVent(kit, "Orca Cargo Roof Intake", new Vector3(0f, 1.03f, -0.45f), new Vector3(0.72f, 0.035f, 0.42f), 5);
            BuildVent(kit, "Orca Rear Heat Grille", new Vector3(0f, 0.86f, -1.02f), new Vector3(0.58f, 0.035f, 0.28f), 4);
            CreatePrimitive(PrimitiveType.Cube, kit, "Rear Cargo Warning Light", new Vector3(0f, 0.62f, -1.62f), new Vector3(0.28f, 0.046f, 0.045f), accentMaterial, true);
            AddOrcaSurfaceDetail(kit, teamMaterial);
            AddBoltLine(kit, "Orca Cargo Bay Bolts", new Vector3(-0.42f, 0.74f, -1.08f), new Vector3(0.21f, 0f, 0f), 5, 0.026f);
            AddBoltLine(kit, "Orca Roof Armor Bolts", new Vector3(-0.28f, 0.91f, 0.16f), new Vector3(0.14f, 0f, 0f), 5, 0.024f);
            AddOrcaAdvancedDetail(kit, teamMaterial);
            AddOrcaProfessionalFinish(kit, teamMaterial);
            AddVehicleReadabilityKit(kit, UnitKind.OrcaLifter, teamMaterial);
            return true;
        }

        private void RemoveChassisChild(Transform root, string childName)
        {
            Transform child = FindDeepChild(root, childName);
            if (child != null)
            {
                DestroyGeneratedObject(child.gameObject);
            }
        }

        private void RemoveTankWeaponChildren(Transform root)
        {
            foreach (LODGroup lodGroup in root.GetComponentsInChildren<LODGroup>(true))
            {
                DestroyGeneratedObject(lodGroup);
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child == null || child == root)
                {
                    continue;
                }

                string name = child.name.ToLowerInvariant();
                bool remove =
                    name.Contains("turret") ||
                    name.Contains("cannon") ||
                    name.Contains("barrel") ||
                    name.Contains("gun") ||
                    name.Contains("muzzle") ||
                    name.Contains("weapon") ||
                    name.Contains("recoil") ||
                    name.Contains("sight") ||
                    name.Contains("lod1") ||
                    name.Contains("lod2");

                if (remove)
                {
                    DestroyGeneratedObject(child.gameObject);
                }
            }
        }

        private void AddHarvesterIndustrialDetail(Transform kit, Material teamMaterial)
        {
            for (int i = 0; i < 6; i++)
            {
                float x = -0.26f + (i % 3) * 0.18f;
                float z = -0.62f + (i / 3) * 0.18f;
                GameObject ore = CreatePrimitive(PrimitiveType.Cube, kit, "Glowing Ore Chunk", new Vector3(x, 1.035f + (i % 2) * 0.018f, z), new Vector3(0.046f, 0.034f, 0.042f), resourceMaterial, true);
                ore.transform.localRotation = Quaternion.Euler(12f + i * 9f, i * 31f, -8f);
            }

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Hydraulic Mount" : "Right Hydraulic Mount", new Vector3(side * 0.63f, 0.44f, 0.72f), new Vector3(0.1f, 0.16f, 0.18f), armorShadowMaterial, true);
                GameObject piston = CreatePrimitive(PrimitiveType.Cylinder, kit, side < 0f ? "Left Collector Hydraulic Piston" : "Right Collector Hydraulic Piston", new Vector3(side * 0.64f, 0.5f, 1.02f), new Vector3(0.028f, 0.46f, 0.028f), tankBareMetalMaterial, true);
                piston.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Mud Guard Wear Plate" : "Right Mud Guard Wear Plate", new Vector3(side * 0.71f, 0.42f, -0.38f), new Vector3(0.04f, 0.2f, 0.74f), tankDustMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Amber Side Beacon" : "Right Amber Side Beacon", new Vector3(side * 0.74f, 0.74f, 0.58f), new Vector3(0.035f, 0.08f, 0.12f), accentMaterial, true);
                AddBoltLine(kit, side < 0f ? "Left Hopper Wear Bolts" : "Right Hopper Wear Bolts", new Vector3(side * 0.68f, 0.92f, -0.72f), new Vector3(0f, 0f, 0.16f), 6, 0.024f);
            }

            CreatePrimitive(PrimitiveType.Cube, kit, "Ore Conveyor Team Status Strip", new Vector3(0f, 0.65f, 0.86f), new Vector3(0.24f, 0.035f, 0.055f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Hopper Rear Soot Plate", new Vector3(0f, 0.82f, -0.9f), new Vector3(0.82f, 0.12f, 0.045f), tankDeepGrimeMaterial, true);
            BuildVent(kit, "Hopper Left Heat Grille", new Vector3(-0.36f, 0.98f, -0.9f), new Vector3(0.34f, 0.035f, 0.18f), 4);
            BuildVent(kit, "Hopper Right Heat Grille", new Vector3(0.36f, 0.98f, -0.9f), new Vector3(0.34f, 0.035f, 0.18f), 4);
            AddHarvesterProfessionalFinish(kit, teamMaterial);
        }

        private void AddHarvesterProfessionalFinish(Transform kit, Material teamMaterial)
        {
            for (int i = 0; i < 5; i++)
            {
                float x = -0.4f + i * 0.2f;
                CreatePrimitive(PrimitiveType.Cube, kit, "Collector Lower Scraper Tooth", new Vector3(x, 0.18f, 1.72f), new Vector3(0.055f, 0.07f, 0.2f), tankBareMetalMaterial, true).transform.localRotation = Quaternion.Euler(-22f, 0f, 0f);
            }

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Hopper Inspection Hatch" : "Right Hopper Inspection Hatch", new Vector3(side * 0.665f, 0.82f, -0.62f), new Vector3(0.032f, 0.18f, 0.28f), trimMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Hopper Dust Scratch" : "Right Hopper Dust Scratch", new Vector3(side * 0.668f, 0.96f, -0.32f), new Vector3(0.026f, 0.06f, 0.36f), tankDustMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Team Micro Ore Label" : "Right Team Micro Ore Label", new Vector3(side * 0.686f, 0.92f, 0.04f), new Vector3(0.022f, 0.075f, 0.14f), teamMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Track Mud Clog" : "Right Track Mud Clog", new Vector3(side * 0.74f, 0.16f, -0.7f), new Vector3(0.08f, 0.08f, 0.18f), tankDustMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Front Hydraulic Hose" : "Right Front Hydraulic Hose", new Vector3(side * 0.54f, 0.42f, 1.34f), new Vector3(0.035f, 0.06f, 0.48f), tankDeepGrimeMaterial, true);
                AddBoltLine(kit, side < 0f ? "Left Hopper Hatch Rivets" : "Right Hopper Hatch Rivets", new Vector3(side * 0.69f, 0.73f, -0.74f), new Vector3(0f, 0f, 0.12f), 4, 0.016f);
            }

            CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Cab Roof Dirty Panel", new Vector3(0f, 1.1f, 0.16f), new Vector3(0.42f, 0.026f, 0.22f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Conveyor Side Warning Label", new Vector3(0.34f, 0.58f, 0.92f), new Vector3(0.12f, 0.035f, 0.06f), hazardMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Conveyor Belt Soot Strip", new Vector3(0f, 0.59f, 0.54f), new Vector3(0.44f, 0.028f, 0.18f), tankDeepGrimeMaterial, true);
            BuildVent(kit, "Harvester Fine Rear Dust Filter", new Vector3(0f, 0.98f, -1.0f), new Vector3(0.52f, 0.032f, 0.12f), 5);
            AddHarvesterFinalTexturePass(kit, teamMaterial);
        }

        private void AddSkyraiderAdvancedDetail(Transform kit, Material teamMaterial)
        {
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                for (int i = 0; i < 3; i++)
                {
                    float z = -0.18f + i * 0.32f;
                    GameObject rocket = CreatePrimitive(PrimitiveType.Cylinder, kit, side < 0f ? "Left Underwing Rocket" : "Right Underwing Rocket", new Vector3(side * 1.62f, 0.48f, z), new Vector3(0.035f, 0.24f, 0.035f), tankBareMetalMaterial, true);
                    rocket.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Rocket Stabilizer Fin", new Vector3(side * 1.62f, 0.48f, z - 0.22f), new Vector3(0.1f, 0.025f, 0.05f), darkMaterial, true);
                }

                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Wing Root Scuff Panel" : "Right Wing Root Scuff Panel", new Vector3(side * 0.62f, 0.99f, 0.02f), new Vector3(0.28f, 0.028f, 0.38f), tankDustMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Service Access Hatch" : "Right Service Access Hatch", new Vector3(side * 0.46f, 0.84f, -0.52f), new Vector3(0.045f, 0.18f, 0.3f), trimMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Intake Soot Streak" : "Right Intake Soot Streak", new Vector3(side * 0.54f, 0.68f, -0.92f), new Vector3(0.045f, 0.08f, 0.34f), tankDeepGrimeMaterial, true);
            }

            CreatePrimitive(PrimitiveType.Cylinder, kit, "Rotor Armored Hub Cap", new Vector3(0f, 1.46f, -0.12f), new Vector3(0.18f, 0.055f, 0.18f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Forward Avionics Service Panel", new Vector3(0f, 0.98f, 0.72f), new Vector3(0.38f, 0.035f, 0.22f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Cyan Targeting Camera", new Vector3(0f, 0.78f, 1.52f), new Vector3(0.115f, 0.04f, 0.036f), subtleGlowMaterial, true);
            AddBoltLine(kit, "Skyraider Service Hatch Bolts", new Vector3(-0.17f, 1.02f, 0.76f), new Vector3(0.085f, 0f, 0f), 5, 0.018f);
        }

        private void AddOrcaAdvancedDetail(Transform kit, Material teamMaterial)
        {
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                GameObject winch = CreatePrimitive(PrimitiveType.Cylinder, kit, side < 0f ? "Left Cargo Winch Drum" : "Right Cargo Winch Drum", new Vector3(side * 0.48f, 0.28f, -0.62f), new Vector3(0.09f, 0.28f, 0.09f), pipeMaterial, true);
                winch.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Cargo Sling Cable" : "Right Cargo Sling Cable", new Vector3(side * 0.48f, 0.02f, -0.28f), new Vector3(0.035f, 0.48f, 0.035f), tankDeepGrimeMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Fan Hazard Tab" : "Right Fan Hazard Tab", new Vector3(side * 1.9f, 0.92f, 0.82f), new Vector3(0.34f, 0.035f, 0.08f), hazardMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Boom Scuff Plate" : "Right Boom Scuff Plate", new Vector3(side * 1.34f, 0.86f, -0.38f), new Vector3(0.38f, 0.028f, 0.22f), tankDustMaterial, true);
            }

            CreatePrimitive(PrimitiveType.Cube, kit, "Cargo Load Crate A", new Vector3(-0.18f, 0.2f, -0.28f), new Vector3(0.28f, 0.22f, 0.3f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Cargo Load Crate B", new Vector3(0.2f, 0.17f, -0.05f), new Vector3(0.24f, 0.18f, 0.24f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Cargo Crate Blue Strap", new Vector3(-0.18f, 0.33f, -0.28f), new Vector3(0.22f, 0.03f, 0.035f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Rear Cargo Bay Soot Plate", new Vector3(0f, 0.58f, -1.36f), new Vector3(0.62f, 0.08f, 0.045f), tankDeepGrimeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Cyan Belly Clamp Status", new Vector3(0f, 0.12f, 0.18f), new Vector3(0.14f, 0.04f, 0.058f), subtleGlowMaterial, true);
            AddBoltLine(kit, "Orca Cargo Winch Bolts", new Vector3(-0.28f, 0.39f, -0.62f), new Vector3(0.14f, 0f, 0f), 5, 0.018f);
        }

        private void AddSkyraiderProfessionalFinish(Transform kit, Material teamMaterial)
        {
            AddAircraftSkinPlateGrid(kit, "Skyraider Left Wing", -1f, 0.98f, -0.06f, 0.7f, 0.18f);
            AddAircraftSkinPlateGrid(kit, "Skyraider Right Wing", 1f, 0.98f, -0.06f, 0.7f, 0.18f);

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Wing Number Plate" : "Right Wing Number Plate", new Vector3(side * 1.18f, 1.005f, 0.18f), new Vector3(0.22f, 0.026f, 0.12f), whiteMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Wing Sooted Gun Access" : "Right Wing Sooted Gun Access", new Vector3(side * 1.48f, 0.92f, 0.42f), new Vector3(0.22f, 0.036f, 0.2f), tankDeepGrimeMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Wing Team Micro Stripe" : "Right Wing Team Micro Stripe", new Vector3(side * 1.35f, 1.02f, -0.31f), new Vector3(0.24f, 0.024f, 0.045f), teamMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Landing Gear Shock" : "Right Landing Gear Shock", new Vector3(side * 0.5f, 0.54f, 0.36f), new Vector3(0.035f, 0.36f, 0.035f), tankBareMetalMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Landing Gear Dust Boot" : "Right Landing Gear Dust Boot", new Vector3(side * 0.5f, 0.34f, 0.36f), new Vector3(0.07f, 0.09f, 0.07f), tankDeepGrimeMaterial, true);
                AddBoltLine(kit, side < 0f ? "Left Wing Skin Rivets" : "Right Wing Skin Rivets", new Vector3(side * 0.68f, 1.018f, -0.44f), new Vector3(side * 0.12f, 0f, 0f), 6, 0.016f);
            }

            CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Dirty Belly Armor Plate", new Vector3(0f, 0.58f, 0.34f), new Vector3(0.34f, 0.04f, 0.62f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Underslung Sensor Ball Mount", new Vector3(0f, 0.55f, 1.02f), new Vector3(0.22f, 0.12f, 0.16f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Sphere, kit, "Skyraider Cyan Sensor Ball", new Vector3(0f, 0.5f, 1.08f), new Vector3(0.095f, 0.095f, 0.095f), glassMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, kit, "Skyraider Rotor Swashplate", new Vector3(0f, 1.39f, -0.12f), new Vector3(0.24f, 0.035f, 0.24f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, kit, "Skyraider Rotor Bearing Shadow", new Vector3(0f, 1.42f, -0.12f), new Vector3(0.14f, 0.05f, 0.14f), tankDeepGrimeMaterial, true);
            BuildVent(kit, "Skyraider Rear Fine Exhaust Grille", new Vector3(0f, 0.84f, -1.07f), new Vector3(0.48f, 0.035f, 0.22f), 5);
            AddSkyraiderFinalTexturePass(kit, teamMaterial);
        }

        private void AddOrcaProfessionalFinish(Transform kit, Material teamMaterial)
        {
            AddAircraftSkinPlateGrid(kit, "Orca Left Boom", -1f, 0.88f, -0.08f, 0.68f, 0.12f);
            AddAircraftSkinPlateGrid(kit, "Orca Right Boom", 1f, 0.88f, -0.08f, 0.68f, 0.12f);

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Cargo Door Hinge Rail" : "Right Cargo Door Hinge Rail", new Vector3(side * 0.64f, 0.62f, -0.46f), new Vector3(0.035f, 0.08f, 0.92f), tankBareMetalMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Cargo Door Dirty Insert" : "Right Cargo Door Dirty Insert", new Vector3(side * 0.615f, 0.64f, -0.18f), new Vector3(0.032f, 0.24f, 0.32f), tankDustMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Fan Maintenance Label" : "Right Fan Maintenance Label", new Vector3(side * 1.9f, 0.99f, -1.05f), new Vector3(0.2f, 0.026f, 0.08f), whiteMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Fan Team Micro Stripe" : "Right Fan Team Micro Stripe", new Vector3(side * 1.9f, 1.01f, 0.82f), new Vector3(0.22f, 0.025f, 0.055f), teamMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Skid Wear Shoe" : "Right Skid Wear Shoe", new Vector3(side * 0.74f, 0.13f, -0.36f), new Vector3(0.18f, 0.045f, 1.18f), tankBareMetalMaterial, true);
                AddBoltLine(kit, side < 0f ? "Left Cargo Door Rivets" : "Right Cargo Door Rivets", new Vector3(side * 0.63f, 0.82f, -0.84f), new Vector3(0f, 0f, 0.17f), 7, 0.016f);
            }

            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Roof Dirty Service Panel A", new Vector3(-0.22f, 1.005f, -0.68f), new Vector3(0.22f, 0.026f, 0.24f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Roof Dirty Service Panel B", new Vector3(0.24f, 1.006f, -0.08f), new Vector3(0.2f, 0.026f, 0.28f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Cargo Load Warning Plaque", new Vector3(0f, 0.58f, -1.6f), new Vector3(0.32f, 0.08f, 0.045f), hazardMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Nose Camera Dark Gimbal", new Vector3(0f, 0.68f, 1.56f), new Vector3(0.18f, 0.12f, 0.06f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Nose Camera Cyan Slit", new Vector3(0f, 0.69f, 1.6f), new Vector3(0.12f, 0.045f, 0.035f), subtleGlowMaterial, true);
            BuildVent(kit, "Orca Cargo Bay Lower Slat Vent", new Vector3(0f, 0.48f, -1.18f), new Vector3(0.54f, 0.035f, 0.18f), 5);
            AddOrcaFinalTexturePass(kit, teamMaterial);
        }

        private void AddHarvesterFinalTexturePass(Transform kit, Material teamMaterial)
        {
            CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Hopper Painted Service Plate", new Vector3(-0.22f, 1.045f, -0.42f), new Vector3(0.28f, 0.022f, 0.22f), infantryPlateMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Hopper Scratched Service Plate", new Vector3(0.26f, 1.046f, -0.18f), new Vector3(0.22f, 0.022f, 0.2f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Low Mud Wash Front", new Vector3(0f, 0.31f, 1.02f), new Vector3(0.92f, 0.045f, 0.12f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Cab White Unit Stencil", new Vector3(0f, 1.17f, 0.08f), new Vector3(0.24f, 0.024f, 0.08f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Cab Muted Team Stencil", new Vector3(0.28f, 1.171f, 0.08f), new Vector3(0.16f, 0.024f, 0.06f), teamMaterial, true);

            for (int i = 0; i < 6; i++)
            {
                float x = -0.42f + i * 0.168f;
                CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Conveyor Cleat Highlight", new Vector3(x, 0.605f, 0.74f), new Vector3(0.035f, 0.03f, 0.54f), tankBareMetalMaterial, true);
            }

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Hopper Layered Lower Armor" : "Right Hopper Layered Lower Armor", new Vector3(side * 0.704f, 0.63f, -0.36f), new Vector3(0.028f, 0.12f, 0.62f), armorHighlightMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Hopper Soot Vertical Seam" : "Right Hopper Soot Vertical Seam", new Vector3(side * 0.708f, 0.8f, -0.1f), new Vector3(0.022f, 0.32f, 0.026f), tankDeepGrimeMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Track Dust Caked Edge" : "Right Track Dust Caked Edge", new Vector3(side * 0.87f, 0.19f, 0.24f), new Vector3(0.045f, 0.07f, 0.94f), tankDustMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Collector Arm Wear Insert" : "Right Collector Arm Wear Insert", new Vector3(side * 0.61f, 0.42f, 1.16f), new Vector3(0.04f, 0.07f, 0.36f), tankBareMetalMaterial, true);
                AddBoltLine(kit, side < 0f ? "Left Harvester Final Side Rivets" : "Right Harvester Final Side Rivets", new Vector3(side * 0.725f, 0.72f, -0.64f), new Vector3(0f, 0f, 0.18f), 6, 0.014f);
            }
        }

        private void AddSkyraiderFinalTexturePass(Transform kit, Material teamMaterial)
        {
            CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Cockpit Center Frame", new Vector3(0f, 1.145f, 0.48f), new Vector3(0.035f, 0.04f, 0.36f), tankDeepGrimeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Nose Paint Chip Left", new Vector3(-0.18f, 0.995f, 1.31f), new Vector3(0.09f, 0.018f, 0.052f), infantryPlateMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Nose Paint Chip Right", new Vector3(0.16f, 0.996f, 1.06f), new Vector3(0.08f, 0.018f, 0.048f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Rotor Warning Stripe", new Vector3(0f, 1.475f, 0.08f), new Vector3(0.1f, 0.018f, 0.28f), hazardMaterial, true);

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Wing Leading Edge Metal Wear" : "Right Wing Leading Edge Metal Wear", new Vector3(side * 1.2f, 1.01f, 0.36f), new Vector3(0.64f, 0.018f, 0.042f), tankBareMetalMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Wing Trailing Panel Seam" : "Right Wing Trailing Panel Seam", new Vector3(side * 1.22f, 1.012f, -0.48f), new Vector3(0.52f, 0.018f, 0.028f), tankDeepGrimeMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Rocket Rail Clamp A" : "Right Rocket Rail Clamp A", new Vector3(side * 1.52f, 0.58f, -0.08f), new Vector3(0.055f, 0.04f, 0.12f), tankBareMetalMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Rocket Rail Clamp B" : "Right Rocket Rail Clamp B", new Vector3(side * 1.52f, 0.58f, 0.26f), new Vector3(0.055f, 0.04f, 0.12f), tankBareMetalMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Fuselage Tiny Team Hash" : "Right Fuselage Tiny Team Hash", new Vector3(side * 0.515f, 0.89f, 0.42f), new Vector3(0.018f, 0.085f, 0.13f), teamMaterial, true);
                AddBoltLine(kit, side < 0f ? "Left Wing Final Rivet Run" : "Right Wing Final Rivet Run", new Vector3(side * 0.72f, 1.025f, 0.34f), new Vector3(side * 0.13f, 0f, 0f), 7, 0.013f);
            }
        }

        private void AddOrcaFinalTexturePass(Transform kit, Material teamMaterial)
        {
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Cargo Roof Off White Hatch", new Vector3(-0.18f, 1.025f, 0.22f), new Vector3(0.24f, 0.022f, 0.18f), infantryPlateMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Cargo Roof Worn Metal Hatch", new Vector3(0.2f, 1.026f, -0.38f), new Vector3(0.22f, 0.022f, 0.2f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Cockpit Center Mullion", new Vector3(0f, 1.045f, 1.18f), new Vector3(0.032f, 0.055f, 0.18f), tankDeepGrimeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Rear Ramp Scuff Wash", new Vector3(0f, 0.5f, -1.54f), new Vector3(0.46f, 0.03f, 0.11f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Loadmaster Team Mark", new Vector3(0f, 0.52f, -0.08f), new Vector3(0.18f, 0.026f, 0.12f), teamMaterial, true);

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Fan Cowling Paint Chip" : "Right Fan Cowling Paint Chip", new Vector3(side * 1.9f, 1.02f, -0.42f), new Vector3(0.22f, 0.018f, 0.05f), infantryPlateMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Boom Long Dirty Seam" : "Right Boom Long Dirty Seam", new Vector3(side * 1.38f, 0.91f, 0.06f), new Vector3(0.54f, 0.018f, 0.026f), tankDeepGrimeMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Cargo Door Lower Track" : "Right Cargo Door Lower Track", new Vector3(side * 0.64f, 0.47f, -0.4f), new Vector3(0.026f, 0.05f, 0.82f), tankBareMetalMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, side < 0f ? "Left Sling Cable Sheave" : "Right Sling Cable Sheave", new Vector3(side * 0.48f, 0.3f, -0.88f), new Vector3(0.12f, 0.035f, 0.05f), tankBareMetalMaterial, true);
                AddBoltLine(kit, side < 0f ? "Left Orca Final Boom Rivets" : "Right Orca Final Boom Rivets", new Vector3(side * 1.12f, 0.905f, 0.22f), new Vector3(side * 0.16f, 0f, 0f), 6, 0.013f);
            }
        }

        private void AddAircraftSkinPlateGrid(Transform kit, string prefix, float side, float y, float zCenter, float width, float depth)
        {
            for (int row = 0; row < 2; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    float x = side * (0.68f + column * width * 0.28f);
                    float z = zCenter - depth * 0.5f + row * depth;
                    Material material = (row + column) % 3 == 0 ? tankDustMaterial : ((row + column) % 3 == 1 ? trimMaterial : armorHighlightMaterial);
                    GameObject plate = CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Varied Skin Plate", new Vector3(x, y, z), new Vector3(0.18f, 0.022f, 0.14f), material, true);
                    plate.transform.localRotation = Quaternion.Euler(0f, side * (column - 1) * 2f, 0f);
                }
            }
        }

        private void AddTankProfessionalFinish(Transform root, Material teamMaterial)
        {
            Transform kit = CreateVisualRoot(root, "Tank Professional Finish Kit", Vector3.zero);
            kit.gameObject.AddComponent<RtsFixedMaterial>();

            CreatePrimitive(PrimitiveType.Cube, kit, "Tank Left Mud Skirt Dirt Gradient", new Vector3(-0.68f, 0.27f, 0.06f), new Vector3(0.026f, 0.12f, 1.28f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Tank Right Mud Skirt Dirt Gradient", new Vector3(0.68f, 0.27f, 0.06f), new Vector3(0.026f, 0.12f, 1.28f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Tank Turret Painted Unit Mark", new Vector3(-0.18f, 0.795f, 0.08f), new Vector3(0.22f, 0.018f, 0.12f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Tank Turret Team Micro Stripe", new Vector3(0.2f, 0.797f, 0.08f), new Vector3(0.22f, 0.018f, 0.08f), teamMaterial, true);
            BuildVent(kit, "Tank Rear Fine Heat Slats", new Vector3(0f, 0.58f, -0.82f), new Vector3(0.56f, 0.03f, 0.16f), 5);
            AddBoltLine(kit, "Tank Side Skirt Extra Rivets Left", new Vector3(-0.68f, 0.42f, -0.54f), new Vector3(0f, 0f, 0.18f), 7, 0.016f);
            AddBoltLine(kit, "Tank Side Skirt Extra Rivets Right", new Vector3(0.68f, 0.42f, -0.54f), new Vector3(0f, 0f, 0.18f), 7, 0.016f);
            AddTankFinalTexturePass(kit, teamMaterial);
        }

        private void AddTankFinalTexturePass(Transform kit, Material teamMaterial)
        {
            AddRoofPanelGrid(kit, "Tank Forward Hull Painted Panel Grid", new Vector3(0f, 0.535f, 0.58f), 0.9f, 0.62f, 4, 3);
            AddTexturePatchGrid(kit, "Tank Forward Hull", new Vector3(0f, 0.552f, 0.58f), 0.76f, 0.48f, 4, 2, true, teamMaterial);
            AddRoofPanelGrid(kit, "Tank Turret Top Fine Seam Grid", new Vector3(0f, 0.815f, -0.04f), 0.6f, 0.48f, 3, 3);

            CreatePrimitive(PrimitiveType.Cube, kit, "Tank Front Spare Track Link A", new Vector3(-0.24f, 0.51f, 0.97f), new Vector3(0.16f, 0.034f, 0.08f), tankDeepGrimeMaterial, true).transform.localRotation = Quaternion.Euler(-9f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cube, kit, "Tank Front Spare Track Link B", new Vector3(0f, 0.512f, 0.98f), new Vector3(0.16f, 0.034f, 0.08f), tankBareMetalMaterial, true).transform.localRotation = Quaternion.Euler(-9f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cube, kit, "Tank Front Spare Track Link C", new Vector3(0.24f, 0.51f, 0.97f), new Vector3(0.16f, 0.034f, 0.08f), tankDeepGrimeMaterial, true).transform.localRotation = Quaternion.Euler(-9f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cube, kit, "Tank Front Lower Mud Wash", new Vector3(0f, 0.34f, 0.98f), new Vector3(0.92f, 0.05f, 0.07f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Tank Front Cyan Driver Optic", new Vector3(-0.42f, 0.54f, 0.9f), new Vector3(0.12f, 0.045f, 0.032f), glowMaterial, true).transform.localRotation = Quaternion.Euler(-9f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cube, kit, "Tank Front Amber Marker Lamp", new Vector3(0.42f, 0.54f, 0.9f), new Vector3(0.1f, 0.045f, 0.032f), accentMaterial, true).transform.localRotation = Quaternion.Euler(-9f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cube, kit, "Tank Front Hazard Scuffed Plate", new Vector3(0.02f, 0.555f, 0.76f), new Vector3(0.34f, 0.022f, 0.1f), hazardMaterial, true).transform.localRotation = Quaternion.Euler(-9f, 0f, 0f);

            CreatePipe(kit, "Tank Left Tow Cable", new Vector3(-0.28f, 0.57f, 0.38f), new Vector3(0.022f, 0.72f, 0.022f), new Vector3(0f, 0f, 90f));
            CreatePipe(kit, "Tank Right Tow Cable", new Vector3(0.28f, 0.57f, 0.38f), new Vector3(0.022f, 0.72f, 0.022f), new Vector3(0f, 0f, 90f));
            CreatePrimitive(PrimitiveType.Cube, kit, "Tank Deck Dirty Hatch Left", new Vector3(-0.34f, 0.565f, -0.26f), new Vector3(0.22f, 0.022f, 0.2f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Tank Deck Off White Hatch Right", new Vector3(0.32f, 0.566f, -0.24f), new Vector3(0.22f, 0.022f, 0.18f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Tank Deck Dark Center Joint", new Vector3(0f, 0.575f, -0.34f), new Vector3(0.035f, 0.018f, 0.42f), tankDeepGrimeMaterial, true);

            CreatePrimitive(PrimitiveType.Cube, kit, "Tank Turret Left Replacement Cheek", new Vector3(-0.34f, 0.745f, 0.15f), new Vector3(0.22f, 0.075f, 0.18f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Tank Turret Right Dirty Cheek", new Vector3(0.34f, 0.744f, 0.1f), new Vector3(0.22f, 0.07f, 0.18f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Tank Turret Rear Sooted Stowage Box", new Vector3(0.02f, 0.73f, -0.42f), new Vector3(0.48f, 0.08f, 0.14f), tankDeepGrimeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Tank Turret Tiny Team Hash", new Vector3(-0.12f, 0.836f, 0.17f), new Vector3(0.11f, 0.018f, 0.1f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Tank Turret White Unit Hash", new Vector3(0.1f, 0.837f, 0.17f), new Vector3(0.1f, 0.018f, 0.08f), whiteMaterial, true);
            AddBoltLine(kit, "Tank Turret Final Rivet Run", new Vector3(-0.26f, 0.838f, -0.26f), new Vector3(0.13f, 0f, 0f), 5, 0.014f);

            for (int i = 0; i < 4; i++)
            {
                float z = 0.74f + i * 0.24f;
                GameObject band = CreatePrimitive(PrimitiveType.Cylinder, kit, "Tank Barrel Dark Cooling Band", new Vector3(0f, 0.65f, z), new Vector3(0.048f, 0.018f, 0.048f), i % 2 == 0 ? tankDeepGrimeMaterial : tankBareMetalMaterial, true);
                band.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                string prefix = side < 0f ? "Left" : "Right";
                CreatePrimitive(PrimitiveType.Cube, kit, "Tank " + prefix + " Upper Layered Armor Plate", new Vector3(side * 0.705f, 0.53f, -0.22f), new Vector3(0.026f, 0.16f, 0.46f), side < 0f ? whiteMaterial : tankDustMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, "Tank " + prefix + " Rear Scuffed Side Plate", new Vector3(side * 0.708f, 0.43f, -0.72f), new Vector3(0.024f, 0.18f, 0.34f), tankBareMetalMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, "Tank " + prefix + " Forward Team Service Tag", new Vector3(side * 0.712f, 0.49f, 0.48f), new Vector3(0.022f, 0.08f, 0.22f), teamMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, "Tank " + prefix + " Lower Track Mud Caked Edge", new Vector3(side * 0.67f, 0.2f, 0.08f), new Vector3(0.042f, 0.055f, 1.28f), tankDustMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, "Tank " + prefix + " Sprocket Grease Streak", new Vector3(side * 0.69f, 0.28f, 0.76f), new Vector3(0.034f, 0.12f, 0.16f), tankDeepGrimeMaterial, true);
                AddBoltLine(kit, "Tank " + prefix + " Final Side Rivets", new Vector3(side * 0.722f, 0.56f, -0.76f), new Vector3(0f, 0f, 0.19f), 8, 0.014f);

                for (int i = 0; i < 4; i++)
                {
                    float z = -0.58f + i * 0.34f;
                    CreatePrimitive(PrimitiveType.Cube, kit, "Tank " + prefix + " Track Pad Highlight", new Vector3(side * 0.675f, 0.135f, z), new Vector3(0.055f, 0.024f, 0.11f), i % 2 == 0 ? tankBareMetalMaterial : tankDeepGrimeMaterial, true);
                }
            }
        }

        private void BuildSkyraiderCoreAirframe(Transform kit, Material teamMaterial)
        {
            CreateTaperedBox(kit, "Skyraider Main Armored Fuselage", new Vector3(0f, 0.78f, 0.08f), new Vector3(0.9f, 0.42f, 1.65f), new Vector3(0.58f, 0.28f, 1.08f), armorHighlightMaterial);
            CreateTaperedBox(kit, "Skyraider Angular Nose", new Vector3(0f, 0.78f, 1.02f), new Vector3(0.72f, 0.32f, 0.78f), new Vector3(0.38f, 0.18f, 0.28f), whiteMaterial);
            CreateTaperedBox(kit, "Skyraider Rear Engine Cowl", new Vector3(0f, 0.72f, -0.74f), new Vector3(0.82f, 0.32f, 0.55f), new Vector3(0.48f, 0.22f, 0.38f), trimMaterial);
            CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Belly Shadow Keel", new Vector3(0f, 0.54f, 0f), new Vector3(0.36f, 0.12f, 1.42f), tankDeepGrimeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Cockpit Glass", new Vector3(0f, 1.04f, 0.48f), new Vector3(0.6f, 0.16f, 0.34f), glassMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Cockpit Dark Frame", new Vector3(0f, 1.15f, 0.48f), new Vector3(0.7f, 0.055f, 0.38f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Nose White Armor Cap", new Vector3(0f, 0.96f, 1.18f), new Vector3(0.58f, 0.08f, 0.36f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Forward Team Mark", new Vector3(0f, 0.98f, 0.2f), new Vector3(0.22f, 0.045f, 0.26f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Recessed Side Intake", new Vector3(-0.47f, 0.79f, -0.12f), new Vector3(0.055f, 0.18f, 0.48f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Recessed Side Intake", new Vector3(0.47f, 0.79f, -0.12f), new Vector3(0.055f, 0.18f, 0.48f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Side Intake Glow Strip", new Vector3(-0.505f, 0.82f, -0.12f), new Vector3(0.028f, 0.044f, 0.2f), subtleGlowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Side Intake Glow Strip", new Vector3(0.505f, 0.82f, -0.12f), new Vector3(0.028f, 0.044f, 0.2f), subtleGlowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Top Panel Joint A", new Vector3(-0.24f, 1.0f, -0.18f), new Vector3(0.035f, 0.035f, 0.86f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Top Panel Joint B", new Vector3(0.24f, 1.0f, -0.18f), new Vector3(0.035f, 0.035f, 0.86f), armorShadowMaterial, true);
            AddRoofPanelGrid(kit, "Skyraider Fuselage Panel Grid", new Vector3(0f, 1.015f, -0.08f), 0.78f, 1.15f, 3, 3);
            CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Left Armor Highlight Rail", new Vector3(-0.48f, 0.94f, -0.08f), new Vector3(0.045f, 0.05f, 1.22f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Right Armor Highlight Rail", new Vector3(0.48f, 0.94f, -0.08f), new Vector3(0.045f, 0.05f, 1.22f), whiteMaterial, true);
            AddBoltLine(kit, "Skyraider Nose Plate Bolts", new Vector3(-0.28f, 0.98f, 0.9f), new Vector3(0.14f, 0f, 0f), 5, 0.024f);
        }

        private void BuildOrcaCoreAirframe(Transform kit, Material teamMaterial)
        {
            CreateTaperedBox(kit, "Orca Main Cargo Fuselage", new Vector3(0f, 0.62f, -0.24f), new Vector3(1.05f, 0.46f, 1.86f), new Vector3(0.78f, 0.3f, 1.36f), concreteMaterial);
            CreateTaperedBox(kit, "Orca White Cargo Roof Armor", new Vector3(0f, 0.9f, -0.32f), new Vector3(0.82f, 0.08f, 1.22f), new Vector3(0.54f, 0.05f, 0.82f), whiteMaterial);
            CreateTaperedBox(kit, "Orca Armored Cockpit Nose", new Vector3(0f, 0.74f, 1.08f), new Vector3(0.78f, 0.34f, 0.68f), new Vector3(0.38f, 0.2f, 0.28f), whiteMaterial);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Cockpit Glass", new Vector3(0f, 0.98f, 1.18f), new Vector3(0.52f, 0.13f, 0.18f), glassMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Nose White Cap", new Vector3(0f, 0.88f, 1.42f), new Vector3(0.46f, 0.08f, 0.2f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Front Sensor Glow", new Vector3(0f, 0.74f, 1.52f), new Vector3(0.16f, 0.048f, 0.038f), subtleGlowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Left Cargo Shadow Recess", new Vector3(-0.55f, 0.56f, -0.28f), new Vector3(0.06f, 0.26f, 0.92f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Right Cargo Shadow Recess", new Vector3(0.55f, 0.56f, -0.28f), new Vector3(0.06f, 0.26f, 0.92f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Left White Cargo Armor", new Vector3(-0.59f, 0.72f, -0.24f), new Vector3(0.05f, 0.24f, 0.7f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Right White Cargo Armor", new Vector3(0.59f, 0.72f, -0.24f), new Vector3(0.05f, 0.24f, 0.7f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Roof Team Stripe", new Vector3(0f, 0.96f, 0.12f), new Vector3(0.26f, 0.04f, 0.24f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Belly Cargo Rail Left", new Vector3(-0.32f, 0.34f, -0.36f), new Vector3(0.08f, 0.08f, 1.18f), tankDeepGrimeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Belly Cargo Rail Right", new Vector3(0.32f, 0.34f, -0.36f), new Vector3(0.08f, 0.08f, 1.18f), tankDeepGrimeMaterial, true);
            AddRoofPanelGrid(kit, "Orca Cargo Roof Panel Grid", new Vector3(0f, 0.975f, -0.3f), 0.92f, 1.28f, 3, 3);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Left Upper Armor Rail", new Vector3(-0.5f, 0.9f, -0.3f), new Vector3(0.05f, 0.055f, 1.26f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Right Upper Armor Rail", new Vector3(0.5f, 0.9f, -0.3f), new Vector3(0.05f, 0.055f, 1.26f), whiteMaterial, true);
            AddBoltLine(kit, "Orca Nose Armor Bolts", new Vector3(-0.28f, 0.94f, 0.88f), new Vector3(0.14f, 0f, 0f), 5, 0.024f);
        }

        private void BuildSkyraiderRotor(Transform parent, Vector3 localPosition, float span)
        {
            Transform rotor = CreateVisualRoot(parent, "Skyraider Main Rotor", localPosition);
            RtsSpinner spinner = rotor.gameObject.AddComponent<RtsSpinner>();
            spinner.DegreesPerSecond = 900f;
            CreatePrimitive(PrimitiveType.Cylinder, rotor, "Rotor Mast", Vector3.zero, new Vector3(0.08f, 0.18f, 0.08f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, rotor, "Rotor Blade A", Vector3.zero, new Vector3(0.14f, 0.035f, span), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, rotor, "Rotor Blade B", Vector3.zero, new Vector3(span, 0.035f, 0.14f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, rotor, "Rotor Tip Light A", new Vector3(0f, 0f, span * 0.48f), new Vector3(0.16f, 0.04f, 0.07f), accentMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, rotor, "Rotor Tip Light B", new Vector3(span * 0.48f, 0f, 0f), new Vector3(0.07f, 0.04f, 0.16f), accentMaterial, true);
        }

        private void AddSkyraiderSurfaceDetail(Transform kit, Material teamMaterial)
        {
            CreateTaperedBox(kit, "Skyraider Dorsal Armor Spine", new Vector3(0f, 1.03f, -0.2f), new Vector3(0.52f, 0.12f, 1.28f), new Vector3(0.34f, 0.08f, 0.72f), whiteMaterial);
            CreatePrimitive(PrimitiveType.Cube, kit, "Dorsal Team Recognition Stripe", new Vector3(0f, 1.115f, -0.18f), new Vector3(0.14f, 0.026f, 0.3f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Fuselage Cheek Armor", new Vector3(-0.48f, 0.83f, 0.5f), new Vector3(0.055f, 0.22f, 0.68f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Fuselage Cheek Armor", new Vector3(0.48f, 0.83f, 0.5f), new Vector3(0.055f, 0.22f, 0.68f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Turbine Exhaust Louver", new Vector3(-0.52f, 0.78f, -0.66f), new Vector3(0.065f, 0.22f, 0.38f), pipeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Turbine Exhaust Louver", new Vector3(0.52f, 0.78f, -0.66f), new Vector3(0.065f, 0.22f, 0.38f), pipeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Forward Landing Skid Shoe", new Vector3(-0.52f, 0.42f, 0.42f), new Vector3(0.14f, 0.075f, 0.44f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Forward Landing Skid Shoe", new Vector3(0.52f, 0.42f, 0.42f), new Vector3(0.14f, 0.075f, 0.44f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Rear Landing Skid Shoe", new Vector3(-0.52f, 0.4f, -0.78f), new Vector3(0.14f, 0.075f, 0.38f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Rear Landing Skid Shoe", new Vector3(0.52f, 0.4f, -0.78f), new Vector3(0.14f, 0.075f, 0.38f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Forward Skid Dirt Pad", new Vector3(-0.52f, 0.37f, 0.42f), new Vector3(0.18f, 0.028f, 0.34f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Forward Skid Dirt Pad", new Vector3(0.52f, 0.37f, 0.42f), new Vector3(0.18f, 0.028f, 0.34f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Wing Tip Beacon", new Vector3(-1.78f, 0.88f, 0.05f), new Vector3(0.08f, 0.055f, 0.14f), glowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Wing Tip Beacon", new Vector3(1.78f, 0.88f, 0.05f), new Vector3(0.08f, 0.055f, 0.14f), accentMaterial, true);
            AddBoltLine(kit, "Skyraider Dorsal Panel Bolts", new Vector3(-0.22f, 1.13f, -0.72f), new Vector3(0.11f, 0f, 0f), 5, 0.022f);
        }

        private void AddOrcaSurfaceDetail(Transform kit, Material teamMaterial)
        {
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Cargo Side Panel Seam", new Vector3(-0.58f, 0.76f, -0.38f), new Vector3(0.045f, 0.32f, 0.92f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Cargo Side Panel Seam", new Vector3(0.58f, 0.76f, -0.38f), new Vector3(0.045f, 0.32f, 0.92f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Cargo Cyan Status Bar", new Vector3(-0.62f, 0.66f, 0.26f), new Vector3(0.04f, 0.054f, 0.18f), subtleGlowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Cargo Cyan Status Bar", new Vector3(0.62f, 0.66f, 0.26f), new Vector3(0.04f, 0.054f, 0.18f), subtleGlowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Forward Fan Strut", new Vector3(-1.38f, 0.68f, 0.64f), new Vector3(0.78f, 0.07f, 0.09f), pipeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Forward Fan Strut", new Vector3(1.38f, 0.68f, 0.64f), new Vector3(0.78f, 0.07f, 0.09f), pipeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Rear Fan Strut", new Vector3(-1.38f, 0.68f, -0.86f), new Vector3(0.78f, 0.07f, 0.09f), pipeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Rear Fan Strut", new Vector3(1.38f, 0.68f, -0.86f), new Vector3(0.78f, 0.07f, 0.09f), pipeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Rear Ramp Hazard Left", new Vector3(-0.28f, 0.46f, -1.6f), new Vector3(0.18f, 0.045f, 0.32f), hazardMaterial, true).transform.localRotation = Quaternion.Euler(0f, 22f, 0f);
            CreatePrimitive(PrimitiveType.Cube, kit, "Rear Ramp Hazard Right", new Vector3(0.28f, 0.46f, -1.6f), new Vector3(0.18f, 0.045f, 0.32f), hazardMaterial, true).transform.localRotation = Quaternion.Euler(0f, -22f, 0f);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Belly Lift Clamp Glow", new Vector3(0f, 0.28f, 0.18f), new Vector3(0.2f, 0.046f, 0.08f), subtleGlowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Orca Dorsal Team Panel", new Vector3(0f, 0.92f, 0.18f), new Vector3(0.16f, 0.03f, 0.12f), teamMaterial, true);
            AddBoltLine(kit, "Orca Boom Fasteners Left", new Vector3(-1.68f, 0.89f, -0.18f), new Vector3(0.17f, 0f, 0f), 5, 0.022f);
            AddBoltLine(kit, "Orca Boom Fasteners Right", new Vector3(1.0f, 0.89f, -0.18f), new Vector3(0.17f, 0f, 0f), 5, 0.022f);
        }

        private void BuildWeaponBarrel(Transform parent, string name, Vector3 localPosition, float radius, float length)
        {
            GameObject barrel = CreatePrimitive(PrimitiveType.Cylinder, parent, name, localPosition, new Vector3(radius, length * 0.5f, radius), pipeMaterial, true);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject muzzle = CreatePrimitive(PrimitiveType.Cylinder, parent, name + " Muzzle", localPosition + new Vector3(0f, 0f, length * 0.52f), new Vector3(radius * 1.35f, radius * 2.2f, radius * 1.35f), darkMaterial, true);
            muzzle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void ImproveImportedAssetRenderers(GameObject visual)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer.GetComponentInParent<RtsPreserveImportedMaterial>() != null)
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                    continue;
                }

                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;

                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = CreateRuntimeCompatibleMaterial(materials[i], new Color(0.52f, 0.52f, 0.46f));

                    if (material.HasProperty("_Metallic"))
                    {
                        material.SetFloat("_Metallic", Mathf.Max(material.GetFloat("_Metallic"), 0.08f));
                    }

                    if (material.HasProperty("_Smoothness"))
                    {
                        material.SetFloat("_Smoothness", Mathf.Max(material.GetFloat("_Smoothness"), 0.22f));
                    }

                    if (material.HasProperty("_Glossiness"))
                    {
                        material.SetFloat("_Glossiness", Mathf.Max(material.GetFloat("_Glossiness"), 0.22f));
                    }

                    Texture mainTexture = material.mainTexture;
                    if (mainTexture != null)
                    {
                        mainTexture.filterMode = FilterMode.Bilinear;
                        mainTexture.anisoLevel = 4;
                    }

                    materials[i] = material;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private void ImproveImportedInfantryRenderers(GameObject visual, UnitKind kind, Material teamMaterial)
        {
            Texture2D servicePalette = CreateInfantryServicePalette(kind, GetMaterialDisplayColor(teamMaterial));
            Color materialTint = Color.Lerp(Color.white, GetInfantryRoleBaseTint(kind), 0.18f);

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer.GetComponentInParent<RtsPreserveImportedMaterial>() != null)
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                    continue;
                }

                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;

                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    Material material = CreateRuntimeCompatibleMaterial(source, materialTint);
                    material.name = (source != null ? source.name : "Infantry") + " " + kind + " Service Runtime";

                    if (material.HasProperty("_MainTex"))
                    {
                        material.SetTexture("_MainTex", servicePalette);
                    }

                    if (material.HasProperty("_BaseMap"))
                    {
                        material.SetTexture("_BaseMap", servicePalette);
                    }

                    if (material.HasProperty("_Color"))
                    {
                        material.SetColor("_Color", materialTint);
                    }

                    if (material.HasProperty("_BaseColor"))
                    {
                        material.SetColor("_BaseColor", materialTint);
                    }

                    if (material.HasProperty("_EmissionMap"))
                    {
                        material.SetTexture("_EmissionMap", null);
                    }

                    if (material.HasProperty("_EmissionColor"))
                    {
                        material.SetColor("_EmissionColor", Color.black);
                    }

                    if (material.HasProperty("_Metallic"))
                    {
                        material.SetFloat("_Metallic", 0.12f);
                    }

                    if (material.HasProperty("_Smoothness"))
                    {
                        material.SetFloat("_Smoothness", 0.26f);
                    }

                    if (material.HasProperty("_Glossiness"))
                    {
                        material.SetFloat("_Glossiness", 0.26f);
                    }

                    Texture mainTexture = material.mainTexture;
                    if (mainTexture != null)
                    {
                        mainTexture.filterMode = FilterMode.Bilinear;
                        mainTexture.anisoLevel = 4;
                    }

                    materials[i] = material;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static Color GetMaterialDisplayColor(Material material)
        {
            if (material == null)
            {
                return Color.white;
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

        private static Material CreateInfantryTeamDetailMaterial(Material teamMaterial)
        {
            Color teamColor = GetMaterialDisplayColor(teamMaterial);
            Color mutedTeam = Color.Lerp(teamColor, new Color(0.42f, 0.45f, 0.38f), 0.78f);
            Color grime = Color.Lerp(new Color(0.035f, 0.04f, 0.035f), teamColor, 0.1f);
            return CreateFineNoiseMaterial(mutedTeam, grime, 137, 0.10f, 0.24f);
        }

        private static Material CreateVehicleTeamDetailMaterial(Material teamMaterial)
        {
            Color teamColor = GetMaterialDisplayColor(teamMaterial);
            Color mutedTeam = Color.Lerp(teamColor, new Color(0.38f, 0.42f, 0.34f), 0.62f);
            Color grime = Color.Lerp(new Color(0.035f, 0.045f, 0.035f), teamColor, 0.16f);
            return CreateIndustrialMaterial(mutedTeam, grime, 149, 0.12f, 0.28f);
        }

        private static Color GetInfantryRoleBaseTint(UnitKind kind)
        {
            switch (kind)
            {
                case UnitKind.RocketSoldier:
                    return new Color(0.40f, 0.43f, 0.31f);
                case UnitKind.Grenadier:
                    return new Color(0.37f, 0.40f, 0.30f);
                case UnitKind.FlameTrooper:
                    return new Color(0.34f, 0.35f, 0.30f);
                case UnitKind.Engineer:
                    return new Color(0.43f, 0.46f, 0.39f);
                default:
                    return new Color(0.38f, 0.42f, 0.31f);
            }
        }

        private static Color GetInfantryRoleAccent(UnitKind kind)
        {
            switch (kind)
            {
                case UnitKind.RocketSoldier:
                    return new Color(0.58f, 0.52f, 0.34f);
                case UnitKind.Grenadier:
                    return new Color(0.72f, 0.58f, 0.22f);
                case UnitKind.FlameTrooper:
                    return new Color(0.84f, 0.38f, 0.09f);
                case UnitKind.Engineer:
                    return new Color(0.18f, 0.52f, 0.58f);
                default:
                    return new Color(0.42f, 0.50f, 0.36f);
            }
        }

        private static Texture2D CreateInfantryServicePalette(UnitKind kind, Color teamColor)
        {
            Color armor = GetInfantryRoleBaseTint(kind);
            Color teamStripe = Color.Lerp(teamColor, new Color(0.36f, 0.40f, 0.35f), 0.82f);
            Color roleAccent = GetInfantryRoleAccent(kind);
            Color[] swatches =
            {
                Color.Lerp(armor, Color.black, 0.38f),
                Color.Lerp(armor, new Color(0.70f, 0.63f, 0.47f), 0.42f),
                new Color(0.34f, 0.35f, 0.31f),
                teamStripe,
                new Color(0.56f, 0.48f, 0.22f),
                Color.Lerp(roleAccent, armor, 0.64f),
                new Color(0.06f, 0.13f, 0.14f),
                new Color(0.73f, 0.72f, 0.63f)
            };

            Texture2D texture = new Texture2D(256, 32, TextureFormat.RGBA32, true);
            texture.name = "Runtime " + kind + " Infantry Service Palette";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 4;

            int swatchWidth = texture.width / swatches.Length;
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    int index = Mathf.Clamp(x / swatchWidth, 0, swatches.Length - 1);
                    Color color = swatches[index];
                    float coarse = Mathf.PerlinNoise((x + index * 29) * 0.09f, (y + index * 17) * 0.18f);
                    float fine = Mathf.PerlinNoise((x + index * 11) * 0.43f, (y + index * 7) * 0.37f);
                    float noise = (coarse - 0.5f) * 0.12f + (fine - 0.5f) * 0.05f;
                    bool swatchSeam = x % swatchWidth <= 1;
                    bool scratch = ((x * 31 + y * 47 + index * 19) % 173) < 3;

                    color.r = Mathf.Clamp01(color.r + noise);
                    color.g = Mathf.Clamp01(color.g + noise);
                    color.b = Mathf.Clamp01(color.b + noise);

                    if (swatchSeam)
                    {
                        color = Color.Lerp(color, Color.black, 0.24f);
                    }

                    if (scratch)
                    {
                        color = Color.Lerp(color, new Color(0.82f, 0.80f, 0.68f), 0.35f);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(true, false);
            return texture;
        }

        private void RemoveImportedColliders(GameObject visual)
        {
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            {
                DestroyGeneratedObject(collider);
            }
        }

        private void ReplaceImportedHarvesterCollector(Transform root)
        {
            foreach (LODGroup lodGroup in root.GetComponentsInChildren<LODGroup>(true))
            {
                DestroyGeneratedObject(lodGroup);
            }

            Transform importedCollector = FindDeepChild(root, "CollectorSpinModel");
            if (importedCollector != null)
            {
                DestroyGeneratedObject(importedCollector.gameObject);
            }

            Transform importedLod1 = FindDeepChild(root, "LOD1");
            if (importedLod1 != null)
            {
                DestroyGeneratedObject(importedLod1.gameObject);
            }

            Transform kit = CreateVisualRoot(root, "Harvester Rebuilt Collector Kit", Vector3.zero);
            kit.gameObject.AddComponent<RtsFixedMaterial>();

            CreatePrimitive(PrimitiveType.Cube, kit, "Readable Collector Crusher Housing", new Vector3(0f, 0.38f, 1.3f), new Vector3(1.08f, 0.22f, 0.32f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Readable Collector White Armor Face", new Vector3(0f, 0.47f, 1.47f), new Vector3(0.72f, 0.08f, 0.04f), whiteMaterial, true);

            GameObject drum = CreatePrimitive(PrimitiveType.Cylinder, kit, "Readable Short Ore Collector Drum", new Vector3(0f, 0.3f, 1.46f), new Vector3(0.16f, 0.36f, 0.16f), tankBareMetalMaterial, true);
            drum.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            RtsSpinner spinner = drum.AddComponent<RtsSpinner>();
            spinner.Axis = Vector3.right;
            spinner.DegreesPerSecond = 320f;

            GameObject leftRing = CreatePrimitive(PrimitiveType.Cylinder, kit, "Readable Collector Left Ring", new Vector3(-0.42f, 0.3f, 1.46f), new Vector3(0.18f, 0.04f, 0.18f), tankBareMetalMaterial, true);
            leftRing.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            GameObject rightRing = CreatePrimitive(PrimitiveType.Cylinder, kit, "Readable Collector Right Ring", new Vector3(0.42f, 0.3f, 1.46f), new Vector3(0.18f, 0.04f, 0.18f), tankBareMetalMaterial, true);
            rightRing.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            for (int i = 0; i < 7; i++)
            {
                float x = -0.36f + i * 0.12f;
                GameObject tooth = CreatePrimitive(PrimitiveType.Cube, kit, "Readable Collector Carbide Tooth", new Vector3(x, 0.22f, 1.6f), new Vector3(0.05f, 0.08f, 0.16f), tankBareMetalMaterial, true);
                tooth.transform.localRotation = Quaternion.Euler(-22f, 0f, 0f);
            }

            CreatePrimitive(PrimitiveType.Cube, kit, "Readable Collector Dark Guard", new Vector3(0f, 0.48f, 1.25f), new Vector3(0.82f, 0.08f, 0.14f), tankDeepGrimeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Readable Collector Hazard Guard", new Vector3(0f, 0.54f, 1.52f), new Vector3(0.58f, 0.045f, 0.08f), hazardMaterial, true);
        }

        private void AddImportedUnitTeamMarkers(Transform root, UnitKind kind, Material teamMaterial)
        {
            if (kind == UnitKind.Harvester)
            {
                CreatePrimitive(PrimitiveType.Cube, root, "Harvester Left Team Armor Plate", new Vector3(-1.12f, 0.83f, -0.35f), new Vector3(0.045f, 0.38f, 0.9f), teamMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, root, "Harvester Right Team Armor Plate", new Vector3(1.12f, 0.83f, -0.35f), new Vector3(0.045f, 0.38f, 0.9f), teamMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, root, "Harvester Roof Team Panel", new Vector3(0f, 1.33f, -0.55f), new Vector3(0.72f, 0.04f, 0.48f), teamMaterial, true);
                return;
            }

            Material infantryTeamMaterial = CreateInfantryTeamDetailMaterial(teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, root, "Rifleman Left Team Shoulder", new Vector3(-0.18f, 0.99f, 0.055f), new Vector3(0.032f, 0.026f, 0.024f), infantryTeamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Rifleman Right Team Shoulder", new Vector3(0.18f, 0.99f, 0.055f), new Vector3(0.032f, 0.026f, 0.024f), infantryTeamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Rifleman Backpack Team Tab", new Vector3(0f, 0.89f, -0.15f), new Vector3(0.07f, 0.07f, 0.018f), infantryTeamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Chest Webbing", new Vector3(0f, 0.9f, 0.2f), new Vector3(0.34f, 0.024f, 0.032f), infantryWebbingMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Left Ammo Pouch", new Vector3(-0.18f, 0.76f, 0.18f), new Vector3(0.075f, 0.1f, 0.04f), infantryCanvasMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Right Ammo Pouch", new Vector3(0.18f, 0.76f, 0.18f), new Vector3(0.075f, 0.1f, 0.04f), infantryCanvasMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Left Knee Plate", new Vector3(-0.13f, 0.36f, 0.13f), new Vector3(0.074f, 0.048f, 0.032f), infantryPlateMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Right Knee Plate", new Vector3(0.13f, 0.36f, 0.13f), new Vector3(0.074f, 0.048f, 0.032f), infantryPlateMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Helmet Visor Glow", new Vector3(0f, 1.34f, 0.22f), new Vector3(0.17f, 0.032f, 0.026f), subtleGlowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Radio Pack", new Vector3(-0.22f, 1.05f, -0.18f), new Vector3(0.08f, 0.24f, 0.065f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, root, "Infantry Radio Whip", new Vector3(-0.27f, 1.32f, -0.2f), new Vector3(0.008f, 0.24f, 0.008f), darkMaterial, true);
            AddInfantryRoleGear(root, kind, infantryTeamMaterial);
            AddInfantryProfessionalFinish(root, kind, infantryTeamMaterial);
            AddInfantryTextureDetailPass(root, kind, infantryTeamMaterial);
            AddInfantryReadabilityKit(root, kind, infantryTeamMaterial);
            AddInfantryFinalSilhouettePass(root, kind, infantryTeamMaterial);
        }

        private void AddInfantryRoleGear(Transform root, UnitKind kind, Material teamMaterial)
        {
            switch (kind)
            {
                case UnitKind.RocketSoldier:
                {
                    GameObject shoulderTube = CreatePrimitive(PrimitiveType.Cylinder, root, "Rocket Soldier Shoulder Launcher", new Vector3(0.26f, 1.08f, 0.48f), new Vector3(0.06f, 0.48f, 0.06f), pipeMaterial, true);
                    shoulderTube.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    GameObject muzzleRing = CreatePrimitive(PrimitiveType.Cylinder, root, "Rocket Launcher Muzzle Ring", new Vector3(0.26f, 1.08f, 0.96f), new Vector3(0.075f, 0.035f, 0.075f), tankDeepGrimeMaterial, true);
                    muzzleRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    CreatePrimitive(PrimitiveType.Cube, root, "Rocket Launcher Team IFF Tab", new Vector3(0.26f, 1.16f, 0.48f), new Vector3(0.07f, 0.026f, 0.13f), teamMaterial, true);

                    CreatePrimitive(PrimitiveType.Cube, root, "Rocket Reload Rack Backplate", new Vector3(-0.03f, 0.98f, -0.19f), new Vector3(0.42f, 0.08f, 0.035f), armorShadowMaterial, true);
                    for (int i = 0; i < 3; i++)
                    {
                        GameObject reload = CreatePrimitive(PrimitiveType.Cylinder, root, "Rocket Reload Tube", new Vector3(-0.15f + i * 0.12f, 0.98f, -0.18f), new Vector3(0.03f, 0.18f, 0.03f), tankBareMetalMaterial, true);
                        reload.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    }

                    break;
                }
                case UnitKind.Grenadier:
                    for (int i = 0; i < 5; i++)
                    {
                        float x = -0.2f + i * 0.1f;
                        CreatePrimitive(PrimitiveType.Sphere, root, "Grenadier Chest Grenade", new Vector3(x, 0.9f, 0.22f), new Vector3(0.045f, 0.055f, 0.045f), tankDeepGrimeMaterial, true);
                        CreatePrimitive(PrimitiveType.Cube, root, "Grenade Safety Tab", new Vector3(x, 0.95f, 0.24f), new Vector3(0.035f, 0.018f, 0.02f), accentMaterial, true);
                    }

                    CreatePrimitive(PrimitiveType.Cube, root, "Grenadier Satchel Charge Pack", new Vector3(0.28f, 0.72f, -0.12f), new Vector3(0.12f, 0.2f, 0.09f), infantryCanvasMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Grenadier Throwing Glove Glow", new Vector3(-0.31f, 0.78f, 0.38f), new Vector3(0.055f, 0.038f, 0.034f), glowMaterial, true);
                    break;
                case UnitKind.FlameTrooper:
                    for (int i = 0; i < 2; i++)
                    {
                        float x = i == 0 ? -0.1f : 0.1f;
                        GameObject fuelTank = CreatePrimitive(PrimitiveType.Cylinder, root, "Flame Trooper Fuel Tank", new Vector3(x, 0.9f, -0.24f), new Vector3(0.055f, 0.34f, 0.055f), tankBareMetalMaterial, true);
                        fuelTank.transform.localRotation = Quaternion.identity;
                    CreatePrimitive(PrimitiveType.Cube, root, "Fuel Tank Heat Label", new Vector3(x, 0.98f, -0.3f), new Vector3(0.052f, 0.062f, 0.018f), hazardMaterial, true);
                    }

                    GameObject hose = CreatePrimitive(PrimitiveType.Cylinder, root, "Flame Hose", new Vector3(0.18f, 0.88f, 0.08f), new Vector3(0.018f, 0.42f, 0.018f), darkMaterial, true);
                    hose.transform.localRotation = Quaternion.Euler(48f, 0f, 16f);
                    GameObject nozzle = CreatePrimitive(PrimitiveType.Cylinder, root, "Flame Projector Nozzle", new Vector3(0.29f, 0.92f, 0.44f), new Vector3(0.034f, 0.1f, 0.034f), pipeMaterial, true);
                    nozzle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    CreatePrimitive(PrimitiveType.Cube, root, "Pilot Flame Glow", new Vector3(0.29f, 0.92f, 0.54f), new Vector3(0.05f, 0.04f, 0.026f), accentMaterial, true);
                    break;
                case UnitKind.Engineer:
                {
                    CreatePrimitive(PrimitiveType.Cube, root, "Engineer Tool Case", new Vector3(-0.28f, 0.62f, 0.06f), new Vector3(0.16f, 0.18f, 0.09f), infantryCanvasMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Engineer Tool Case Team Stripe", new Vector3(-0.28f, 0.7f, 0.13f), new Vector3(0.085f, 0.026f, 0.022f), teamMaterial, true);
                    GameObject wrenchHandle = CreatePrimitive(PrimitiveType.Cylinder, root, "Engineer Wrench Handle", new Vector3(0.28f, 0.84f, 0.3f), new Vector3(0.018f, 0.24f, 0.018f), tankBareMetalMaterial, true);
                    wrenchHandle.transform.localRotation = Quaternion.Euler(52f, 0f, -24f);
                    CreatePrimitive(PrimitiveType.Cube, root, "Engineer Wrench Jaw", new Vector3(0.34f, 0.94f, 0.46f), new Vector3(0.08f, 0.035f, 0.035f), tankBareMetalMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Engineer Helmet Lamp", new Vector3(0f, 1.48f, 0.28f), new Vector3(0.062f, 0.032f, 0.026f), glowMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Engineer Repair Tablet", new Vector3(0.2f, 0.78f, 0.28f), new Vector3(0.095f, 0.06f, 0.03f), glassMaterial, true);
                    break;
                }
            }
        }

        private void AddInfantryProfessionalFinish(Transform root, UnitKind kind, Material teamMaterial)
        {
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Left Shoulder Hard Plate", new Vector3(-0.215f, 1.065f, 0.095f), new Vector3(0.068f, 0.042f, 0.046f), infantryPlateMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Right Shoulder Hard Plate", new Vector3(0.215f, 1.065f, 0.095f), new Vector3(0.068f, 0.042f, 0.046f), infantryPlateMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Chest Team Micro Tab", new Vector3(0.08f, 1.04f, 0.235f), new Vector3(0.12f, 0.04f, 0.028f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Belt Dark Buckle", new Vector3(0f, 0.68f, 0.18f), new Vector3(0.3f, 0.035f, 0.032f), infantryWebbingMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Left Hip Pouch", new Vector3(-0.215f, 0.66f, 0.04f), new Vector3(0.052f, 0.082f, 0.038f), infantryCanvasMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Right Hip Pouch", new Vector3(0.215f, 0.66f, 0.04f), new Vector3(0.052f, 0.082f, 0.038f), infantryCanvasMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Left Boot Dust Plate", new Vector3(-0.135f, 0.12f, 0.17f), new Vector3(0.078f, 0.03f, 0.046f), infantryCanvasMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Right Boot Dust Plate", new Vector3(0.135f, 0.12f, 0.17f), new Vector3(0.078f, 0.03f, 0.046f), infantryCanvasMaterial, true);

            switch (kind)
            {
                case UnitKind.RocketSoldier:
                    CreatePrimitive(PrimitiveType.Cube, root, "Rocket Soldier Backblast Shield", new Vector3(0.26f, 1.03f, 0.2f), new Vector3(0.16f, 0.12f, 0.045f), tankBareMetalMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Rocket Soldier Spare Warhead Cap", new Vector3(-0.12f, 0.84f, -0.18f), new Vector3(0.048f, 0.038f, 0.03f), hazardMaterial, true);
                    break;
                case UnitKind.Grenadier:
                    CreatePrimitive(PrimitiveType.Cube, root, "Grenadier Armored Throwing Pad", new Vector3(-0.34f, 0.93f, 0.24f), new Vector3(0.08f, 0.11f, 0.044f), infantryPlateMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Grenadier Satchel Team Tag", new Vector3(0.28f, 0.78f, -0.04f), new Vector3(0.055f, 0.032f, 0.022f), teamMaterial, true);
                    break;
                case UnitKind.FlameTrooper:
                    CreatePrimitive(PrimitiveType.Cube, root, "Flame Trooper Heat Apron", new Vector3(0f, 0.74f, 0.19f), new Vector3(0.22f, 0.19f, 0.026f), infantryPlateMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Flame Trooper Regulator Glow", new Vector3(0.14f, 0.98f, -0.18f), new Vector3(0.08f, 0.06f, 0.035f), accentMaterial, true);
                    break;
                case UnitKind.Engineer:
                    CreatePrimitive(PrimitiveType.Cube, root, "Engineer Shoulder Patch", new Vector3(-0.22f, 1.14f, 0.14f), new Vector3(0.055f, 0.03f, 0.022f), teamMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Engineer Cable Spool", new Vector3(-0.18f, 0.82f, -0.24f), new Vector3(0.12f, 0.12f, 0.055f), pipeMaterial, true);
                    break;
                default:
                    CreatePrimitive(PrimitiveType.Cube, root, "Rifleman Foregrip", new Vector3(0.25f, 0.92f, 0.43f), new Vector3(0.034f, 0.09f, 0.034f), tankDeepGrimeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Rifleman Spare Magazine", new Vector3(-0.07f, 0.76f, 0.2f), new Vector3(0.06f, 0.15f, 0.045f), darkMaterial, true);
                    break;
            }
        }

        private void AddInfantryTextureDetailPass(Transform root, UnitKind kind, Material teamMaterial)
        {
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Chest Center Strap", new Vector3(-0.03f, 0.94f, 0.238f), new Vector3(0.028f, 0.19f, 0.018f), infantryWebbingMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Chest Lower Strap", new Vector3(0.06f, 0.85f, 0.24f), new Vector3(0.16f, 0.024f, 0.018f), infantryWebbingMaterial, true);

            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Abdomen Dirty Armor Insert", new Vector3(-0.06f, 0.82f, 0.222f), new Vector3(0.13f, 0.055f, 0.023f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Small White Chest Chip", new Vector3(-0.08f, 1.06f, 0.238f), new Vector3(0.055f, 0.028f, 0.018f), infantryPlateMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Helmet Left Ear Plate", new Vector3(-0.13f, 1.31f, 0.14f), new Vector3(0.026f, 0.046f, 0.02f), infantryPlateMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Helmet Right Ear Plate", new Vector3(0.13f, 1.31f, 0.14f), new Vector3(0.026f, 0.046f, 0.02f), infantryPlateMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Helmet Brow Soot Line", new Vector3(0f, 1.35f, 0.246f), new Vector3(0.14f, 0.014f, 0.012f), infantryWebbingMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Visor Lower Frame", new Vector3(0f, 1.285f, 0.235f), new Vector3(0.135f, 0.012f, 0.012f), infantryWebbingMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Neck Seal", new Vector3(0f, 1.18f, 0.08f), new Vector3(0.19f, 0.036f, 0.09f), infantryWebbingMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Infantry Chest Plate Lower Edge", new Vector3(0f, 0.97f, 0.232f), new Vector3(0.24f, 0.018f, 0.018f), tankBareMetalMaterial, true);

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                CreatePrimitive(PrimitiveType.Cube, root, side < 0f ? "Left Elbow Scuff Plate" : "Right Elbow Scuff Plate", new Vector3(side * 0.292f, 0.88f, 0.2f), new Vector3(0.036f, 0.036f, 0.026f), infantryPlateMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, root, side < 0f ? "Left Shin Dust Streak" : "Right Shin Dust Streak", new Vector3(side * 0.12f, 0.24f, 0.155f), new Vector3(0.03f, 0.11f, 0.014f), infantryCanvasMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, root, side < 0f ? "Left Boot Toe Worn Metal" : "Right Boot Toe Worn Metal", new Vector3(side * 0.14f, 0.06f, 0.255f), new Vector3(0.09f, 0.02f, 0.025f), tankBareMetalMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, root, side < 0f ? "Left Shoulder Rivet" : "Right Shoulder Rivet", new Vector3(side * 0.235f, 1.118f, 0.125f), new Vector3(0.018f, 0.018f, 0.018f), darkMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, root, side < 0f ? "Left Hip Strap Tail" : "Right Hip Strap Tail", new Vector3(side * 0.21f, 0.6f, 0.11f), new Vector3(0.02f, 0.072f, 0.014f), infantryWebbingMaterial, true);
            }

            switch (kind)
            {
                case UnitKind.RocketSoldier:
                    CreatePrimitive(PrimitiveType.Cube, root, "Rocket Launcher Clamp A", new Vector3(0.26f, 1.11f, 0.34f), new Vector3(0.078f, 0.024f, 0.032f), tankBareMetalMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Rocket Launcher Clamp B", new Vector3(0.26f, 1.11f, 0.64f), new Vector3(0.078f, 0.024f, 0.032f), tankBareMetalMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Rocket Launcher Sooted Rear", new Vector3(0.26f, 1.08f, 0.03f), new Vector3(0.085f, 0.056f, 0.04f), tankDeepGrimeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Rocket Launcher Warning Stripe", new Vector3(0.26f, 1.13f, 0.78f), new Vector3(0.07f, 0.018f, 0.075f), hazardMaterial, true);
                    break;
                case UnitKind.Grenadier:
                    CreatePrimitive(PrimitiveType.Cube, root, "Grenadier Chest Bandolier Strap", new Vector3(-0.09f, 0.98f, 0.252f), new Vector3(0.028f, 0.24f, 0.018f), infantryWebbingMaterial, true);
                    for (int i = 0; i < 4; i++)
                    {
                        CreatePrimitive(PrimitiveType.Cube, root, "Grenadier Pin Pull Ring", new Vector3(-0.16f + i * 0.1f, 0.955f, 0.275f), new Vector3(0.028f, 0.014f, 0.012f), tankBareMetalMaterial, true);
                    }

                    break;
                case UnitKind.FlameTrooper:
                    CreatePrimitive(PrimitiveType.Cube, root, "Flame Trooper Hose Clamp A", new Vector3(0.16f, 0.89f, 0.18f), new Vector3(0.05f, 0.018f, 0.03f), tankBareMetalMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Flame Trooper Hose Clamp B", new Vector3(0.24f, 0.9f, 0.34f), new Vector3(0.045f, 0.016f, 0.026f), tankBareMetalMaterial, true);
                    CreatePrimitive(PrimitiveType.Sphere, root, "Flame Trooper Regulator Gauge", new Vector3(0.04f, 0.98f, -0.18f), new Vector3(0.04f, 0.04f, 0.016f), glassMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Flame Nozzle Heat Shield", new Vector3(0.29f, 0.91f, 0.47f), new Vector3(0.075f, 0.032f, 0.06f), tankBareMetalMaterial, true);
                    break;
                case UnitKind.Engineer:
                    GameObject cableSpool = CreatePrimitive(PrimitiveType.Cylinder, root, "Engineer Cable Spool Round Face", new Vector3(-0.18f, 0.82f, -0.255f), new Vector3(0.07f, 0.018f, 0.07f), tankBareMetalMaterial, true);
                    cableSpool.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    CreatePrimitive(PrimitiveType.Cube, root, "Engineer Tablet Dark Frame", new Vector3(0.2f, 0.78f, 0.305f), new Vector3(0.12f, 0.075f, 0.014f), infantryWebbingMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Engineer Tool Case Latch", new Vector3(-0.28f, 0.66f, 0.116f), new Vector3(0.055f, 0.022f, 0.014f), tankBareMetalMaterial, true);
                    break;
                default:
                    CreatePrimitive(PrimitiveType.Cube, root, "Rifleman Receiver Edge Highlight", new Vector3(0.24f, 0.98f, 0.34f), new Vector3(0.028f, 0.018f, 0.06f), tankBareMetalMaterial, true);
                    break;
            }
        }

        private void AddInfantryReadabilityKit(Transform root, UnitKind kind, Material teamMaterial)
        {
            Transform kit = CreateVisualRoot(root, "Infantry Readable Finish Kit", Vector3.zero);
            kit.gameObject.AddComponent<RtsFixedMaterial>();

            CreatePrimitive(PrimitiveType.Cube, kit, "Readable Chest White Armor Plate", new Vector3(0f, 1.015f, 0.276f), new Vector3(0.26f, 0.13f, 0.028f), infantryPlateMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Readable Chest Dark Upper Seam", new Vector3(0f, 1.092f, 0.292f), new Vector3(0.29f, 0.018f, 0.014f), infantryWebbingMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Readable Chest Dark Lower Seam", new Vector3(0f, 0.932f, 0.292f), new Vector3(0.25f, 0.016f, 0.014f), infantryWebbingMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Readable Helmet Cyan Slit", new Vector3(0f, 1.365f, 0.258f), new Vector3(0.18f, 0.024f, 0.018f), subtleGlowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Readable Helmet White Brow Plate", new Vector3(0f, 1.405f, 0.22f), new Vector3(0.22f, 0.04f, 0.055f), infantryPlateMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Readable Backpack Dark Core", new Vector3(0f, 0.98f, -0.21f), new Vector3(0.22f, 0.32f, 0.075f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Readable Backpack Team Stripe", new Vector3(0f, 1.08f, -0.258f), new Vector3(0.16f, 0.044f, 0.018f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Readable Belt Utility Block", new Vector3(0f, 0.705f, 0.205f), new Vector3(0.34f, 0.055f, 0.03f), infantryWebbingMaterial, true);

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                string prefix = side < 0f ? "Left" : "Right";
                CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Readable Team Shoulder Plate", new Vector3(side * 0.245f, 1.09f, 0.13f), new Vector3(0.082f, 0.055f, 0.04f), teamMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Readable Forearm White Strip", new Vector3(side * 0.325f, 0.875f, 0.255f), new Vector3(0.04f, 0.115f, 0.024f), infantryPlateMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Readable Thigh Gear Pouch", new Vector3(side * 0.205f, 0.575f, 0.12f), new Vector3(0.075f, 0.13f, 0.04f), infantryCanvasMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Readable Knee White Plate", new Vector3(side * 0.13f, 0.37f, 0.165f), new Vector3(0.095f, 0.058f, 0.034f), infantryPlateMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Readable Boot Black Sole", new Vector3(side * 0.14f, 0.04f, 0.24f), new Vector3(0.12f, 0.028f, 0.08f), darkMaterial, true);
            }

            switch (kind)
            {
                case UnitKind.RocketSoldier:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Readable Rocket Tube White Label", new Vector3(0.26f, 1.155f, 0.62f), new Vector3(0.088f, 0.032f, 0.18f), infantryPlateMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Readable Rocket Tube Hazard Label", new Vector3(0.26f, 1.155f, 0.86f), new Vector3(0.082f, 0.03f, 0.1f), hazardMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Readable Backblast Soot Panel", new Vector3(0.26f, 1.075f, 0.11f), new Vector3(0.105f, 0.075f, 0.052f), tankDeepGrimeMaterial, true);
                    break;
                case UnitKind.Grenadier:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Readable Grenadier Diagonal Bandolier", new Vector3(-0.08f, 0.985f, 0.302f), new Vector3(0.055f, 0.28f, 0.018f), infantryWebbingMaterial, true).transform.localRotation = Quaternion.Euler(0f, 0f, -16f);
                    for (int i = 0; i < 4; i++)
                    {
                        CreatePrimitive(PrimitiveType.Cube, kit, "Readable Grenade Amber Cap", new Vector3(-0.16f + i * 0.105f, 0.925f, 0.315f), new Vector3(0.042f, 0.024f, 0.018f), accentMaterial, true);
                    }

                    break;
                case UnitKind.FlameTrooper:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Readable Flame Apron Soot Plate", new Vector3(0f, 0.78f, 0.252f), new Vector3(0.25f, 0.18f, 0.026f), tankDeepGrimeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Readable Flame Apron White Chip", new Vector3(-0.055f, 0.84f, 0.272f), new Vector3(0.11f, 0.038f, 0.018f), infantryPlateMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Readable Fuel Tank Amber Band", new Vector3(0.1f, 1.04f, -0.305f), new Vector3(0.11f, 0.035f, 0.018f), accentMaterial, true);
                    break;
                case UnitKind.Engineer:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Readable Engineer Tablet Cyan Face", new Vector3(0.22f, 0.805f, 0.326f), new Vector3(0.13f, 0.085f, 0.018f), glassMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Readable Engineer Tool Case White Label", new Vector3(-0.3f, 0.675f, 0.14f), new Vector3(0.09f, 0.04f, 0.018f), infantryPlateMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Readable Engineer Helmet Lamp Glow", new Vector3(0f, 1.45f, 0.255f), new Vector3(0.09f, 0.035f, 0.02f), glowMaterial, true);
                    break;
                default:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Readable Rifle Receiver White Scratch", new Vector3(0.25f, 0.985f, 0.36f), new Vector3(0.034f, 0.018f, 0.06f), tankBareMetalMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Readable Rifle Muzzle Soot", new Vector3(0.28f, 1.02f, 0.52f), new Vector3(0.04f, 0.024f, 0.028f), tankDeepGrimeMaterial, true);
                    break;
            }
        }

        private void AddInfantryFinalSilhouettePass(Transform root, UnitKind kind, Material teamMaterial)
        {
            Transform kit = CreateVisualRoot(root, "Infantry Final Silhouette Polish Kit", Vector3.zero);
            kit.gameObject.AddComponent<RtsFixedMaterial>();

            CreatePrimitive(PrimitiveType.Cube, kit, "Infantry Final Chest Left Armor Facet", new Vector3(-0.09f, 1.025f, 0.31f), new Vector3(0.105f, 0.145f, 0.026f), whiteMaterial, true).transform.localRotation = Quaternion.Euler(0f, 0f, -4f);
            CreatePrimitive(PrimitiveType.Cube, kit, "Infantry Final Chest Right Armor Facet", new Vector3(0.1f, 1.01f, 0.31f), new Vector3(0.105f, 0.125f, 0.026f), infantryPlateMaterial, true).transform.localRotation = Quaternion.Euler(0f, 0f, 4f);
            CreatePrimitive(PrimitiveType.Cube, kit, "Infantry Final Abdomen Dark Plate Gap", new Vector3(0f, 0.88f, 0.305f), new Vector3(0.22f, 0.032f, 0.02f), infantryWebbingMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Infantry Final Team Collar Chip", new Vector3(0.12f, 1.15f, 0.245f), new Vector3(0.09f, 0.032f, 0.02f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Infantry Final Helmet Top Scuffed Panel", new Vector3(-0.04f, 1.49f, 0.09f), new Vector3(0.18f, 0.026f, 0.15f), infantryPlateMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Infantry Final Helmet Rear Dark Band", new Vector3(0f, 1.405f, -0.075f), new Vector3(0.22f, 0.035f, 0.026f), infantryWebbingMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Infantry Final Visor Glass Highlight", new Vector3(0.048f, 1.36f, 0.276f), new Vector3(0.08f, 0.018f, 0.014f), glowMaterial, true);

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                string prefix = side < 0f ? "Left" : "Right";
                CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Infantry Final Shoulder Side Shell", new Vector3(side * 0.285f, 1.055f, 0.13f), new Vector3(0.055f, 0.085f, 0.07f), side < 0f ? whiteMaterial : infantryPlateMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Infantry Final Forearm Dark Undersuit", new Vector3(side * 0.34f, 0.84f, 0.19f), new Vector3(0.035f, 0.13f, 0.028f), infantryWebbingMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Infantry Final Thigh Armor Face", new Vector3(side * 0.12f, 0.49f, 0.18f), new Vector3(0.08f, 0.14f, 0.03f), side < 0f ? infantryPlateMaterial : tankDustMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Infantry Final Shin White Insert", new Vector3(side * 0.12f, 0.27f, 0.19f), new Vector3(0.065f, 0.11f, 0.024f), whiteMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Infantry Final Boot Sole Worn Edge", new Vector3(side * 0.14f, 0.075f, 0.295f), new Vector3(0.105f, 0.024f, 0.035f), tankBareMetalMaterial, true);
            }

            switch (kind)
            {
                case UnitKind.RocketSoldier:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Rocket Soldier Final Rear Blast Blanket", new Vector3(0.03f, 1.03f, -0.285f), new Vector3(0.34f, 0.18f, 0.032f), tankDeepGrimeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Rocket Soldier Final Tube White Serial Plate", new Vector3(0.26f, 1.19f, 0.42f), new Vector3(0.085f, 0.024f, 0.18f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Rocket Soldier Final Tube Team Clamp", new Vector3(0.26f, 1.19f, 0.67f), new Vector3(0.082f, 0.024f, 0.1f), teamMaterial, true);
                    break;
                case UnitKind.Grenadier:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Grenadier Final Chest White Grenade Label", new Vector3(-0.18f, 1.005f, 0.322f), new Vector3(0.09f, 0.028f, 0.018f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Grenadier Final Satchel Armor Face", new Vector3(0.295f, 0.73f, -0.025f), new Vector3(0.07f, 0.13f, 0.026f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Grenadier Final Throwing Arm Team Flash", new Vector3(-0.325f, 0.95f, 0.255f), new Vector3(0.05f, 0.095f, 0.018f), teamMaterial, true);
                    break;
                case UnitKind.FlameTrooper:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Flame Trooper Final Sooted Face Shield", new Vector3(0f, 1.315f, 0.285f), new Vector3(0.19f, 0.062f, 0.018f), tankDeepGrimeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Flame Trooper Final Fuel Harness Crossbar", new Vector3(0f, 0.99f, -0.31f), new Vector3(0.3f, 0.045f, 0.02f), tankBareMetalMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Flame Trooper Final Apron Hazard Chip", new Vector3(0.07f, 0.79f, 0.282f), new Vector3(0.08f, 0.038f, 0.014f), hazardMaterial, true);
                    break;
                case UnitKind.Engineer:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Engineer Final Helmet White Service Stripe", new Vector3(0f, 1.505f, 0.17f), new Vector3(0.18f, 0.018f, 0.055f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Engineer Final Chest Tool Badge", new Vector3(-0.08f, 1.02f, 0.335f), new Vector3(0.085f, 0.042f, 0.016f), teamMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Engineer Final Tablet Cable", new Vector3(0.12f, 0.76f, 0.25f), new Vector3(0.02f, 0.12f, 0.014f), infantryWebbingMaterial, true);
                    break;
                default:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Rifleman Final Stock White Wear", new Vector3(0.2f, 1.02f, 0.23f), new Vector3(0.055f, 0.025f, 0.06f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Rifleman Final Team Magazine Band", new Vector3(0.25f, 0.93f, 0.33f), new Vector3(0.055f, 0.028f, 0.04f), teamMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Rifleman Final Barrel Heat Line", new Vector3(0.29f, 1.055f, 0.58f), new Vector3(0.032f, 0.018f, 0.15f), tankBareMetalMaterial, true);
                    break;
            }
        }

        private void AddVehicleReadabilityKit(Transform root, UnitKind kind, Material teamMaterial)
        {
            Transform kit = CreateVisualRoot(root, kind + " Readable Finish Kit", Vector3.zero);
            kit.gameObject.AddComponent<RtsFixedMaterial>();

            switch (kind)
            {
                case UnitKind.Tank:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Tank Readable Front White Armor", new Vector3(0f, 0.525f, 0.96f), new Vector3(0.82f, 0.035f, 0.2f), whiteMaterial, true).transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Tank Readable Front Grime Seam", new Vector3(0f, 0.49f, 1.08f), new Vector3(0.9f, 0.024f, 0.035f), tankDeepGrimeMaterial, true).transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Tank Readable Turret Team Plate", new Vector3(0f, 0.81f, -0.04f), new Vector3(0.5f, 0.03f, 0.26f), teamMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Tank Readable Turret White Range Box", new Vector3(0.3f, 0.74f, 0.29f), new Vector3(0.24f, 0.08f, 0.09f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Tank Readable Cyan Sight", new Vector3(0.3f, 0.74f, 0.355f), new Vector3(0.13f, 0.045f, 0.03f), glowMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Tank Readable Engine Soot Deck", new Vector3(0f, 0.555f, -0.7f), new Vector3(0.8f, 0.025f, 0.28f), tankDeepGrimeMaterial, true);
                    AddBoltLine(kit, "Tank Readable Glacis Bolt Run", new Vector3(-0.34f, 0.545f, 0.99f), new Vector3(0.17f, 0f, 0f), 5, 0.018f);
                    break;
                case UnitKind.Harvester:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Readable Hopper White Panel", new Vector3(0f, 1.09f, -0.38f), new Vector3(0.76f, 0.035f, 0.42f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Readable Ore Dirt Wash", new Vector3(0f, 1.12f, -0.72f), new Vector3(0.72f, 0.03f, 0.18f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Readable Conveyor Hazard Bar", new Vector3(0f, 0.65f, 0.62f), new Vector3(0.5f, 0.035f, 0.06f), hazardMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Readable Cab Team Plate", new Vector3(0f, 1.18f, 0.08f), new Vector3(0.42f, 0.034f, 0.13f), teamMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Readable Collector Soot Lip", new Vector3(0f, 0.26f, 1.62f), new Vector3(0.58f, 0.06f, 0.075f), tankDeepGrimeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Readable Rear Roof Panel Grid A", new Vector3(-0.28f, 1.18f, 0.22f), new Vector3(0.48f, 0.03f, 0.34f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Readable Rear Roof Panel Grid B", new Vector3(0.3f, 1.185f, 0.48f), new Vector3(0.42f, 0.03f, 0.3f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Readable Roof Center Dark Seam", new Vector3(0f, 1.205f, 0.34f), new Vector3(0.035f, 0.022f, 0.62f), armorShadowMaterial, true);
                    BuildVent(kit, "Harvester Readable Roof Intake Grille", new Vector3(0.42f, 1.21f, -0.02f), new Vector3(0.34f, 0.032f, 0.24f), 5);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Readable Front Cab White Brow", new Vector3(0f, 1.22f, -1.16f), new Vector3(0.72f, 0.055f, 0.06f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Readable Cab Glass Band", new Vector3(0f, 0.98f, -1.205f), new Vector3(0.54f, 0.11f, 0.035f), glassMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Readable Collector Top Guard", new Vector3(0f, 0.54f, 1.4f), new Vector3(0.72f, 0.055f, 0.11f), tankBareMetalMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Readable Collector Amber Warning Guard", new Vector3(0f, 0.62f, 1.38f), new Vector3(0.56f, 0.045f, 0.07f), accentMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Readable Intake Dark Mouth", new Vector3(0f, 0.42f, 1.55f), new Vector3(0.62f, 0.12f, 0.045f), tankDeepGrimeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Readable Intake White Guard", new Vector3(0f, 0.49f, 1.59f), new Vector3(0.5f, 0.05f, 0.035f), whiteMaterial, true);
                    AddBoltLine(kit, "Harvester Readable Hopper Rivets", new Vector3(-0.36f, 1.13f, -0.18f), new Vector3(0.18f, 0f, 0f), 5, 0.016f);

                    for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                    {
                        float side = sideIndex == 0 ? -1f : 1f;
                        string prefix = side < 0f ? "Left" : "Right";
                        CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Harvester Readable Large Side White Plate", new Vector3(side * 0.9f, 0.78f, -0.32f), new Vector3(0.035f, 0.28f, 0.58f), whiteMaterial, true);
                        CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Harvester Readable Side Dark Rib A", new Vector3(side * 0.93f, 0.78f, -0.62f), new Vector3(0.022f, 0.34f, 0.032f), armorShadowMaterial, true);
                        CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Harvester Readable Side Dark Rib B", new Vector3(side * 0.93f, 0.78f, -0.04f), new Vector3(0.022f, 0.34f, 0.032f), armorShadowMaterial, true);
                        CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Harvester Readable Lower Mud Skirt", new Vector3(side * 0.93f, 0.34f, -0.08f), new Vector3(0.05f, 0.14f, 0.96f), tankDeepGrimeMaterial, true);
                        CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Harvester Readable Hydraulic Hose", new Vector3(side * 0.73f, 0.5f, 1.02f), new Vector3(0.035f, 0.06f, 0.36f), darkMaterial, true).transform.localRotation = Quaternion.Euler(0f, side * 4f, 0f);

                        for (int i = 0; i < 4; i++)
                        {
                            float z = -0.88f + i * 0.43f;
                            GameObject hub = CreatePrimitive(PrimitiveType.Cylinder, kit, prefix + " Harvester Readable Road Wheel Hub", new Vector3(side * 1.13f, 0.28f, z), new Vector3(0.08f, 0.018f, 0.08f), tankBareMetalMaterial, true);
                            hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                        }
                    }

                    break;
                case UnitKind.Skyraider:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Readable Nose White Strike Plate", new Vector3(0f, 1.015f, 1.08f), new Vector3(0.58f, 0.03f, 0.24f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Readable Cockpit Cyan Glint", new Vector3(0f, 1.145f, 0.48f), new Vector3(0.42f, 0.035f, 0.05f), glowMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Readable Center Team Stripe", new Vector3(0f, 1.05f, -0.12f), new Vector3(0.2f, 0.028f, 0.44f), teamMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Readable Rear Heat Stain", new Vector3(0f, 0.88f, -1.08f), new Vector3(0.62f, 0.035f, 0.12f), tankDeepGrimeMaterial, true);
                    for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                    {
                        float side = sideIndex == 0 ? -1f : 1f;
                        string prefix = side < 0f ? "Left" : "Right";
                        CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Skyraider Readable Wing White Panel", new Vector3(side * 1.1f, 1.035f, -0.08f), new Vector3(0.48f, 0.026f, 0.24f), whiteMaterial, true);
                        CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Skyraider Readable Wing Dark Seam", new Vector3(side * 1.1f, 1.04f, 0.18f), new Vector3(0.52f, 0.02f, 0.035f), tankDeepGrimeMaterial, true);
                        CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Skyraider Readable Hardpoint Hazard", new Vector3(side * 1.56f, 0.78f, 0.2f), new Vector3(0.18f, 0.035f, 0.26f), hazardMaterial, true);
                    }

                    break;
                case UnitKind.OrcaLifter:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Orca Readable Cargo Roof White Plate", new Vector3(0f, 1.035f, -0.32f), new Vector3(0.66f, 0.03f, 0.52f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Orca Readable Cargo Roof Team Stripe", new Vector3(0f, 1.06f, 0.1f), new Vector3(0.24f, 0.026f, 0.24f), teamMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Orca Readable Rear Ramp Hazard", new Vector3(0f, 0.54f, -1.58f), new Vector3(0.52f, 0.035f, 0.095f), hazardMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Orca Readable Nose Cyan Sensor", new Vector3(0f, 0.74f, 1.63f), new Vector3(0.16f, 0.05f, 0.035f), subtleGlowMaterial, true);
                    for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                    {
                        float side = sideIndex == 0 ? -1f : 1f;
                        string prefix = side < 0f ? "Left" : "Right";
                        CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Orca Readable Fan Hazard Tab", new Vector3(side * 1.9f, 1.02f, 0.82f), new Vector3(0.34f, 0.028f, 0.07f), hazardMaterial, true);
                        CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Orca Readable Boom White Plate", new Vector3(side * 1.34f, 0.9f, -0.1f), new Vector3(0.46f, 0.026f, 0.18f), whiteMaterial, true);
                        CreatePrimitive(PrimitiveType.Cube, kit, prefix + " Orca Readable Skid Dirt Shoe", new Vector3(side * 0.74f, 0.14f, -0.36f), new Vector3(0.2f, 0.046f, 0.72f), tankDeepGrimeMaterial, true);
                    }

                    break;
            }

            AddVehicleFinalSurfacePolish(root, kind, teamMaterial);
        }

        private void AddVehicleFinalSurfacePolish(Transform root, UnitKind kind, Material teamMaterial)
        {
            Transform kit = CreateVisualRoot(root, kind + " Final Surface Polish Kit", Vector3.zero);
            kit.gameObject.AddComponent<RtsFixedMaterial>();

            switch (kind)
            {
                case UnitKind.Tank:
                    AddRoofPanelGrid(kit, "Tank Final Hull Micro Panel Grid", new Vector3(0f, 0.585f, 0.08f), 1.08f, 1.55f, 5, 4);
                    AddTexturePatchGrid(kit, "Tank Final Hull", new Vector3(0f, 0.604f, 0.06f), 0.92f, 1.28f, 5, 3, true, teamMaterial);
                    AddRoofPanelGrid(kit, "Tank Final Turret Micro Panel Grid", new Vector3(0f, 0.842f, -0.02f), 0.64f, 0.52f, 3, 3);

                    CreatePrimitive(PrimitiveType.Cube, kit, "Tank Final White Turret Unit Plate", new Vector3(-0.22f, 0.864f, 0.08f), new Vector3(0.28f, 0.022f, 0.16f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Tank Final Team Turret Hash", new Vector3(0.22f, 0.866f, 0.08f), new Vector3(0.22f, 0.022f, 0.13f), teamMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Tank Final Turret Rear Bedroll", new Vector3(0f, 0.81f, -0.5f), new Vector3(0.42f, 0.085f, 0.13f), infantryCanvasMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Tank Final Turret Bedroll Strap", new Vector3(0f, 0.865f, -0.5f), new Vector3(0.36f, 0.022f, 0.035f), tankDeepGrimeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Tank Final Glacis Dust Wash", new Vector3(0f, 0.505f, 0.96f), new Vector3(0.86f, 0.028f, 0.1f), tankDustMaterial, true).transform.localRotation = Quaternion.Euler(-9f, 0f, 0f);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Tank Final Front Tow Hook Left", new Vector3(-0.36f, 0.38f, 1.08f), new Vector3(0.12f, 0.04f, 0.06f), tankBareMetalMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Tank Final Front Tow Hook Right", new Vector3(0.36f, 0.38f, 1.08f), new Vector3(0.12f, 0.04f, 0.06f), tankBareMetalMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Tank Final Barrel Thermal Sleeve", new Vector3(0f, 0.675f, 1.2f), new Vector3(0.16f, 0.046f, 0.68f), tankDeepGrimeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cylinder, kit, "Tank Final Muzzle Wear Ring", new Vector3(0f, 0.67f, 1.7f), new Vector3(0.072f, 0.03f, 0.072f), tankBareMetalMaterial, true).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                    for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                    {
                        float side = sideIndex == 0 ? -1f : 1f;
                        string prefix = side < 0f ? "Left" : "Right";
                        CreatePrimitive(PrimitiveType.Cube, kit, "Tank Final " + prefix + " Skirt White Replacement Plate", new Vector3(side * 0.705f, 0.41f, 0.38f), new Vector3(0.028f, 0.18f, 0.36f), whiteMaterial, true);
                        CreatePrimitive(PrimitiveType.Cube, kit, "Tank Final " + prefix + " Skirt Dirty Replacement Plate", new Vector3(side * 0.708f, 0.38f, -0.26f), new Vector3(0.026f, 0.16f, 0.42f), tankDustMaterial, true);
                        CreatePrimitive(PrimitiveType.Cube, kit, "Tank Final " + prefix + " Track Lower Mud Line", new Vector3(side * 0.735f, 0.18f, 0.0f), new Vector3(0.042f, 0.055f, 1.42f), tankDustMaterial, true);
                        CreatePrimitive(PrimitiveType.Cube, kit, "Tank Final " + prefix + " Side Tool Rail", new Vector3(side * 0.71f, 0.55f, -0.5f), new Vector3(0.026f, 0.045f, 0.58f), tankBareMetalMaterial, true);
                        AddBoltLine(kit, "Tank Final " + prefix + " Skirt Micro Rivets", new Vector3(side * 0.735f, 0.5f, -0.54f), new Vector3(0f, 0f, 0.17f), 8, 0.012f);
                    }

                    break;
                case UnitKind.Harvester:
                    AddRoofPanelGrid(kit, "Harvester Final Hopper Micro Panel Grid", new Vector3(0f, 1.205f, 0.24f), 1.0f, 1.12f, 4, 4);
                    AddTexturePatchGrid(kit, "Harvester Final Hopper", new Vector3(0f, 1.228f, 0.24f), 0.86f, 0.9f, 4, 3, true, teamMaterial);

                    CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Final Cab White Crew Plate", new Vector3(-0.24f, 1.315f, -0.78f), new Vector3(0.32f, 0.026f, 0.22f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Final Cab Team Crew Plate", new Vector3(0.24f, 1.318f, -0.78f), new Vector3(0.24f, 0.025f, 0.2f), teamMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Final Cab Wiper Bar", new Vector3(0f, 1.03f, -1.215f), new Vector3(0.52f, 0.025f, 0.026f), darkMaterial, true).transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Final Intake Shadow Mouth", new Vector3(0f, 0.44f, 1.64f), new Vector3(0.7f, 0.13f, 0.046f), tankDeepGrimeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Final Intake Hazard Teeth", new Vector3(0f, 0.54f, 1.68f), new Vector3(0.62f, 0.04f, 0.052f), hazardMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Final Conveyor Belt Dark Wear", new Vector3(0f, 0.61f, 0.86f), new Vector3(0.48f, 0.026f, 0.62f), tankDeepGrimeMaterial, true);

                    for (int i = 0; i < 6; i++)
                    {
                        float x = -0.36f + i * 0.144f;
                        CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Final Collector Tooth Highlight", new Vector3(x, 0.3f, 1.78f), new Vector3(0.045f, 0.065f, 0.16f), tankBareMetalMaterial, true).transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);
                    }

                    for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                    {
                        float side = sideIndex == 0 ? -1f : 1f;
                        string prefix = side < 0f ? "Left" : "Right";
                        CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Final " + prefix + " Side Tall White Armor", new Vector3(side * 0.92f, 0.82f, -0.32f), new Vector3(0.03f, 0.34f, 0.5f), whiteMaterial, true);
                        CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Final " + prefix + " Lower Mud Apron", new Vector3(side * 0.95f, 0.34f, 0.0f), new Vector3(0.044f, 0.14f, 1.08f), tankDustMaterial, true);
                        CreatePrimitive(PrimitiveType.Cube, kit, "Harvester Final " + prefix + " Hydraulic Cable", new Vector3(side * 0.62f, 0.5f, 1.22f), new Vector3(0.036f, 0.052f, 0.5f), tankDeepGrimeMaterial, true);
                        AddBoltLine(kit, "Harvester Final " + prefix + " Hopper Rivet Run", new Vector3(side * 0.76f, 1.03f, -0.34f), new Vector3(0f, 0f, 0.16f), 7, 0.012f);
                    }

                    break;
                case UnitKind.Skyraider:
                    AddAircraftSkinPlateGrid(kit, "Skyraider Final Left Wing", -1f, 1.055f, -0.04f, 0.74f, 0.2f);
                    AddAircraftSkinPlateGrid(kit, "Skyraider Final Right Wing", 1f, 1.055f, -0.04f, 0.74f, 0.2f);

                    CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Final Nose White Strike Chip", new Vector3(-0.18f, 1.02f, 1.2f), new Vector3(0.22f, 0.022f, 0.1f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Final Nose Soot Chip", new Vector3(0.2f, 1.018f, 1.02f), new Vector3(0.2f, 0.022f, 0.12f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Final Cockpit Center Mullion", new Vector3(0f, 1.165f, 0.48f), new Vector3(0.032f, 0.044f, 0.38f), tankDeepGrimeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Final Rear Exhaust Soot Wash", new Vector3(0f, 0.86f, -1.12f), new Vector3(0.58f, 0.035f, 0.16f), tankDeepGrimeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cylinder, kit, "Skyraider Final Rotor Hub Warning Ring", new Vector3(0f, 1.465f, -0.12f), new Vector3(0.24f, 0.022f, 0.24f), hazardMaterial, true);

                    for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                    {
                        float side = sideIndex == 0 ? -1f : 1f;
                        string prefix = side < 0f ? "Left" : "Right";
                        CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Final " + prefix + " Wing Leading Edge Wear", new Vector3(side * 1.14f, 1.075f, 0.36f), new Vector3(0.72f, 0.018f, 0.04f), tankBareMetalMaterial, true);
                        CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Final " + prefix + " Hardpoint Clamp Front", new Vector3(side * 1.5f, 0.59f, 0.3f), new Vector3(0.055f, 0.045f, 0.14f), tankBareMetalMaterial, true);
                        CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Final " + prefix + " Hardpoint Clamp Rear", new Vector3(side * 1.5f, 0.59f, -0.06f), new Vector3(0.055f, 0.045f, 0.14f), tankBareMetalMaterial, true);
                        CreatePrimitive(PrimitiveType.Cube, kit, "Skyraider Final " + prefix + " Team Wing Hash", new Vector3(side * 1.34f, 1.08f, -0.35f), new Vector3(0.22f, 0.018f, 0.052f), teamMaterial, true);
                        AddBoltLine(kit, "Skyraider Final " + prefix + " Wing Rivet Run", new Vector3(side * 0.72f, 1.082f, 0.32f), new Vector3(side * 0.14f, 0f, 0f), 7, 0.012f);
                    }

                    break;
                case UnitKind.OrcaLifter:
                    AddAircraftSkinPlateGrid(kit, "Orca Final Left Boom", -1f, 0.93f, -0.08f, 0.72f, 0.16f);
                    AddAircraftSkinPlateGrid(kit, "Orca Final Right Boom", 1f, 0.93f, -0.08f, 0.72f, 0.16f);

                    CreatePrimitive(PrimitiveType.Cube, kit, "Orca Final Cargo Roof Panel Grid Plate", new Vector3(0f, 1.055f, -0.4f), new Vector3(0.78f, 0.024f, 0.5f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Orca Final Cargo Roof Soot Seam", new Vector3(0f, 1.08f, -0.08f), new Vector3(0.06f, 0.018f, 0.68f), tankDeepGrimeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Orca Final Cockpit Dark Mullion", new Vector3(0f, 1.065f, 1.18f), new Vector3(0.032f, 0.052f, 0.24f), tankDeepGrimeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Orca Final Rear Ramp Dirt Rub", new Vector3(0f, 0.52f, -1.58f), new Vector3(0.54f, 0.026f, 0.1f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Orca Final Sling Hook Block", new Vector3(0f, 0.12f, -0.28f), new Vector3(0.18f, 0.14f, 0.18f), tankBareMetalMaterial, true);

                    for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                    {
                        float side = sideIndex == 0 ? -1f : 1f;
                        string prefix = side < 0f ? "Left" : "Right";
                        CreatePrimitive(PrimitiveType.Cube, kit, "Orca Final " + prefix + " Fan Cowling White Chip", new Vector3(side * 1.9f, 1.035f, -0.45f), new Vector3(0.24f, 0.018f, 0.052f), whiteMaterial, true);
                        CreatePrimitive(PrimitiveType.Cube, kit, "Orca Final " + prefix + " Fan Cowling Hazard Tick", new Vector3(side * 1.9f, 1.035f, 0.82f), new Vector3(0.28f, 0.018f, 0.052f), hazardMaterial, true);
                        CreatePrimitive(PrimitiveType.Cube, kit, "Orca Final " + prefix + " Boom Lower Dirt Streak", new Vector3(side * 1.34f, 0.82f, -0.16f), new Vector3(0.5f, 0.02f, 0.035f), tankDustMaterial, true);
                        CreatePrimitive(PrimitiveType.Cube, kit, "Orca Final " + prefix + " Cargo Door Team Stamp", new Vector3(side * 0.64f, 0.67f, -0.54f), new Vector3(0.028f, 0.1f, 0.18f), teamMaterial, true);
                        AddBoltLine(kit, "Orca Final " + prefix + " Boom Rivet Run", new Vector3(side * 1.02f, 0.925f, 0.18f), new Vector3(side * 0.16f, 0f, 0f), 7, 0.012f);
                    }

                    break;
                default:
                    Destroy(kit.gameObject);
                    break;
            }
        }

        private void ImproveImportedTankRenderers(GameObject visual)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;

                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    Material material = CreateRuntimeCompatibleMaterial(source, new Color(0.55f, 0.56f, 0.48f));
                    SetFloatIfPresent(material, "_Metallic", 0.16f);
                    SetFloatIfPresent(material, "_Smoothness", 0.28f);
                    SetFloatIfPresent(material, "_Glossiness", 0.28f);
                    if (material.HasProperty("_EmissionColor"))
                    {
                        material.SetColor("_EmissionColor", new Color(0.04f, 0.22f, 0.24f) * 0.45f);
                    }

                    Texture mainTexture = material.mainTexture;
                    if (mainTexture != null)
                    {
                        mainTexture.filterMode = FilterMode.Bilinear;
                        mainTexture.anisoLevel = 4;
                    }

                    materials[i] = material;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private void AddImportedTankLightDetailKit(Transform root, Material teamMaterial)
        {
            Transform kit = CreateVisualRoot(root, "TX-9R Detail Accents", Vector3.zero);
            kit.gameObject.AddComponent<RtsFixedMaterial>();

            CreatePrimitive(PrimitiveType.Cube, kit, "Dust Worn Front Glacis", new Vector3(0f, 0.41f, 0.63f), new Vector3(0.92f, 0.018f, 0.26f), tankDustMaterial, true).transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cube, kit, "Dust Worn Engine Deck", new Vector3(0f, 0.52f, -0.47f), new Vector3(0.74f, 0.016f, 0.32f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Small Turret Dust Patch", new Vector3(-0.08f, 0.71f, -0.05f), new Vector3(0.36f, 0.014f, 0.28f), tankDustMaterial, true);

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                float x = side * 0.62f;
                for (int i = 0; i < 5; i++)
                {
                    float z = -0.72f + i * 0.36f;
                    GameObject hub = CreatePrimitive(PrimitiveType.Cylinder, kit, "Road Wheel Worn Metal Hub", new Vector3(x + side * 0.022f, 0.22f, z), new Vector3(0.058f, 0.014f, 0.058f), tankBareMetalMaterial, true);
                    hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            }

            for (int i = 0; i < 4; i++)
            {
                float z = 0.42f + i * 0.32f;
                GameObject band = CreatePrimitive(PrimitiveType.Cylinder, kit, "Single Barrel Wear Band", new Vector3(0f, 0.65f, z), new Vector3(0.044f, 0.032f, 0.044f), tankBareMetalMaterial, true);
                band.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            GameObject muzzleSoot = CreatePrimitive(PrimitiveType.Cylinder, kit, "Single Barrel Muzzle Soot", new Vector3(0f, 0.65f, 1.62f), new Vector3(0.06f, 0.04f, 0.06f), tankDeepGrimeMaterial, true);
            muzzleSoot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Deck Stowage", new Vector3(-0.34f, 0.55f, -0.62f), new Vector3(0.18f, 0.06f, 0.16f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Rolled Bedroll", new Vector3(0.34f, 0.56f, -0.62f), new Vector3(0.2f, 0.05f, 0.12f), tankDustMaterial, true);

            CreatePrimitive(PrimitiveType.Cube, kit, "Front White Armor Applique", new Vector3(0f, 0.49f, 0.84f), new Vector3(0.72f, 0.028f, 0.18f), whiteMaterial, true).transform.localRotation = Quaternion.Euler(-9f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left White Side Skirt", new Vector3(-0.66f, 0.34f, 0.04f), new Vector3(0.035f, 0.22f, 1.24f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right White Side Skirt", new Vector3(0.66f, 0.34f, 0.04f), new Vector3(0.035f, 0.22f, 1.24f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Left Team Recognition Plate", new Vector3(-0.69f, 0.46f, 0.34f), new Vector3(0.028f, 0.12f, 0.36f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Right Team Recognition Plate", new Vector3(0.69f, 0.46f, 0.34f), new Vector3(0.028f, 0.12f, 0.36f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Top Team Panel", new Vector3(0f, 0.78f, -0.04f), new Vector3(0.42f, 0.02f, 0.26f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret White Rangefinder Housing", new Vector3(0.29f, 0.72f, 0.26f), new Vector3(0.22f, 0.07f, 0.08f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Cyan Rangefinder Lens", new Vector3(0.29f, 0.72f, 0.32f), new Vector3(0.12f, 0.045f, 0.035f), glowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Rear Exhaust Grille", new Vector3(0f, 0.48f, -0.92f), new Vector3(0.62f, 0.04f, 0.08f), darkMaterial, true);
            CreatePipe(kit, "Left Rear Exhaust Pipe", new Vector3(-0.34f, 0.54f, -0.92f), new Vector3(0.045f, 0.28f, 0.045f), new Vector3(90f, 0f, 0f));
            CreatePipe(kit, "Right Rear Exhaust Pipe", new Vector3(0.34f, 0.54f, -0.92f), new Vector3(0.045f, 0.28f, 0.045f), new Vector3(90f, 0f, 0f));

            for (int i = 0; i < 5; i++)
            {
                float z = -0.62f + i * 0.28f;
                CreatePrimitive(PrimitiveType.Cube, kit, "Left Reactive Armor Brick", new Vector3(-0.52f, 0.58f, z), new Vector3(0.16f, 0.055f, 0.12f), armorHighlightMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, "Right Reactive Armor Brick", new Vector3(0.52f, 0.58f, z), new Vector3(0.16f, 0.055f, 0.12f), armorHighlightMaterial, true);
            }

            for (int i = 0; i < 6; i++)
            {
                float z = -0.76f + i * 0.3f;
                CreatePrimitive(PrimitiveType.Cube, kit, "Left Track Cleat Highlight", new Vector3(-0.64f, 0.14f, z), new Vector3(0.06f, 0.035f, 0.13f), tankBareMetalMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, "Right Track Cleat Highlight", new Vector3(0.64f, 0.14f, z), new Vector3(0.06f, 0.035f, 0.13f), tankBareMetalMaterial, true);
            }

            AddBoltLine(kit, "Front Glacis Applique Bolts", new Vector3(-0.28f, 0.51f, 0.94f), new Vector3(0.14f, 0f, 0f), 5, 0.022f);
            AddBoltLine(kit, "Turret Detail Bolts", new Vector3(-0.24f, 0.79f, -0.2f), new Vector3(0.12f, 0f, 0f), 5, 0.02f);
        }

        private void BuildBattleTankVisual(Transform root, Material teamMaterial)
        {
            Transform tank = CreateVisualRoot(root, "Detailed Battle Tank", Vector3.zero);

            CreatePrimitive(PrimitiveType.Cube, tank, "Lower Armored Hull", new Vector3(0f, 0.48f, 0f), new Vector3(1.72f, 0.46f, 2.22f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, tank, "Upper Fighting Compartment", new Vector3(0f, 0.78f, -0.12f), new Vector3(1.42f, 0.36f, 1.58f), armorHighlightMaterial, true);
            GameObject glacis = CreatePrimitive(PrimitiveType.Cube, tank, "Sloped Front Glacis", new Vector3(0f, 0.73f, 1.02f), new Vector3(1.48f, 0.16f, 0.72f), armorHighlightMaterial, true);
            glacis.transform.localRotation = Quaternion.Euler(-14f, 0f, 0f);
            GameObject rearPlate = CreatePrimitive(PrimitiveType.Cube, tank, "Rear Engine Plate", new Vector3(0f, 0.83f, -0.98f), new Vector3(1.46f, 0.14f, 0.5f), armorMaterial, true);
            rearPlate.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);

            CreatePrimitive(PrimitiveType.Cube, tank, "Left Track Skirt", new Vector3(-0.94f, 0.43f, 0f), new Vector3(0.18f, 0.62f, 2.42f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, tank, "Right Track Skirt", new Vector3(0.94f, 0.43f, 0f), new Vector3(0.18f, 0.62f, 2.42f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, tank, "Left Upper Side Armor", new Vector3(-0.88f, 0.76f, 0.02f), new Vector3(0.1f, 0.28f, 2.18f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, tank, "Right Upper Side Armor", new Vector3(0.88f, 0.76f, 0.02f), new Vector3(0.1f, 0.28f, 2.18f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, tank, "Left Team Side Plate", new Vector3(-1.04f, 0.72f, 0.42f), new Vector3(0.05f, 0.16f, 0.58f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, tank, "Right Team Side Plate", new Vector3(1.04f, 0.72f, 0.42f), new Vector3(0.05f, 0.16f, 0.58f), teamMaterial);

            BuildTankTrack(tank, -1f);
            BuildTankTrack(tank, 1f);
            BuildTankDeckDetails(tank);

            CreatePrimitive(PrimitiveType.Cylinder, tank, "Turret Bearing", new Vector3(0f, 0.98f, -0.03f), new Vector3(0.74f, 0.1f, 0.74f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, tank, "Turret Armor Block", new Vector3(0f, 1.2f, 0.02f), new Vector3(1.08f, 0.42f, 0.9f), armorMaterial, true);
            GameObject turretFront = CreatePrimitive(PrimitiveType.Cube, tank, "Turret Sloped Mantlet", new Vector3(0f, 1.17f, 0.52f), new Vector3(0.72f, 0.32f, 0.28f), armorHighlightMaterial, true);
            turretFront.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cube, tank, "Turret Team Plate", new Vector3(0f, 1.42f, -0.08f), new Vector3(0.52f, 0.055f, 0.34f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cylinder, tank, "Commander Hatch", new Vector3(-0.28f, 1.47f, -0.18f), new Vector3(0.22f, 0.06f, 0.22f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, tank, "Loader Hatch", new Vector3(0.28f, 1.46f, -0.2f), new Vector3(0.18f, 0.05f, 0.18f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, tank, "Periscope Cluster", new Vector3(0f, 1.5f, 0.22f), new Vector3(0.42f, 0.08f, 0.12f), glassMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, tank, "Coaxial Sight Glow", new Vector3(-0.22f, 1.22f, 0.68f), new Vector3(0.12f, 0.08f, 0.05f), glowMaterial, true);

            BuildTankBarrel(tank);
            CreatePrimitive(PrimitiveType.Cylinder, tank, "Antenna Mast", new Vector3(0.45f, 1.78f, -0.48f), new Vector3(0.035f, 0.55f, 0.035f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Sphere, tank, "Antenna Tip", new Vector3(0.45f, 2.35f, -0.48f), new Vector3(0.07f, 0.07f, 0.07f), accentMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, tank, "Left Headlamp", new Vector3(-0.42f, 0.66f, 1.16f), new Vector3(0.16f, 0.1f, 0.06f), glowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, tank, "Right Headlamp", new Vector3(0.42f, 0.66f, 1.16f), new Vector3(0.16f, 0.1f, 0.06f), glowMaterial, true);
            AddVehicleReadabilityKit(tank, UnitKind.Tank, teamMaterial);
        }

        private void BuildTankTrack(Transform tank, float side)
        {
            float x = side * 1.05f;
            CreatePrimitive(PrimitiveType.Cube, tank, side < 0f ? "Left Track Belt" : "Right Track Belt", new Vector3(x, 0.34f, 0f), new Vector3(0.18f, 0.36f, 2.48f), darkMaterial, true);

            for (int i = 0; i < 6; i++)
            {
                float z = -0.9f + i * 0.36f;
                GameObject wheel = CreatePrimitive(PrimitiveType.Cylinder, tank, "Road Wheel", new Vector3(x + side * 0.02f, 0.34f, z), new Vector3(0.24f, 0.08f, 0.24f), pipeMaterial, true);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                GameObject hub = CreatePrimitive(PrimitiveType.Cylinder, tank, "Road Wheel Hub", new Vector3(x + side * 0.055f, 0.34f, z), new Vector3(0.1f, 0.035f, 0.1f), armorHighlightMaterial, true);
                hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

            for (int i = 0; i < 9; i++)
            {
                float z = -1.12f + i * 0.28f;
                CreatePrimitive(PrimitiveType.Cube, tank, "Lower Tread Pad", new Vector3(x, 0.12f, z), new Vector3(0.24f, 0.055f, 0.16f), trimMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, tank, "Upper Tread Pad", new Vector3(x, 0.62f, z), new Vector3(0.22f, 0.05f, 0.14f), trimMaterial, true);
            }

            GameObject frontSprocket = CreatePrimitive(PrimitiveType.Cylinder, tank, "Front Sprocket", new Vector3(x + side * 0.02f, 0.36f, 1.14f), new Vector3(0.3f, 0.1f, 0.3f), trimMaterial, true);
            frontSprocket.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            GameObject rearIdler = CreatePrimitive(PrimitiveType.Cylinder, tank, "Rear Idler", new Vector3(x + side * 0.02f, 0.36f, -1.14f), new Vector3(0.28f, 0.1f, 0.28f), trimMaterial, true);
            rearIdler.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        private void BuildTankDeckDetails(Transform tank)
        {
            BuildVent(tank, "Engine Deck Grille A", new Vector3(-0.36f, 1.0f, -0.78f), new Vector3(0.46f, 0.04f, 0.34f), 4);
            BuildVent(tank, "Engine Deck Grille B", new Vector3(0.36f, 1.0f, -0.78f), new Vector3(0.46f, 0.04f, 0.34f), 4);
            CreatePrimitive(PrimitiveType.Cube, tank, "Left Storage Box", new Vector3(-0.48f, 1.02f, -0.34f), new Vector3(0.34f, 0.12f, 0.22f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, tank, "Right Storage Box", new Vector3(0.48f, 1.02f, -0.34f), new Vector3(0.34f, 0.12f, 0.22f), trimMaterial, true);

            for (int i = 0; i < 5; i++)
            {
                float z = -0.92f + i * 0.34f;
                CreatePrimitive(PrimitiveType.Cube, tank, "Hull Bolt Left", new Vector3(-0.66f, 1.0f, z), new Vector3(0.055f, 0.035f, 0.055f), darkMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, tank, "Hull Bolt Right", new Vector3(0.66f, 1.0f, z), new Vector3(0.055f, 0.035f, 0.055f), darkMaterial, true);
            }
        }

        private void BuildTankBarrel(Transform tank)
        {
            GameObject baseSleeve = CreatePrimitive(PrimitiveType.Cylinder, tank, "Gun Base Sleeve", new Vector3(0f, 1.2f, 0.72f), new Vector3(0.18f, 0.3f, 0.18f), pipeMaterial, true);
            baseSleeve.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject barrelA = CreatePrimitive(PrimitiveType.Cylinder, tank, "Main Gun Barrel A", new Vector3(0f, 1.2f, 1.15f), new Vector3(0.075f, 0.64f, 0.075f), pipeMaterial, true);
            barrelA.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject barrelB = CreatePrimitive(PrimitiveType.Cylinder, tank, "Main Gun Barrel B", new Vector3(0f, 1.2f, 1.72f), new Vector3(0.06f, 0.5f, 0.06f), pipeMaterial, true);
            barrelB.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject muzzle = CreatePrimitive(PrimitiveType.Cylinder, tank, "Muzzle Brake Core", new Vector3(0f, 1.2f, 2.08f), new Vector3(0.1f, 0.18f, 0.1f), darkMaterial, true);
            muzzle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cube, tank, "Muzzle Brake Left Port", new Vector3(-0.12f, 1.2f, 2.08f), new Vector3(0.08f, 0.12f, 0.2f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, tank, "Muzzle Brake Right Port", new Vector3(0.12f, 1.2f, 2.08f), new Vector3(0.08f, 0.12f, 0.2f), darkMaterial, true);
        }

        private void BuildHarvesterVisual(Transform root, Material teamMaterial)
        {
            Transform harvester = CreateVisualRoot(root, "Industrial Harvester", Vector3.zero);

            CreatePrimitive(PrimitiveType.Cube, harvester, "Tracked Chassis", new Vector3(0f, 0.38f, 0.04f), new Vector3(1.66f, 0.42f, 2.18f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, harvester, "Armored Hopper", new Vector3(0f, 0.76f, 0.42f), new Vector3(1.34f, 0.72f, 1.34f), neutralMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, harvester, "Sloped Ore Bin", new Vector3(0f, 1.06f, 0.48f), new Vector3(1.12f, 0.18f, 1.0f), tankDustMaterial, true).transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cube, harvester, "Forward Cab", new Vector3(0f, 0.84f, -0.76f), new Vector3(1.16f, 0.72f, 0.78f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, harvester, "Cab Armor Brow", new Vector3(0f, 1.22f, -0.86f), new Vector3(1.24f, 0.12f, 0.38f), armorHighlightMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, harvester, "Cab Windshield", new Vector3(0f, 0.96f, -1.17f), new Vector3(0.78f, 0.22f, 0.06f), glassMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, harvester, "Left Team Door Plate", new Vector3(-0.64f, 0.78f, -0.64f), new Vector3(0.05f, 0.28f, 0.36f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, harvester, "Right Team Door Plate", new Vector3(0.64f, 0.78f, -0.64f), new Vector3(0.05f, 0.28f, 0.36f), teamMaterial);

            BuildHarvesterTrack(harvester, -1f);
            BuildHarvesterTrack(harvester, 1f);

            CreatePrimitive(PrimitiveType.Cube, harvester, "Ore Conveyor Bed", new Vector3(0f, 0.74f, 1.22f), new Vector3(0.6f, 0.14f, 0.92f), darkMaterial, true);
            for (int i = 0; i < 4; i++)
            {
                GameObject roller = CreatePrimitive(PrimitiveType.Cylinder, harvester, "Ore Conveyor Roller", new Vector3(0f, 0.85f, 0.9f + i * 0.2f), new Vector3(0.052f, 0.34f, 0.052f), pipeMaterial, true);
                roller.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

            GameObject collectorDrum = CreatePrimitive(PrimitiveType.Cylinder, harvester, "Rotary Collector Drum", new Vector3(0f, 0.28f, 1.46f), new Vector3(0.18f, 0.42f, 0.18f), tankBareMetalMaterial, true);
            collectorDrum.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            RtsSpinner drumSpinner = collectorDrum.AddComponent<RtsSpinner>();
            drumSpinner.Axis = Vector3.right;
            drumSpinner.DegreesPerSecond = 260f;
            for (int i = 0; i < 6; i++)
            {
                GameObject tooth = CreatePrimitive(PrimitiveType.Cube, harvester, "Collector Cutter Tooth", new Vector3(-0.34f + i * 0.136f, 0.3f, 1.68f), new Vector3(0.045f, 0.18f, 0.065f), tankBareMetalMaterial, true);
                tooth.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
            }

            BuildVent(harvester, "Harvester Engine Grille", new Vector3(0.48f, 1.16f, 0.02f), new Vector3(0.38f, 0.04f, 0.52f), 4);
            GameObject sideScrubber = CreatePrimitive(PrimitiveType.Cylinder, harvester, "Compact Side Exhaust Scrubber", new Vector3(-0.78f, 0.92f, 0.06f), new Vector3(0.08f, 0.28f, 0.08f), tankBareMetalMaterial, true);
            sideScrubber.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            BuildSmokeStack(harvester, "Harvester Stub Stack", new Vector3(-0.48f, 1.28f, -0.06f), 0.08f, 0.38f, false);
            CreatePrimitive(PrimitiveType.Cube, harvester, "Left Headlamp", new Vector3(-0.34f, 0.68f, -1.18f), new Vector3(0.16f, 0.1f, 0.05f), glowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, harvester, "Right Headlamp", new Vector3(0.34f, 0.68f, -1.18f), new Vector3(0.16f, 0.1f, 0.05f), glowMaterial, true);
            CreatePrimitive(PrimitiveType.Sphere, harvester, "Amber Roof Beacon", new Vector3(0.42f, 1.58f, -0.54f), new Vector3(0.11f, 0.11f, 0.11f), accentMaterial, true);
            AddBoltLine(harvester, "Harvester Hopper Bolts", new Vector3(-0.5f, 1.16f, 1.02f), new Vector3(0.2f, 0f, 0f), 6, 0.035f);
            AddHarvesterBodyTexturePass(harvester, teamMaterial);
            AddVehicleReadabilityKit(harvester, UnitKind.Harvester, teamMaterial);
        }

        private void AddHarvesterBodyTexturePass(Transform harvester, Material teamMaterial)
        {
            CreatePrimitive(PrimitiveType.Cube, harvester, "Hopper Left White Service Plate", new Vector3(-0.42f, 1.17f, 0.54f), new Vector3(0.34f, 0.028f, 0.34f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, harvester, "Hopper Right Scuffed Service Plate", new Vector3(0.34f, 1.172f, 0.18f), new Vector3(0.32f, 0.026f, 0.28f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, harvester, "Hopper Dark Center Seam", new Vector3(0f, 1.19f, 0.5f), new Vector3(0.035f, 0.022f, 0.78f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, harvester, "Cab Roof White Armor Insert", new Vector3(-0.22f, 1.285f, -0.76f), new Vector3(0.34f, 0.03f, 0.28f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, harvester, "Cab Roof Team Stripe", new Vector3(0.26f, 1.292f, -0.74f), new Vector3(0.2f, 0.028f, 0.24f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, harvester, "Cab Brow Soot Underline", new Vector3(0f, 1.155f, -1.12f), new Vector3(0.86f, 0.045f, 0.045f), tankDeepGrimeMaterial, true);

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                string prefix = side < 0f ? "Left" : "Right";
                CreatePrimitive(PrimitiveType.Cube, harvester, prefix + " Layered Side Armor Upper", new Vector3(side * 0.72f, 0.83f, 0.28f), new Vector3(0.045f, 0.24f, 0.66f), armorHighlightMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, harvester, prefix + " Layered Side Armor Lower", new Vector3(side * 0.78f, 0.55f, 0.22f), new Vector3(0.04f, 0.18f, 0.84f), tankDustMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, harvester, prefix + " White Crew Door Insert", new Vector3(side * 0.66f, 0.82f, -0.76f), new Vector3(0.045f, 0.24f, 0.26f), whiteMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, harvester, prefix + " Cyan Crew Window Slit", new Vector3(side * 0.665f, 0.98f, -0.96f), new Vector3(0.04f, 0.07f, 0.18f), glassMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, harvester, prefix + " Track Dust Rub Strip", new Vector3(side * 0.93f, 0.2f, 0.12f), new Vector3(0.045f, 0.06f, 1.26f), tankDustMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, harvester, prefix + " Collector Lift Arm Wear Face", new Vector3(side * 0.54f, 0.4f, 1.18f), new Vector3(0.11f, 0.07f, 0.38f), tankBareMetalMaterial, true);
                AddBoltLine(harvester, prefix + " Side Armor Rivets", new Vector3(side * 0.805f, 0.7f, -0.08f), new Vector3(0f, 0f, 0.17f), 7, 0.018f);
            }

            for (int i = 0; i < 8; i++)
            {
                float x = -0.42f + (i % 4) * 0.28f;
                float z = 0.22f + (i / 4) * 0.26f;
                GameObject ore = CreatePrimitive(PrimitiveType.Cube, harvester, "Hopper Visible Ore Chunk", new Vector3(x, 1.195f + (i % 2) * 0.018f, z), new Vector3(0.055f, 0.038f, 0.052f), resourceMaterial, true);
                ore.transform.localRotation = Quaternion.Euler(8f + i * 7f, i * 23f, -6f);
            }

            BuildVent(harvester, "Cab Roof Fine Intake", new Vector3(0.38f, 1.29f, -0.46f), new Vector3(0.34f, 0.032f, 0.18f), 4);
            BuildVent(harvester, "Rear Hopper Dust Filter", new Vector3(0f, 1.13f, 1.1f), new Vector3(0.52f, 0.035f, 0.16f), 5);
            CreatePrimitive(PrimitiveType.Cube, harvester, "Collector Hazard Painted Edge", new Vector3(0f, 0.52f, 1.58f), new Vector3(0.56f, 0.045f, 0.06f), hazardMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, harvester, "Collector Black Intake Mouth", new Vector3(0f, 0.42f, 1.62f), new Vector3(0.62f, 0.12f, 0.055f), tankDeepGrimeMaterial, true);
        }

        private void BuildHarvesterTrack(Transform harvester, float side)
        {
            float x = side * 0.91f;
            CreatePrimitive(PrimitiveType.Cube, harvester, side < 0f ? "Left Track Belt" : "Right Track Belt", new Vector3(x, 0.28f, 0.05f), new Vector3(0.18f, 0.34f, 2.28f), tankDeepGrimeMaterial, true);
            for (int i = 0; i < 5; i++)
            {
                float z = -0.78f + i * 0.4f;
                GameObject wheel = CreatePrimitive(PrimitiveType.Cylinder, harvester, "Harvester Road Wheel", new Vector3(x + side * 0.035f, 0.26f, z), new Vector3(0.17f, 0.055f, 0.17f), tankBareMetalMaterial, true);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

            for (int i = 0; i < 8; i++)
            {
                float z = -1.0f + i * 0.28f;
                CreatePrimitive(PrimitiveType.Cube, harvester, "Harvester Tread Pad", new Vector3(x, 0.08f, z), new Vector3(0.24f, 0.05f, 0.15f), trimMaterial, true);
            }
        }

        private void BuildRiflemanVisual(Transform root, Material teamMaterial)
        {
            Transform soldier = CreateVisualRoot(root, "Armored Rifleman", Vector3.zero);

            CreatePrimitive(PrimitiveType.Capsule, soldier, "Combat Fatigues", new Vector3(0f, 0.82f, 0f), new Vector3(0.34f, 0.5f, 0.34f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, soldier, "Chest Armor Plate", new Vector3(0f, 1.04f, 0.14f), new Vector3(0.46f, 0.36f, 0.1f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, soldier, "Small Team IFF Plate", new Vector3(0.13f, 1.13f, 0.2f), new Vector3(0.16f, 0.08f, 0.04f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, soldier, "Back Power Pack", new Vector3(0f, 1.02f, -0.22f), new Vector3(0.38f, 0.42f, 0.12f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Sphere, soldier, "Ballistic Helmet", new Vector3(0f, 1.5f, 0.03f), new Vector3(0.34f, 0.27f, 0.34f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, soldier, "Helmet Visor", new Vector3(0f, 1.49f, 0.32f), new Vector3(0.32f, 0.08f, 0.05f), glassMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, soldier, "Helmet Brow Guard", new Vector3(0f, 1.59f, 0.18f), new Vector3(0.38f, 0.06f, 0.16f), armorHighlightMaterial, true);

            BuildInfantryArm(soldier, -1f);
            BuildInfantryArm(soldier, 1f);
            BuildInfantryLeg(soldier, -1f);
            BuildInfantryLeg(soldier, 1f);

            GameObject rifleBody = CreatePrimitive(PrimitiveType.Cube, soldier, "Assault Rifle Receiver", new Vector3(0.28f, 1.02f, 0.42f), new Vector3(0.12f, 0.1f, 0.42f), darkMaterial, true);
            rifleBody.transform.localRotation = Quaternion.Euler(0f, -8f, 0f);
            GameObject rifleBarrel = CreatePrimitive(PrimitiveType.Cylinder, soldier, "Assault Rifle Barrel", new Vector3(0.32f, 1.04f, 0.77f), new Vector3(0.035f, 0.34f, 0.035f), pipeMaterial, true);
            rifleBarrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cube, soldier, "Rifle Magazine", new Vector3(0.25f, 0.9f, 0.36f), new Vector3(0.08f, 0.18f, 0.1f), darkMaterial, true).transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cube, soldier, "Shoulder Radio", new Vector3(-0.29f, 1.22f, -0.08f), new Vector3(0.12f, 0.14f, 0.1f), tankDeepGrimeMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, soldier, "Radio Antenna", new Vector3(-0.34f, 1.48f, -0.12f), new Vector3(0.012f, 0.28f, 0.012f), darkMaterial, true);
        }

        private void BuildInfantryArm(Transform soldier, float side)
        {
            GameObject upper = CreatePrimitive(PrimitiveType.Capsule, soldier, side < 0f ? "Left Upper Arm" : "Right Upper Arm", new Vector3(side * 0.32f, 1.08f, 0.08f), new Vector3(0.11f, 0.25f, 0.11f), armorMaterial, true);
            upper.transform.localRotation = Quaternion.Euler(0f, 0f, side * -24f);
            GameObject forearm = CreatePrimitive(PrimitiveType.Capsule, soldier, side < 0f ? "Left Forearm Guard" : "Right Forearm Guard", new Vector3(side * 0.37f, 0.88f, 0.28f), new Vector3(0.095f, 0.24f, 0.095f), armorHighlightMaterial, true);
            forearm.transform.localRotation = Quaternion.Euler(56f, 0f, side * 16f);
            CreatePrimitive(PrimitiveType.Sphere, soldier, side < 0f ? "Left Glove" : "Right Glove", new Vector3(side * 0.36f, 0.78f, 0.42f), new Vector3(0.1f, 0.08f, 0.1f), darkMaterial, true);
        }

        private void BuildInfantryLeg(Transform soldier, float side)
        {
            GameObject thigh = CreatePrimitive(PrimitiveType.Capsule, soldier, side < 0f ? "Left Armored Thigh" : "Right Armored Thigh", new Vector3(side * 0.14f, 0.42f, 0f), new Vector3(0.13f, 0.3f, 0.13f), armorMaterial, true);
            thigh.transform.localRotation = Quaternion.Euler(4f, 0f, side * 6f);
            GameObject boot = CreatePrimitive(PrimitiveType.Capsule, soldier, side < 0f ? "Left Shin Guard" : "Right Shin Guard", new Vector3(side * 0.16f, 0.14f, 0.04f), new Vector3(0.12f, 0.23f, 0.12f), darkMaterial, true);
            boot.transform.localRotation = Quaternion.Euler(-6f, 0f, side * 4f);
            CreatePrimitive(PrimitiveType.Cube, soldier, side < 0f ? "Left Boot" : "Right Boot", new Vector3(side * 0.16f, 0.02f, 0.12f), new Vector3(0.18f, 0.08f, 0.28f), darkMaterial, true);
        }

        private void BuildSkyraiderVisual(Transform root, Material teamMaterial)
        {
            Transform frame = CreateVisualRoot(root, "Skyraider Airframe", new Vector3(0f, 1.35f, 0f));
            RtsHoverBob hover = frame.gameObject.AddComponent<RtsHoverBob>();
            hover.Amplitude = 0.1f;
            hover.Frequency = 1.9f;

            CreatePrimitive(PrimitiveType.Cube, frame, "Armored Nose", new Vector3(0f, 0.02f, 1.15f), new Vector3(0.85f, 0.45f, 0.95f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Fuselage", new Vector3(0f, 0f, -0.15f), new Vector3(1.15f, 0.62f, 2.25f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Cockpit Glass", new Vector3(0f, 0.32f, 0.78f), new Vector3(0.78f, 0.28f, 0.52f), glassMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Main Wing", new Vector3(0f, -0.08f, -0.08f), new Vector3(3.4f, 0.12f, 0.72f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Tail Boom", new Vector3(0f, 0.02f, -1.55f), new Vector3(0.58f, 0.42f, 1.15f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Tail Fin", new Vector3(0f, 0.48f, -2.05f), new Vector3(0.16f, 0.88f, 0.62f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Tail Stabilizer", new Vector3(0f, 0.24f, -1.95f), new Vector3(1.45f, 0.1f, 0.42f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Team Stripe", new Vector3(0f, 0.36f, -0.22f), new Vector3(0.78f, 0.055f, 0.48f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, frame, "Nose Armor Plate", new Vector3(0f, 0.31f, 1.18f), new Vector3(0.52f, 0.06f, 0.42f), armorHighlightMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Left Wing Panel", new Vector3(-0.92f, 0.03f, -0.08f), new Vector3(0.62f, 0.045f, 0.42f), armorHighlightMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Right Wing Panel", new Vector3(0.92f, 0.03f, -0.08f), new Vector3(0.62f, 0.045f, 0.42f), armorHighlightMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Left Intake Shadow", new Vector3(-0.66f, -0.05f, 0.72f), new Vector3(0.24f, 0.32f, 0.28f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Right Intake Shadow", new Vector3(0.66f, -0.05f, 0.72f), new Vector3(0.24f, 0.32f, 0.28f), armorShadowMaterial, true);

            CreatePrimitive(PrimitiveType.Cylinder, frame, "Nose Cannon", new Vector3(0f, -0.18f, 1.78f), new Vector3(0.16f, 0.46f, 0.16f), darkMaterial, true).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cube, frame, "Left Rocket Pod", new Vector3(-0.9f, -0.34f, 0.42f), new Vector3(0.32f, 0.24f, 0.86f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Right Rocket Pod", new Vector3(0.9f, -0.34f, 0.42f), new Vector3(0.32f, 0.24f, 0.86f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Nose Lamp", new Vector3(0f, -0.02f, 1.66f), new Vector3(0.26f, 0.14f, 0.08f), glowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Left Warning Light", new Vector3(-1.72f, -0.02f, -0.08f), new Vector3(0.16f, 0.08f, 0.28f), accentMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Right Warning Light", new Vector3(1.72f, -0.02f, -0.08f), new Vector3(0.16f, 0.08f, 0.28f), accentMaterial, true);

            Transform rotor = CreateVisualRoot(frame, "Skyraider Rotor", new Vector3(0f, 0.55f, -0.2f));
            RtsSpinner spinner = rotor.gameObject.AddComponent<RtsSpinner>();
            spinner.DegreesPerSecond = 980f;
            CreatePrimitive(PrimitiveType.Cylinder, rotor, "Rotor Hub", Vector3.zero, new Vector3(0.28f, 0.14f, 0.28f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, rotor, "Rotor Blade A", Vector3.zero, new Vector3(0.18f, 0.035f, 3.8f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, rotor, "Rotor Blade B", Vector3.zero, new Vector3(3.8f, 0.035f, 0.18f), darkMaterial, true);

            BuildSkyraiderDetailPass(frame);
            AddVehicleReadabilityKit(frame, UnitKind.Skyraider, teamMaterial);
        }

        private void BuildSkyraiderDetailPass(Transform frame)
        {
            BuildVent(frame, "Skyraider Engine Deck Grille", new Vector3(0f, 0.35f, -0.72f), new Vector3(0.72f, 0.04f, 0.42f), 5);
            CreatePrimitive(PrimitiveType.Cube, frame, "Left Landing Skid", new Vector3(-0.55f, -0.42f, -0.18f), new Vector3(0.12f, 0.08f, 1.72f), tankDeepGrimeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Right Landing Skid", new Vector3(0.55f, -0.42f, -0.18f), new Vector3(0.12f, 0.08f, 1.72f), tankDeepGrimeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Left Skid Strut Front", new Vector3(-0.42f, -0.25f, 0.58f), new Vector3(0.08f, 0.36f, 0.08f), pipeMaterial, true).transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            CreatePrimitive(PrimitiveType.Cube, frame, "Right Skid Strut Front", new Vector3(0.42f, -0.25f, 0.58f), new Vector3(0.08f, 0.36f, 0.08f), pipeMaterial, true).transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            CreatePrimitive(PrimitiveType.Cube, frame, "Left Skid Strut Rear", new Vector3(-0.42f, -0.25f, -0.8f), new Vector3(0.08f, 0.36f, 0.08f), pipeMaterial, true).transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            CreatePrimitive(PrimitiveType.Cube, frame, "Right Skid Strut Rear", new Vector3(0.42f, -0.25f, -0.8f), new Vector3(0.08f, 0.36f, 0.08f), pipeMaterial, true).transform.localRotation = Quaternion.Euler(0f, 0f, 18f);

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                CreatePrimitive(PrimitiveType.Cube, frame, side < 0f ? "Left Wing Weapon Rail" : "Right Wing Weapon Rail", new Vector3(side * 1.28f, -0.22f, 0.16f), new Vector3(0.12f, 0.08f, 0.94f), pipeMaterial, true);
                for (int i = 0; i < 3; i++)
                {
                    GameObject missile = CreatePrimitive(PrimitiveType.Cylinder, frame, "Skyraider Wing Missile", new Vector3(side * (1.12f + i * 0.18f), -0.32f, 0.24f), new Vector3(0.045f, 0.34f, 0.045f), tankBareMetalMaterial, true);
                    missile.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    CreatePrimitive(PrimitiveType.Cube, frame, "Missile Fin", new Vector3(side * (1.12f + i * 0.18f), -0.32f, -0.08f), new Vector3(0.12f, 0.035f, 0.06f), darkMaterial, true);
                }

                GameObject exhaust = CreatePrimitive(PrimitiveType.Cylinder, frame, side < 0f ? "Left Turbine Exhaust" : "Right Turbine Exhaust", new Vector3(side * 0.48f, 0.05f, -1.22f), new Vector3(0.16f, 0.18f, 0.16f), tankDeepGrimeMaterial, true);
                exhaust.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                CreatePrimitive(PrimitiveType.Cube, frame, side < 0f ? "Left Wing Panel Seam" : "Right Wing Panel Seam", new Vector3(side * 1.4f, 0.055f, -0.1f), new Vector3(0.04f, 0.035f, 0.66f), tankDeepGrimeMaterial, true);
            }

            CreatePrimitive(PrimitiveType.Cube, frame, "Nose Sensor Glass", new Vector3(0f, 0.14f, 1.62f), new Vector3(0.22f, 0.12f, 0.07f), glassMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Tail Heat Shield", new Vector3(0f, 0.02f, -2.18f), new Vector3(0.44f, 0.18f, 0.06f), tankBareMetalMaterial, true);
            AddBoltLine(frame, "Skyraider Cowling Bolts", new Vector3(-0.34f, 0.3f, 0.86f), new Vector3(0.17f, 0f, 0f), 5, 0.03f);
        }

        private void BuildOrcaLifterVisual(Transform root, Material teamMaterial)
        {
            Transform frame = CreateVisualRoot(root, "Orca Lifter Airframe", new Vector3(0f, 1.25f, 0f));
            RtsHoverBob hover = frame.gameObject.AddComponent<RtsHoverBob>();
            hover.Amplitude = 0.08f;
            hover.Frequency = 1.45f;

            CreatePrimitive(PrimitiveType.Cube, frame, "Forward Hull", new Vector3(0f, 0f, 0.82f), new Vector3(1.35f, 0.58f, 1.45f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Cargo Spine", new Vector3(0f, 0.05f, -0.35f), new Vector3(1.65f, 0.52f, 2.1f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Cockpit Glass", new Vector3(0f, 0.28f, 1.45f), new Vector3(0.88f, 0.28f, 0.52f), glassMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Left Wing", new Vector3(-1.55f, -0.05f, 0.12f), new Vector3(2.05f, 0.16f, 0.56f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Right Wing", new Vector3(1.55f, -0.05f, 0.12f), new Vector3(2.05f, 0.16f, 0.56f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Tail Boom", new Vector3(0f, 0.03f, -1.75f), new Vector3(0.78f, 0.45f, 1.3f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Left Tail Fin", new Vector3(-0.48f, 0.56f, -2.2f), new Vector3(0.22f, 1.05f, 0.48f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Right Tail Fin", new Vector3(0.48f, 0.56f, -2.2f), new Vector3(0.22f, 1.05f, 0.48f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Team Stripe", new Vector3(0f, 0.37f, 0.05f), new Vector3(1.08f, 0.06f, 0.58f), teamMaterial);
            CreatePrimitive(PrimitiveType.Cube, frame, "Cargo Roof Plate", new Vector3(0f, 0.36f, -0.58f), new Vector3(1.18f, 0.05f, 0.72f), armorHighlightMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Left Wing Plate", new Vector3(-1.55f, 0.05f, 0.12f), new Vector3(0.95f, 0.045f, 0.34f), armorHighlightMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Right Wing Plate", new Vector3(1.55f, 0.05f, 0.12f), new Vector3(0.95f, 0.045f, 0.34f), armorHighlightMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Left Cargo Shadow", new Vector3(-0.9f, -0.04f, -0.45f), new Vector3(0.22f, 0.32f, 0.72f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Right Cargo Shadow", new Vector3(0.9f, -0.04f, -0.45f), new Vector3(0.22f, 0.32f, 0.72f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, frame, "Left Nose Gun", new Vector3(-0.28f, -0.24f, 1.72f), new Vector3(0.11f, 0.42f, 0.11f), darkMaterial, true).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cylinder, frame, "Right Nose Gun", new Vector3(0.28f, -0.24f, 1.72f), new Vector3(0.11f, 0.42f, 0.11f), darkMaterial, true).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cube, frame, "Nose Lamp", new Vector3(0f, -0.08f, 1.58f), new Vector3(0.3f, 0.14f, 0.08f), glowMaterial, true);

            BuildDuctedFan(frame, "Left Duct Fan", new Vector3(-2.35f, 0f, 0.08f));
            BuildDuctedFan(frame, "Right Duct Fan", new Vector3(2.35f, 0f, 0.08f));
            BuildOrcaLifterDetailPass(frame);
            AddVehicleReadabilityKit(frame, UnitKind.OrcaLifter, teamMaterial);
        }

        private void BuildOrcaLifterDetailPass(Transform frame)
        {
            BuildVent(frame, "Orca Roof Intake", new Vector3(0f, 0.42f, -0.22f), new Vector3(0.92f, 0.045f, 0.5f), 5);
            BuildVent(frame, "Orca Rear Heat Vent", new Vector3(0f, 0.28f, -1.25f), new Vector3(0.76f, 0.04f, 0.42f), 4);
            CreatePrimitive(PrimitiveType.Cube, frame, "Rear Cargo Ramp", new Vector3(0f, -0.1f, -2.38f), new Vector3(0.9f, 0.14f, 0.42f), armorShadowMaterial, true).transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Cube, frame, "Underbody Cargo Rail", new Vector3(0f, -0.35f, -0.42f), new Vector3(0.34f, 0.1f, 1.65f), pipeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Magnetic Sling Clamp", new Vector3(0f, -0.52f, 0.04f), new Vector3(0.46f, 0.16f, 0.28f), accentMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Left Landing Foot", new Vector3(-0.78f, -0.46f, -0.62f), new Vector3(0.34f, 0.08f, 0.58f), tankDeepGrimeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Right Landing Foot", new Vector3(0.78f, -0.46f, -0.62f), new Vector3(0.34f, 0.08f, 0.58f), tankDeepGrimeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Left Forward Landing Foot", new Vector3(-0.72f, -0.42f, 0.82f), new Vector3(0.28f, 0.07f, 0.44f), tankDeepGrimeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, frame, "Right Forward Landing Foot", new Vector3(0.72f, -0.42f, 0.82f), new Vector3(0.28f, 0.07f, 0.44f), tankDeepGrimeMaterial, true);

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                CreatePrimitive(PrimitiveType.Cube, frame, side < 0f ? "Left Cargo Door Seam" : "Right Cargo Door Seam", new Vector3(side * 0.86f, 0.16f, -0.46f), new Vector3(0.045f, 0.32f, 0.78f), tankDeepGrimeMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, frame, side < 0f ? "Left Fan Support Pylon" : "Right Fan Support Pylon", new Vector3(side * 1.84f, -0.02f, 0.08f), new Vector3(0.62f, 0.12f, 0.2f), armorHighlightMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, frame, side < 0f ? "Left Nav Light" : "Right Nav Light", new Vector3(side * 2.46f, 0.28f, 0.72f), new Vector3(0.12f, 0.08f, 0.08f), side < 0f ? glowMaterial : accentMaterial, true);
            }

            AddBoltLine(frame, "Orca Nose Plate Bolts", new Vector3(-0.42f, 0.34f, 1.28f), new Vector3(0.21f, 0f, 0f), 5, 0.03f);
        }

        private void BuildDuctedFan(Transform parent, string name, Vector3 localPosition)
        {
            Transform fan = CreateVisualRoot(parent, name, localPosition);
            CreatePrimitive(PrimitiveType.Cylinder, fan, "Armored Duct Cowling", Vector3.zero, new Vector3(0.74f, 0.16f, 0.74f), armorHighlightMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, fan, "Dark Fan Recess", new Vector3(0f, 0.025f, 0f), new Vector3(0.56f, 0.08f, 0.56f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, fan, "Cyan Turbine Core", new Vector3(0f, 0.082f, 0f), new Vector3(0.32f, 0.025f, 0.32f), subtleGlowMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, fan, "Outer Blue Marker Ring", new Vector3(0f, 0.096f, 0f), new Vector3(0.76f, 0.018f, 0.76f), glassMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, fan, "Cowling Top Plate", new Vector3(0f, 0.116f, 0f), new Vector3(0.66f, 0.02f, 0.66f), armorMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, fan, "Fan Open Recess", new Vector3(0f, 0.132f, 0f), new Vector3(0.5f, 0.02f, 0.5f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, fan, "Duct Guard A", new Vector3(0f, 0.16f, 0f), new Vector3(0.065f, 0.035f, 1.15f), pipeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, fan, "Duct Guard B", new Vector3(0f, 0.16f, 0f), new Vector3(1.15f, 0.035f, 0.065f), pipeMaterial, true);
            GameObject diagonalGuardA = CreatePrimitive(PrimitiveType.Cube, fan, "Duct Guard Diagonal A", new Vector3(0f, 0.162f, 0f), new Vector3(0.055f, 0.03f, 1.08f), pipeMaterial, true);
            diagonalGuardA.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            GameObject diagonalGuardB = CreatePrimitive(PrimitiveType.Cube, fan, "Duct Guard Diagonal B", new Vector3(0f, 0.162f, 0f), new Vector3(0.055f, 0.03f, 1.08f), pipeMaterial, true);
            diagonalGuardB.transform.localRotation = Quaternion.Euler(0f, -45f, 0f);

            Transform blades = CreateVisualRoot(fan, "Fan Blades", new Vector3(0f, 0.18f, 0f));
            RtsSpinner spinner = blades.gameObject.AddComponent<RtsSpinner>();
            spinner.DegreesPerSecond = 1180f;
            CreatePrimitive(PrimitiveType.Cube, blades, "Blade A", Vector3.zero, new Vector3(0.12f, 0.03f, 0.88f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, blades, "Blade B", Vector3.zero, new Vector3(0.88f, 0.03f, 0.12f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Sphere, fan, "Fan Hub Cap", new Vector3(0f, 0.2f, 0f), new Vector3(0.16f, 0.08f, 0.16f), armorShadowMaterial, true);
        }

        private Transform BuildStructureVisual(Transform root, StructureKind kind, RtsTeam team)
        {
            Material teamMaterial = GetTeamMaterial(team);
            StructureStats stats = RtsBalance.GetStructure(kind);
            Transform targetHead = null;

            if (kind != StructureKind.Turret && kind != StructureKind.PowerPlant && TryBuildImportedStructureVisual(root, kind, team, teamMaterial, out targetHead))
            {
                return targetHead;
            }

            switch (kind)
            {
                case StructureKind.Refinery:
                    BuildRefineryVisual(root, teamMaterial);
                    break;
                case StructureKind.Barracks:
                    BuildBarracksVisual(root, teamMaterial);
                    break;
                case StructureKind.WarFactory:
                    BuildWarFactoryVisual(root, teamMaterial);
                    break;
                case StructureKind.PowerPlant:
                    BuildPowerPlantVisual(root, teamMaterial);
                    break;
                case StructureKind.Turret:
                    targetHead = BuildTurretVisual(root, teamMaterial);
                    break;
                case StructureKind.DualHelipad:
                    BuildDualHelipadVisual(root, teamMaterial);
                    break;
                default:
                    BuildCommandCenterVisual(root, teamMaterial, stats);
                    break;
            }

            AddStructureDetailPass(root, kind, teamMaterial);
            AddAssetStoreStructureDressing(root, kind, teamMaterial);
            return targetHead;
        }

        private bool TryBuildImportedStructureVisual(Transform root, StructureKind kind, RtsTeam team, Material teamMaterial, out Transform targetHead)
        {
            targetHead = null;
            string resourcePath;
            float scale;
            Vector3 rotation;

            switch (kind)
            {
                case StructureKind.CommandCenter:
                    resourcePath = "StructureModels/Bastion_CommunicationsCenter";
                    scale = 0.95f;
                    rotation = Vector3.zero;
                    break;
                case StructureKind.Refinery:
                    resourcePath = "StructureModels/Bastion_Refinery";
                    scale = 0.72f;
                    rotation = Vector3.zero;
                    break;
                case StructureKind.Barracks:
                    resourcePath = "StructureModels/Bastion_Barracks";
                    scale = 0.76f;
                    rotation = Vector3.zero;
                    break;
                case StructureKind.WarFactory:
                    resourcePath = UnityEngine.Resources.Load<GameObject>("StructureModels/Bastion_WarFactoryCMV2") != null
                        ? "StructureModels/Bastion_WarFactoryCMV2"
                        : "StructureModels/Bastion_WarFactory";
                    scale = resourcePath.EndsWith("CMV2") ? 0.42f : 0.72f;
                    rotation = resourcePath.EndsWith("CMV2") ? new Vector3(0f, 180f, 0f) : Vector3.zero;
                    break;
                case StructureKind.PowerPlant:
                    resourcePath = "StructureModels/Bastion_PowerPlant";
                    scale = 0.78f;
                    rotation = Vector3.zero;
                    break;
                case StructureKind.Turret:
                    resourcePath = "StructureModels/Bastion_Turret";
                    scale = 0.88f;
                    rotation = Vector3.zero;
                    break;
                default:
                    return false;
            }

            GameObject prefab = UnityEngine.Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                return false;
            }

            GameObject visual = Instantiate(prefab, root, false);
            visual.name = prefab.name + " Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(rotation);
            visual.transform.localScale = Vector3.one * scale;
            visual.AddComponent<RtsFixedMaterial>();
            ImproveImportedAssetRenderers(visual);
            bool highDetailWarFactory = resourcePath.EndsWith("CMV2");
            ApplyPolishedStructurePalette(visual, team);

            RemoveImportedColliders(visual);

            if (kind == StructureKind.WarFactory)
            {
                ApplyImportedWarFactoryTeamColor(visual, team);
            }

            if (kind == StructureKind.Turret)
            {
                targetHead = FindDeepChild(visual.transform, "WeaponYaw");
            }

            AddImportedStructureTeamMarkers(root, kind, teamMaterial, highDetailWarFactory);
            AddImportedStructureHardware(root, kind, highDetailWarFactory, teamMaterial);
            AddAssetStoreStructureDressing(root, kind, teamMaterial);
            return true;
        }

        private static void ApplyImportedWarFactoryTeamColor(GameObject visual, RtsTeam team)
        {
            if (visual == null)
            {
                return;
            }

            Color teamColor = RtsBalance.TeamColor(team);
            BastionWarFactoryCMV2.BastionTeamColor[] teamColorComponents = visual.GetComponentsInChildren<BastionWarFactoryCMV2.BastionTeamColor>(true);
            for (int i = 0; i < teamColorComponents.Length; i++)
            {
                if (teamColorComponents[i] != null)
                {
                    teamColorComponents[i].ApplyTeamColor(teamColor);
                }
            }

            if (Application.isPlaying)
            {
                visual.SendMessage("ApplyTeamColor", teamColor, SendMessageOptions.DontRequireReceiver);
            }
        }

        private void AddAssetStoreStructureDressing(Transform root, StructureKind kind, Material teamMaterial)
        {
            switch (kind)
            {
                case StructureKind.CommandCenter:
                    InstantiateAssetStorePrefab(root, "AssetStore/Structures/ItHappy_Radiostation_001", "Imported Command Radio Station Annex", new Vector3(-2.65f, 0.08f, -1.7f), new Vector3(0f, -18f, 0f), 0.52f);
                    InstantiateAssetStorePrefab(root, "AssetStore/Structures/ItHappy_Tower_003", "Imported Command Antenna Tower", new Vector3(2.35f, 0.08f, -1.55f), new Vector3(0f, 24f, 0f), 0.5f);
                    InstantiateAssetStorePrefab(root, "AssetStore/Props/ItHappy_Box_022", "Imported Command Supply Stack", new Vector3(-2.85f, 0.08f, 1.9f), new Vector3(0f, 35f, 0f), 0.34f);
                    break;
                case StructureKind.Refinery:
                    InstantiateAssetStorePrefab(root, "AssetStore/Props/ItHappy_Generator_004", "Imported Refinery Auxiliary Generator", new Vector3(2.65f, 0.08f, -1.9f), new Vector3(0f, 90f, 0f), 0.42f);
                    InstantiateAssetStorePrefab(root, "AssetStore/Props/ItHappy_Barrel_005", "Imported Refinery Chemical Drums", new Vector3(2.25f, 0.08f, 2.45f), new Vector3(0f, -14f, 0f), 0.38f);
                    InstantiateAssetStorePrefab(root, "AssetStore/Props/ItHappy_Barrier_006", "Imported Refinery Dock Barrier", new Vector3(-2.65f, 0.08f, 2.55f), new Vector3(0f, 8f, 0f), 0.44f);
                    break;
                case StructureKind.Barracks:
                    InstantiateAssetStorePrefab(root, "AssetStore/Structures/ItHappy_Tent_010", "Imported Barracks Field Tent", new Vector3(2.2f, 0.08f, -1.55f), new Vector3(0f, -28f, 0f), 0.48f);
                    InstantiateAssetStorePrefab(root, "AssetStore/Props/ItHappy_Box_022", "Imported Barracks Equipment Crates", new Vector3(-2.1f, 0.08f, 1.72f), new Vector3(0f, 55f, 0f), 0.34f);
                    InstantiateAssetStorePrefab(root, "AssetStore/Props/ItHappy_Hedgehog_001", "Imported Barracks Defensive Hedgehog", new Vector3(2.25f, 0.08f, 1.82f), new Vector3(0f, 20f, 0f), 0.36f);
                    break;
                case StructureKind.WarFactory:
                    InstantiateAssetStorePrefab(root, "AssetStore/Props/ItHappy_Generator_004", "Imported Factory Yard Generator", new Vector3(-3.4f, 0.08f, -2.35f), new Vector3(0f, -90f, 0f), 0.46f);
                    InstantiateAssetStorePrefab(root, "AssetStore/Props/ItHappy_Box_022", "Imported Factory Parts Crates", new Vector3(3.55f, 0.08f, -2.2f), new Vector3(0f, 42f, 0f), 0.38f);
                    InstantiateAssetStorePrefab(root, "AssetStore/Props/ItHappy_Barrier_006", "Imported Factory Bay Barricade Left", new Vector3(-3.05f, 0.08f, 3.15f), new Vector3(0f, 10f, 0f), 0.42f);
                    InstantiateAssetStorePrefab(root, "AssetStore/Props/ItHappy_Barrier_006", "Imported Factory Bay Barricade Right", new Vector3(3.05f, 0.08f, 3.15f), new Vector3(0f, -10f, 0f), 0.42f);
                    break;
                case StructureKind.PowerPlant:
                    InstantiateAssetStorePrefab(root, "AssetStore/Props/ItHappy_Generator_004", "Imported Power External Generator", new Vector3(2.15f, 0.08f, 1.3f), new Vector3(0f, 90f, 0f), 0.44f);
                    InstantiateAssetStorePrefab(root, "AssetStore/Props/ItHappy_Barrel_005", "Imported Power Coolant Drums", new Vector3(-2.0f, 0.08f, 1.45f), new Vector3(0f, -18f, 0f), 0.36f);
                    break;
                case StructureKind.Turret:
                    InstantiateAssetStorePrefab(root, "AssetStore/Props/ItHappy_Hedgehog_001", "Imported Turret Anti Vehicle Hedgehog", new Vector3(-1.35f, 0.08f, -1.2f), new Vector3(0f, 15f, 0f), 0.34f);
                    InstantiateAssetStorePrefab(root, "AssetStore/Props/ItHappy_Box_022", "Imported Turret Ammo Crates", new Vector3(1.32f, 0.08f, -1.18f), new Vector3(0f, -24f, 0f), 0.3f);
                    break;
                case StructureKind.DualHelipad:
                    InstantiateAssetStorePrefab(root, "AssetStore/Props/ItHappy_Generator_004", "Imported Helipad Power Cart", new Vector3(0f, 0.08f, -3.2f), new Vector3(0f, 90f, 0f), 0.38f);
                    InstantiateAssetStorePrefab(root, "AssetStore/Props/ItHappy_Barrier_006", "Imported Helipad Safety Barrier Left", new Vector3(-3.25f, 0.08f, 2.25f), new Vector3(0f, 8f, 0f), 0.4f);
                    InstantiateAssetStorePrefab(root, "AssetStore/Props/ItHappy_Barrier_006", "Imported Helipad Safety Barrier Right", new Vector3(3.25f, 0.08f, 2.25f), new Vector3(0f, -8f, 0f), 0.4f);
                    break;
            }

            CreatePrimitive(PrimitiveType.Cube, root, kind + " Imported Asset Team Ground Plate", new Vector3(0f, 0.082f, -2.65f), new Vector3(0.78f, 0.026f, 0.26f), teamMaterial, true);
        }

        private void ApplyPolishedStructurePalette(GameObject visual, RtsTeam team)
        {
            Texture2D palette = CreatePolishedStructurePalette(RtsBalance.TeamColor(team));
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    Color tint = GetImportedStructureMaterialTint(source, RtsBalance.TeamColor(team));
                    Material material = CreateRuntimeCompatibleMaterial(source, tint);
                    if (material.HasProperty("_BaseMap"))
                    {
                        material.SetTexture("_BaseMap", palette);
                    }

                    if (material.HasProperty("_MainTex"))
                    {
                        material.SetTexture("_MainTex", palette);
                    }

                    if (material.HasProperty("_BaseColor"))
                    {
                        material.SetColor("_BaseColor", tint);
                    }

                    if (material.HasProperty("_Color"))
                    {
                        material.SetColor("_Color", tint);
                    }

                    if (material.HasProperty("_Metallic"))
                    {
                        material.SetFloat("_Metallic", 0.08f);
                    }

                    if (material.HasProperty("_Smoothness"))
                    {
                        material.SetFloat("_Smoothness", 0.28f);
                    }

                    if (material.HasProperty("_Glossiness"))
                    {
                        material.SetFloat("_Glossiness", 0.28f);
                    }

                    materials[i] = material;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private Color GetImportedStructureMaterialTint(Material source, Color teamColor)
        {
            if (source == null)
            {
                return new Color(0.44f, 0.45f, 0.38f);
            }

            string materialName = source.name.ToLowerInvariant();
            if (materialName.Contains("emissive") || materialName.Contains("glow") || materialName.Contains("light") || materialName.Contains("cyan"))
            {
                return Color.white;
            }

            if (materialName.Contains("team") || materialName.Contains("blue"))
            {
                return Color.Lerp(teamColor, Color.white, 0.12f);
            }

            if (materialName.Contains("orange") || materialName.Contains("hazard") || materialName.Contains("warning"))
            {
                return new Color(0.95f, 0.58f, 0.16f);
            }

            if (materialName.Contains("white") || materialName.Contains("panel"))
            {
                return new Color(0.78f, 0.77f, 0.68f);
            }

            if (materialName.Contains("metal") || materialName.Contains("pipe") || materialName.Contains("rail"))
            {
                return new Color(0.42f, 0.42f, 0.36f);
            }

            Color sourceColor = Color.white;
            if (source.HasProperty("_BaseColor"))
            {
                sourceColor = source.GetColor("_BaseColor");
            }
            else if (source.HasProperty("_Color"))
            {
                sourceColor = source.GetColor("_Color");
            }

            float luminance = sourceColor.r * 0.2126f + sourceColor.g * 0.7152f + sourceColor.b * 0.0722f;
            if (luminance > 0.78f)
            {
                return new Color(0.74f, 0.73f, 0.64f);
            }

            if (luminance > 0.48f)
            {
                return new Color(0.48f, 0.49f, 0.41f);
            }

            if (luminance > 0.24f)
            {
                return new Color(0.31f, 0.34f, 0.25f);
            }

            return new Color(0.13f, 0.14f, 0.12f);
        }

        private Texture2D CreatePolishedStructurePalette(Color teamColor)
        {
            Color[] swatches =
            {
                new Color(0.62f, 0.63f, 0.58f),
                new Color(0.43f, 0.45f, 0.42f),
                new Color(0.16f, 0.18f, 0.18f),
                new Color(0.82f, 0.82f, 0.74f),
                teamColor,
                new Color(0.10f, 0.62f, 0.72f),
                new Color(0.92f, 0.64f, 0.14f),
                new Color(0.62f, 0.22f, 0.10f)
            };

            Texture2D texture = new Texture2D(512, 128, TextureFormat.RGBA32, true);
            texture.name = "Runtime Polished Structure Palette";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 2;

            int swatchWidth = texture.width / swatches.Length;
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    int index = Mathf.Clamp(x / swatchWidth, 0, swatches.Length - 1);
                    Color color = swatches[index];
                    float largeNoise = Mathf.PerlinNoise((x + index * 31) * 0.035f, (y + index * 47) * 0.055f);
                    float fineNoise = Mathf.PerlinNoise((x + index * 13) * 0.21f, (y + index * 17) * 0.19f);
                    float noise = (largeNoise - 0.5f) * 0.08f + (fineNoise - 0.5f) * 0.035f;
                    bool seam = x % 64 <= 1 || y % 32 <= 1;
                    bool scratch = ((x * 23 + y * 41 + index * 19) % 149) < 2;
                    bool bottomGrime = y < texture.height * 0.22f && largeNoise > 0.48f;
                    if (seam)
                    {
                        color = Color.Lerp(color, new Color(0.06f, 0.065f, 0.06f), 0.32f);
                    }
                    else if (bottomGrime)
                    {
                        color = Color.Lerp(color, new Color(0.12f, 0.11f, 0.09f), 0.25f);
                    }
                    else if (scratch)
                    {
                        color = Color.Lerp(color, Color.white, 0.12f);
                    }

                    texture.SetPixel(x, y, new Color(
                        Mathf.Clamp01(color.r + noise),
                        Mathf.Clamp01(color.g + noise),
                        Mathf.Clamp01(color.b + noise),
                        color.a));
                }
            }

            texture.Apply(true, true);
            return texture;
        }

        private Transform FindDeepChild(Transform root, string childName)
        {
            if (root.name == childName)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                Transform match = FindDeepChild(child, childName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private void AddImportedStructureTeamMarkers(Transform root, StructureKind kind, Material teamMaterial, bool highDetailWarFactory)
        {
            switch (kind)
            {
                case StructureKind.CommandCenter:
                    CreatePrimitive(PrimitiveType.Cube, root, "Command Center Team Roof Panel", new Vector3(0f, 4.7f, 0.1f), new Vector3(1.05f, 0.06f, 0.58f), teamMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Command Center Team Fascia", new Vector3(0f, 2.15f, 3.18f), new Vector3(1.45f, 0.18f, 0.055f), teamMaterial, true);
                    break;
                case StructureKind.Refinery:
                    CreatePrimitive(PrimitiveType.Cube, root, "Refinery Team Silo Plate", new Vector3(-1.8f, 2.36f, -0.54f), new Vector3(0.08f, 0.62f, 0.72f), teamMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Refinery Team Dock Plate", new Vector3(1.9f, 0.9f, 2.78f), new Vector3(0.84f, 0.18f, 0.06f), teamMaterial, true);
                    break;
                case StructureKind.Barracks:
                    CreatePrimitive(PrimitiveType.Cube, root, "Barracks Team Roof Plate", new Vector3(0f, 2.46f, -0.35f), new Vector3(1.1f, 0.055f, 0.62f), teamMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Barracks Team Door Header", new Vector3(0f, 1.34f, 2.0f), new Vector3(1.2f, 0.16f, 0.055f), teamMaterial, true);
                    break;
                case StructureKind.WarFactory:
                    if (!highDetailWarFactory)
                    {
                        CreatePrimitive(PrimitiveType.Cube, root, "Factory Team Bay Header", new Vector3(0f, 2.18f, 3.25f), new Vector3(2.2f, 0.18f, 0.06f), teamMaterial, true);
                    }

                    CreatePrimitive(PrimitiveType.Cube, root, "Factory Team Roof Signal Plate", new Vector3(0f, 2.68f, 0.2f), new Vector3(highDetailWarFactory ? 1.1f : 1.5f, 0.055f, 0.55f), teamMaterial, true);
                    break;
                case StructureKind.PowerPlant:
                    CreatePrimitive(PrimitiveType.Cube, root, "Power Plant Team Cowling", new Vector3(0f, 2.22f, -0.35f), new Vector3(0.9f, 0.08f, 0.48f), teamMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Power Plant Team Service Door", new Vector3(1.74f, 0.9f, 0.45f), new Vector3(0.055f, 0.58f, 0.48f), teamMaterial, true);
                    break;
                case StructureKind.Turret:
                    CreatePrimitive(PrimitiveType.Cube, root, "Turret Team Armor Plate", new Vector3(0f, 0.78f, 1.08f), new Vector3(0.72f, 0.16f, 0.055f), teamMaterial, true);
                    break;
            }
        }

        private void AddImportedStructureHardware(Transform root, StructureKind kind, bool highDetailWarFactory, Material teamMaterial)
        {
            AddIsometricStructureBasePlate(root, kind, highDetailWarFactory);
            AddImportedStructurePanelization(root, kind, highDetailWarFactory, teamMaterial);
            AddImportedStructureWeathering(root, kind, highDetailWarFactory, teamMaterial);
            AddIsometricTextureDressing(root, kind, highDetailWarFactory, teamMaterial);
            AddImportedSimpleStructureProfessionalFinish(root, kind, highDetailWarFactory, teamMaterial);
            AddStructureReadabilityKit(root, kind, highDetailWarFactory, teamMaterial);
            AddStructureFinalPolishPass(root, kind, highDetailWarFactory, teamMaterial);

            switch (kind)
            {
                case StructureKind.CommandCenter:
                    BuildVent(root, "Imported Command Roof Heat Exchanger", new Vector3(-1.25f, 4.52f, -0.75f), new Vector3(0.82f, 0.04f, 0.48f), 6);
                    BuildVent(root, "Imported Command Front Intake", new Vector3(0f, 1.62f, 3.24f), new Vector3(1.18f, 0.05f, 0.42f), 7);
                    CreatePipe(root, "Imported Command Side Cable A", new Vector3(2.95f, 1.35f, -0.25f), new Vector3(0.07f, 1.7f, 0.07f), new Vector3(0f, 0f, 90f));
                    CreatePipe(root, "Imported Command Side Cable B", new Vector3(2.95f, 1.1f, 0.55f), new Vector3(0.06f, 1.35f, 0.06f), new Vector3(0f, 0f, 90f));
                    BuildAntennaCluster(root, new Vector3(-2.05f, 5.0f, -1.55f));
                    break;
                case StructureKind.Refinery:
                    BuildVent(root, "Imported Refinery Silo Service Grille", new Vector3(-2.15f, 2.02f, -0.52f), new Vector3(0.05f, 0.62f, 0.7f), 5);
                    CreatePipe(root, "Imported Refinery Transfer Pipe Left", new Vector3(0.25f, 0.58f, 2.35f), new Vector3(0.08f, 1.65f, 0.08f), new Vector3(0f, 0f, 90f));
                    CreatePipe(root, "Imported Refinery Transfer Pipe Right", new Vector3(1.55f, 0.58f, 2.35f), new Vector3(0.08f, 1.25f, 0.08f), new Vector3(0f, 0f, 90f));
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Refinery Pump Block", new Vector3(2.2f, 0.48f, 1.72f), new Vector3(0.72f, 0.26f, 0.3f), pipeMaterial, true);
                    BuildCatwalkRail(root, "Imported Refinery Silo Rail", new Vector3(-1.52f, 2.8f, -0.15f), 1.25f);
                    break;
                case StructureKind.Barracks:
                    BuildVent(root, "Imported Barracks Roof HVAC Grille", new Vector3(1.08f, 2.42f, -0.75f), new Vector3(0.72f, 0.04f, 0.44f), 5);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Barracks Door Frame", new Vector3(0f, 0.92f, 2.02f), new Vector3(1.1f, 0.76f, 0.05f), darkMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Barracks Side Locker", new Vector3(-1.78f, 0.46f, 1.62f), new Vector3(0.34f, 0.42f, 0.42f), trimMaterial, true);
                    AddBoltLine(root, "Imported Barracks Awning Bolts", new Vector3(-0.58f, 1.34f, 2.06f), new Vector3(0.19f, 0f, 0f), 7, 0.026f);
                    break;
                case StructureKind.WarFactory:
                    if (!highDetailWarFactory)
                    {
                        BuildVent(root, "Imported Factory Roof Intake", new Vector3(0f, 2.86f, -0.62f), new Vector3(1.4f, 0.05f, 0.62f), 8);
                        BuildRobotArm(root, "Imported Factory Service Arm Left", new Vector3(-1.2f, 0.42f, 1.45f), -1f);
                        BuildRobotArm(root, "Imported Factory Service Arm Right", new Vector3(1.2f, 0.42f, 1.45f), 1f);
                    }

                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Factory Hazard Strip Left", new Vector3(-2.25f, 0.12f, 3.55f), new Vector3(0.42f, 0.035f, 1.1f), hazardMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Factory Hazard Strip Right", new Vector3(2.25f, 0.12f, 3.55f), new Vector3(0.42f, 0.035f, 1.1f), hazardMaterial, true);
                    break;
                case StructureKind.PowerPlant:
                    BuildVent(root, "Imported Power Turbine Exhaust Grille", new Vector3(0f, 2.58f, -0.55f), new Vector3(0.92f, 0.045f, 0.52f), 6);
                    CreatePipe(root, "Imported Power Cable Run A", new Vector3(-1.15f, 0.38f, 1.45f), new Vector3(0.07f, 1.25f, 0.07f), new Vector3(0f, 0f, 90f));
                    CreatePipe(root, "Imported Power Cable Run B", new Vector3(0.15f, 0.38f, 1.45f), new Vector3(0.07f, 1.25f, 0.07f), new Vector3(0f, 0f, 90f));
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Power Fuse Box", new Vector3(1.74f, 1.25f, -0.72f), new Vector3(0.06f, 0.32f, 0.34f), accentMaterial, true);
                    AddSiloBand(root, "Imported Power Main Turbine Team Collar", new Vector3(-0.18f, 2.36f, -0.42f), 0.78f);
                    AddSiloBand(root, "Imported Power Main Turbine Soot Collar", new Vector3(-0.18f, 1.82f, -0.42f), 0.8f);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Power Turbine White Cap Plate", new Vector3(-0.36f, 2.9f, -0.42f), new Vector3(0.42f, 0.035f, 0.3f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Power Turbine Dirty Cap Plate", new Vector3(0.2f, 2.905f, -0.22f), new Vector3(0.36f, 0.035f, 0.28f), tankDustMaterial, true);
                    BuildVent(root, "Imported Power Turbine Cap Louvers", new Vector3(-0.12f, 2.93f, -0.72f), new Vector3(0.52f, 0.035f, 0.22f), 5);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Power Raised Turbine Service Module", new Vector3(-0.24f, 3.18f, -0.38f), new Vector3(0.52f, 0.18f, 0.36f), trimMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Power Raised Turbine White Hatch", new Vector3(-0.24f, 3.29f, -0.38f), new Vector3(0.38f, 0.035f, 0.24f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Power Raised Turbine Cyan Gauge", new Vector3(0.08f, 3.18f, -0.16f), new Vector3(0.14f, 0.07f, 0.045f), glassMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Power Front Transformer Coil C", new Vector3(-1.28f, 0.74f, 2.18f), new Vector3(0.26f, 0.42f, 0.08f), pipeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Power Front Transformer Coil D", new Vector3(-0.92f, 0.74f, 2.18f), new Vector3(0.26f, 0.42f, 0.08f), pipeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Power Cyan Load Meter", new Vector3(1.78f, 1.08f, 0.12f), new Vector3(0.045f, 0.18f, 0.28f), glassMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Power Amber Breaker Handles", new Vector3(1.79f, 0.82f, 0.54f), new Vector3(0.04f, 0.18f, 0.32f), accentMaterial, true);
                    break;
                case StructureKind.Turret:
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Ammo Locker Left", new Vector3(-0.92f, 0.38f, -0.62f), new Vector3(0.36f, 0.28f, 0.44f), trimMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Ammo Locker Right", new Vector3(0.92f, 0.38f, -0.62f), new Vector3(0.36f, 0.28f, 0.44f), trimMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Armored Mantlet Block", new Vector3(0f, 1.02f, 0.95f), new Vector3(0.86f, 0.34f, 0.22f), armorShadowMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret White Mantlet Face", new Vector3(0f, 1.05f, 1.08f), new Vector3(0.66f, 0.2f, 0.055f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Team IFF Lens Bar", new Vector3(0f, 1.22f, 1.115f), new Vector3(0.52f, 0.055f, 0.04f), teamMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Top Service Slab", new Vector3(0f, 1.42f, -0.08f), new Vector3(0.96f, 0.06f, 0.7f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Top White Hatch", new Vector3(-0.26f, 1.485f, -0.18f), new Vector3(0.32f, 0.04f, 0.24f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Top Dark Hatch", new Vector3(0.24f, 1.49f, -0.08f), new Vector3(0.26f, 0.04f, 0.22f), armorShadowMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Roof White Armor Plate A", new Vector3(-0.34f, 1.78f, -0.18f), new Vector3(0.42f, 0.035f, 0.46f), whiteMaterial, true).transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Roof Dirty Armor Plate B", new Vector3(0.24f, 1.78f, 0.08f), new Vector3(0.38f, 0.035f, 0.4f), tankDustMaterial, true).transform.localRotation = Quaternion.Euler(0f, 0f, 12f);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Roof Dark Center Ridge", new Vector3(0f, 1.84f, -0.02f), new Vector3(0.08f, 0.045f, 0.78f), armorShadowMaterial, true);
                    BuildVent(root, "Imported Turret Roof Exhaust Louver", new Vector3(0.34f, 1.83f, -0.34f), new Vector3(0.38f, 0.035f, 0.24f), 4);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Roof Cyan Service Light", new Vector3(-0.34f, 1.84f, 0.28f), new Vector3(0.18f, 0.04f, 0.055f), glowMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Roof Amber Service Light", new Vector3(0.3f, 1.84f, 0.32f), new Vector3(0.16f, 0.04f, 0.055f), accentMaterial, true);
                    AddBoltLine(root, "Imported Turret Roof Plate Rivets", new Vector3(-0.42f, 1.83f, 0.12f), new Vector3(0.16f, 0f, 0f), 6, 0.016f);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Raised Roof Sensor Box", new Vector3(-0.24f, 2.04f, -0.08f), new Vector3(0.42f, 0.2f, 0.34f), trimMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Raised Roof White Access Plate", new Vector3(-0.24f, 2.16f, -0.08f), new Vector3(0.32f, 0.035f, 0.24f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Raised Roof Radar Fin", new Vector3(0.26f, 2.02f, -0.2f), new Vector3(0.08f, 0.28f, 0.34f), armorShadowMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Left Wall Replacement Panel", new Vector3(-0.62f, 1.42f, 0.0f), new Vector3(0.045f, 0.36f, 0.5f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Left Wall White Service Panel", new Vector3(-0.64f, 1.35f, -0.42f), new Vector3(0.045f, 0.28f, 0.32f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Front Upper Dirty Plate", new Vector3(0.32f, 1.5f, 0.72f), new Vector3(0.34f, 0.24f, 0.05f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Front Upper White Plate", new Vector3(-0.32f, 1.5f, 0.72f), new Vector3(0.32f, 0.22f, 0.05f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Left Cheek Armor Stack", new Vector3(-0.72f, 1.08f, 0.24f), new Vector3(0.055f, 0.38f, 0.56f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Right Cheek Dirty Stack", new Vector3(0.72f, 1.08f, 0.2f), new Vector3(0.055f, 0.34f, 0.52f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Cyan Rangefinder Pod", new Vector3(0.45f, 1.18f, 0.78f), new Vector3(0.24f, 0.1f, 0.08f), glassMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Amber Armed Beacon", new Vector3(-0.43f, 1.2f, 0.78f), new Vector3(0.1f, 0.1f, 0.065f), accentMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Barrel Top Heat Shield", new Vector3(0f, 1.11f, 1.42f), new Vector3(0.32f, 0.08f, 0.72f), tankBareMetalMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Barrel Lower Recoil Guard", new Vector3(0f, 0.94f, 1.36f), new Vector3(0.28f, 0.06f, 0.62f), armorShadowMaterial, true);
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Imported Turret Forward Cooling Collar", new Vector3(0f, 1.0f, 1.82f), new Vector3(0.18f, 0.052f, 0.18f), tankBareMetalMaterial, true).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Imported Turret Rear Cooling Collar", new Vector3(0f, 1.0f, 1.3f), new Vector3(0.2f, 0.055f, 0.2f), pipeMaterial, true).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Base Front Replacement Plate", new Vector3(0f, 0.62f, 0.98f), new Vector3(0.98f, 0.16f, 0.055f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Base Left Service Box", new Vector3(-1.08f, 0.42f, 0.28f), new Vector3(0.3f, 0.3f, 0.38f), pipeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Base Right Power Box", new Vector3(1.08f, 0.42f, 0.28f), new Vector3(0.3f, 0.32f, 0.38f), pipeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Rear Shell Crate Stack", new Vector3(0.02f, 0.32f, -1.12f), new Vector3(0.62f, 0.28f, 0.36f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Imported Turret Rear Crate Team Strap", new Vector3(0.02f, 0.49f, -1.12f), new Vector3(0.5f, 0.035f, 0.055f), teamMaterial, true);
                    CreatePipe(root, "Imported Turret Ground Power Cable", new Vector3(-0.48f, 0.24f, 1.08f), new Vector3(0.045f, 0.9f, 0.045f), new Vector3(0f, 0f, 90f));
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Imported Turret Short Radio Mast A", new Vector3(0.34f, 1.76f, -0.32f), new Vector3(0.018f, 0.36f, 0.018f), darkMaterial, true);
                    CreatePrimitive(PrimitiveType.Sphere, root, "Imported Turret Mast Amber Tip", new Vector3(0.34f, 2.14f, -0.32f), new Vector3(0.055f, 0.055f, 0.055f), accentMaterial, true);
                    AddBoltLine(root, "Imported Turret Mantlet Bolt Row", new Vector3(-0.28f, 1.16f, 1.13f), new Vector3(0.14f, 0f, 0f), 5, 0.018f);
                    AddBoltLine(root, "Imported Turret Ring Bolts", new Vector3(-0.52f, 0.82f, 0.68f), new Vector3(0.17f, 0f, 0f), 7, 0.026f);
                    break;
            }
        }

        private void AddStructureReadabilityKit(Transform root, StructureKind kind, bool highDetailWarFactory, Material teamMaterial)
        {
            if (kind == StructureKind.Turret)
            {
                Transform turretKit = CreateVisualRoot(root, "Turret Readable Finish Kit", Vector3.zero);
                turretKit.gameObject.AddComponent<RtsFixedMaterial>();
                CreatePrimitive(PrimitiveType.Cube, turretKit, "Turret Readable Mantlet White Face", new Vector3(0f, 1.08f, 1.1f), new Vector3(0.68f, 0.16f, 0.045f), whiteMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, turretKit, "Turret Readable Team IFF Bar", new Vector3(0f, 1.24f, 1.12f), new Vector3(0.48f, 0.055f, 0.04f), teamMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, turretKit, "Turret Readable Barrel Heat Shield", new Vector3(0f, 1.06f, 1.45f), new Vector3(0.3f, 0.06f, 0.7f), tankBareMetalMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, turretKit, "Turret Readable Base Front Armor", new Vector3(0f, 0.62f, 1.02f), new Vector3(0.92f, 0.14f, 0.05f), tankDustMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, turretKit, "Turret Readable Base Team Ring Segment", new Vector3(0f, 0.84f, 1.04f), new Vector3(0.78f, 0.055f, 0.045f), teamMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, turretKit, "Turret Readable Cyan Optic", new Vector3(-0.28f, 1.02f, 1.13f), new Vector3(0.12f, 0.06f, 0.035f), glowMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, turretKit, "Turret Readable Amber Armed Light", new Vector3(0.32f, 1.02f, 1.13f), new Vector3(0.09f, 0.07f, 0.035f), accentMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, turretKit, "Turret Readable Left Base Cabinet", new Vector3(-1.04f, 0.38f, 0.16f), new Vector3(0.28f, 0.28f, 0.34f), trimMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, turretKit, "Turret Readable Right Base Cabinet", new Vector3(1.04f, 0.38f, 0.16f), new Vector3(0.28f, 0.28f, 0.34f), trimMaterial, true);
                AddBoltLine(turretKit, "Turret Readable Mantlet Bolts", new Vector3(-0.28f, 1.17f, 1.14f), new Vector3(0.14f, 0f, 0f), 5, 0.018f);
                return;
            }

            if (!TryGetStructureDressingSpec(kind, highDetailWarFactory, out Vector2 footprint, out float roofY, out float wallMidY, out float frontZ, out float sideX))
            {
                return;
            }

            Transform kit = CreateVisualRoot(root, kind + " Readable Finish Kit", Vector3.zero);
            kit.gameObject.AddComponent<RtsFixedMaterial>();

            float facadeWidth = Mathf.Min(footprint.x * 0.72f, sideX * 1.75f);
            float roofWidth = Mathf.Max(1.4f, footprint.x * 0.36f);
            float roofDepth = Mathf.Max(1.0f, footprint.y * 0.24f);

            CreatePrimitive(PrimitiveType.Cube, kit, kind + " Readable Front White Armor Plate", new Vector3(-facadeWidth * 0.22f, wallMidY + 0.36f, frontZ + 0.07f), new Vector3(facadeWidth * 0.34f, 0.22f, 0.05f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, kind + " Readable Front Weathered Insert", new Vector3(facadeWidth * 0.2f, wallMidY + 0.28f, frontZ + 0.075f), new Vector3(facadeWidth * 0.32f, 0.16f, 0.045f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, kind + " Readable Front Team Service Bar", new Vector3(0f, wallMidY + 0.62f, frontZ + 0.082f), new Vector3(facadeWidth * 0.48f, 0.07f, 0.045f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, kind + " Readable Lower Soot Kick Plate", new Vector3(0f, Mathf.Max(0.42f, wallMidY - 0.62f), frontZ + 0.076f), new Vector3(facadeWidth * 0.82f, 0.085f, 0.045f), tankDeepGrimeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, kind + " Readable Cyan Status Strip", new Vector3(-facadeWidth * 0.34f, wallMidY + 0.05f, frontZ + 0.09f), new Vector3(0.34f, 0.07f, 0.04f), glowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, kind + " Readable Amber Warning Block", new Vector3(facadeWidth * 0.34f, wallMidY + 0.03f, frontZ + 0.09f), new Vector3(0.18f, 0.08f, 0.04f), accentMaterial, true);

            CreatePrimitive(PrimitiveType.Cube, kit, kind + " Readable Roof Dirty Service Panel", new Vector3(-roofWidth * 0.18f, roofY + 0.07f, -roofDepth * 0.12f), new Vector3(roofWidth * 0.42f, 0.035f, roofDepth * 0.42f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, kind + " Readable Roof White Service Panel", new Vector3(roofWidth * 0.25f, roofY + 0.075f, roofDepth * 0.14f), new Vector3(roofWidth * 0.38f, 0.035f, roofDepth * 0.34f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, kind + " Readable Roof Dark Center Seam", new Vector3(0f, roofY + 0.082f, 0f), new Vector3(0.045f, 0.028f, roofDepth * 1.12f), armorShadowMaterial, true);
            BuildVent(kit, kind + " Readable Roof Heat Grille", new Vector3(-roofWidth * 0.34f, roofY + 0.09f, roofDepth * 0.3f), new Vector3(0.68f, 0.035f, 0.32f), 5);

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                string prefix = side < 0f ? "Left" : "Right";
                CreatePrimitive(PrimitiveType.Cube, kit, kind + " Readable " + prefix + " Side Replacement Plate", new Vector3(side * (sideX + 0.045f), wallMidY + 0.18f, 0.25f), new Vector3(0.045f, 0.4f, footprint.y * 0.18f), side < 0f ? whiteMaterial : tankDustMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, kind + " Readable " + prefix + " Side Dark Cable Run", new Vector3(side * (sideX + 0.055f), wallMidY - 0.34f, -footprint.y * 0.16f), new Vector3(0.04f, 0.08f, footprint.y * 0.34f), tankDeepGrimeMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, kind + " Readable " + prefix + " Side Team Tab", new Vector3(side * (sideX + 0.06f), wallMidY + 0.62f, -footprint.y * 0.04f), new Vector3(0.035f, 0.16f, 0.42f), teamMaterial, true);
            }

            AddBoltLine(kit, kind + " Readable Facade Bolt Row", new Vector3(-facadeWidth * 0.38f, wallMidY + 0.75f, frontZ + 0.095f), new Vector3(facadeWidth * 0.095f, 0f, 0f), 9, 0.02f);

            switch (kind)
            {
                case StructureKind.CommandCenter:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Command Readable Roof Comms Crate", new Vector3(1.35f, roofY + 0.18f, 0.55f), new Vector3(0.44f, 0.22f, 0.34f), trimMaterial, true);
                    CreatePipe(kit, "Command Readable Roof Cable Bundle", new Vector3(0.72f, roofY + 0.11f, 0.32f), new Vector3(0.045f, 1.05f, 0.045f), new Vector3(0f, 0f, 90f));
                    CreatePrimitive(PrimitiveType.Cube, kit, "Command Readable Tall Window Band", new Vector3(0f, wallMidY + 0.12f, frontZ + 0.1f), new Vector3(1.32f, 0.16f, 0.04f), glassMaterial, true);
                    break;
                case StructureKind.Refinery:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Refinery Readable Ore Glow Window", new Vector3(-1.25f, 0.9f, frontZ + 0.1f), new Vector3(0.52f, 0.13f, 0.045f), resourceMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Refinery Readable Pump Warning Stripe", new Vector3(1.75f, 0.62f, frontZ - 0.42f), new Vector3(0.58f, 0.055f, 0.12f), hazardMaterial, true);
                    CreatePipe(kit, "Refinery Readable Transfer Pipe", new Vector3(0.4f, 0.62f, frontZ - 0.62f), new Vector3(0.075f, 1.8f, 0.075f), new Vector3(0f, 0f, 90f));
                    break;
                case StructureKind.Barracks:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Barracks Readable Door Dark Recess", new Vector3(0f, 0.74f, frontZ + 0.1f), new Vector3(1.02f, 0.52f, 0.045f), darkMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Barracks Readable Awning White Lip", new Vector3(0f, 1.18f, frontZ + 0.14f), new Vector3(1.4f, 0.08f, 0.24f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Barracks Readable Crate Stack", new Vector3(-sideX + 0.34f, 0.36f, frontZ - 0.56f), new Vector3(0.42f, 0.32f, 0.34f), tankDustMaterial, true);
                    break;
                case StructureKind.WarFactory:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Factory Readable Bay Dark Interior", new Vector3(0f, 0.9f, frontZ + 0.12f), new Vector3(2.5f, 0.9f, 0.055f), darkMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Factory Readable Bay Cyan Work Light", new Vector3(0f, 1.42f, frontZ + 0.16f), new Vector3(1.28f, 0.075f, 0.04f), glowMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Factory Readable Ramp Hazard Left", new Vector3(-1.2f, 0.16f, frontZ + 0.84f), new Vector3(0.38f, 0.04f, 0.64f), hazardMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Factory Readable Ramp Hazard Right", new Vector3(1.2f, 0.16f, frontZ + 0.84f), new Vector3(0.38f, 0.04f, 0.64f), hazardMaterial, true);
                    BuildVent(kit, "Factory Readable Roof Twin Intake", new Vector3(0.72f, roofY + 0.1f, -0.42f), new Vector3(0.76f, 0.035f, 0.34f), 6);
                    break;
                case StructureKind.PowerPlant:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Power Readable Transformer Coil Left", new Vector3(-1.05f, 0.72f, frontZ + 0.05f), new Vector3(0.24f, 0.42f, 0.05f), pipeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Power Readable Transformer Coil Right", new Vector3(-0.72f, 0.72f, frontZ + 0.05f), new Vector3(0.24f, 0.42f, 0.05f), pipeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Power Readable Orange Core Strip", new Vector3(1.1f, wallMidY + 0.12f, frontZ + 0.08f), new Vector3(0.12f, 0.52f, 0.04f), accentMaterial, true);
                    CreatePipe(kit, "Power Readable Ground Cable", new Vector3(0.3f, 0.35f, frontZ - 0.34f), new Vector3(0.055f, 1.4f, 0.055f), new Vector3(0f, 0f, 90f));
                    break;
                case StructureKind.DualHelipad:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Helipad Readable Control Booth Window", new Vector3(0f, 1.08f, frontZ + 0.1f), new Vector3(0.78f, 0.16f, 0.045f), glassMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Helipad Readable Fuel Hose Rack", new Vector3(-sideX + 0.54f, 0.46f, -1.65f), new Vector3(0.62f, 0.12f, 0.28f), pipeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Helipad Readable Pad Warning Block", new Vector3(sideX - 0.72f, 0.32f, frontZ - 0.36f), new Vector3(0.46f, 0.045f, 0.38f), hazardMaterial, true);
                    break;
            }
        }

        private void AddStructureFinalPolishPass(Transform root, StructureKind kind, bool highDetailWarFactory, Material teamMaterial)
        {
            if (kind == StructureKind.Turret)
            {
                AddTurretFinalPolishPass(root, teamMaterial);
                return;
            }

            if (!TryGetStructureDressingSpec(kind, highDetailWarFactory, out Vector2 footprint, out float roofY, out float wallMidY, out float frontZ, out float sideX))
            {
                return;
            }

            Transform kit = CreateVisualRoot(root, kind + " Final Texture Polish Kit", Vector3.zero);
            kit.gameObject.AddComponent<RtsFixedMaterial>();

            float roofWidth = Mathf.Max(1.8f, footprint.x * 0.48f);
            float roofDepth = Mathf.Max(1.2f, footprint.y * 0.34f);
            float facadeWidth = Mathf.Min(footprint.x * 0.76f, sideX * 1.92f);
            Material[] roofMaterials =
            {
                whiteMaterial,
                tankDustMaterial,
                trimMaterial,
                tankBareMetalMaterial,
                armorShadowMaterial
            };

            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 6; column++)
                {
                    if ((column + row * 2) % 4 == 3)
                    {
                        continue;
                    }

                    float x = Mathf.Lerp(-roofWidth * 0.44f, roofWidth * 0.44f, (column + 0.5f) / 6f);
                    float z = Mathf.Lerp(-roofDepth * 0.42f, roofDepth * 0.42f, (row + 0.5f) / 3f);
                    float patchWidth = roofWidth / 6f * (0.34f + ((column + row) % 3) * 0.08f);
                    float patchDepth = roofDepth / 3f * (0.22f + ((column * 2 + row) % 3) * 0.06f);
                    Material material = roofMaterials[(column + row * 3) % roofMaterials.Length];
                    GameObject patch = CreatePrimitive(PrimitiveType.Cube, kit, kind + " Final Roof Paint Breakup", new Vector3(x, roofY + 0.105f, z), new Vector3(patchWidth, 0.024f, patchDepth), material, true);
                    patch.transform.localRotation = Quaternion.Euler(0f, ((column - row) % 3) * 2f, 0f);
                }
            }

            for (int i = 0; i < 7; i++)
            {
                float t = i / 6f;
                float x = Mathf.Lerp(-facadeWidth * 0.42f, facadeWidth * 0.42f, t);
                Material material = i % 3 == 0 ? whiteMaterial : (i % 3 == 1 ? tankDustMaterial : trimMaterial);
                float height = 0.14f + (i % 2) * 0.08f;
                CreatePrimitive(PrimitiveType.Cube, kit, kind + " Final Facade Varied Armor Chip", new Vector3(x, wallMidY + 0.22f + (i % 3) * 0.18f, frontZ + 0.115f), new Vector3(facadeWidth * 0.075f, height, 0.038f), material, true);
            }

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                string prefix = side < 0f ? "Left" : "Right";
                CreatePrimitive(PrimitiveType.Cube, kit, kind + " Final " + prefix + " Sooted Service Rail", new Vector3(side * (sideX + 0.075f), wallMidY + 0.18f, -footprint.y * 0.16f), new Vector3(0.035f, 0.06f, footprint.y * 0.44f), tankDeepGrimeMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, kind + " Final " + prefix + " Team ID Tab", new Vector3(side * (sideX + 0.08f), wallMidY + 0.62f, footprint.y * 0.12f), new Vector3(0.032f, 0.14f, 0.36f), teamMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, kind + " Final " + prefix + " White Replacement Patch", new Vector3(side * (sideX + 0.082f), wallMidY - 0.16f, footprint.y * 0.02f), new Vector3(0.028f, 0.24f, 0.48f), side < 0f ? whiteMaterial : tankDustMaterial, true);
                AddBoltLine(kit, kind + " Final " + prefix + " Side Micro Rivets", new Vector3(side * (sideX + 0.095f), wallMidY + 0.42f, -footprint.y * 0.18f), new Vector3(0f, 0f, 0.14f), 6, 0.014f);
            }

            CreatePrimitive(PrimitiveType.Cube, kit, kind + " Final Front Small Serial Plate", new Vector3(-facadeWidth * 0.34f, wallMidY + 0.72f, frontZ + 0.122f), new Vector3(0.34f, 0.08f, 0.035f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, kind + " Final Front Team Micro Plate", new Vector3(-facadeWidth * 0.08f, wallMidY + 0.72f, frontZ + 0.124f), new Vector3(0.22f, 0.07f, 0.034f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, kind + " Final Front Amber Maintenance Tag", new Vector3(facadeWidth * 0.34f, wallMidY + 0.56f, frontZ + 0.124f), new Vector3(0.16f, 0.08f, 0.035f), accentMaterial, true);
            BuildVent(kit, kind + " Final Micro Vent Cluster", new Vector3(facadeWidth * 0.18f, wallMidY + 0.72f, frontZ + 0.128f), new Vector3(0.58f, 0.035f, 0.22f), 5);
            CreatePipe(kit, kind + " Final Roof Cable Tray", new Vector3(roofWidth * 0.28f, roofY + 0.15f, -roofDepth * 0.08f), new Vector3(0.04f, roofDepth * 0.56f, 0.04f), new Vector3(90f, 0f, 0f));

            switch (kind)
            {
                case StructureKind.CommandCenter:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Command Final Dish Calibration Box", new Vector3(1.24f, roofY + 0.26f, 0.74f), new Vector3(0.38f, 0.18f, 0.28f), pipeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Command Final Tall Cyan Window Slit", new Vector3(0.72f, wallMidY + 1.08f, frontZ + 0.13f), new Vector3(0.34f, 0.16f, 0.035f), glassMaterial, true);
                    BuildAntennaCluster(kit, new Vector3(-1.82f, roofY + 0.26f, -1.2f));
                    break;
                case StructureKind.Refinery:
                    CreatePipe(kit, "Refinery Final Low Pipe Rack A", new Vector3(1.1f, 0.52f, frontZ - 0.48f), new Vector3(0.06f, 1.5f, 0.06f), new Vector3(0f, 0f, 90f));
                    CreatePipe(kit, "Refinery Final Low Pipe Rack B", new Vector3(1.1f, 0.68f, frontZ - 0.58f), new Vector3(0.045f, 1.34f, 0.045f), new Vector3(0f, 0f, 90f));
                    AddBoltLine(kit, "Refinery Final Silo Ladder Rungs", new Vector3(-2.52f, 0.92f, -0.62f), new Vector3(0f, 0.16f, 0f), 7, 0.014f);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Refinery Final Ore Spill", new Vector3(2.45f, 0.085f, frontZ - 0.78f), new Vector3(0.44f, 0.032f, 0.28f), resourceMaterial, true);
                    break;
                case StructureKind.Barracks:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Barracks Final Entrance Step Wear", new Vector3(0f, 0.12f, frontZ + 0.28f), new Vector3(1.35f, 0.035f, 0.32f), tankBareMetalMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Barracks Final Roof Supply Crate", new Vector3(-0.92f, roofY + 0.24f, 0.72f), new Vector3(0.34f, 0.24f, 0.28f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Barracks Final Door Cyan Reader", new Vector3(0.58f, 1.06f, frontZ + 0.14f), new Vector3(0.14f, 0.12f, 0.035f), glowMaterial, true);
                    break;
                case StructureKind.WarFactory:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Factory Final Bay Cross Beam", new Vector3(0f, wallMidY + 0.14f, frontZ + 0.15f), new Vector3(2.6f, 0.085f, 0.035f), tankBareMetalMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Factory Final Conveyor Center Dirt Track", new Vector3(0f, 0.17f, frontZ + 0.72f), new Vector3(0.64f, 0.035f, 1.12f), tankDeepGrimeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Factory Final Bay Left Cyan Lamp", new Vector3(-1.16f, wallMidY + 0.38f, frontZ + 0.16f), new Vector3(0.28f, 0.07f, 0.034f), glowMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Factory Final Bay Right Amber Lamp", new Vector3(1.16f, wallMidY + 0.38f, frontZ + 0.16f), new Vector3(0.22f, 0.07f, 0.034f), accentMaterial, true);
                    BuildVent(kit, "Factory Final Roof Large Service Intake", new Vector3(-0.94f, roofY + 0.14f, -0.76f), new Vector3(0.86f, 0.035f, 0.34f), 6);
                    break;
                case StructureKind.PowerPlant:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Power Final Transformer Warning Face", new Vector3(-1.08f, 0.82f, frontZ + 0.13f), new Vector3(0.52f, 0.16f, 0.035f), hazardMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Power Final Orange Heat Window", new Vector3(1.12f, wallMidY + 0.32f, frontZ + 0.13f), new Vector3(0.13f, 0.48f, 0.035f), accentMaterial, true);
                    CreatePipe(kit, "Power Final Ground Cable Coil", new Vector3(-0.44f, 0.22f, frontZ - 0.52f), new Vector3(0.045f, 1.24f, 0.045f), new Vector3(0f, 0f, 90f));
                    break;
                case StructureKind.DualHelipad:
                    CreatePrimitive(PrimitiveType.Cube, kit, "Helipad Final Pad White Guidance A", new Vector3(-2.05f, 0.11f, 0.15f), new Vector3(0.5f, 0.025f, 0.08f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Helipad Final Pad White Guidance B", new Vector3(2.05f, 0.11f, 0.15f), new Vector3(0.5f, 0.025f, 0.08f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, kit, "Helipad Final Fuel Cart", new Vector3(-2.78f, 0.22f, -1.82f), new Vector3(0.48f, 0.28f, 0.34f), pipeMaterial, true);
                    CreatePipe(kit, "Helipad Final Fuel Hose Loop", new Vector3(-2.22f, 0.16f, -1.62f), new Vector3(0.04f, 0.78f, 0.04f), new Vector3(0f, 0f, 90f));
                    break;
            }
        }

        private void AddTurretFinalPolishPass(Transform root, Material teamMaterial)
        {
            Transform kit = CreateVisualRoot(root, "Turret Final Professional Polish Kit", Vector3.zero);
            kit.gameObject.AddComponent<RtsFixedMaterial>();

            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Final Raised Concrete Plinth", new Vector3(0f, 0.055f, 0f), new Vector3(3.24f, 0.05f, 3.24f), concreteMaterial, true);
            AddRoofPanelGrid(kit, "Turret Final Plinth Tile Grid", new Vector3(0f, 0.095f, 0f), 3.05f, 3.05f, 4, 4);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Final Front Hazard Threshold", new Vector3(0f, 0.13f, 1.42f), new Vector3(1.8f, 0.026f, 0.18f), hazardMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Final Rear Utility Cover", new Vector3(0.4f, 0.14f, -1.18f), new Vector3(0.58f, 0.03f, 0.38f), tankBareMetalMaterial, true);

            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Final Front White Armor Cheek", new Vector3(-0.34f, 1.2f, 1.15f), new Vector3(0.34f, 0.22f, 0.042f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Final Front Dirty Armor Cheek", new Vector3(0.34f, 1.18f, 1.15f), new Vector3(0.32f, 0.2f, 0.042f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Final Cyan Optic Slit", new Vector3(0f, 1.33f, 1.18f), new Vector3(0.46f, 0.055f, 0.034f), glowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Final Team Ring Plate", new Vector3(0f, 0.9f, 1.12f), new Vector3(0.76f, 0.06f, 0.035f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Final Roof Offset Hatch", new Vector3(-0.28f, 1.63f, -0.08f), new Vector3(0.36f, 0.035f, 0.32f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Final Roof Dark Service Hatch", new Vector3(0.28f, 1.63f, 0.16f), new Vector3(0.32f, 0.035f, 0.3f), armorShadowMaterial, true);
            BuildVent(kit, "Turret Final Roof Micro Louver", new Vector3(0.28f, 1.66f, -0.34f), new Vector3(0.38f, 0.032f, 0.22f), 4);

            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Final Barrel Wear Sleeve", new Vector3(0f, 1.06f, 1.58f), new Vector3(0.24f, 0.07f, 0.64f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Final Barrel Soot Underside", new Vector3(0f, 0.98f, 1.58f), new Vector3(0.2f, 0.045f, 0.58f), tankDeepGrimeMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, kit, "Turret Final Muzzle Bright Wear Ring", new Vector3(0f, 1.02f, 1.98f), new Vector3(0.14f, 0.035f, 0.14f), tankBareMetalMaterial, true).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                string prefix = side < 0f ? "Left" : "Right";
                CreatePrimitive(PrimitiveType.Cube, kit, "Turret Final " + prefix + " Side Ammo Cabinet", new Vector3(side * 1.14f, 0.38f, -0.34f), new Vector3(0.28f, 0.34f, 0.52f), trimMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, "Turret Final " + prefix + " Side Armor Stack", new Vector3(side * 0.78f, 1.08f, 0.18f), new Vector3(0.04f, 0.34f, 0.48f), side < 0f ? whiteMaterial : tankDustMaterial, true);
                CreatePrimitive(PrimitiveType.Cube, kit, "Turret Final " + prefix + " Base Cable Run", new Vector3(side * 0.92f, 0.24f, 0.62f), new Vector3(0.04f, 0.05f, 0.68f), darkMaterial, true);
                AddBoltLine(kit, "Turret Final " + prefix + " Armor Micro Bolts", new Vector3(side * 0.82f, 1.24f, 0.0f), new Vector3(0f, 0f, 0.13f), 5, 0.014f);
            }

            CreatePrimitive(PrimitiveType.Cube, kit, "Turret Final Spent Shell Bin Label", new Vector3(-0.84f, 0.56f, -0.92f), new Vector3(0.22f, 0.08f, 0.04f), hazardMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, kit, "Turret Final Short Sensor Mast", new Vector3(0.42f, 1.86f, -0.28f), new Vector3(0.018f, 0.42f, 0.018f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Sphere, kit, "Turret Final Amber Mast Tip", new Vector3(0.42f, 2.3f, -0.28f), new Vector3(0.055f, 0.055f, 0.055f), accentMaterial, true);
            AddBoltLine(kit, "Turret Final Mantlet Rivet Row", new Vector3(-0.34f, 1.32f, 1.19f), new Vector3(0.17f, 0f, 0f), 5, 0.015f);
        }

        private void AddImportedSimpleStructureProfessionalFinish(Transform root, StructureKind kind, bool highDetailWarFactory, Material teamMaterial)
        {
            switch (kind)
            {
                case StructureKind.CommandCenter:
                    CreatePrimitive(PrimitiveType.Cube, root, "Command Upper Roof Grime Insert A", new Vector3(-1.3f, 4.58f, 0.65f), new Vector3(0.86f, 0.035f, 0.44f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Command Upper Roof Grime Insert B", new Vector3(0.92f, 4.58f, -0.35f), new Vector3(0.72f, 0.035f, 0.5f), trimMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Command Dish Service Crate Stack", new Vector3(1.55f, 4.74f, 0.62f), new Vector3(0.42f, 0.26f, 0.36f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Command Dish Power Junction", new Vector3(0.18f, 4.72f, 0.86f), new Vector3(0.34f, 0.18f, 0.28f), pipeMaterial, true);
                    CreatePipe(root, "Command Dish Feed Cable", new Vector3(0.88f, 4.72f, 0.68f), new Vector3(0.045f, 1.08f, 0.045f), new Vector3(0f, 0f, 90f));
                    CreatePrimitive(PrimitiveType.Cube, root, "Command Left Wall Replacement Plate A", new Vector3(-3.05f, 1.95f, 0.2f), new Vector3(0.045f, 0.46f, 0.62f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Command Left Wall Dirty Access Hatch", new Vector3(-3.06f, 1.26f, -0.95f), new Vector3(0.045f, 0.52f, 0.56f), trimMaterial, true);
                    BuildVent(root, "Command Left Wall Side Intake", new Vector3(-3.07f, 2.26f, 1.15f), new Vector3(0.045f, 0.52f, 0.36f), 5);
                    AddBoltLine(root, "Command Roof Service Rivets", new Vector3(-1.86f, 4.64f, 0.96f), new Vector3(0.28f, 0f, 0f), 9, 0.018f);
                    break;
                case StructureKind.Barracks:
                    CreatePrimitive(PrimitiveType.Cube, root, "Barracks Sloped Roof Dirty Plate A", new Vector3(-0.88f, 2.47f, 0.18f), new Vector3(0.72f, 0.035f, 0.52f), tankDustMaterial, true).transform.localRotation = Quaternion.Euler(0f, 0f, -6f);
                    CreatePrimitive(PrimitiveType.Cube, root, "Barracks Sloped Roof Dirty Plate B", new Vector3(0.82f, 2.49f, -0.62f), new Vector3(0.62f, 0.035f, 0.46f), trimMaterial, true).transform.localRotation = Quaternion.Euler(0f, 0f, -6f);
                    BuildVent(root, "Barracks Sloped Roof Intake A", new Vector3(-1.02f, 2.54f, -0.78f), new Vector3(0.54f, 0.035f, 0.3f), 5);
                    BuildVent(root, "Barracks Rear Roof Intake B", new Vector3(0.88f, 2.42f, -1.22f), new Vector3(0.58f, 0.035f, 0.28f), 5);
                    CreatePrimitive(PrimitiveType.Cube, root, "Barracks Side Sandbag Row A", new Vector3(1.96f, 0.52f, 0.65f), new Vector3(0.05f, 0.14f, 0.34f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Barracks Side Sandbag Row B", new Vector3(1.96f, 0.52f, 0.18f), new Vector3(0.05f, 0.14f, 0.34f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Barracks Wall Armor Patch Large", new Vector3(-1.98f, 1.18f, 0.28f), new Vector3(0.045f, 0.55f, 0.72f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Barracks Field Locker Pair", new Vector3(1.62f, 0.44f, 1.9f), new Vector3(0.48f, 0.42f, 0.28f), trimMaterial, true);
                    AddBoltLine(root, "Barracks Sloped Roof Rivet Run", new Vector3(-1.25f, 2.56f, 0.7f), new Vector3(0.28f, 0f, 0f), 10, 0.018f);
                    break;
                case StructureKind.PowerPlant:
                    AddSiloBand(root, "Power Plant Main Turbine Dark Upper Band", new Vector3(-0.18f, 2.12f, -0.42f), 0.82f);
                    AddSiloBand(root, "Power Plant Main Turbine Dirty Lower Band", new Vector3(-0.18f, 1.58f, -0.42f), 0.8f);
                    CreatePrimitive(PrimitiveType.Cube, root, "Power Turbine Top Service Hatch A", new Vector3(-0.44f, 2.82f, -0.42f), new Vector3(0.42f, 0.035f, 0.28f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Power Turbine Top Service Hatch B", new Vector3(0.18f, 2.82f, -0.18f), new Vector3(0.36f, 0.035f, 0.26f), trimMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Power Front Transformer Coil A", new Vector3(-1.25f, 0.74f, 2.22f), new Vector3(0.28f, 0.46f, 0.08f), pipeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Power Front Transformer Coil B", new Vector3(-0.88f, 0.74f, 2.22f), new Vector3(0.28f, 0.46f, 0.08f), pipeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Power Right Wall Fuse Cabinet", new Vector3(1.78f, 1.08f, 0.92f), new Vector3(0.045f, 0.48f, 0.44f), trimMaterial, true);
                    CreatePipe(root, "Power Roof Conduit Loop", new Vector3(0.52f, 2.72f, 0.18f), new Vector3(0.04f, 0.86f, 0.04f), new Vector3(90f, 0f, 0f));
                    AddBoltLine(root, "Power Turbine Cap Rivets", new Vector3(-0.75f, 2.84f, -0.72f), new Vector3(0.18f, 0f, 0f), 9, 0.018f);
                    break;
                case StructureKind.Turret:
                    CreatePrimitive(PrimitiveType.Cube, root, "Turret Head Top Armor Plate", new Vector3(0f, 1.36f, -0.1f), new Vector3(0.92f, 0.055f, 0.62f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Turret Gun Heat Shield Top", new Vector3(0f, 1.12f, 1.16f), new Vector3(0.28f, 0.08f, 0.72f), tankBareMetalMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Turret Gun Heat Shield Bottom", new Vector3(0f, 0.98f, 1.16f), new Vector3(0.24f, 0.06f, 0.62f), armorShadowMaterial, true);
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Turret Barrel Forward Cooling Ring", new Vector3(0f, 1.0f, 1.85f), new Vector3(0.18f, 0.05f, 0.18f), tankBareMetalMaterial, true).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Turret Barrel Rear Cooling Ring", new Vector3(0f, 1.0f, 1.28f), new Vector3(0.2f, 0.055f, 0.2f), pipeMaterial, true).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    CreatePrimitive(PrimitiveType.Cube, root, "Turret Recoil Rail Left", new Vector3(-0.22f, 0.9f, 1.34f), new Vector3(0.055f, 0.055f, 0.9f), tankDeepGrimeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Turret Recoil Rail Right", new Vector3(0.22f, 0.9f, 1.34f), new Vector3(0.055f, 0.055f, 0.9f), tankDeepGrimeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Turret Left Cheek Replacement Plate", new Vector3(-0.72f, 1.08f, 0.28f), new Vector3(0.05f, 0.34f, 0.54f), whiteMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Turret Right Cheek Dirty Plate", new Vector3(0.72f, 1.08f, 0.22f), new Vector3(0.05f, 0.32f, 0.5f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Turret Commander Hatch", new Vector3(-0.24f, 1.43f, -0.28f), new Vector3(0.28f, 0.06f, 0.22f), armorShadowMaterial, true);
                    CreatePrimitive(PrimitiveType.Cylinder, root, "Turret Short Radio Mast", new Vector3(0.36f, 1.72f, -0.34f), new Vector3(0.018f, 0.34f, 0.018f), darkMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Turret Base Front Armor Segment", new Vector3(0f, 0.62f, 0.98f), new Vector3(0.92f, 0.16f, 0.055f), trimMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Turret Base Left Armor Segment", new Vector3(-0.98f, 0.58f, 0.08f), new Vector3(0.055f, 0.16f, 0.72f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Turret Base Right Armor Segment", new Vector3(0.98f, 0.58f, 0.08f), new Vector3(0.055f, 0.16f, 0.72f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Turret Base Cable Junction", new Vector3(-0.62f, 0.28f, 0.72f), new Vector3(0.28f, 0.22f, 0.26f), pipeMaterial, true);
                    CreatePipe(root, "Turret External Power Cable", new Vector3(-0.34f, 0.24f, 1.05f), new Vector3(0.04f, 0.78f, 0.04f), new Vector3(0f, 0f, 90f));
                    CreatePrimitive(PrimitiveType.Cube, root, "Turret Ammo Crate Stack Rear", new Vector3(0.12f, 0.26f, -1.12f), new Vector3(0.48f, 0.22f, 0.32f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Turret White Side Armor Patch", new Vector3(-0.74f, 0.92f, 0.2f), new Vector3(0.045f, 0.32f, 0.44f), whiteMaterial, true);
                    AddBoltLine(root, "Turret Top Armor Rivets", new Vector3(-0.36f, 1.405f, -0.36f), new Vector3(0.18f, 0f, 0f), 5, 0.018f);
                    break;
            }
        }

        private void AddImportedStructureWeathering(Transform root, StructureKind kind, bool highDetailWarFactory, Material teamMaterial)
        {
            switch (kind)
            {
                case StructureKind.CommandCenter:
                    AddFacadeStrip(root, "Command Weathered Front Armor Plate A", new Vector3(-1.15f, 2.28f, 3.22f), new Vector3(0.74f, 0.18f, 0.05f), tankDustMaterial);
                    AddFacadeStrip(root, "Command Weathered Front Armor Plate B", new Vector3(1.22f, 2.18f, 3.22f), new Vector3(0.82f, 0.15f, 0.05f), trimMaterial);
                    AddFacadeStrip(root, "Command Lower Grime Kick Plate", new Vector3(0f, 1.22f, 3.23f), new Vector3(3.6f, 0.08f, 0.05f), tankDeepGrimeMaterial);
                    AddFacadeStrip(root, "Command Right Side Service Hatch", new Vector3(3.12f, 1.72f, 0.72f), new Vector3(0.05f, 0.54f, 0.62f), trimMaterial);
                    AddFacadeStrip(root, "Command Right Side Rust Streak", new Vector3(3.13f, 1.26f, 1.55f), new Vector3(0.045f, 0.7f, 0.08f), tankDustMaterial);
                    BuildVent(root, "Command Auxiliary Roof Louver", new Vector3(-0.55f, 4.54f, 1.08f), new Vector3(0.72f, 0.04f, 0.42f), 5);
                    CreatePrimitive(PrimitiveType.Cube, root, "Command Roof Cable Junction", new Vector3(1.18f, 4.66f, 0.74f), new Vector3(0.48f, 0.16f, 0.34f), pipeMaterial, true);
                    CreatePipe(root, "Command Roof Cable Run", new Vector3(1.58f, 4.72f, 0.12f), new Vector3(0.045f, 1.1f, 0.045f), new Vector3(90f, 0f, 0f));
                    AddBoltLine(root, "Command Weathered Plate Bolts", new Vector3(-1.48f, 2.4f, 3.25f), new Vector3(0.18f, 0f, 0f), 7, 0.024f);
                    break;
                case StructureKind.Refinery:
                    AddFacadeStrip(root, "Refinery Front Sooted Dock Panel", new Vector3(-0.9f, 1.06f, 3.04f), new Vector3(0.86f, 0.28f, 0.05f), tankDeepGrimeMaterial);
                    AddFacadeStrip(root, "Refinery Front Replacement Plate", new Vector3(1.14f, 1.52f, 3.04f), new Vector3(0.92f, 0.18f, 0.05f), trimMaterial);
                    AddFacadeStrip(root, "Refinery Amber Process Warning", new Vector3(2.02f, 1.12f, 2.74f), new Vector3(0.42f, 0.08f, 0.05f), accentMaterial);
                    AddSiloBand(root, "Refinery Silo Dirty Middle Band", new Vector3(-1.95f, 1.72f, -0.62f), 0.54f);
                    AddSiloBand(root, "Refinery Silo Rust Lower Band", new Vector3(-1.95f, 0.92f, -0.62f), 0.5f);
                    BuildVent(root, "Refinery Pump Service Vent", new Vector3(1.85f, 1.24f, 2.96f), new Vector3(0.62f, 0.045f, 0.28f), 4);
                    CreatePrimitive(PrimitiveType.Cube, root, "Refinery Ore Sample Crate A", new Vector3(2.7f, 0.22f, 1.98f), new Vector3(0.36f, 0.28f, 0.32f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Refinery Ore Sample Crate B", new Vector3(2.34f, 0.18f, 2.2f), new Vector3(0.28f, 0.2f, 0.28f), trimMaterial, true);
                    CreatePipe(root, "Refinery Low Transfer Hose", new Vector3(0.92f, 0.36f, 2.82f), new Vector3(0.055f, 1.2f, 0.055f), new Vector3(0f, 0f, 90f));
                    AddBoltLine(root, "Refinery Dock Plate Bolts", new Vector3(-1.3f, 1.22f, 3.07f), new Vector3(0.18f, 0f, 0f), 6, 0.024f);
                    break;
                case StructureKind.Barracks:
                    AddFacadeStrip(root, "Barracks Lower Wall Grime Plate", new Vector3(0f, 0.64f, 2.05f), new Vector3(2.7f, 0.1f, 0.05f), tankDeepGrimeMaterial);
                    AddFacadeStrip(root, "Barracks Door Scuffed Armor", new Vector3(0f, 1.04f, 2.06f), new Vector3(0.92f, 0.34f, 0.05f), armorShadowMaterial);
                    AddFacadeStrip(root, "Barracks Front White Replacement Plate", new Vector3(-1.16f, 1.18f, 2.06f), new Vector3(0.58f, 0.2f, 0.05f), whiteMaterial);
                    AddFacadeStrip(root, "Barracks Right Side Weathered Hatch", new Vector3(1.96f, 0.86f, -0.62f), new Vector3(0.05f, 0.44f, 0.52f), trimMaterial);
                    BuildVent(root, "Barracks Side Intake Stack", new Vector3(-1.96f, 1.28f, -0.62f), new Vector3(0.05f, 0.5f, 0.38f), 4);
                    CreatePrimitive(PrimitiveType.Cube, root, "Barracks Supply Crate Front", new Vector3(-1.5f, 0.25f, 2.28f), new Vector3(0.34f, 0.3f, 0.36f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Barracks Field Generator", new Vector3(1.54f, 0.3f, 2.2f), new Vector3(0.42f, 0.32f, 0.34f), pipeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Barracks Cyan Door Status", new Vector3(0.58f, 1.2f, 2.08f), new Vector3(0.22f, 0.06f, 0.045f), glowMaterial, true);
                    AddBoltLine(root, "Barracks Door Plate Bolts", new Vector3(-0.42f, 1.22f, 2.09f), new Vector3(0.14f, 0f, 0f), 7, 0.022f);
                    break;
                case StructureKind.WarFactory:
                    if (highDetailWarFactory)
                    {
                        AddFacadeStrip(root, "CMV2 Bay Soot Kick Plate", new Vector3(0f, 0.92f, 3.18f), new Vector3(2.8f, 0.08f, 0.05f), tankDeepGrimeMaterial);
                        AddFacadeStrip(root, "CMV2 Left Scuffed White Bay Plate", new Vector3(-1.32f, 1.52f, 3.18f), new Vector3(0.58f, 0.22f, 0.05f), tankDustMaterial);
                        AddFacadeStrip(root, "CMV2 Right Scuffed White Bay Plate", new Vector3(1.32f, 1.52f, 3.18f), new Vector3(0.58f, 0.22f, 0.05f), tankDustMaterial);
                        BuildVent(root, "CMV2 Rear Exhaust Soot Louver", new Vector3(2.1f, 2.86f, -1.9f), new Vector3(0.7f, 0.045f, 0.36f), 5);
                    }
                    else
                    {
                        AddFacadeStrip(root, "Factory Bay Soot Kick Plate", new Vector3(0f, 1.1f, 3.44f), new Vector3(3.2f, 0.08f, 0.05f), tankDeepGrimeMaterial);
                        AddFacadeStrip(root, "Factory Scuffed Side Armor A", new Vector3(-2.42f, 1.58f, 2.38f), new Vector3(0.05f, 0.5f, 0.62f), tankDustMaterial);
                        AddFacadeStrip(root, "Factory Scuffed Side Armor B", new Vector3(2.42f, 1.58f, 2.38f), new Vector3(0.05f, 0.5f, 0.62f), tankDustMaterial);
                    }

                    CreatePrimitive(PrimitiveType.Cube, root, "Factory Front Tool Locker", new Vector3(-2.84f, 0.42f, 3.22f), new Vector3(0.4f, 0.42f, 0.28f), trimMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Factory Coil Hose Reel", new Vector3(2.72f, 0.42f, 3.08f), new Vector3(0.38f, 0.2f, 0.34f), pipeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Factory Amber Bay Beacon Left", new Vector3(-1.82f, 1.62f, 3.22f), new Vector3(0.08f, 0.18f, 0.045f), accentMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Factory Amber Bay Beacon Right", new Vector3(1.82f, 1.62f, 3.22f), new Vector3(0.08f, 0.18f, 0.045f), accentMaterial, true);
                    AddBoltLine(root, "Factory Weathered Bay Bolts", new Vector3(-1.42f, 1.12f, 3.25f), new Vector3(0.2f, 0f, 0f), 15, 0.022f);
                    break;
                case StructureKind.PowerPlant:
                    AddFacadeStrip(root, "Power Plant Lower Soot Plate", new Vector3(0f, 0.72f, 2.2f), new Vector3(2.5f, 0.1f, 0.05f), tankDeepGrimeMaterial);
                    AddFacadeStrip(root, "Power Plant Service Panel Left", new Vector3(-1.08f, 1.08f, 2.2f), new Vector3(0.52f, 0.34f, 0.05f), trimMaterial);
                    AddFacadeStrip(root, "Power Plant Service Panel Right", new Vector3(1.08f, 1.12f, 2.2f), new Vector3(0.48f, 0.3f, 0.05f), tankDustMaterial);
                    AddSiloBand(root, "Power Plant Turbine Heat Stain Band", new Vector3(0.05f, 1.44f, -0.1f), 0.45f);
                    CreatePrimitive(PrimitiveType.Cube, root, "Power Plant Cyan Gauge Cluster", new Vector3(1.78f, 1.1f, 0.04f), new Vector3(0.045f, 0.26f, 0.32f), glassMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Power Plant Transformer Box", new Vector3(-1.58f, 0.42f, 1.62f), new Vector3(0.48f, 0.42f, 0.36f), pipeMaterial, true);
                    CreatePipe(root, "Power Plant Ground Cable Bundle", new Vector3(-0.48f, 0.22f, 1.86f), new Vector3(0.055f, 1.24f, 0.055f), new Vector3(0f, 0f, 90f));
                    AddBoltLine(root, "Power Plant Service Bolts", new Vector3(-1.35f, 1.24f, 2.23f), new Vector3(0.22f, 0f, 0f), 13, 0.022f);
                    break;
                case StructureKind.Turret:
                    AddFacadeStrip(root, "Turret Base Scuffed Front Plate", new Vector3(0f, 0.48f, 1.08f), new Vector3(0.84f, 0.12f, 0.05f), tankDustMaterial);
                    AddFacadeStrip(root, "Turret Head Dark Optics Housing", new Vector3(0.38f, 1.04f, 0.96f), new Vector3(0.22f, 0.12f, 0.05f), darkMaterial);
                    CreatePrimitive(PrimitiveType.Cube, root, "Turret Cyan Targeting Lens", new Vector3(0.38f, 1.04f, 1.0f), new Vector3(0.12f, 0.06f, 0.04f), glowMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Turret Spent Shell Bin", new Vector3(-1.08f, 0.28f, 0.32f), new Vector3(0.32f, 0.22f, 0.42f), tankBareMetalMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Turret Power Conduit Box", new Vector3(1.08f, 0.34f, 0.28f), new Vector3(0.28f, 0.32f, 0.32f), pipeMaterial, true);
                    AddBoltLine(root, "Turret Scuffed Plate Bolts", new Vector3(-0.32f, 0.56f, 1.1f), new Vector3(0.16f, 0f, 0f), 5, 0.02f);
                    break;
            }
        }

        private void AddIsometricTextureDressing(Transform root, StructureKind kind, bool highDetailWarFactory, Material teamMaterial)
        {
            if (kind == StructureKind.Turret)
            {
                AddTurretTextureDressing(root, teamMaterial);
                return;
            }

            if (!TryGetStructureDressingSpec(kind, highDetailWarFactory, out Vector2 footprint, out float roofY, out float wallMidY, out float frontZ, out float sideX))
            {
                return;
            }

            float roofWidth = footprint.x * 0.62f;
            float roofDepth = footprint.y * 0.42f;
            AddRoofPanelGrid(root, kind + " Painted Roof Seam Grid", new Vector3(0f, roofY, -0.08f), roofWidth, roofDepth, 5, 4);
            AddTexturePatchGrid(root, kind + " Roof", new Vector3(0f, roofY + 0.028f, -0.08f), roofWidth * 0.9f, roofDepth * 0.85f, 5, 4, true, teamMaterial);

            float facadeWidth = footprint.x * 0.68f;
            AddFacadePanelGrid(root, kind + " Front", new Vector3(0f, wallMidY, frontZ), facadeWidth, 1.6f, 5, 3, false, teamMaterial);
            AddFacadePanelGrid(root, kind + " Right Side", new Vector3(sideX, wallMidY, -0.18f), footprint.y * 0.56f, 1.35f, 4, 3, true, teamMaterial);

            AddGrimeStreaks(root, kind + " Front", frontZ + 0.025f, facadeWidth, wallMidY + 0.62f);
            AddGrimeStreaks(root, kind + " Side", sideX + 0.025f, footprint.y * 0.52f, wallMidY + 0.48f, true);

            CreatePrimitive(PrimitiveType.Cube, root, kind + " Front Cyan Service Light", new Vector3(-facadeWidth * 0.32f, wallMidY + 0.38f, frontZ + 0.04f), new Vector3(0.42f, 0.065f, 0.04f), glowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, kind + " Front Amber Warning Light", new Vector3(facadeWidth * 0.34f, wallMidY + 0.18f, frontZ + 0.04f), new Vector3(0.22f, 0.08f, 0.04f), accentMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, kind + " Low Soot Base", new Vector3(0f, Mathf.Max(0.42f, wallMidY - 0.72f), frontZ + 0.035f), new Vector3(facadeWidth * 0.92f, 0.075f, 0.045f), tankDeepGrimeMaterial, true);

            BuildVent(root, kind + " Extra Front Intake", new Vector3(facadeWidth * 0.18f, wallMidY + 0.44f, frontZ + 0.045f), new Vector3(0.72f, 0.04f, 0.28f), 5);
            AddBoltLine(root, kind + " Facade Texture Bolts", new Vector3(-facadeWidth * 0.42f, wallMidY + 0.76f, frontZ + 0.055f), new Vector3(facadeWidth * 0.105f, 0f, 0f), 9, 0.022f);

            for (int i = 0; i < 5; i++)
            {
                float x = Mathf.Lerp(-facadeWidth * 0.42f, facadeWidth * 0.42f, i / 4f);
                CreatePrimitive(PrimitiveType.Cube, root, kind + " Roof Edge Dark Bracket", new Vector3(x, roofY + 0.06f, frontZ - 0.16f), new Vector3(0.16f, 0.08f, 0.12f), armorShadowMaterial, true);
            }
        }

        private bool TryGetStructureDressingSpec(StructureKind kind, bool highDetailWarFactory, out Vector2 footprint, out float roofY, out float wallMidY, out float frontZ, out float sideX)
        {
            footprint = GetImportedStructureBasePlateSize(kind, highDetailWarFactory);
            roofY = 2.2f;
            wallMidY = 1.25f;
            frontZ = footprint.y * 0.5f - 0.18f;
            sideX = footprint.x * 0.5f - 0.18f;

            switch (kind)
            {
                case StructureKind.CommandCenter:
                    roofY = 3.68f;
                    wallMidY = 2.05f;
                    frontZ = 3.22f;
                    sideX = 3.12f;
                    return true;
                case StructureKind.Refinery:
                    roofY = 2.24f;
                    wallMidY = 1.28f;
                    frontZ = 3.04f;
                    sideX = 2.82f;
                    return true;
                case StructureKind.Barracks:
                    roofY = 2.1f;
                    wallMidY = 1.12f;
                    frontZ = 2.06f;
                    sideX = 1.96f;
                    return true;
                case StructureKind.WarFactory:
                    roofY = highDetailWarFactory ? 2.76f : 2.82f;
                    wallMidY = highDetailWarFactory ? 1.42f : 1.52f;
                    frontZ = highDetailWarFactory ? 3.18f : 3.44f;
                    sideX = highDetailWarFactory ? 3.65f : 3.92f;
                    return true;
                case StructureKind.PowerPlant:
                    roofY = 2.08f;
                    wallMidY = 1.05f;
                    frontZ = 2.2f;
                    sideX = 1.78f;
                    return true;
                case StructureKind.DualHelipad:
                    roofY = 1.68f;
                    wallMidY = 0.9f;
                    frontZ = 2.38f;
                    sideX = 3.86f;
                    return true;
                default:
                    return false;
            }
        }

        private void AddTexturePatchGrid(Transform root, string name, Vector3 center, float width, float depth, int columns, int rows, bool horizontal, Material teamMaterial)
        {
            Material[] materials =
            {
                armorHighlightMaterial,
                trimMaterial,
                tankDustMaterial,
                tankBareMetalMaterial,
                concreteMaterial,
                armorShadowMaterial
            };

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    if ((column + row * 2) % 5 == 4)
                    {
                        continue;
                    }

                    float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, (column + 0.5f) / columns);
                    float z = Mathf.Lerp(-depth * 0.5f, depth * 0.5f, (row + 0.5f) / rows);
                    float cellWidth = width / columns;
                    float cellDepth = depth / rows;
                    float inset = ((column * 7 + row * 3) % 5) * 0.018f;
                    Material material = materials[(column * 3 + row * 5) % materials.Length];
                    Vector3 scale = horizontal
                        ? new Vector3(cellWidth * (0.64f + inset), 0.022f, cellDepth * (0.56f + inset))
                        : new Vector3(cellWidth * 0.62f, cellDepth * 0.56f, 0.032f);
                    Vector3 position = horizontal
                        ? center + new Vector3(x, 0f, z)
                        : center + new Vector3(x, z, 0f);

                    GameObject patch = CreatePrimitive(PrimitiveType.Cube, root, name + " Texture Patch", position, scale, material, true);
                    if (horizontal && (column + row) % 3 == 0)
                    {
                        patch.transform.localRotation = Quaternion.Euler(0f, ((column - row) % 2) * 2f, 0f);
                    }
                }
            }
        }

        private void AddFacadePanelGrid(Transform root, string name, Vector3 center, float width, float height, int columns, int rows, bool sideFacing, Material teamMaterial)
        {
            for (int column = 0; column <= columns; column++)
            {
                float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, column / (float)columns);
                Vector3 position = sideFacing
                    ? center + new Vector3(0f, 0f, x)
                    : center + new Vector3(x, 0f, 0f);
                Vector3 scale = sideFacing
                    ? new Vector3(0.018f, height * 0.78f, 0.02f)
                    : new Vector3(0.02f, height * 0.78f, 0.018f);
                CreatePrimitive(PrimitiveType.Cube, root, name + " Vertical Seam", position, scale, armorShadowMaterial, true);
            }

            for (int row = 0; row <= rows; row++)
            {
                float y = Mathf.Lerp(-height * 0.5f, height * 0.5f, row / (float)rows);
                Vector3 position = center + new Vector3(0f, y, 0f);
                Vector3 scale = sideFacing
                    ? new Vector3(0.018f, 0.02f, width * 0.92f)
                    : new Vector3(width * 0.92f, 0.02f, 0.018f);
                CreatePrimitive(PrimitiveType.Cube, root, name + " Horizontal Seam", position, scale, armorShadowMaterial, true);
            }

            Material[] materials = { trimMaterial, tankDustMaterial, armorHighlightMaterial, tankBareMetalMaterial, concreteMaterial };
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    if ((column * 2 + row) % 4 == 1)
                    {
                        continue;
                    }

                    float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, (column + 0.5f) / columns);
                    float y = Mathf.Lerp(-height * 0.5f, height * 0.5f, (row + 0.5f) / rows);
                    Material material = materials[(column + row * 2) % materials.Length];
                    Vector3 position = sideFacing
                        ? center + new Vector3(0.018f, y, x)
                        : center + new Vector3(x, y, 0.018f);
                    Vector3 scale = sideFacing
                        ? new Vector3(0.024f, height / rows * 0.38f, width / columns * 0.48f)
                        : new Vector3(width / columns * 0.48f, height / rows * 0.38f, 0.024f);
                    CreatePrimitive(PrimitiveType.Cube, root, name + " Varied Armor Insert", position, scale, material, true);
                }
            }
        }

        private void AddGrimeStreaks(Transform root, string name, float surfaceCoordinate, float width, float topY, bool sideFacing = false)
        {
            for (int i = 0; i < 7; i++)
            {
                float t = i / 6f;
                float lateral = Mathf.Lerp(-width * 0.42f, width * 0.42f, t);
                float length = 0.18f + ((i * 37) % 5) * 0.055f;
                float y = topY - length * 0.5f - (i % 3) * 0.12f;
                Material material = i % 2 == 0 ? tankDustMaterial : tankDeepGrimeMaterial;
                Vector3 position = sideFacing
                    ? new Vector3(surfaceCoordinate, y, lateral)
                    : new Vector3(lateral, y, surfaceCoordinate);
                Vector3 scale = sideFacing
                    ? new Vector3(0.032f, length, 0.035f)
                    : new Vector3(0.035f, length, 0.032f);
                CreatePrimitive(PrimitiveType.Cube, root, name + " Vertical Grime Streak", position, scale, material, true);
            }
        }

        private void AddTurretTextureDressing(Transform root, Material teamMaterial)
        {
            AddSiloBand(root, "Turret Dirty Lower Ring", new Vector3(0f, 0.38f, 0f), 1.08f);
            AddSiloBand(root, "Turret Team Upper Ring", new Vector3(0f, 0.86f, 0f), 0.92f);
            AddBoltLine(root, "Turret Extra Ring Bolts A", new Vector3(-0.72f, 0.82f, 0.72f), new Vector3(0.18f, 0f, 0f), 9, 0.024f);
            CreatePrimitive(PrimitiveType.Cube, root, "Turret Front Replacement Armor", new Vector3(0f, 0.72f, 1.08f), new Vector3(0.92f, 0.16f, 0.045f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Turret Team IFF Stripe", new Vector3(0f, 0.96f, 1.1f), new Vector3(0.7f, 0.055f, 0.045f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Turret Amber Armed Light", new Vector3(-0.42f, 1.1f, 1.04f), new Vector3(0.08f, 0.08f, 0.045f), accentMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Turret Cyan Optics Glint", new Vector3(0.42f, 1.1f, 1.04f), new Vector3(0.1f, 0.065f, 0.045f), glowMaterial, true);
        }

        private void AddImportedStructurePanelization(Transform root, StructureKind kind, bool highDetailWarFactory, Material teamMaterial)
        {
            switch (kind)
            {
                case StructureKind.CommandCenter:
                    AddRoofPanelGrid(root, "Command Main Roof Panels", new Vector3(0f, 3.62f, -0.25f), 4.4f, 2.9f, 4, 3);
                    AddFacadeStrip(root, "Command Front Upper Team Trim", new Vector3(0f, 2.72f, 3.18f), new Vector3(3.6f, 0.055f, 0.045f), teamMaterial);
                    AddFacadeStrip(root, "Command Front Lower Shadow Joint", new Vector3(0f, 1.95f, 3.19f), new Vector3(3.9f, 0.045f, 0.045f), armorShadowMaterial);
                    AddFacadeStrip(root, "Command Right Vertical Panel Joint", new Vector3(3.05f, 2.2f, 0.25f), new Vector3(0.045f, 1.8f, 0.055f), armorShadowMaterial);
                    AddFacadeStrip(root, "Command Tower Window Band", new Vector3(0.72f, 5.08f, -0.92f), new Vector3(0.58f, 0.16f, 0.055f), glassMaterial);
                    break;
                case StructureKind.Refinery:
                    AddRoofPanelGrid(root, "Refinery Processing Roof Panels", new Vector3(0.45f, 2.22f, -0.25f), 3.2f, 2.1f, 4, 3);
                    AddSiloBand(root, "Refinery Main Silo Lower Band", new Vector3(-1.95f, 1.35f, -0.62f), 0.52f);
                    AddSiloBand(root, "Refinery Main Silo Upper Band", new Vector3(-1.95f, 2.18f, -0.62f), 0.52f);
                    AddFacadeStrip(root, "Refinery Front Team Service Band", new Vector3(0.2f, 1.26f, 3.02f), new Vector3(2.8f, 0.06f, 0.045f), teamMaterial);
                    AddFacadeStrip(root, "Refinery Lower Wall Joint", new Vector3(0.2f, 0.82f, 3.03f), new Vector3(3.3f, 0.045f, 0.045f), armorShadowMaterial);
                    break;
                case StructureKind.Barracks:
                    AddRoofPanelGrid(root, "Barracks Roof Panels", new Vector3(0f, 2.08f, -0.25f), 3.7f, 2.6f, 4, 3);
                    AddFacadeStrip(root, "Barracks Front Team Trim", new Vector3(0f, 1.45f, 2.03f), new Vector3(2.6f, 0.055f, 0.045f), teamMaterial);
                    AddFacadeStrip(root, "Barracks Front Panel Joint", new Vector3(0f, 0.9f, 2.04f), new Vector3(2.85f, 0.045f, 0.045f), armorShadowMaterial);
                    AddFacadeStrip(root, "Barracks Side Window Left", new Vector3(-1.92f, 1.12f, 0.15f), new Vector3(0.045f, 0.2f, 0.42f), glassMaterial);
                    AddFacadeStrip(root, "Barracks Side Window Right", new Vector3(1.92f, 1.12f, 0.15f), new Vector3(0.045f, 0.2f, 0.42f), glassMaterial);
                    break;
                case StructureKind.WarFactory:
                    if (highDetailWarFactory)
                    {
                        AddRoofPanelGrid(root, "CMV2 Roof Panel Overlay", new Vector3(0f, 2.72f, 0.25f), 3.3f, 2.3f, 4, 3);
                        AddFacadeStrip(root, "CMV2 Bay Team Trim", new Vector3(0f, 1.72f, 3.16f), new Vector3(2.8f, 0.055f, 0.045f), teamMaterial);
                    }
                    else
                    {
                        AddRoofPanelGrid(root, "Factory Roof Panels", new Vector3(0f, 2.78f, -0.2f), 4.4f, 3.0f, 5, 3);
                        AddFacadeStrip(root, "Factory Front Team Trim", new Vector3(0f, 1.9f, 3.42f), new Vector3(3.8f, 0.055f, 0.045f), teamMaterial);
                    }
                    break;
                case StructureKind.PowerPlant:
                    AddRoofPanelGrid(root, "Power Plant Roof Panels", new Vector3(0f, 2.08f, -0.25f), 3.2f, 2.5f, 4, 3);
                    AddSiloBand(root, "Power Plant Turbine Lower Band", new Vector3(0.05f, 1.1f, -0.1f), 0.43f);
                    AddSiloBand(root, "Power Plant Turbine Upper Band", new Vector3(0.05f, 1.72f, -0.1f), 0.43f);
                    AddFacadeStrip(root, "Power Plant Team Front Band", new Vector3(0f, 1.18f, 2.18f), new Vector3(2.45f, 0.055f, 0.045f), teamMaterial);
                    AddFacadeStrip(root, "Power Plant Service Joint", new Vector3(0f, 0.72f, 2.19f), new Vector3(2.7f, 0.045f, 0.045f), armorShadowMaterial);
                    break;
                case StructureKind.Turret:
                    AddSiloBand(root, "Turret Base Armor Band", new Vector3(0f, 0.68f, 0f), 0.92f);
                    AddFacadeStrip(root, "Turret Team Ring Marker", new Vector3(0f, 0.92f, 1.08f), new Vector3(0.72f, 0.055f, 0.045f), teamMaterial);
                    break;
            }
        }

        private void AddRoofPanelGrid(Transform root, string name, Vector3 center, float width, float depth, int columns, int rows)
        {
            CreatePrimitive(PrimitiveType.Cube, root, name + " Dark Border Front", center + new Vector3(0f, 0f, depth * 0.5f), new Vector3(width, 0.028f, 0.035f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, name + " Dark Border Rear", center + new Vector3(0f, 0f, -depth * 0.5f), new Vector3(width, 0.028f, 0.035f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, name + " Dark Border Left", center + new Vector3(-width * 0.5f, 0f, 0f), new Vector3(0.035f, 0.028f, depth), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, name + " Dark Border Right", center + new Vector3(width * 0.5f, 0f, 0f), new Vector3(0.035f, 0.028f, depth), armorShadowMaterial, true);

            for (int i = 1; i < columns; i++)
            {
                float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, i / (float)columns);
                CreatePrimitive(PrimitiveType.Cube, root, name + " Column Seam", center + new Vector3(x, 0.004f, 0f), new Vector3(0.03f, 0.026f, depth * 0.88f), armorShadowMaterial, true);
            }

            for (int i = 1; i < rows; i++)
            {
                float z = Mathf.Lerp(-depth * 0.5f, depth * 0.5f, i / (float)rows);
                CreatePrimitive(PrimitiveType.Cube, root, name + " Row Seam", center + new Vector3(0f, 0.004f, z), new Vector3(width * 0.88f, 0.026f, 0.03f), armorShadowMaterial, true);
            }
        }

        private void AddFacadeStrip(Transform root, string name, Vector3 center, Vector3 scale, Material material)
        {
            CreatePrimitive(PrimitiveType.Cube, root, name, center, scale, material, true);
        }

        private void AddSiloBand(Transform root, string name, Vector3 center, float radius)
        {
            GameObject band = CreatePrimitive(PrimitiveType.Cylinder, root, name, center, new Vector3(radius, 0.035f, radius), armorShadowMaterial, true);
            band.transform.localRotation = Quaternion.identity;
        }

        private void AddIsometricStructureBasePlate(Transform root, StructureKind kind, bool highDetailWarFactory)
        {
            Vector2 size = GetImportedStructureBasePlateSize(kind, highDetailWarFactory);
            float width = size.x;
            float depth = size.y;

            CreatePrimitive(PrimitiveType.Cube, root, "Isometric Concrete Foundation", new Vector3(0f, -0.025f, 0f), new Vector3(width, 0.05f, depth), concreteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Foundation Front Curb", new Vector3(0f, 0.035f, depth * 0.5f), new Vector3(width, 0.08f, 0.09f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Foundation Rear Curb", new Vector3(0f, 0.035f, -depth * 0.5f), new Vector3(width, 0.08f, 0.09f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Foundation Left Curb", new Vector3(-width * 0.5f, 0.035f, 0f), new Vector3(0.09f, 0.08f, depth), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Foundation Right Curb", new Vector3(width * 0.5f, 0.035f, 0f), new Vector3(0.09f, 0.08f, depth), tankBareMetalMaterial, true);

            int columns = Mathf.Clamp(Mathf.RoundToInt(width / 1.05f), 4, 10);
            int rows = Mathf.Clamp(Mathf.RoundToInt(depth / 1.05f), 4, 10);
            for (int i = 1; i < columns; i++)
            {
                float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, i / (float)columns);
                CreatePrimitive(PrimitiveType.Cube, root, "Concrete Tile Seam X", new Vector3(x, 0.015f, 0f), new Vector3(0.025f, 0.018f, depth * 0.92f), armorShadowMaterial, true);
            }

            for (int i = 1; i < rows; i++)
            {
                float z = Mathf.Lerp(-depth * 0.5f, depth * 0.5f, i / (float)rows);
                CreatePrimitive(PrimitiveType.Cube, root, "Concrete Tile Seam Z", new Vector3(0f, 0.016f, z), new Vector3(width * 0.92f, 0.018f, 0.025f), armorShadowMaterial, true);
            }

            float roadZ = depth * 0.5f - 0.55f;
            CreatePrimitive(PrimitiveType.Cube, root, "Painted Access Lane", new Vector3(0f, 0.04f, roadZ), new Vector3(width * 0.62f, 0.022f, 0.045f), roadLineMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Painted Service Stripe Left", new Vector3(-width * 0.31f, 0.041f, roadZ - 0.25f), new Vector3(0.045f, 0.022f, 0.42f), hazardMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Painted Service Stripe Right", new Vector3(width * 0.31f, 0.041f, roadZ - 0.25f), new Vector3(0.045f, 0.022f, 0.42f), hazardMaterial, true);

            CreatePrimitive(PrimitiveType.Cube, root, "Small Utility Crate A", new Vector3(-width * 0.42f, 0.18f, depth * 0.36f), new Vector3(0.34f, 0.26f, 0.32f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Small Utility Crate B", new Vector3(-width * 0.35f, 0.14f, depth * 0.27f), new Vector3(0.28f, 0.18f, 0.28f), tankDustMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Ground Power Junction", new Vector3(width * 0.42f, 0.18f, -depth * 0.34f), new Vector3(0.28f, 0.32f, 0.24f), pipeMaterial, true);
            CreatePipe(root, "Foundation Cable Trench", new Vector3(width * 0.24f, 0.04f, -depth * 0.28f), new Vector3(0.055f, width * 0.36f, 0.055f), new Vector3(0f, 0f, 90f));
        }

        private Vector2 GetImportedStructureBasePlateSize(StructureKind kind, bool highDetailWarFactory)
        {
            switch (kind)
            {
                case StructureKind.CommandCenter:
                    return new Vector2(7.7f, 7.4f);
                case StructureKind.Refinery:
                    return new Vector2(7.3f, 6.4f);
                case StructureKind.Barracks:
                    return new Vector2(5.5f, 4.8f);
                case StructureKind.WarFactory:
                    return highDetailWarFactory ? new Vector2(8.9f, 8.0f) : new Vector2(7.5f, 6.8f);
                case StructureKind.PowerPlant:
                    return new Vector2(5.2f, 4.8f);
                case StructureKind.Turret:
                    return new Vector2(3.9f, 3.9f);
                case StructureKind.DualHelipad:
                    return new Vector2(8.6f, 6.2f);
                default:
                    return new Vector2(6.2f, 5.8f);
            }
        }

        private void AddStructureDetailPass(Transform root, StructureKind kind, Material teamMaterial)
        {
            AddPerimeterHardware(root, kind);
            AddIsometricTextureDressing(root, kind, false, teamMaterial);
            AddStructureReadabilityKit(root, kind, false, teamMaterial);
            AddStructureFinalPolishPass(root, kind, false, teamMaterial);

            switch (kind)
            {
                case StructureKind.CommandCenter:
                    CreatePrimitive(PrimitiveType.Cube, root, "Command Side Service Door", new Vector3(-2.78f, 0.62f, -1.2f), new Vector3(0.06f, 0.6f, 0.5f), darkMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Command Cable Trunk A", new Vector3(2.9f, 1.22f, -0.4f), new Vector3(0.08f, 0.08f, 1.55f), pipeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Command Cable Trunk B", new Vector3(2.9f, 0.88f, -0.4f), new Vector3(0.07f, 0.07f, 1.4f), pipeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Command Roof Sensor Box", new Vector3(-0.62f, 2.04f, 0.82f), new Vector3(0.52f, 0.2f, 0.38f), trimMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Command Roof Team Panel", new Vector3(0.46f, 2.02f, 0.8f), new Vector3(0.62f, 0.055f, 0.4f), teamMaterial);
                    BuildAntennaCluster(root, new Vector3(-1.88f, 2.18f, -1.46f));
                    break;
                case StructureKind.Refinery:
                    CreatePrimitive(PrimitiveType.Cube, root, "Refinery Pump Manifold", new Vector3(2.28f, 0.58f, 1.72f), new Vector3(0.72f, 0.26f, 0.28f), pipeMaterial, true);
                    CreatePipe(root, "Refinery Low Transfer Pipe A", new Vector3(1.52f, 0.52f, 1.68f), new Vector3(0.08f, 1.15f, 0.08f), new Vector3(0f, 0f, 90f));
                    CreatePipe(root, "Refinery Low Transfer Pipe B", new Vector3(0.05f, 0.52f, 1.68f), new Vector3(0.08f, 1.15f, 0.08f), new Vector3(0f, 0f, 90f));
                    CreatePrimitive(PrimitiveType.Cube, root, "Refinery Ore Glow Window A", new Vector3(-0.52f, 0.86f, 2.66f), new Vector3(0.28f, 0.12f, 0.045f), resourceMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Refinery Ore Glow Window B", new Vector3(-1.88f, 0.86f, 2.66f), new Vector3(0.28f, 0.12f, 0.045f), resourceMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Refinery Catwalk Deck", new Vector3(1.4f, 1.68f, -1.28f), new Vector3(1.7f, 0.08f, 0.42f), armorShadowMaterial, true);
                    BuildCatwalkRail(root, "Refinery Silo Catwalk", new Vector3(1.4f, 1.96f, -1.28f), 1.7f);
                    break;
                case StructureKind.Barracks:
                    CreatePrimitive(PrimitiveType.Cube, root, "Barracks Door Awning", new Vector3(0f, 1.1f, 1.72f), new Vector3(1.46f, 0.12f, 0.42f), armorHighlightMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Barracks Left Window", new Vector3(-1.08f, 0.88f, 1.5f), new Vector3(0.42f, 0.16f, 0.06f), glassMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Barracks Right Window", new Vector3(1.08f, 0.88f, 1.5f), new Vector3(0.42f, 0.16f, 0.06f), glassMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Barracks Crate Stack A", new Vector3(-1.62f, 0.34f, 1.3f), new Vector3(0.32f, 0.28f, 0.36f), tankDustMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Barracks Crate Stack B", new Vector3(-1.28f, 0.28f, 1.34f), new Vector3(0.3f, 0.22f, 0.32f), trimMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Barracks HVAC Unit", new Vector3(1.24f, 1.42f, -1.12f), new Vector3(0.72f, 0.24f, 0.48f), pipeMaterial, true);
                    break;
                case StructureKind.WarFactory:
                    CreatePrimitive(PrimitiveType.Cube, root, "Factory Door Rail Left", new Vector3(-1.62f, 1.02f, 2.58f), new Vector3(0.08f, 1.1f, 0.08f), tankBareMetalMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Factory Door Rail Right", new Vector3(1.62f, 1.02f, 2.58f), new Vector3(0.08f, 1.1f, 0.08f), tankBareMetalMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Factory Overhead Crane Beam", new Vector3(0f, 1.48f, 1.52f), new Vector3(2.42f, 0.12f, 0.12f), accentMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Factory Crane Hook", new Vector3(0.3f, 1.08f, 1.52f), new Vector3(0.12f, 0.42f, 0.08f), tankBareMetalMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Factory Side Tool Locker", new Vector3(-3.94f, 0.66f, -0.72f), new Vector3(0.34f, 0.72f, 0.5f), trimMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Factory Wall Warning Strip", new Vector3(3.98f, 0.96f, 0.88f), new Vector3(0.045f, 0.12f, 1.08f), hazardMaterial, true);
                    break;
                case StructureKind.PowerPlant:
                    CreatePrimitive(PrimitiveType.Cube, root, "Power Capacitor Bank A", new Vector3(-1.08f, 0.56f, -1.42f), new Vector3(0.32f, 0.72f, 0.22f), pipeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Power Capacitor Bank B", new Vector3(-0.56f, 0.56f, -1.42f), new Vector3(0.32f, 0.72f, 0.22f), pipeMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Power Service Gauge", new Vector3(1.58f, 0.98f, -0.16f), new Vector3(0.05f, 0.18f, 0.18f), glassMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Power Cable Bundle", new Vector3(-0.5f, 0.32f, 1.7f), new Vector3(1.45f, 0.08f, 0.08f), darkMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Power Exhaust Heat Shield", new Vector3(1.1f, 1.12f, -0.9f), new Vector3(0.58f, 0.06f, 0.58f), tankBareMetalMaterial, true);
                    break;
                case StructureKind.Turret:
                    CreatePrimitive(PrimitiveType.Cube, root, "Turret Left Ammo Box", new Vector3(-0.92f, 0.34f, -0.48f), new Vector3(0.36f, 0.28f, 0.46f), trimMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Turret Right Ammo Box", new Vector3(0.92f, 0.34f, -0.48f), new Vector3(0.36f, 0.28f, 0.46f), trimMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Turret Rangefinder", new Vector3(0.42f, 1.22f, 0.54f), new Vector3(0.34f, 0.12f, 0.1f), glassMaterial, true);
                    AddBoltLine(root, "Turret Base Bolts Front", new Vector3(-0.52f, 0.7f, 0.72f), new Vector3(0.17f, 0f, 0f), 7, 0.035f);
                    break;
                case StructureKind.DualHelipad:
                    CreatePrimitive(PrimitiveType.Cube, root, "Helipad Fuel Tank", new Vector3(0f, 0.62f, -2.02f), new Vector3(1.14f, 0.42f, 0.42f), pipeMaterial, true);
                    CreatePipe(root, "Helipad Fuel Hose Left", new Vector3(-1.22f, 0.52f, -1.72f), new Vector3(0.07f, 0.92f, 0.07f), new Vector3(0f, 0f, 90f));
                    CreatePipe(root, "Helipad Fuel Hose Right", new Vector3(1.22f, 0.52f, -1.72f), new Vector3(0.07f, 0.92f, 0.07f), new Vector3(0f, 0f, 90f));
                    CreatePrimitive(PrimitiveType.Cube, root, "Helipad Control Booth", new Vector3(0f, 0.78f, 2.02f), new Vector3(0.92f, 0.82f, 0.58f), armorMaterial, true);
                    CreatePrimitive(PrimitiveType.Cube, root, "Helipad Booth Glass", new Vector3(0f, 1.02f, 2.34f), new Vector3(0.62f, 0.18f, 0.06f), glassMaterial, true);
                    BuildAntennaCluster(root, new Vector3(0.44f, 1.38f, 2.08f));
                    break;
            }
        }

        private void AddPerimeterHardware(Transform root, StructureKind kind)
        {
            float extent = kind == StructureKind.WarFactory || kind == StructureKind.DualHelipad ? 3.2f : 1.8f;
            CreatePrimitive(PrimitiveType.Cube, root, "Perimeter Service Light A", new Vector3(-extent, 0.34f, extent), new Vector3(0.16f, 0.08f, 0.08f), glowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Perimeter Service Light B", new Vector3(extent, 0.34f, extent), new Vector3(0.16f, 0.08f, 0.08f), glowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Perimeter Cable Run A", new Vector3(-extent, 0.24f, 0f), new Vector3(0.06f, 0.06f, extent * 1.24f), pipeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Perimeter Cable Run B", new Vector3(extent, 0.24f, 0f), new Vector3(0.06f, 0.06f, extent * 1.24f), pipeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Front Maintenance Hatch", new Vector3(0f, 0.28f, extent + 0.08f), new Vector3(0.72f, 0.08f, 0.12f), tankBareMetalMaterial, true);
        }

        private void BuildAntennaCluster(Transform root, Vector3 basePosition)
        {
            CreatePrimitive(PrimitiveType.Cube, root, "Antenna Equipment Box", basePosition, new Vector3(0.34f, 0.18f, 0.28f), trimMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, root, "Antenna Mast A", basePosition + new Vector3(-0.08f, 0.48f, 0f), new Vector3(0.025f, 0.46f, 0.025f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, root, "Antenna Mast B", basePosition + new Vector3(0.11f, 0.36f, 0.04f), new Vector3(0.018f, 0.34f, 0.018f), darkMaterial, true);
            CreatePrimitive(PrimitiveType.Sphere, root, "Antenna Beacon Tip", basePosition + new Vector3(-0.08f, 0.96f, 0f), new Vector3(0.06f, 0.06f, 0.06f), accentMaterial, true);
        }

        private void BuildDualHelipadVisual(Transform root, Material teamMaterial)
        {
            AddIsometricStructureBasePlate(root, StructureKind.DualHelipad, false);
            CreatePrimitive(PrimitiveType.Cube, root, "Helipad Reinforced Apron", new Vector3(0f, 0.11f, 0.02f), new Vector3(7.75f, 0.18f, 4.95f), concreteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Helipad Rear Service Deck", new Vector3(0f, 0.22f, -2.02f), new Vector3(7.15f, 0.12f, 0.68f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Helipad Front Taxi Apron", new Vector3(0f, 0.22f, 2.08f), new Vector3(6.55f, 0.12f, 0.72f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Center Taxi Stripe", new Vector3(0f, 0.305f, 2.1f), new Vector3(0.12f, 0.035f, 0.64f), roadLineMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Left Taxi Stripe", new Vector3(-2.1f, 0.305f, 2.1f), new Vector3(0.12f, 0.035f, 0.64f), roadLineMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Right Taxi Stripe", new Vector3(2.1f, 0.305f, 2.1f), new Vector3(0.12f, 0.035f, 0.64f), roadLineMaterial, true);

            BuildPolishedHelipadPad(root, "Left", new Vector3(-2.2f, 0.3f, 0.02f), teamMaterial);
            BuildPolishedHelipadPad(root, "Right", new Vector3(2.2f, 0.3f, 0.02f), teamMaterial);

            CreatePrimitive(PrimitiveType.Cube, root, "Operations Core Lower Block", new Vector3(0f, 0.68f, -0.45f), new Vector3(1.48f, 0.9f, 1.65f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Operations Core Dark Side Bay", new Vector3(-0.82f, 0.58f, -0.34f), new Vector3(0.14f, 0.62f, 1.2f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Operations Core Dark Side Bay", new Vector3(0.82f, 0.58f, -0.34f), new Vector3(0.14f, 0.62f, 1.2f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Control Room Upper Block", new Vector3(0f, 1.34f, -0.04f), new Vector3(1.88f, 0.48f, 1.16f), concreteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Control Window Band", new Vector3(0f, 1.4f, 0.57f), new Vector3(1.48f, 0.18f, 0.06f), glassMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Control Team Fascia", new Vector3(0f, 1.1f, 0.62f), new Vector3(1.32f, 0.11f, 0.055f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Control Roof Team Plate", new Vector3(0f, 1.61f, -0.08f), new Vector3(0.78f, 0.055f, 0.46f), teamMaterial, true);
            AddRoofPanelGrid(root, "Helipad Control Roof Panels", new Vector3(0f, 1.6f, -0.08f), 1.72f, 1.0f, 3, 2);
            BuildVent(root, "Control Roof HVAC Grille", new Vector3(-0.52f, 1.66f, -0.44f), new Vector3(0.46f, 0.035f, 0.28f), 4);

            CreatePrimitive(PrimitiveType.Cylinder, root, "Radar Pedestal", new Vector3(0.48f, 1.86f, -0.42f), new Vector3(0.2f, 0.22f, 0.2f), pipeMaterial, true);
            GameObject radarDish = CreatePrimitive(PrimitiveType.Cube, root, "Radar Dish Panel", new Vector3(0.48f, 2.12f, -0.42f), new Vector3(0.88f, 0.08f, 0.42f), whiteMaterial, true);
            radarDish.transform.localRotation = Quaternion.Euler(0f, 22f, 12f);
            BuildAntennaCluster(root, new Vector3(-0.58f, 1.72f, -0.55f));

            CreatePrimitive(PrimitiveType.Cube, root, "Left Fuel Service Tank", new Vector3(-3.45f, 0.54f, -1.84f), new Vector3(0.34f, 0.52f, 0.34f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Right Fuel Service Tank", new Vector3(3.45f, 0.54f, -1.84f), new Vector3(0.34f, 0.52f, 0.34f), whiteMaterial, true);
            CreatePipe(root, "Left Helipad Fuel Pipe", new Vector3(-3.0f, 0.42f, -1.52f), new Vector3(0.055f, 0.92f, 0.055f), new Vector3(0f, 0f, 90f));
            CreatePipe(root, "Right Helipad Fuel Pipe", new Vector3(3.0f, 0.42f, -1.52f), new Vector3(0.055f, 0.92f, 0.055f), new Vector3(0f, 0f, 90f));
            CreatePrimitive(PrimitiveType.Cube, root, "Left Service Hose Reel", new Vector3(-3.42f, 0.36f, -1.25f), new Vector3(0.38f, 0.14f, 0.32f), pipeMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Right Service Hose Reel", new Vector3(3.42f, 0.36f, -1.25f), new Vector3(0.38f, 0.14f, 0.32f), pipeMaterial, true);

            BuildCatwalkRail(root, "Left Rear Safety Rail", new Vector3(-3.92f, 0.48f, -1.05f), 2.15f);
            BuildCatwalkRail(root, "Right Rear Safety Rail", new Vector3(3.92f, 0.48f, -1.05f), 2.15f);

            for (int i = 0; i < 8; i++)
            {
                float x = i < 4 ? -3.48f : 3.48f;
                float z = -1.42f + (i % 4) * 0.92f;
                CreatePrimitive(PrimitiveType.Cube, root, "Helipad Edge Beacon", new Vector3(x, 0.46f, z), new Vector3(0.12f, 0.08f, 0.08f), i % 2 == 0 ? glowMaterial : accentMaterial, true);
            }

            AddBoltLine(root, "Helipad Front Curb Bolts", new Vector3(-3.15f, 0.34f, 2.42f), new Vector3(0.42f, 0f, 0f), 16, 0.03f);
        }

        private void BuildPolishedHelipadPad(Transform root, string prefix, Vector3 center, Material teamMaterial)
        {
            CreatePrimitive(PrimitiveType.Cube, root, prefix + " Landing Pad Square Apron", center, new Vector3(3.25f, 0.18f, 3.25f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, prefix + " Landing Pad Dark Rear Joint", center + new Vector3(0f, 0.11f, -1.52f), new Vector3(2.7f, 0.045f, 0.08f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, prefix + " Landing Pad Dark Front Joint", center + new Vector3(0f, 0.11f, 1.52f), new Vector3(2.7f, 0.045f, 0.08f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, prefix + " Landing Pad Left Joint", center + new Vector3(-1.52f, 0.11f, 0f), new Vector3(0.08f, 0.045f, 2.7f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, prefix + " Landing Pad Right Joint", center + new Vector3(1.52f, 0.11f, 0f), new Vector3(0.08f, 0.045f, 2.7f), armorShadowMaterial, true);

            Vector3 deck = center + new Vector3(0f, 0.135f, 0f);
            CreatePrimitive(PrimitiveType.Cylinder, root, prefix + " Yellow Safety Ring", deck, new Vector3(1.42f, 0.026f, 1.42f), roadLineMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, root, prefix + " Light Landing Disk", deck + new Vector3(0f, 0.016f, 0f), new Vector3(1.26f, 0.026f, 1.26f), concreteMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, root, prefix + " Steel Touchdown Ring", deck + new Vector3(0f, 0.033f, 0f), new Vector3(0.82f, 0.018f, 0.82f), tankBareMetalMaterial, true);
            CreatePrimitive(PrimitiveType.Cylinder, root, prefix + " Inner Landing Fill", deck + new Vector3(0f, 0.047f, 0f), new Vector3(0.7f, 0.018f, 0.7f), concreteMaterial, true);
            BuildHelipadMarking(root, deck + new Vector3(0f, 0.075f, 0f));

            CreatePrimitive(PrimitiveType.Cube, root, prefix + " Team Pad Header", center + new Vector3(0f, 0.23f, -1.38f), new Vector3(1.18f, 0.055f, 0.16f), teamMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, prefix + " Cyan Approach Light", center + new Vector3(0f, 0.24f, 1.37f), new Vector3(0.82f, 0.055f, 0.08f), glowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, prefix + " Hazard Plate Left", center + new Vector3(-1.12f, 0.23f, 1.38f), new Vector3(0.34f, 0.04f, 0.42f), hazardMaterial, true).transform.localRotation = Quaternion.Euler(0f, 22f, 0f);
            CreatePrimitive(PrimitiveType.Cube, root, prefix + " Hazard Plate Right", center + new Vector3(1.12f, 0.23f, 1.38f), new Vector3(0.34f, 0.04f, 0.42f), hazardMaterial, true).transform.localRotation = Quaternion.Euler(0f, -22f, 0f);
            AddBoltLine(root, prefix + " Pad Armor Bolts", center + new Vector3(-1.14f, 0.225f, -1.15f), new Vector3(0.38f, 0f, 0f), 7, 0.026f);
        }

        private void BuildHelipadMarking(Transform root, Vector3 center)
        {
            CreatePrimitive(PrimitiveType.Cube, root, "Helipad Mark Left", center + new Vector3(-0.36f, 0f, 0f), new Vector3(0.14f, 0.035f, 1.02f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Helipad Mark Right", center + new Vector3(0.36f, 0f, 0f), new Vector3(0.14f, 0.035f, 1.02f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Helipad Mark Center", center, new Vector3(0.72f, 0.035f, 0.14f), whiteMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Helipad Mark Shadow Left", center + new Vector3(-0.52f, -0.006f, 0f), new Vector3(0.055f, 0.028f, 1.1f), armorShadowMaterial, true);
            CreatePrimitive(PrimitiveType.Cube, root, "Helipad Mark Shadow Right", center + new Vector3(0.52f, -0.006f, 0f), new Vector3(0.055f, 0.028f, 1.1f), armorShadowMaterial, true);
        }

        private Transform CreateVisualRoot(Transform parent, string name, Vector3 localPosition)
        {
            GameObject visualRoot = new GameObject(name);
            visualRoot.transform.SetParent(parent, false);
            visualRoot.transform.localPosition = localPosition;
            return visualRoot.transform;
        }

        private GameObject CreateWingSlab(Transform parent, string name, float side, Vector3 center, float span, float rootChord, float tipChord, float thickness, Material material)
        {
            float inboardX = -side * span * 0.45f;
            float outboardX = side * span * 0.55f;
            Vector2[] points =
            {
                new Vector2(inboardX, rootChord * 0.5f),
                new Vector2(outboardX, tipChord * 0.38f),
                new Vector2(outboardX, -tipChord * 0.62f),
                new Vector2(inboardX, -rootChord * 0.5f)
            };

            return CreateQuadPrism(parent, name, center, points, thickness, material);
        }

        private GameObject CreateFinSlab(Transform parent, string name, Vector3 center, float side, Material material)
        {
            GameObject fin = CreateTaperedBox(parent, name, center, new Vector3(0.12f, 0.58f, 0.38f), new Vector3(0.06f, 0.38f, 0.18f), material);
            fin.transform.localRotation = Quaternion.Euler(0f, 0f, side * 14f);
            return fin;
        }

        private GameObject CreateTaperedBox(Transform parent, string name, Vector3 center, Vector3 baseSize, Vector3 topSize, Material material)
        {
            float bottomY = -baseSize.y * 0.5f;
            float topY = baseSize.y * 0.5f;
            Vector3[] vertices =
            {
                new Vector3(-baseSize.x * 0.5f, bottomY, -baseSize.z * 0.5f),
                new Vector3(baseSize.x * 0.5f, bottomY, -baseSize.z * 0.5f),
                new Vector3(baseSize.x * 0.5f, bottomY, baseSize.z * 0.5f),
                new Vector3(-baseSize.x * 0.5f, bottomY, baseSize.z * 0.5f),
                new Vector3(-topSize.x * 0.5f, topY, -topSize.z * 0.5f),
                new Vector3(topSize.x * 0.5f, topY, -topSize.z * 0.5f),
                new Vector3(topSize.x * 0.5f, topY, topSize.z * 0.5f),
                new Vector3(-topSize.x * 0.5f, topY, topSize.z * 0.5f)
            };

            int[] triangles =
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4,
                1, 2, 6, 1, 6, 5,
                2, 3, 7, 2, 7, 6,
                3, 0, 4, 3, 4, 7
            };

            return CreateMeshObject(parent, name, center, vertices, triangles, material);
        }

        private GameObject CreateQuadPrism(Transform parent, string name, Vector3 center, Vector2[] points, float thickness, Material material)
        {
            Vector3[] vertices =
            {
                new Vector3(points[0].x, -thickness * 0.5f, points[0].y),
                new Vector3(points[1].x, -thickness * 0.5f, points[1].y),
                new Vector3(points[2].x, -thickness * 0.5f, points[2].y),
                new Vector3(points[3].x, -thickness * 0.5f, points[3].y),
                new Vector3(points[0].x, thickness * 0.5f, points[0].y),
                new Vector3(points[1].x, thickness * 0.5f, points[1].y),
                new Vector3(points[2].x, thickness * 0.5f, points[2].y),
                new Vector3(points[3].x, thickness * 0.5f, points[3].y)
            };

            int[] triangles =
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4,
                1, 2, 6, 1, 6, 5,
                2, 3, 7, 2, 7, 6,
                3, 0, 4, 3, 4, 7
            };

            return CreateMeshObject(parent, name, center, vertices, triangles, material);
        }

        private GameObject CreateMeshObject(Transform parent, string name, Vector3 center, Vector3[] vertices, int[] triangles, Material material)
        {
            GameObject meshObject = new GameObject(name);
            meshObject.transform.SetParent(parent, false);
            meshObject.transform.localPosition = center;

            Mesh mesh = new Mesh();
            mesh.name = name + " Mesh";
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshFilter filter = meshObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = meshObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            meshObject.AddComponent<RtsFixedMaterial>();
            return meshObject;
        }

        private GameObject CreatePrimitive(PrimitiveType type, Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material, bool preserveMaterial = false)
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
                DestroyGeneratedObject(collider);
            }

            if (preserveMaterial)
            {
                primitive.AddComponent<RtsFixedMaterial>();
            }

            return primitive;
        }

        private static void DestroyGeneratedObject(Object target)
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
