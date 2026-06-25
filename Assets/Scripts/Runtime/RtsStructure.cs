using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace QuestCommandRTS
{
    public class RtsStructure : RtsEntity
    {
        public StructureKind StructureKind = StructureKind.CommandCenter;
        public float FootprintRadius = 2f;
        public int PowerProvided;
        public int PowerUsed;

        public virtual void Initialize(RtsTeam team, StructureKind kind)
        {
            StructureKind = kind;
            StructureStats stats = RtsBalance.GetStructure(kind);
            FootprintRadius = stats.FootprintRadius;
            PowerProvided = stats.PowerProvided;
            PowerUsed = stats.PowerUsed;
            Initialize(team, stats.Name, stats.Health, stats.FootprintRadius + 0.25f);
        }

        protected override void Start()
        {
            base.Start();
            if (RtsGame.HasInstance)
            {
                RtsGame.Instance.RecalculatePower();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (RtsGame.HasInstance)
            {
                RtsGame.Instance.RecalculatePower();
            }
        }
    }

    public sealed class RefineryStructure : RtsStructure
    {
    }

    public sealed class ProductionStructure : RtsStructure
    {
        public int QueueCount => queue.Count + (activeKind.HasValue ? 1 : 0);
        public int PendingQueueCount => queue.Count;
        public bool HasActiveProduction => activeKind.HasValue;
        public UnitKind ActiveProductionKind => activeKind.HasValue ? activeKind.Value : UnitKind.Rifleman;
        public float ActiveProductionProgress => activeDuration <= 0f || !activeKind.HasValue ? 0f : 1f - Mathf.Clamp01(activeRemaining / activeDuration);
        public bool CanCancelLastQueuedUnit => queue.Count > 0;
        public bool CanCancelProduction => queue.Count > 0 || activeKind.HasValue;
        public bool HasRallyPoint { get; private set; }
        public Vector3 RallyPoint { get; private set; }

        private readonly List<UnitKind> queue = new List<UnitKind>();
        private UnitKind? activeKind;
        private float activeRemaining;
        private float activeDuration;
        private LineRenderer rallyLine;
        private GameObject rallyMarker;

        private void Update()
        {
            if (!RtsGame.HasInstance || RtsGame.Instance.IsMatchOver || RtsGame.Instance.Clock.IsPaused)
            {
                RefreshRallyVisual();
                return;
            }

            using (RtsProfilerMarkers.Production.Auto())
            {
                if (activeKind == null)
                {
                    StartNextItem();
                }

                if (activeKind == null)
                {
                    RefreshRallyVisual();
                    return;
                }

                float powerMultiplier = RtsGame.Instance.Resources.HasLowPower ? 0.45f : 1f;
                activeRemaining -= RtsGame.Instance.Clock.DeltaTime * powerMultiplier;

                if (activeRemaining <= 0f)
                {
                    UnitKind completed = activeKind.Value;
                    activeKind = null;
                    RtsUnit unit = RtsGame.Instance.CreateUnit(Team, completed, GetSpawnPoint());
                    if (unit != null && Team == RtsTeam.Player && HasRallyPoint)
                    {
                        unit.IssueMove(RallyPoint + Random.insideUnitSphere * 1.25f);
                    }

                    StartNextItem();
                }
            }

            RefreshRallyVisual();
        }

        public bool CanTrain(UnitKind kind)
        {
            if (RtsBalance.IsInfantry(kind))
            {
                return StructureKind == StructureKind.Barracks || StructureKind == StructureKind.CommandCenter;
            }

            switch (kind)
            {
                case UnitKind.Harvester:
                    return StructureKind == StructureKind.Refinery || StructureKind == StructureKind.WarFactory || StructureKind == StructureKind.CommandCenter;
                case UnitKind.Tank:
                case UnitKind.LightTank:
                case UnitKind.MediumTank:
                case UnitKind.HeavyTank:
                    return StructureKind == StructureKind.WarFactory;
                default:
                    return false;
            }
        }

        public bool QueueUnit(UnitKind kind)
        {
            if (!CanTrain(kind))
            {
                return false;
            }

            UnitStats stats = RtsBalance.GetUnit(kind);
            if (!RtsGame.Instance.Resources.TrySpend(stats.Cost))
            {
                RtsGame.Instance.SpawnFloatingText("Need credits", transform.position + Vector3.up * 2.2f, Color.yellow);
                return false;
            }

            queue.Add(kind);
            RtsGame.Instance.SpawnFloatingText(stats.Name + " queued", transform.position + Vector3.up * 2.2f, Color.white);
            return true;
        }

        public bool TryCancelLastQueuedUnit(out UnitKind canceledKind, out int refund)
        {
            canceledKind = UnitKind.Rifleman;
            refund = 0;

            if (queue.Count <= 0 || !RtsGame.HasInstance)
            {
                return false;
            }

            int index = queue.Count - 1;
            canceledKind = queue[index];
            queue.RemoveAt(index);

            UnitStats stats = RtsBalance.GetUnit(canceledKind);
            refund = stats.Cost;
            RtsGame.Instance.Resources.Add(refund);
            RtsGame.Instance.SpawnFloatingText("Canceled +" + refund, transform.position + Vector3.up * 2.2f, new Color(0.75f, 1f, 0.82f));
            return true;
        }

        public bool TryCancelProduction(out UnitKind canceledKind, out int refund)
        {
            if (TryCancelLastQueuedUnit(out canceledKind, out refund))
            {
                return true;
            }

            canceledKind = UnitKind.Rifleman;
            refund = 0;

            if (!activeKind.HasValue || !RtsGame.HasInstance)
            {
                return false;
            }

            canceledKind = activeKind.Value;
            activeKind = null;
            activeRemaining = 0f;
            activeDuration = 0f;

            UnitStats stats = RtsBalance.GetUnit(canceledKind);
            refund = stats.Cost;
            RtsGame.Instance.Resources.Add(refund);
            RtsGame.Instance.SpawnFloatingText("Canceled +" + refund, transform.position + Vector3.up * 2.2f, new Color(0.75f, 1f, 0.82f));
            return true;
        }

        public bool TryGetQueuedUnit(int index, out UnitKind kind)
        {
            if (index >= 0 && index < queue.Count)
            {
                kind = queue[index];
                return true;
            }

            kind = UnitKind.Rifleman;
            return false;
        }

#if UNITY_EDITOR
        public void StartNextProductionForTests()
        {
            if (activeKind == null)
            {
                StartNextItem();
            }
        }
#endif

        public RtsProductionSaveData CaptureProductionState()
        {
            RtsProductionSaveData data = new RtsProductionSaveData
            {
                hasActiveProduction = activeKind.HasValue,
                activeKind = activeKind.HasValue ? activeKind.Value.ToString() : string.Empty,
                activeRemaining = activeRemaining,
                activeDuration = activeDuration,
                hasRallyPoint = HasRallyPoint,
                rallyPoint = new Vector3Data(RallyPoint)
            };

            for (int i = 0; i < queue.Count; i++)
            {
                data.queue.Add(queue[i].ToString());
            }

            return data;
        }

        public void RestoreProductionState(RtsProductionSaveData data)
        {
            queue.Clear();
            activeKind = null;
            activeRemaining = 0f;
            activeDuration = 0f;
            HasRallyPoint = false;

            if (data == null)
            {
                RefreshRallyVisual();
                return;
            }

            if (data.hasActiveProduction && Enum.TryParse(data.activeKind, out UnitKind parsedActive))
            {
                activeKind = parsedActive;
                activeDuration = Mathf.Max(0.1f, data.activeDuration);
                activeRemaining = Mathf.Clamp(data.activeRemaining, 0f, activeDuration);
            }

            if (data.queue != null)
            {
                for (int i = 0; i < data.queue.Count; i++)
                {
                    if (Enum.TryParse(data.queue[i], out UnitKind queuedKind))
                    {
                        queue.Add(queuedKind);
                    }
                }
            }

            if (data.hasRallyPoint)
            {
                RallyPoint = RtsGame.HasInstance ? RtsGame.Instance.ClampWorldPoint(data.rallyPoint.ToVector3()) : data.rallyPoint.ToVector3();
                HasRallyPoint = true;
                EnsureRallyVisual();
            }

            RefreshRallyVisual();
        }

        public void SetRallyPoint(Vector3 point)
        {
            if (!RtsGame.HasInstance)
            {
                return;
            }

            RallyPoint = RtsGame.Instance.ClampWorldPoint(point);
            HasRallyPoint = true;
            EnsureRallyVisual();
            RefreshRallyVisual();
        }

        public string GetQueueSummary()
        {
            if (activeKind == null)
            {
                return QueueCount > 0 ? QueueCount + " queued" : "Idle";
            }

            UnitStats stats = RtsBalance.GetUnit(activeKind.Value);
            float progress = activeDuration <= 0f ? 1f : 1f - Mathf.Clamp01(activeRemaining / activeDuration);
            return stats.Name + " " + Mathf.RoundToInt(progress * 100f) + "%";
        }

        protected override void OnDestroy()
        {
            if (rallyMarker != null)
            {
                Destroy(rallyMarker);
            }

            base.OnDestroy();
        }

        private void StartNextItem()
        {
            if (queue.Count <= 0)
            {
                return;
            }

            activeKind = queue[0];
            queue.RemoveAt(0);
            UnitStats stats = RtsBalance.GetUnit(activeKind.Value);
            activeDuration = Mathf.Max(0.1f, stats.BuildTime);
            activeRemaining = activeDuration;
        }

        private Vector3 GetSpawnPoint()
        {
            Vector3 forward = transform.forward.sqrMagnitude > 0.01f ? transform.forward : Vector3.forward;
            Vector3 point = transform.position + forward * (FootprintRadius + 2.2f);
            point.x += Random.Range(-0.8f, 0.8f);
            point.z += Random.Range(-0.8f, 0.8f);
            point.y = 0f;
            return point;
        }

        private void EnsureRallyVisual()
        {
            if (rallyLine == null)
            {
                GameObject lineObject = new GameObject("Rally Line");
                lineObject.transform.SetParent(transform, true);
                rallyLine = lineObject.AddComponent<LineRenderer>();
                rallyLine.useWorldSpace = true;
                rallyLine.positionCount = 2;
                rallyLine.widthMultiplier = 0.055f;
                rallyLine.material = RtsGame.CreateMaterial(new Color(0.4f, 0.9f, 1f, 0.8f));
            }

            if (rallyMarker == null)
            {
                rallyMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rallyMarker.name = "Rally Marker";
                rallyMarker.transform.SetParent(transform, true);
                rallyMarker.transform.localScale = new Vector3(0.9f, 0.035f, 0.9f);

                Collider markerCollider = rallyMarker.GetComponent<Collider>();
                if (markerCollider != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(markerCollider);
                    }
                    else
                    {
                        DestroyImmediate(markerCollider);
                    }
                }

                Renderer renderer = rallyMarker.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = RtsGame.CreateMaterial(new Color(0.35f, 0.95f, 1f, 0.85f));
                }
            }
        }

        private void RefreshRallyVisual()
        {
            bool visible = HasRallyPoint && IsSelected;

            if (rallyLine != null)
            {
                rallyLine.enabled = visible;
                if (visible)
                {
                    rallyLine.SetPosition(0, transform.position + Vector3.up * 0.1f);
                    rallyLine.SetPosition(1, RallyPoint + Vector3.up * 0.1f);
                }
            }

            if (rallyMarker != null)
            {
                rallyMarker.SetActive(visible);
                if (visible)
                {
                    rallyMarker.transform.position = RallyPoint + Vector3.up * 0.04f;
                }
            }
        }
    }

    public sealed class TurretStructure : RtsStructure
    {
        public float AttackRange = 11f;
        public float Damage = 22f;
        public float AttackCooldown = 0.9f;

        private float nextAttackTime;
        private Transform head;

        public void SetHead(Transform turretHead)
        {
            head = turretHead;
        }

        public override void Initialize(RtsTeam team, StructureKind kind)
        {
            base.Initialize(team, kind);

            switch (kind)
            {
                case StructureKind.GunTower:
                    AttackRange = 14f;
                    Damage = 34f;
                    AttackCooldown = 0.85f;
                    break;
                case StructureKind.AdvancedGunTower:
                    AttackRange = 18f;
                    Damage = 58f;
                    AttackCooldown = 1.2f;
                    break;
                default:
                    AttackRange = 11f;
                    Damage = 22f;
                    AttackCooldown = 0.9f;
                    break;
            }
        }

        private void Update()
        {
            if (!RtsGame.HasInstance || RtsGame.Instance.IsMatchOver || RtsGame.Instance.Clock.IsPaused || RtsGame.Instance.Clock.SimulationTime < nextAttackTime)
            {
                return;
            }

            RtsEntity target = RtsGame.Instance.FindClosestEnemy(Team, transform.position, AttackRange);
            if (target == null)
            {
                return;
            }

            nextAttackTime = RtsGame.Instance.Clock.SimulationTime + AttackCooldown;
            Vector3 aimPoint = target.GroundPosition + Vector3.up * 0.8f;

            if (head != null)
            {
                Vector3 flat = new Vector3(aimPoint.x - head.position.x, 0f, aimPoint.z - head.position.z);
                if (flat.sqrMagnitude > 0.01f)
                {
                    head.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
                }
            }

            target.TakeDamage(Damage, this);
            RtsGame.Instance.SpawnTracer(transform.position + Vector3.up * 1.4f, aimPoint, Team);
        }
    }
}
