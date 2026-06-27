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

    public sealed class RtsSpinner : MonoBehaviour
    {
        public float DegreesPerSecond = 720f;

        private void Update()
        {
            float deltaTime = RtsGame.HasInstance ? RtsGame.Instance.Clock.DeltaTime : Time.deltaTime;
            if (RtsGame.HasInstance && RtsGame.Instance.Clock.IsPaused)
            {
                return;
            }

            transform.Rotate(Vector3.up, DegreesPerSecond * deltaTime, Space.Self);
        }
    }

    public sealed class RtsHoverBob : MonoBehaviour
    {
        public float Amplitude = 0.08f;
        public float Frequency = 1.5f;

        private Vector3 baseLocalPosition;
        private float phaseOffset;

        private void Awake()
        {
            baseLocalPosition = transform.localPosition;
            phaseOffset = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            float time = RtsGame.HasInstance ? RtsGame.Instance.Clock.SimulationTime : Time.time;
            transform.localPosition = baseLocalPosition + Vector3.up * (Mathf.Sin(time * Frequency + phaseOffset) * Amplitude);
        }
    }

    public sealed class RtsSmokePuff : MonoBehaviour
    {
        public float Lifetime = 0.7f;
        public float MaxScale = 0.55f;

        private float age;
        private Vector3 startScale;
        private Renderer puffRenderer;

        private void Awake()
        {
            startScale = transform.localScale;
            puffRenderer = GetComponent<Renderer>();
        }

        private void Update()
        {
            float deltaTime = RtsGame.HasInstance ? RtsGame.Instance.Clock.DeltaTime : Time.deltaTime;
            if (RtsGame.HasInstance && RtsGame.Instance.Clock.IsPaused)
            {
                return;
            }

            age += Mathf.Max(0f, deltaTime);
            float t = Mathf.Clamp01(age / Mathf.Max(0.01f, Lifetime));
            transform.localScale = Vector3.Lerp(startScale, Vector3.one * MaxScale, SmoothStep(t));
            if (puffRenderer != null)
            {
                Color color = puffRenderer.material.color;
                color.a = Mathf.Lerp(0.42f, 0.03f, t);
                puffRenderer.material.color = color;
            }

            if (age >= Lifetime)
            {
                Destroy(gameObject);
            }
        }

        private static float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }

    public sealed class RtsProjectileTrailEmitter : MonoBehaviour
    {
        private RtsProjectileKind kind;
        private Transform effectParent;
        private float nextSmokeTime;
        private bool emitsSmoke;

        public void Initialize(RtsProjectileKind projectileKind, RtsTeam team, Transform parent)
        {
            kind = projectileKind;
            effectParent = parent;
            emitsSmoke = kind == RtsProjectileKind.Rocket || kind == RtsProjectileKind.Grenade;
            ConfigureTrail(team);
        }

        private void Update()
        {
            if (!emitsSmoke)
            {
                return;
            }

            if (RtsGame.HasInstance && RtsGame.Instance.Clock.IsPaused)
            {
                return;
            }

            float now = RtsGame.HasInstance ? RtsGame.Instance.Clock.SimulationTime : Time.time;
            if (now < nextSmokeTime)
            {
                return;
            }

            nextSmokeTime = now + (kind == RtsProjectileKind.Rocket ? 0.045f : 0.065f);
            SpawnSmokePuff(transform.position - transform.forward * 0.22f);
        }

        private void ConfigureTrail(RtsTeam team)
        {
            TrailRenderer trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = GetTrailTime(kind);
            trail.startWidth = GetTrailStartWidth(kind);
            trail.endWidth = 0.02f;
            trail.minVertexDistance = 0.05f;
            trail.autodestruct = false;
            trail.material = RtsGame.CreateMaterial(GetTrailColor(kind, team));
        }

        private void SpawnSmokePuff(Vector3 point)
        {
            GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = "Projectile Smoke Puff";
            puff.transform.SetParent(effectParent, true);
            puff.transform.position = point + Random.insideUnitSphere * 0.08f;
            puff.transform.localScale = Vector3.one * 0.08f;

            Renderer renderer = puff.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = RtsGame.CreateMaterial(new Color(0.36f, 0.37f, 0.32f, 0.42f));
            }

            Collider collider = puff.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            RtsSmokePuff smokePuff = puff.AddComponent<RtsSmokePuff>();
            smokePuff.Lifetime = kind == RtsProjectileKind.Rocket ? 0.75f : 0.52f;
            smokePuff.MaxScale = kind == RtsProjectileKind.Rocket ? 0.72f : 0.48f;
        }

        private static float GetTrailTime(RtsProjectileKind projectileKind)
        {
            switch (projectileKind)
            {
                case RtsProjectileKind.Rocket:
                    return 0.5f;
                case RtsProjectileKind.Grenade:
                    return 0.34f;
                case RtsProjectileKind.TankShell:
                case RtsProjectileKind.DefenseShell:
                    return 0.22f;
                case RtsProjectileKind.FlameBolt:
                    return 0.2f;
                default:
                    return 0.12f;
            }
        }

        private static float GetTrailStartWidth(RtsProjectileKind projectileKind)
        {
            switch (projectileKind)
            {
                case RtsProjectileKind.Rocket:
                    return 0.22f;
                case RtsProjectileKind.Grenade:
                    return 0.16f;
                case RtsProjectileKind.TankShell:
                case RtsProjectileKind.DefenseShell:
                    return 0.09f;
                case RtsProjectileKind.FlameBolt:
                    return 0.18f;
                default:
                    return 0.04f;
            }
        }

        private static Color GetTrailColor(RtsProjectileKind projectileKind, RtsTeam team)
        {
            switch (projectileKind)
            {
                case RtsProjectileKind.Rocket:
                    return new Color(0.42f, 0.42f, 0.35f, 0.62f);
                case RtsProjectileKind.Grenade:
                    return new Color(0.38f, 0.4f, 0.32f, 0.38f);
                case RtsProjectileKind.FlameBolt:
                    return new Color(1f, 0.45f, 0.1f, 0.7f);
                default:
                    return team == RtsTeam.Enemy ? new Color(1f, 0.34f, 0.2f, 0.74f) : new Color(0.5f, 0.9f, 1f, 0.74f);
            }
        }
    }

    public sealed class RtsProjectile : MonoBehaviour
    {
        private RtsProjectileKind projectileKind;
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
            RtsProjectileKind kind,
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
            projectileKind = kind;
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

            if (RtsGame.HasInstance && RtsGame.Instance.Audio != null)
            {
                if (projectileKind == RtsProjectileKind.Rocket || splashDamage > 0f || splashRadius > 0f)
                {
                    RtsGame.Instance.Audio.PlayExplosion(impactPoint);
                }
                else
                {
                    RtsGame.Instance.Audio.PlayImpact(impactPoint);
                }
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
                if (RtsBalance.IsAircraft(unit.UnitKind))
                {
                    return 1.45f;
                }

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
