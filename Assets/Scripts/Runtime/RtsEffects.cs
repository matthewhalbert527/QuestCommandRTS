using UnityEngine;

namespace QuestCommandRTS
{
    public enum RtsCommandPreviewMode
    {
        Hidden,
        Move,
        Target,
        Guard
    }

    public sealed class RtsTimedDestroy : MonoBehaviour
    {
        public float Lifetime = 1f;

        private void Update()
        {
            Lifetime -= Time.deltaTime;
            if (Lifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    public sealed class RtsTracerFade : MonoBehaviour
    {
        public float Lifetime = 0.12f;
        public float Width = 0.065f;

        private LineRenderer line;
        private Color startColor;
        private float startLifetime;

        private void Awake()
        {
            line = GetComponent<LineRenderer>();
            startLifetime = Lifetime;
            if (line != null)
            {
                startColor = line.startColor;
                line.widthMultiplier = Width;
            }
        }

        private void Update()
        {
            Lifetime -= Time.deltaTime;
            float alpha = startLifetime <= 0f ? 0f : Mathf.Clamp01(Lifetime / startLifetime);

            if (line != null)
            {
                Color color = startColor;
                color.a = alpha;
                line.startColor = color;
                line.endColor = color;
                line.widthMultiplier = Width * Mathf.Lerp(0.25f, 1f, alpha);
            }

            if (Lifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    public sealed class RtsSmokePuff : MonoBehaviour
    {
        public float Lifetime = 0.55f;
        public float MaxScale = 0.55f;

        private Renderer puffRenderer;
        private Color startColor;
        private float startLifetime;

        private void Awake()
        {
            puffRenderer = GetComponent<Renderer>();
            startLifetime = Lifetime;
            startColor = puffRenderer != null && puffRenderer.sharedMaterial != null ? puffRenderer.sharedMaterial.color : Color.gray;
        }

        private void Update()
        {
            Lifetime -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(Lifetime / Mathf.Max(0.01f, startLifetime));
            transform.localScale = Vector3.one * Mathf.Lerp(0.12f, MaxScale, t);

            if (puffRenderer != null && puffRenderer.sharedMaterial != null)
            {
                Color color = startColor;
                color.a = Mathf.Lerp(startColor.a, 0f, t);
                puffRenderer.sharedMaterial.color = color;
                puffRenderer.sharedMaterial.SetColor("_Color", color);
                puffRenderer.sharedMaterial.SetColor("_BaseColor", color);
            }

            if (Lifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    public sealed class RtsSmokeEmitter : MonoBehaviour
    {
        public float Interval = 0.5f;
        public float Jitter = 0.18f;
        public float Radius = 0.12f;
        public float Lifetime = 1.4f;
        public float MaxScale = 0.85f;
        public Color SmokeColor = new Color(0.22f, 0.23f, 0.21f, 0.42f);

        private float nextEmitTime;

        private void OnEnable()
        {
            nextEmitTime = Time.time + Random.Range(0f, Interval);
        }

        private void Update()
        {
            if (Time.time < nextEmitTime)
            {
                return;
            }

            nextEmitTime = Time.time + Mathf.Max(0.08f, Interval + Random.Range(-Jitter, Jitter));
            SpawnPuff();
        }

        private void SpawnPuff()
        {
            GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = "Stack Smoke";
            puff.transform.position = transform.position + Random.insideUnitSphere * Radius;
            puff.transform.localScale = Vector3.one * 0.1f;
            puff.GetComponent<Renderer>().sharedMaterial = CreateTransparentMaterial(SmokeColor);

            Collider collider = puff.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            RtsSmokePuff smokePuff = puff.AddComponent<RtsSmokePuff>();
            smokePuff.Lifetime = Lifetime;
            smokePuff.MaxScale = MaxScale;
        }

        private static Material CreateTransparentMaterial(Color color)
        {
            Material material = RtsGame.CreateMaterial(color);
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
    }

    public sealed class RtsProjectileTrail : MonoBehaviour
    {
        private Vector3 start;
        private Vector3 end;
        private RtsTeam team;
        private UnitKind kind;
        private Transform warhead;
        private float duration;
        private float age;
        private float nextSmokeTime;
        private bool arrived;

        public void Initialize(Vector3 from, Vector3 to, RtsTeam projectileTeam, UnitKind projectileKind)
        {
            start = from;
            end = to;
            team = projectileTeam;
            kind = projectileKind;
            transform.position = start;
            duration = Mathf.Clamp(Vector3.Distance(start, end) / GetSpeed(projectileKind), 0.16f, 0.55f);

            BuildWarhead();
            BuildTrail();
        }

        private void Update()
        {
            if (arrived)
            {
                return;
            }

            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / Mathf.Max(0.01f, duration));
            Vector3 previous = transform.position;
            transform.position = Vector3.Lerp(start, end, SmoothStep(t));

            Vector3 direction = transform.position - previous;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }

            if (Time.time >= nextSmokeTime)
            {
                nextSmokeTime = Time.time + 0.035f;
                SpawnSmokePuff(transform.position - transform.forward * 0.24f, t);
            }

            if (t >= 1f)
            {
                arrived = true;
                if (warhead != null)
                {
                    warhead.gameObject.SetActive(false);
                }

                SpawnImpactPuff();
                Destroy(gameObject, 0.7f);
            }
        }

        private void BuildWarhead()
        {
            GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            shell.name = "Warhead";
            shell.transform.SetParent(transform, false);
            shell.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shell.transform.localScale = GetWarheadScale(kind);
            shell.GetComponent<Renderer>().sharedMaterial = CreateEffectMaterial(GetWarheadColor(team));

            Collider shellCollider = shell.GetComponent<Collider>();
            if (shellCollider != null)
            {
                Destroy(shellCollider);
            }

            warhead = shell.transform;
        }

        private void BuildTrail()
        {
            TrailRenderer trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = kind == UnitKind.Skyraider ? 0.38f : 0.5f;
            trail.startWidth = kind == UnitKind.OrcaLifter ? 0.28f : 0.2f;
            trail.endWidth = 0.02f;
            trail.minVertexDistance = 0.06f;
            trail.autodestruct = false;
            trail.material = CreateEffectMaterial(new Color(0.42f, 0.43f, 0.38f, 0.52f));
        }

        private void SpawnSmokePuff(Vector3 point, float travelT)
        {
            GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = "Rocket Smoke";
            puff.transform.SetParent(transform.parent, true);
            puff.transform.position = point + Random.insideUnitSphere * 0.08f;
            puff.transform.localScale = Vector3.one * 0.1f;
            puff.GetComponent<Renderer>().sharedMaterial = CreateEffectMaterial(new Color(0.34f, 0.35f, 0.31f, Mathf.Lerp(0.42f, 0.16f, travelT)));

            Collider puffCollider = puff.GetComponent<Collider>();
            if (puffCollider != null)
            {
                Destroy(puffCollider);
            }

            RtsSmokePuff smokePuff = puff.AddComponent<RtsSmokePuff>();
            smokePuff.Lifetime = Mathf.Lerp(0.45f, 0.7f, travelT);
            smokePuff.MaxScale = kind == UnitKind.OrcaLifter ? 0.72f : 0.54f;
        }

        private void SpawnImpactPuff()
        {
            GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = "Projectile Impact Puff";
            puff.transform.SetParent(transform.parent, true);
            puff.transform.position = end;
            puff.transform.localScale = Vector3.one * 0.18f;
            puff.GetComponent<Renderer>().sharedMaterial = CreateEffectMaterial(new Color(1f, 0.55f, 0.16f, 0.75f));

            Collider puffCollider = puff.GetComponent<Collider>();
            if (puffCollider != null)
            {
                Destroy(puffCollider);
            }

            RtsSmokePuff smokePuff = puff.AddComponent<RtsSmokePuff>();
            smokePuff.Lifetime = 0.34f;
            smokePuff.MaxScale = kind == UnitKind.Tank ? 0.85f : 0.65f;

            if (RtsGame.HasInstance && RtsGame.Instance.Audio != null)
            {
                RtsGame.Instance.Audio.PlayImpact(end);
            }
        }

        private static float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }

        private static float GetSpeed(UnitKind projectileKind)
        {
            switch (projectileKind)
            {
                case UnitKind.OrcaLifter:
                    return 38f;
                case UnitKind.Skyraider:
                    return 46f;
                default:
                    return 34f;
            }
        }

        private static Vector3 GetWarheadScale(UnitKind projectileKind)
        {
            switch (projectileKind)
            {
                case UnitKind.OrcaLifter:
                    return new Vector3(0.16f, 0.32f, 0.16f);
                case UnitKind.Skyraider:
                    return new Vector3(0.12f, 0.28f, 0.12f);
                default:
                    return new Vector3(0.18f, 0.38f, 0.18f);
            }
        }

        private static Color GetWarheadColor(RtsTeam projectileTeam)
        {
            return projectileTeam == RtsTeam.Enemy ? new Color(1f, 0.36f, 0.1f, 1f) : new Color(0.58f, 0.95f, 1f, 1f);
        }

        private static Material CreateEffectMaterial(Color color)
        {
            Material material = RtsGame.CreateMaterial(color);
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
    }

    public sealed class RtsExplosionEffect : MonoBehaviour
    {
        public float Radius = 2f;
        public float Lifetime = 0.9f;

        private readonly Vector3[] debrisVelocity = new Vector3[12];
        private LineRenderer shockRing;
        private Transform flash;
        private Transform smoke;
        private Transform[] debris;
        private Color flashColor = new Color(1f, 0.62f, 0.18f);
        private Color smokeColor = new Color(0.16f, 0.14f, 0.12f, 0.8f);
        private float age;

        private void Start()
        {
            flash = CreateSphere("Explosion Core", Vector3.zero, 0.34f, flashColor).transform;
            smoke = CreateSphere("Smoke Bloom", Vector3.zero, 0.58f, smokeColor).transform;
            shockRing = CreateRing("Shock Ring");
            debris = new Transform[debrisVelocity.Length];

            for (int i = 0; i < debris.Length; i++)
            {
                float angle = (Mathf.PI * 2f * i) / debris.Length;
                float speed = Random.Range(2.7f, 5.4f) * Radius;
                debrisVelocity[i] = new Vector3(Mathf.Cos(angle) * speed, Random.Range(0.9f, 2.2f), Mathf.Sin(angle) * speed);
                debris[i] = CreateSphere("Debris Spark", Vector3.zero, Random.Range(0.08f, 0.16f), new Color(1f, 0.78f, 0.28f)).transform;
            }

            DrawRing(shockRing, 0.25f, 48);
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / Mathf.Max(0.01f, Lifetime));
            float hot = 1f - t;

            if (flash != null)
            {
                flash.localScale = Vector3.one * Mathf.Lerp(Radius * 0.45f, Radius * 1.2f, t);
                SetRendererColor(flash, new Color(flashColor.r, flashColor.g, flashColor.b, hot));
            }

            if (smoke != null)
            {
                smoke.localScale = Vector3.one * Mathf.Lerp(Radius * 0.6f, Radius * 1.75f, t);
                SetRendererColor(smoke, new Color(smokeColor.r, smokeColor.g, smokeColor.b, hot * 0.55f));
            }

            if (shockRing != null)
            {
                float ringRadius = Mathf.Lerp(Radius * 0.35f, Radius * 2.2f, t);
                DrawRing(shockRing, ringRadius, 56);
                Color ringColor = new Color(1f, 0.78f, 0.38f, hot * 0.72f);
                shockRing.startColor = ringColor;
                shockRing.endColor = ringColor;
                shockRing.widthMultiplier = Mathf.Lerp(0.16f, 0.025f, t);
            }

            for (int i = 0; i < debris.Length; i++)
            {
                if (debris[i] == null)
                {
                    continue;
                }

                debrisVelocity[i] += Vector3.down * (7.5f * Time.deltaTime);
                debris[i].localPosition += debrisVelocity[i] * Time.deltaTime;
                debris[i].localScale = Vector3.one * Mathf.Lerp(0.18f, 0.02f, t);
                SetRendererColor(debris[i], new Color(1f, 0.72f, 0.24f, hot));
            }

            if (age >= Lifetime)
            {
                Destroy(gameObject);
            }
        }

        private GameObject CreateSphere(string sphereName, Vector3 localPosition, float scale, Color color)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = sphereName;
            sphere.transform.SetParent(transform, false);
            sphere.transform.localPosition = localPosition;
            sphere.transform.localScale = Vector3.one * scale;
            sphere.GetComponent<Renderer>().sharedMaterial = CreateEffectMaterial(color);

            Collider collider = sphere.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            return sphere;
        }

        private LineRenderer CreateRing(string ringName)
        {
            GameObject ringObject = new GameObject(ringName);
            ringObject.transform.SetParent(transform, false);
            LineRenderer line = ringObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.widthMultiplier = 0.08f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.material = RtsGame.CreateMaterial(Color.white);
            return line;
        }

        private static Material CreateEffectMaterial(Color color)
        {
            Material material = RtsGame.CreateMaterial(color);
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

        private static void DrawRing(LineRenderer line, float ringRadius, int segments)
        {
            line.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = (Mathf.PI * 2f * i) / segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * ringRadius, 0f, Mathf.Sin(angle) * ringRadius));
            }
        }

        private static void SetRendererColor(Transform target, Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null || renderer.sharedMaterial == null)
            {
                return;
            }

            renderer.sharedMaterial.color = color;
            renderer.sharedMaterial.SetColor("_Color", color);
            renderer.sharedMaterial.SetColor("_BaseColor", color);
        }
    }

    public sealed class FloatingText : MonoBehaviour
    {
        public float Lifetime = 1.3f;
        public Vector3 Velocity = new Vector3(0f, 1.1f, 0f);

        private TextMesh textMesh;
        private Color startColor;

        private void Awake()
        {
            textMesh = GetComponent<TextMesh>();
            startColor = textMesh != null ? textMesh.color : Color.white;
        }

        private void Update()
        {
            Lifetime -= Time.deltaTime;
            transform.position += Velocity * Time.deltaTime;

            if (Camera.main != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position, Vector3.up);
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
    }

    public sealed class RtsSpinner : MonoBehaviour
    {
        public Vector3 Axis = Vector3.up;
        public float DegreesPerSecond = 720f;

        private void Update()
        {
            transform.Rotate(Axis, DegreesPerSecond * Time.deltaTime, Space.Self);
        }
    }

    public sealed class RtsHoverBob : MonoBehaviour
    {
        public float Amplitude = 0.12f;
        public float Frequency = 1.6f;

        private Vector3 baseLocalPosition;
        private float phase;

        private void Awake()
        {
            baseLocalPosition = transform.localPosition;
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            float offset = Mathf.Sin(Time.time * Frequency + phase) * Amplitude;
            transform.localPosition = baseLocalPosition + Vector3.up * offset;
        }
    }

    public sealed class RtsCommandPreview : MonoBehaviour
    {
        private LineRenderer ring;
        private LineRenderer glyph;
        private RtsCommandPreviewMode mode = RtsCommandPreviewMode.Hidden;
        private float radius = 1f;

        private void Awake()
        {
            ring = CreateLine("Ring", true, 0.055f);
            glyph = CreateLine("Glyph", true, 0.075f);
            Hide();
        }

        public void SetPreview(RtsCommandPreviewMode nextMode, Vector3 worldPosition, float nextRadius)
        {
            if (nextMode == RtsCommandPreviewMode.Hidden)
            {
                Hide();
                return;
            }

            transform.position = new Vector3(worldPosition.x, 0.09f, worldPosition.z);
            bool redraw = mode != nextMode || Mathf.Abs(radius - nextRadius) > 0.01f;
            mode = nextMode;
            radius = Mathf.Max(0.75f, nextRadius);

            if (redraw)
            {
                DrawPreview();
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            mode = RtsCommandPreviewMode.Hidden;
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (mode == RtsCommandPreviewMode.Hidden)
            {
                return;
            }

            float pulse = 1f + Mathf.Sin(Time.time * 5.5f) * 0.08f;
            transform.localScale = new Vector3(pulse, 1f, pulse);

            float spinSpeed = mode == RtsCommandPreviewMode.Move ? 55f : 115f;
            transform.rotation = Quaternion.Euler(0f, Time.time * spinSpeed, 0f);

            float alpha = 0.68f + Mathf.Sin(Time.time * 7f) * 0.22f;
            ApplyColor(GetRingColor(alpha), GetGlyphColor(alpha));
        }

        private void DrawPreview()
        {
            DrawRing(ring, radius, 48);

            switch (mode)
            {
                case RtsCommandPreviewMode.Target:
                    DrawDiamond(glyph, radius * 1.04f);
                    break;
                case RtsCommandPreviewMode.Guard:
                    DrawShield(glyph, radius * 0.98f);
                    break;
                default:
                    DrawMoveArrow(glyph, radius * 0.9f);
                    break;
            }
        }

        private LineRenderer CreateLine(string lineName, bool loop, float width)
        {
            GameObject lineObject = new GameObject(lineName);
            lineObject.transform.SetParent(transform, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = loop;
            line.widthMultiplier = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.material = RtsGame.CreateMaterial(Color.white);
            return line;
        }

        private void DrawRing(LineRenderer line, float ringRadius, int segments)
        {
            line.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = (Mathf.PI * 2f * i) / segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * ringRadius, 0f, Mathf.Sin(angle) * ringRadius));
            }
        }

        private void DrawMoveArrow(LineRenderer line, float size)
        {
            line.positionCount = 3;
            line.SetPosition(0, new Vector3(0f, 0f, size));
            line.SetPosition(1, new Vector3(size * 0.64f, 0f, -size * 0.46f));
            line.SetPosition(2, new Vector3(-size * 0.64f, 0f, -size * 0.46f));
        }

        private void DrawDiamond(LineRenderer line, float size)
        {
            line.positionCount = 4;
            line.SetPosition(0, new Vector3(0f, 0f, size));
            line.SetPosition(1, new Vector3(size, 0f, 0f));
            line.SetPosition(2, new Vector3(0f, 0f, -size));
            line.SetPosition(3, new Vector3(-size, 0f, 0f));
        }

        private void DrawShield(LineRenderer line, float size)
        {
            line.positionCount = 5;
            line.SetPosition(0, new Vector3(-size * 0.68f, 0f, size * 0.48f));
            line.SetPosition(1, new Vector3(size * 0.68f, 0f, size * 0.48f));
            line.SetPosition(2, new Vector3(size * 0.58f, 0f, -size * 0.2f));
            line.SetPosition(3, new Vector3(0f, 0f, -size * 0.9f));
            line.SetPosition(4, new Vector3(-size * 0.58f, 0f, -size * 0.2f));
        }

        private void ApplyColor(Color ringColor, Color glyphColor)
        {
            ring.startColor = ringColor;
            ring.endColor = ringColor;
            glyph.startColor = glyphColor;
            glyph.endColor = glyphColor;
        }

        private Color GetRingColor(float alpha)
        {
            switch (mode)
            {
                case RtsCommandPreviewMode.Target:
                    return new Color(1f, 0.22f, 0.14f, alpha);
                case RtsCommandPreviewMode.Guard:
                    return new Color(1f, 0.82f, 0.24f, alpha);
                default:
                    return new Color(0.28f, 0.9f, 1f, alpha);
            }
        }

        private Color GetGlyphColor(float alpha)
        {
            switch (mode)
            {
                case RtsCommandPreviewMode.Target:
                    return new Color(1f, 0.58f, 0.36f, alpha);
                case RtsCommandPreviewMode.Guard:
                    return new Color(0.75f, 1f, 0.45f, alpha);
                default:
                    return new Color(0.62f, 1f, 0.78f, alpha);
            }
        }
    }
}
