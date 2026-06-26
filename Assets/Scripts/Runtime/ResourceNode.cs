using System.Collections.Generic;
using UnityEngine;

namespace QuestCommandRTS
{
    public sealed class ResourceNode : MonoBehaviour
    {
        public int Amount = 3500;
        public int MaxAmount = 3500;

        public int PersistentId { get; private set; }
        public bool IsDepleted => Amount <= 0;

        private Transform visualRoot;
        private Transform glowRoot;
        private Transform oreBed;
        private readonly List<Transform> orePieces = new List<Transform>();
        private readonly List<Vector3> oreBaseScales = new List<Vector3>();
        private readonly List<float> oreVisibilityThresholds = new List<float>();
        private Material healthyMaterial;
        private Material depletedMaterial;
        private Material glowMaterial;
        private Material highlightMaterial;
        private Material shadowMaterial;

        private void Awake()
        {
            EnsureCollider();
        }

        public void Initialize(int amount, Material healthy, Material depleted)
        {
            EnsureCollider();
            Amount = amount;
            MaxAmount = amount;
            healthyMaterial = healthy;
            depletedMaterial = depleted;
            CreateSupplementalMaterials();
            BuildVisuals();
            RefreshVisuals();
        }

        public void InitializeForRestore(int maxAmount, int amount, Material healthy, Material depleted)
        {
            EnsureCollider();
            MaxAmount = Mathf.Max(1, maxAmount);
            Amount = Mathf.Clamp(amount, 0, MaxAmount);
            healthyMaterial = healthy;
            depletedMaterial = depleted;
            CreateSupplementalMaterials();
            BuildVisuals();
            RefreshVisuals();
        }

        public void AssignPersistentId(int persistentId)
        {
            PersistentId = Mathf.Max(0, persistentId);
        }

        public int Harvest(int requested)
        {
            if (requested <= 0 || Amount <= 0)
            {
                return 0;
            }

            int taken = Mathf.Min(requested, Amount);
            Amount -= taken;
            RefreshVisuals();
            return taken;
        }

        public int Replenish(int amount)
        {
            if (amount <= 0 || Amount >= MaxAmount)
            {
                return 0;
            }

            int added = Mathf.Min(amount, MaxAmount - Amount);
            Amount += added;
            RefreshVisuals();
            return added;
        }

        private void EnsureCollider()
        {
            SphereCollider collider = gameObject.GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<SphereCollider>();
            }

            collider.radius = 2.4f;
            collider.center = new Vector3(0f, 0.7f, 0f);
        }

        private void BuildVisuals()
        {
            if (visualRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(visualRoot.gameObject);
                }
                else
                {
                    DestroyImmediate(visualRoot.gameObject);
                }
            }

            orePieces.Clear();
            oreBaseScales.Clear();
            oreVisibilityThresholds.Clear();

            GameObject root = new GameObject("Ore Cluster Visuals");
            root.transform.SetParent(transform, false);
            visualRoot = root.transform;

            GameObject bed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bed.name = "Ore Dark Bed";
            bed.transform.SetParent(visualRoot, false);
            bed.transform.localPosition = new Vector3(0f, 0.045f, 0f);
            bed.transform.localScale = new Vector3(2.35f, 0.045f, 2.35f);
            bed.transform.localRotation = Quaternion.Euler(0f, 17f, 0f);
            AssignMaterialAndRemoveCollider(bed, shadowMaterial);
            oreBed = bed.transform;

            GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            glow.name = "Ore Glow Pool";
            glow.transform.SetParent(visualRoot, false);
            glow.transform.localPosition = new Vector3(0f, 0.09f, 0f);
            glow.transform.localScale = new Vector3(1.78f, 0.018f, 1.78f);
            AssignMaterialAndRemoveCollider(glow, glowMaterial);
            glowRoot = glow.transform;

            const int pieceCount = 16;
            for (int i = 0; i < pieceCount; i++)
            {
                float angle = (i * 137.5f + (i % 3) * 11f) * Mathf.Deg2Rad;
                float radius = 0.28f + (i % 5) * 0.28f;
                float height = 0.72f + (i % 4) * 0.18f;
                PrimitiveType primitive = i % 5 == 0 ? PrimitiveType.Cube : i % 4 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cylinder;
                GameObject orePiece = GameObject.CreatePrimitive(primitive);
                orePiece.name = "Ore Piece " + (i + 1).ToString("00");
                orePiece.transform.SetParent(visualRoot, false);
                orePiece.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, 0.2f + height * 0.28f, Mathf.Sin(angle) * radius);
                orePiece.transform.localRotation = Quaternion.Euler((i % 3 - 1) * 8f, i * 29f, (i % 4 - 1.5f) * 10f);
                Vector3 baseScale = primitive == PrimitiveType.Sphere
                    ? new Vector3(0.28f + (i % 2) * 0.08f, 0.22f + (i % 3) * 0.06f, 0.28f)
                    : primitive == PrimitiveType.Cube
                        ? new Vector3(0.25f + (i % 2) * 0.08f, height, 0.22f + (i % 3) * 0.05f)
                        : new Vector3(0.22f + (i % 3) * 0.06f, height, 0.22f + (i % 2) * 0.06f);
                orePiece.transform.localScale = baseScale;
                AssignMaterialAndRemoveCollider(orePiece, i % 4 == 0 ? highlightMaterial : healthyMaterial);
                orePieces.Add(orePiece.transform);
                oreBaseScales.Add(baseScale);
                oreVisibilityThresholds.Add(Mathf.Lerp(0.06f, 0.92f, i / (float)(pieceCount - 1)));

