using UnityEngine;

namespace QuestCommandRTS
{
    public class RtsUnit : RtsEntity
    {
        public UnitKind UnitKind = UnitKind.Rifleman;
        public float MoveSpeed = 4f;
        public float TurnSpeed = 540f;
        public float StopDistance = 0.3f;
        public float AttackRange = 6f;
        public float Damage = 8f;
        public float AttackCooldown = 0.8f;

        protected Vector3 destination;
        protected bool hasDestination;
        protected RtsEntity attackTarget;

        private float nextAttackTime;

        protected override void Awake()
        {
            base.Awake();
            destination = transform.position;
        }

        protected virtual void Update()
        {
            TickOrders(Time.deltaTime);
        }

        public virtual void Initialize(RtsTeam team, UnitKind kind)
        {
            UnitKind = kind;
            UnitStats stats = RtsBalance.GetUnit(kind);
            MoveSpeed = stats.MoveSpeed;
            AttackRange = stats.AttackRange;
            Damage = stats.Damage;
            AttackCooldown = stats.AttackCooldown;

            float radius = kind == UnitKind.Tank ? 1.1f : 0.72f;
            Initialize(team, stats.Name, stats.Health, radius);
        }

        public virtual void IssueMove(Vector3 worldPosition)
        {
            attackTarget = null;
            hasDestination = true;
            destination = ClampToMap(worldPosition);
        }

        public virtual void IssueAttack(RtsEntity target)
        {
            if (target == null || target.Team == Team || Damage <= 0f)
            {
                return;
            }

            attackTarget = target;
            hasDestination = false;
        }

        public bool IsIdle()
        {
            return !hasDestination && attackTarget == null;
        }

        protected void TickOrders(float deltaTime)
        {
            if (attackTarget != null && (!attackTarget.IsAlive || attackTarget.Team == Team))
            {
                attackTarget = null;
            }

            if (attackTarget != null)
            {
                TickAttackOrder(deltaTime);
                return;
            }

            if (hasDestination)
            {
                if (MoveToward(destination, deltaTime, StopDistance))
                {
                    hasDestination = false;
                }
            }
        }

        protected bool MoveToward(Vector3 targetPosition, float deltaTime, float stoppingDistance)
        {
            Vector3 target = ClampToMap(targetPosition);
            Vector3 current = transform.position;
            Vector3 flatDelta = new Vector3(target.x - current.x, 0f, target.z - current.z);
            float distance = flatDelta.magnitude;

            if (distance <= Mathf.Max(0.05f, stoppingDistance))
            {
                return true;
            }

            Vector3 direction = flatDelta / distance;
            Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, TurnSpeed * deltaTime);

            float step = Mathf.Min(distance - stoppingDistance, MoveSpeed * deltaTime);
            transform.position = current + direction * Mathf.Max(0f, step);
            return false;
        }

        private void TickAttackOrder(float deltaTime)
        {
            float distance = PlanarDistance(transform.position, attackTarget.transform.position);
            float desiredRange = Mathf.Max(1.2f, AttackRange * 0.88f);

            if (distance > desiredRange)
            {
                MoveToward(attackTarget.transform.position, deltaTime, desiredRange);
                return;
            }

            FacePoint(attackTarget.transform.position, deltaTime);

            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + Mathf.Max(0.15f, AttackCooldown);
                attackTarget.TakeDamage(Damage, this);
                RtsGame.Instance.SpawnTracer(GroundPosition + Vector3.up * 0.8f, attackTarget.GroundPosition + Vector3.up * 0.8f, Team);
            }
        }

        protected void FacePoint(Vector3 point, float deltaTime)
        {
            Vector3 direction = new Vector3(point.x - transform.position.x, 0f, point.z - transform.position.z);
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, TurnSpeed * deltaTime);
        }

        protected static float PlanarDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        protected static Vector3 ClampToMap(Vector3 position)
        {
            float limit = RtsBalance.MapHalfSize - 1f;
            return new Vector3(Mathf.Clamp(position.x, -limit, limit), 0f, Mathf.Clamp(position.z, -limit, limit));
        }
    }

    public sealed class HarvesterUnit : RtsUnit
    {
        public int Cargo { get; private set; }
        public int CargoCapacity = 700;
        public float HarvestRatePerSecond = 170f;

        private enum HarvestState
        {
            Idle,
            MovingToResource,
            Harvesting,
            Returning
        }

        private HarvestState state;
        private ResourceNode targetNode;
        private RefineryStructure homeRefinery;
        private float harvestAccumulator;

        public override void IssueMove(Vector3 worldPosition)
        {
            state = HarvestState.Idle;
            targetNode = null;
            base.IssueMove(worldPosition);
        }

        public void IssueHarvest(ResourceNode node, RefineryStructure refinery)
        {
            if (node == null || refinery == null)
            {
                return;
            }

            targetNode = node;
            homeRefinery = refinery;
            state = HarvestState.MovingToResource;
            hasDestination = false;
            attackTarget = null;
        }

        protected override void Update()
        {
            switch (state)
            {
                case HarvestState.MovingToResource:
                    TickMovingToResource(Time.deltaTime);
                    break;
                case HarvestState.Harvesting:
                    TickHarvesting(Time.deltaTime);
                    break;
                case HarvestState.Returning:
                    TickReturning(Time.deltaTime);
                    break;
                default:
                    base.Update();
                    break;
            }
        }

        private void TickMovingToResource(float deltaTime)
        {
            if (targetNode == null || targetNode.IsDepleted)
            {
                state = HarvestState.Idle;
                return;
            }

            if (MoveToward(targetNode.transform.position, deltaTime, 1.6f))
            {
                harvestAccumulator = 0f;
                state = HarvestState.Harvesting;
            }
        }

        private void TickHarvesting(float deltaTime)
        {
            if (targetNode == null || targetNode.IsDepleted || Cargo >= CargoCapacity)
            {
                state = HarvestState.Returning;
                return;
            }

            FacePoint(targetNode.transform.position, deltaTime);
            harvestAccumulator += HarvestRatePerSecond * deltaTime;
            int amount = Mathf.FloorToInt(harvestAccumulator);

            if (amount <= 0)
            {
                return;
            }

            harvestAccumulator -= amount;
            Cargo += targetNode.Harvest(Mathf.Min(amount, CargoCapacity - Cargo));

            if (Cargo >= CargoCapacity || targetNode.IsDepleted)
            {
                state = HarvestState.Returning;
            }
        }

        private void TickReturning(float deltaTime)
        {
            if (homeRefinery == null || !homeRefinery.IsAlive)
            {
                homeRefinery = RtsGame.Instance.FindNearestPlayerRefinery(transform.position);
                if (homeRefinery == null)
                {
                    state = HarvestState.Idle;
                    return;
                }
            }

            if (!MoveToward(homeRefinery.transform.position, deltaTime, homeRefinery.FootprintRadius + 1.2f))
            {
                return;
            }

            if (Cargo > 0)
            {
                RtsGame.Instance.Resources.Add(Cargo);
                RtsGame.Instance.SpawnFloatingText("+" + Cargo, transform.position + Vector3.up * 1.5f, new Color(0.55f, 1f, 0.55f));
                Cargo = 0;
            }

            if (targetNode != null && !targetNode.IsDepleted)
            {
                state = HarvestState.MovingToResource;
            }
            else
            {
                state = HarvestState.Idle;
            }
        }
    }
}
