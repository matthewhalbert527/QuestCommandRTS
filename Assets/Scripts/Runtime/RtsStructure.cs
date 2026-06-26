using System.Collections.Generic;
using UnityEngine;

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

        private readonly Queue<UnitKind> queue = new Queue<UnitKind>();
        private UnitKind? activeKind;
        private float activeRemaining;
        private float activeDuration;

        private void Update()
        {
            if (activeKind == null)
            {
                StartNextItem();
            }

            if (activeKind == null)
            {
                return;
            }

            float powerMultiplier = RtsGame.Instance.Resources.HasLowPower ? 0.45f : 1f;
            activeRemaining -= Time.deltaTime * powerMultiplier;

            if (activeRemaining <= 0f)
            {
                UnitKind completed = activeKind.Value;
                activeKind = null;
                RtsGame.Instance.CreateUnit(Team, completed, GetSpawnPoint());
                StartNextItem();
            }
        }

        public bool CanTrain(UnitKind kind)
        {
            switch (kind)
            {
                case UnitKind.Rifleman:
                    return StructureKind == StructureKind.Barracks || StructureKind == StructureKind.CommandCenter;
                case UnitKind.Harvester:
                    return StructureKind == StructureKind.Refinery || StructureKind == StructureKind.WarFactory || StructureKind == StructureKind.CommandCenter;
                case UnitKind.Tank:
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

            queue.Enqueue(kind);
            RtsGame.Instance.SpawnFloatingText(stats.Name, transform.position + Vector3.up * 2.2f, Color.white);
            return true;
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

        private void StartNextItem()
        {
            if (queue.Count <= 0)
            {
                return;
            }

            activeKind = queue.Dequeue();
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

        private void Update()
        {
            if (!RtsGame.HasInstance || Time.time < nextAttackTime)
            {
                return;
            }

            RtsEntity target = RtsGame.Instance.FindClosestEnemy(Team, transform.position, AttackRange);
            if (target == null)
            {
                return;
            }

            nextAttackTime = Time.time + AttackCooldown;
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