                if (i % 3 == 0)
                {
                    GameObject glint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    glint.name = "Ore Glint " + (i + 1).ToString("00");
                    glint.transform.SetParent(orePiece.transform, false);
                    glint.transform.localPosition = new Vector3(0.08f, 0.52f, -0.04f);
                    glint.transform.localScale = Vector3.one * 0.28f;
                    AssignMaterialAndRemoveCollider(glint, glowMaterial);
                }
            }
        }

        private void RefreshVisuals()
        {
            if (visualRoot == null)
            {
                return;
            }

            float fullness = MaxAmount <= 0 ? 0f : Mathf.Clamp01((float)Amount / MaxAmount);
            visualRoot.localScale = Vector3.one;

            if (oreBed != null)
            {
                oreBed.localScale = new Vector3(2.1f, 0.045f, 2.1f) * Mathf.Lerp(0.72f, 1f, fullness);
                Renderer bedRenderer = oreBed.GetComponent<Renderer>();
                if (bedRenderer != null)
                {
                    bedRenderer.sharedMaterial = IsDepleted ? depletedMaterial : shadowMaterial;
                }
            }

            if (glowRoot != null)
            {
                glowRoot.gameObject.SetActive(fullness > 0.02f);
                glowRoot.localScale = new Vector3(1.78f, 0.018f, 1.78f) * Mathf.Lerp(0.45f, 1f, fullness);
            }

            for (int i = 0; i < orePieces.Count; i++)
            {
                Transform piece = orePieces[i];
                if (piece == null)
                {
                    continue;
                }

                bool visible = fullness > 0.02f && fullness >= oreVisibilityThresholds[i];
                piece.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                float localFullness = Mathf.InverseLerp(oreVisibilityThresholds[i], 1f, fullness);
                piece.localScale = oreBaseScales[i] * Mathf.Lerp(0.38f, 1f, localFullness);
            }

            SphereCollider collider = gameObject.GetComponent<SphereCollider>();
            if (collider != null)
            {
                collider.enabled = !IsDepleted;
                collider.radius = Mathf.Lerp(1.35f, 2.55f, fullness);
                collider.center = new Vector3(0f, Mathf.Lerp(0.25f, 0.7f, fullness), 0f);
            }
        }

        private void CreateSupplementalMaterials()
        {
            glowMaterial = CreateEmissiveMaterial(new Color(0.5f, 1f, 0.76f), 1.45f);
            highlightMaterial = CreateEmissiveMaterial(new Color(0.78f, 1f, 0.88f), 0.75f);
            shadowMaterial = RtsGame.CreateMaterial(new Color(0.045f, 0.11f, 0.075f));
        }

        private static Material CreateEmissiveMaterial(Color color, float emissionScale)
        {
            Material material = RtsGame.CreateMaterial(color);
            Color emission = color * emissionScale;
            emission.a = 1f;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
            return material;
        }

        private static void AssignMaterialAndRemoveCollider(GameObject target, Material material)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            Collider collider = target.GetComponent<Collider>();
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
        }

