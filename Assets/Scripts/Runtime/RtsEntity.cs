using System;
using UnityEngine;

namespace QuestCommandRTS
{
    [DisallowMultipleComponent]
    public class RtsEntity : MonoBehaviour
    {
        public RtsTeam Team = RtsTeam.Neutral;
        public string DisplayName = "Entity";
        public float MaxHealth = 100f;
        public float SelectionRadius = 0.9f;

        public float Health { get; private set; }
        public int PersistentId { get; private set; }
        public bool IsAlive => Health > 0f;
        public bool IsSelected { get; private set; }
        public float HealthPercent => MaxHealth <= 0f ? 0f : Mathf.Clamp01(Health / MaxHealth);
        public Vector3 GroundPosition => new Vector3(transform.position.x, 0f, transform.position.z);

        public event Action<RtsEntity> Destroyed;

        private LineRenderer selectionRing;
        private Transform healthBarRoot;
        private Transform healthBarFill;
        private Renderer healthBarBackgroundRenderer;
        private Renderer healthBarFillRenderer;
        private Material healthBarFillMaterial;
        private MaterialPropertyBlock propertyBlock;

        protected virtual void Awake()
        {
            Health = MaxHealth;
            propertyBlock = new MaterialPropertyBlock();
            EnsureCollider();
            EnsureSelectionRing();
            EnsureHealthBar();
        }

        protected virtual void Start()
        {
            if (RtsGame.HasInstance)
            {
                RtsGame.Instance.RegisterEntity(this);
            }

            ApplyTeamTint();
        }

        protected virtual void LateUpdate()
        {
            if (selectionRing != null && selectionRing.enabled)
            {
                selectionRing.transform.position = GroundPosition + Vector3.up * 0.03f;
            }

            UpdateHealthBarPose();
        }

        protected virtual void OnDestroy()
        {
            if (RtsGame.HasInstance)
            {
                RtsGame.Instance.UnregisterEntity(this);
            }
        }

        public virtual void Initialize(RtsTeam team, string displayName, float maxHealth, float selectionRadius)
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            EnsureCollider();
            if (selectionRing == null)
            {
                EnsureSelectionRing();
            }

            if (healthBarRoot == null)
            {
                EnsureHealthBar();
            }

            Team = team;
            DisplayName = displayName;
            MaxHealth = maxHealth;
            Health = maxHealth;
            SelectionRadius = selectionRadius;
            DrawSelectionRing();
            LayoutHealthBar();
            ApplyTeamTint();
            UpdateHealthBarVisual();
        }

        public void AssignPersistentId(int persistentId)
        {
            PersistentId = Mathf.Max(0, persistentId);
        }

        public void SetHealthForRestore(float health)
        {
            Health = Mathf.Clamp(health, 0f, MaxHealth);
            UpdateHealthBarVisual();
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;

            if (selectionRing != null)
            {
                selectionRing.enabled = selected;
            }

            UpdateHealthBarVisual();
        }

        public virtual void TakeDamage(float amount, RtsEntity attacker)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            Health = Mathf.Max(0f, Health - amount);
            UpdateHealthBarVisual();
            OnDamaged(amount, attacker);

