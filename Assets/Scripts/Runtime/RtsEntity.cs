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
        public bool IsAlive => Health > 0f;
        public bool IsSelected { get; private set; }
        public float HealthPercent => MaxHealth <= 0f ? 0f : Mathf.Clamp01(Health / MaxHealth);
        public Vector3 GroundPosition => new Vector3(transform.position.x, 0f, transform.position.z);

        public event Action<RtsEntity> Destroyed;

        private LineRenderer selectionRing;
        private MaterialPropertyBlock propertyBlock;

        protected virtual void Awake()
        {
            Health = MaxHealth;
            propertyBlock = new MaterialPropertyBlock();
            EnsureCollider();
            EnsureSelectionRing();
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

            Team = team;
            DisplayName = displayName;
            MaxHealth = maxHealth;
            Health = maxHealth;
            SelectionRadius = selectionRadius;
            DrawSelectionRing();
            ApplyTeamTint();
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;

            if (selectionRing != null)
            {
                selectionRing.enabled = selected;
            }
        }

        public virtual void TakeDamage(float amount, RtsEntity attacker)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            Health = Mathf.Max(0f, Health - amount);

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
        }

        protected virtual void Die(RtsEntity attacker)
        {
            Destroyed?.Invoke(this);
            CreateDeathMarker();
            Destroy(gameObject);
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
    }
}