#if UNITY_EDITOR
        public float FullnessForTests => MaxAmount <= 0 ? 0f : Mathf.Clamp01((float)Amount / MaxAmount);
        public int OrePieceCountForTests => orePieces.Count;
        public int VisibleOrePieceCountForTests
        {
            get
            {
                int count = 0;
                for (int i = 0; i < orePieces.Count; i++)
                {
                    if (orePieces[i] != null && orePieces[i].gameObject.activeSelf)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
#endif
    }

    public sealed class ResourceFieldRegenerator : MonoBehaviour
    {
        private readonly List<ResourceNode> nodes = new List<ResourceNode>();
        private Transform spinner;
        private Transform glowPulse;
        private float accumulator;
        private float pulseTime;

        public float RegenerationRatePerSecond = 58f;

        public void Initialize(IEnumerable<ResourceNode> fieldNodes, Material resourceMaterial, Material depletedMaterial, Material glowMaterial, Material minerMaterial)
        {
            nodes.Clear();
            if (fieldNodes != null)
            {
                foreach (ResourceNode node in fieldNodes)
                {
                    if (node != null)
                    {
                        nodes.Add(node);
                    }
                }
            }

            BuildVisuals(resourceMaterial, depletedMaterial, glowMaterial, minerMaterial);
        }

        private void Update()
        {
            if (!RtsGame.HasInstance || RtsGame.Instance.Clock == null || RtsGame.Instance.Clock.IsPaused)
            {
                return;
            }

            TickRegeneration(RtsGame.Instance.Clock.DeltaTime);
        }

        private void TickRegeneration(float deltaTime)
        {
            float safeDelta = Mathf.Max(0f, deltaTime);
            AnimateMiner(safeDelta);
            if (nodes.Count == 0 || safeDelta <= 0f)
            {
                return;
            }

            accumulator += RegenerationRatePerSecond * safeDelta;
            int amount = Mathf.FloorToInt(accumulator);
            if (amount <= 0)
            {
                return;
            }

            ResourceNode target = FindMostDepletedNode();
            if (target == null)
            {
                accumulator = Mathf.Min(accumulator, 1f);
                return;
            }

            int added = target.Replenish(amount);
            accumulator -= added;
            if (added <= 0)
            {
                accumulator = 0f;
            }
        }

        private ResourceNode FindMostDepletedNode()
        {
            ResourceNode best = null;
            float bestFullness = 1f;
            for (int i = 0; i < nodes.Count; i++)
            {
                ResourceNode node = nodes[i];
                if (node == null || node.Amount >= node.MaxAmount)
                {
                    continue;
                }

                float fullness = node.MaxAmount <= 0 ? 1f : (float)node.Amount / node.MaxAmount;
                if (best == null || fullness < bestFullness)
                {
                    best = node;
                    bestFullness = fullness;
                }
            }

            return best;
        }

        private void AnimateMiner(float deltaTime)
        {
            if (spinner != null)
            {
                spinner.localRotation *= Quaternion.Euler(0f, deltaTime * 34f, 0f);
            }

            if (glowPulse != null)
            {
                pulseTime += deltaTime;
                float scale = 1f + Mathf.Sin(pulseTime * 2.2f) * 0.08f;
                glowPulse.localScale = new Vector3(2.1f * scale, 0.028f, 2.1f * scale);
            }
        }

        private void BuildVisuals(Material resourceMaterial, Material depletedMaterial, Material glowMaterial, Material minerMaterial)
        {
            ClearChildren(transform);

            GameObject basePlate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePlate.name = "Ore Field Miner Base";
            basePlate.transform.SetParent(transform, false);
            basePlate.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            basePlate.transform.localScale = new Vector3(1.35f, 0.1f, 1.35f);
            AssignMaterialAndRemoveCollider(basePlate, depletedMaterial);

            GameObject glowRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            glowRing.name = "Ore Field Miner Glow Ring";
            glowRing.transform.SetParent(transform, false);
            glowRing.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            glowRing.transform.localScale = new Vector3(2.1f, 0.028f, 2.1f);
            AssignMaterialAndRemoveCollider(glowRing, glowMaterial != null ? glowMaterial : resourceMaterial);
            glowPulse = glowRing.transform;

            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.name = "Ore Field Miner Column";
            tower.transform.SetParent(transform, false);
            tower.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            tower.transform.localScale = new Vector3(0.34f, 0.72f, 0.34f);
            AssignMaterialAndRemoveCollider(tower, minerMaterial != null ? minerMaterial : depletedMaterial);

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            head.name = "Ore Field Miner Spinner";
            head.transform.SetParent(transform, false);
            head.transform.localPosition = new Vector3(0f, 1.38f, 0f);
            head.transform.localScale = new Vector3(0.82f, 0.12f, 0.82f);
            AssignMaterialAndRemoveCollider(head, resourceMaterial);
            spinner = head.transform;

            for (int i = 0; i < 4; i++)
            {
                GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arm.name = "Ore Field Miner Arm " + (i + 1);
                arm.transform.SetParent(spinner, false);
                arm.transform.localPosition = Quaternion.Euler(0f, i * 90f, 0f) * new Vector3(0.62f, 0f, 0f);
                arm.transform.localRotation = Quaternion.Euler(0f, i * 90f, 0f);
                arm.transform.localScale = new Vector3(0.84f, 0.08f, 0.12f);
                AssignMaterialAndRemoveCollider(arm, minerMaterial != null ? minerMaterial : depletedMaterial);
            }
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                GameObject child = root.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private static void AssignMaterialAndRemoveCollider(GameObject target, Material material)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            Collider collider = target.GetComponent<Collider>();
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
        }

#if UNITY_EDITOR
        public int LinkedNodeCountForTests => nodes.Count;

        public bool ContainsNodeForTests(ResourceNode node)
        {
            return node != null && nodes.Contains(node);
        }

        public void TickRegenerationForTests(float deltaTime)
        {
            TickRegeneration(deltaTime);
        }
#endif
    }
}