            if (Health <= 0f)
            {
                Die(attacker);
            }
        }

        public virtual void Repair(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            Health = Mathf.Min(MaxHealth, Health + amount);
            UpdateHealthBarVisual();
        }

        public void RefreshVisibilityDependentVisuals()
        {
            UpdateHealthBarVisual();
        }

        protected virtual void Die(RtsEntity attacker)
        {
            Destroyed?.Invoke(this);
            CreateDeathMarker();
            Destroy(gameObject);
        }

        protected virtual void OnDamaged(float amount, RtsEntity attacker)
        {
        }

        private void EnsureCollider()
        {
            if (GetComponent<Collider>() != null)
            {
                return;
            }

            CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.7f, 0f);
            capsule.height = 1.4f;
            capsule.radius = Mathf.Max(0.35f, SelectionRadius * 0.55f);
        }

        private void EnsureSelectionRing()
        {
            GameObject ringObject = new GameObject("Selection Ring");
            ringObject.transform.SetParent(transform, false);
            ringObject.transform.localPosition = Vector3.up * 0.03f;

            selectionRing = ringObject.AddComponent<LineRenderer>();
            selectionRing.useWorldSpace = false;
            selectionRing.loop = true;
            selectionRing.positionCount = 48;
            selectionRing.widthMultiplier = 0.055f;
            selectionRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            selectionRing.receiveShadows = false;

            Material ringMaterial = new Material(Shader.Find("Sprites/Default"));
            ringMaterial.color = Color.white;
            selectionRing.material = ringMaterial;
            selectionRing.enabled = false;

            DrawSelectionRing();
        }

        private void EnsureHealthBar()
        {
            GameObject barObject = new GameObject("Health Bar");
            healthBarRoot = barObject.transform;
            healthBarRoot.SetParent(transform, false);

            Material backgroundMaterial = RtsGame.CreateMaterial(new Color(0.02f, 0.025f, 0.028f, 0.92f));
            healthBarFillMaterial = RtsGame.CreateMaterial(new Color(0.35f, 1f, 0.45f, 0.96f));

            GameObject background = GameObject.CreatePrimitive(PrimitiveType.Cube);
            background.name = "Health Background";
            background.transform.SetParent(healthBarRoot, false);
            background.transform.localPosition = Vector3.zero;
            background.transform.localScale = new Vector3(1.08f, 0.1f, 0.035f);
            healthBarBackgroundRenderer = background.GetComponent<Renderer>();
            healthBarBackgroundRenderer.sharedMaterial = backgroundMaterial;
            RemovePrimitiveCollider(background);

            GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fill.name = "Health Fill";
            healthBarFill = fill.transform;
            healthBarFill.SetParent(healthBarRoot, false);
            healthBarFill.localPosition = new Vector3(-0.02f, 0.011f, -0.022f);
            healthBarFill.localScale = new Vector3(1f, 0.075f, 0.04f);
            healthBarFillRenderer = fill.GetComponent<Renderer>();
            healthBarFillRenderer.sharedMaterial = healthBarFillMaterial;
            RemovePrimitiveCollider(fill);

            LayoutHealthBar();
            UpdateHealthBarVisual();
        }

        private void DrawSelectionRing()
        {
            if (selectionRing == null)
            {
                return;
            }

            float radius = Mathf.Max(0.45f, SelectionRadius);
            for (int i = 0; i < selectionRing.positionCount; i++)
            {
                float angle = (Mathf.PI * 2f * i) / selectionRing.positionCount;
                selectionRing.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }

            selectionRing.startColor = RtsBalance.TeamColor(Team);
            selectionRing.endColor = Color.white;
        }

        private void LayoutHealthBar()
        {
            if (healthBarRoot == null)
            {
                return;
            }

            float width = Mathf.Clamp(SelectionRadius * 1.55f, 1.1f, 6.2f);
            float height = Mathf.Clamp(SelectionRadius * 0.9f + 1.25f, 1.6f, 5.5f);
            healthBarRoot.localPosition = Vector3.up * height;
            healthBarRoot.localScale = new Vector3(width, 1f, 1f);
        }

        private void UpdateHealthBarPose()
        {
            if (healthBarRoot == null || !healthBarRoot.gameObject.activeSelf)
            {
                return;
            }

            LayoutHealthBar();
            Transform viewCamera = RtsGame.HasInstance ? RtsGame.Instance.GetViewCameraTransform() : null;
            if (viewCamera == null && Camera.main != null)
            {
                viewCamera = Camera.main.transform;
            }

            if (viewCamera != null)
            {
                healthBarRoot.rotation = Quaternion.LookRotation(healthBarRoot.position - viewCamera.position, Vector3.up);
            }
        }

        private void UpdateHealthBarVisual()
        {
            if (healthBarRoot == null || healthBarFill == null)
            {
                return;
            }

            float percent = HealthPercent;
            bool visible = IsAlive && (IsSelected || percent < 0.999f);
            if (visible && Team == RtsTeam.Enemy && RtsGame.HasInstance)
            {
                visible = RtsGame.Instance.IsEntityVisible(this);
            }

            healthBarRoot.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            if (healthBarBackgroundRenderer != null)
            {
                healthBarBackgroundRenderer.enabled = true;
            }

            if (healthBarFillRenderer != null)
            {
                healthBarFillRenderer.enabled = true;
            }

            float fillWidth = Mathf.Max(0.02f, percent);
            healthBarFill.localScale = new Vector3(fillWidth, 0.075f, 0.04f);
            healthBarFill.localPosition = new Vector3(-0.52f + fillWidth * 0.5f, 0.011f, -0.022f);

            Color color = percent > 0.55f ? new Color(0.35f, 1f, 0.45f, 0.96f) :
                percent > 0.25f ? new Color(1f, 0.82f, 0.28f, 0.96f) :
                new Color(1f, 0.28f, 0.2f, 0.96f);
            healthBarFillMaterial.color = color;
            healthBarFillMaterial.SetColor("_Color", color);
            healthBarFillMaterial.SetColor("_BaseColor", color);
        }

        protected void ApplyTeamTint()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            Color color = RtsBalance.TeamColor(Team);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == selectionRing)
                {
                    continue;
                }

                if (healthBarRoot != null && renderer.transform.IsChildOf(healthBarRoot))
                {
                    continue;
                }

                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_Color", color);
                propertyBlock.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void CreateDeathMarker()
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = DisplayName + " Wreck";
            marker.transform.position = GroundPosition + Vector3.up * 0.03f;
            marker.transform.localScale = new Vector3(SelectionRadius * 1.2f, 0.04f, SelectionRadius * 1.2f);

            Collider markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null)
            {
                Destroy(markerCollider);
            }

            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = RtsGame.CreateMaterial(new Color(0.08f, 0.075f, 0.065f));
            }

            Destroy(marker, 12f);
        }

        private static void RemovePrimitiveCollider(GameObject primitive)
        {
            Collider collider = primitive.GetComponent<Collider>();
            if (collider == null)
            {
                return;
            }

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
}
