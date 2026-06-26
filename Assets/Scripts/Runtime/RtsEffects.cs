using UnityEngine;

namespace QuestCommandRTS
{
    public sealed class RtsTimedDestroy : MonoBehaviour
    {
        public float Lifetime = 1f;

        private void Update()
        {
            float deltaTime = RtsGame.HasInstance ? RtsGame.Instance.Clock.DeltaTime : Time.deltaTime;
            if (RtsGame.HasInstance && RtsGame.Instance.Clock.IsPaused)
            {
                return;
            }

            Lifetime -= deltaTime;
            if (Lifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    public sealed class FloatingText : MonoBehaviour
    {
        public float Lifetime = 1.3f;
        public Vector3 Velocity = new Vector3(0f, 1.1f, 0f);

        private TextMesh textMesh;
        private Transform viewCamera;
        private Color startColor;

        private void Awake()
        {
            textMesh = GetComponent<TextMesh>();
            startColor = textMesh != null ? textMesh.color : Color.white;
        }

        private void Update()
        {
            float deltaTime = RtsGame.HasInstance ? RtsGame.Instance.Clock.DeltaTime : Time.deltaTime;
            if (RtsGame.HasInstance && RtsGame.Instance.Clock.IsPaused)
            {
                return;
            }

            Lifetime -= deltaTime;
            transform.position += Velocity * deltaTime;

            if (viewCamera == null)
            {
                viewCamera = ResolveViewCamera();
            }

            if (viewCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - viewCamera.position, Vector3.up);
            }

            if (textMesh != null)
            {
                Color color = startColor;
                color.a = Mathf.Clamp01(Lifetime / 1.3f);
                textMesh.color = color;
            }

            if (Lifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private static Transform ResolveViewCamera()
        {
            if (RtsGame.HasInstance)
            {
                return RtsGame.Instance.GetViewCameraTransform();
            }

            Camera mainCamera = Camera.main;
            return mainCamera != null ? mainCamera.transform : null;
        }
    }

    public sealed class RtsProjectile : MonoBehaviour
    {
        private RtsTeam attackerTeam;
        private RtsEntity attacker;
        private RtsEntity target;
        private Vector3 startPoint;
        private Vector3 impactPoint;
        private float directDamage;
        private float splashDamage;
        private float splashRadius;
        private float speed;
        private float arcHeight;
        private float elapsed;
        private float travelTime;
        private bool initialized;

        public void Initialize(
            RtsTeam team,
            RtsEntity source,
            RtsEntity targetEntity,
            Vector3 from,
            Vector3 to,
            float damage,
            float projectileSpeed,
            float radius,
            float areaDamage,
            float arc)
        {
            attackerTeam = team;
            attacker = source;
            target = targetEntity;
            startPoint = from;
            impactPoint = to;
            directDamage = Mathf.Max(0f, damage);
            splashRadius = Mathf.Max(0f, radius);
            splashDamage = Mathf.Max(0f, areaDamage);
            speed = Mathf.Max(0.1f, projectileSpeed);
            arcHeight = Mathf.Max(0f, arc);
            elapsed = 0f;

            float distance = Vector3.Distance(startPoint, impactPoint);
            travelTime = Mathf.Clamp(distance / speed, 0.08f, 1.8f);
            transform.position = startPoint;
            initialized = true;
        }

        private void Update()
        {
            float deltaTime = RtsGame.HasInstance ? RtsGame.Instance.Clock.DeltaTime : Time.deltaTime;
            if (RtsGame.HasInstance && RtsGame.Instance.Clock.IsPaused)
            {
                return;
            }

            TickProjectile(deltaTime);
        }

#if UNITY_EDITOR
        public void TickProjectileForTests(float deltaTime)
        {
            TickProjectile(deltaTime);
        }
#endif

        private void TickProjectile(float deltaTime)
        {
            if (!initialized)
            {
                return;
            }

            elapsed += Mathf.Max(0f, deltaTime);
            if (target != null && target.IsAlive)
            {
                impactPoint = target.GroundPosition + Vector3.up * GetTargetHeight(target);
            }

            float progress = Mathf.Clamp01(elapsed / travelTime);
            Vector3 position = Vector3.Lerp(startPoint, impactPoint, progress);
            if (arcHeight > 0f)
            {
                position.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;
            }

            Vector3 previous = transform.position;
            transform.position = position;
            Vector3 velocity = position - previous;
            if (velocity.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            }

            if (progress >= 1f)
            {
                ResolveImpact();
            }
        }

        private void ResolveImpact()
        {
            initialized = false;

            if (target != null && target.IsAlive && directDamage > 0f)
            {
                target.TakeDamage(directDamage, attacker);
            }

            if (RtsGame.HasInstance && splashDamage > 0f && splashRadius > 0f)
            {
                RtsGame.Instance.DamageEnemiesInRadius(attackerTeam, impactPoint, splashRadius, splashDamage, attacker, target);
                RtsGame.Instance.SpawnImpactPulse(impactPoint, attackerTeam, splashRadius);
            }

            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        private static float GetTargetHeight(RtsEntity entity)
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
    }
}
