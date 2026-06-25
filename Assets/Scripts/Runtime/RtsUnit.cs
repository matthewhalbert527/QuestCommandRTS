using System.Collections.Generic;
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
        protected Vector3 attackMoveDestination;
        protected bool hasDestination;
        protected bool hasAttackMoveDestination;
        protected RtsEntity attackTarget;
        protected MediumTankUnit boardingTarget;

        private float nextAttackTime;

        protected override void Awake()
        {
            base.Awake();
            destination = transform.position;
        }

        protected virtual void Update()
        {
            if (RtsGame.HasInstance && (RtsGame.Instance.IsMatchOver || RtsGame.Instance.Clock.IsPaused))
            {
                return;
            }

            TickOrders(GetDeltaTime());
        }

        public virtual void Initialize(RtsTeam team, UnitKind kind)
        {
            UnitKind = RtsBalance.NormalizeUnitKind(kind);
            UnitStats stats = RtsBalance.GetUnit(kind);
            MoveSpeed = stats.MoveSpeed;
            AttackRange = stats.AttackRange;
            Damage = stats.Damage;
            AttackCooldown = stats.AttackCooldown;

            float radius = GetSelectionRadius(UnitKind);
            Initialize(team, stats.Name, stats.Health, radius);
        }

        public virtual void IssueMove(Vector3 worldPosition)
        {
            boardingTarget = null;
            attackTarget = null;
            hasDestination = true;
            hasAttackMoveDestination = false;
            destination = ClampToMap(worldPosition);
        }

        public virtual void IssueAttack(RtsEntity target)
        {
            if (target == null || target.Team == Team || Damage <= 0f)
            {
                return;
            }

            boardingTarget = null;
            attackTarget = target;
            hasDestination = false;
            hasAttackMoveDestination = false;
        }

        public virtual void IssueAttackMove(Vector3 worldPosition)
        {
            boardingTarget = null;
            attackTarget = null;
            hasDestination = false;
            hasAttackMoveDestination = true;
            attackMoveDestination = ClampToMap(worldPosition);
        }

        public virtual bool CanBoardMediumTank(MediumTankUnit target)
        {
            return UnitKind == UnitKind.Rifleman && target != null && target.Team == Team && target.CanLoadPassenger(this);
        }

        public virtual void IssueBoardMediumTank(MediumTankUnit target)
        {
            if (!CanBoardMediumTank(target))
            {
                return;
            }

            boardingTarget = target;
            attackTarget = null;
            hasDestination = false;
            hasAttackMoveDestination = false;
        }

        public virtual void IssueStop()
        {
            boardingTarget = null;
            attackTarget = null;
            hasDestination = false;
            hasAttackMoveDestination = false;
            destination = transform.position;
            attackMoveDestination = transform.position;
        }

        public bool IsIdle()
        {
            return !hasDestination && !hasAttackMoveDestination && attackTarget == null;
        }

#if UNITY_EDITOR
        public void TickOrdersForTests(float deltaTime)
        {
            TickOrders(deltaTime);
        }
#endif

        public RtsUnitOrderSaveData CaptureOrderState()
        {
            RtsUnitOrderSaveData data = new RtsUnitOrderSaveData
            {
                orderType = "None",
                destination = new Vector3Data(hasAttackMoveDestination ? attackMoveDestination : destination)
            };

            if (attackTarget != null && attackTarget.IsAlive)
            {
                data.orderType = hasAttackMoveDestination ? "AttackMove" : "Attack";
                data.targetEntityId = attackTarget.PersistentId;
                return data;
            }

            if (boardingTarget != null && boardingTarget.IsAlive)
            {
                data.orderType = "Board";
                data.targetEntityId = boardingTarget.PersistentId;
                data.destination = new Vector3Data(boardingTarget.transform.position);
                return data;
            }

            if (hasDestination)
            {
                data.orderType = "Move";
                return data;
            }

            if (hasAttackMoveDestination)
            {
                data.orderType = "AttackMove";
            }

            return data;
        }

        public void RestoreOrderState(RtsUnitOrderSaveData data, Dictionary<int, RtsEntity> entityById)
        {
            attackTarget = null;
            boardingTarget = null;
            hasDestination = false;
            hasAttackMoveDestination = false;

            if (data == null)
            {
                return;
            }

            destination = ClampToMap(data.destination.ToVector3());
            if (data.orderType == "Move")
            {
                hasDestination = true;
                return;
            }

            if (data.orderType == "AttackMove")
            {
                attackMoveDestination = ClampToMap(data.destination.ToVector3());
                hasAttackMoveDestination = true;
                if (entityById != null && entityById.TryGetValue(data.targetEntityId, out RtsEntity attackMoveTarget))
                {
                    attackTarget = attackMoveTarget;
                }

                return;
            }

            if (data.orderType == "Board" && entityById != null && entityById.TryGetValue(data.targetEntityId, out RtsEntity boardTarget))
            {
                MediumTankUnit mediumTank = boardTarget as MediumTankUnit;
                if (mediumTank != null)
                {
                    IssueBoardMediumTank(mediumTank);
                }

                return;
            }

            if (data.orderType == "Attack" && entityById != null && entityById.TryGetValue(data.targetEntityId, out RtsEntity target))
            {
                IssueAttack(target);
            }
        }

        protected void TickOrders(float deltaTime)
        {
            if (attackTarget != null && (!attackTarget.IsAlive || attackTarget.Team == Team))
            {
                attackTarget = null;
            }

            if (boardingTarget != null)
            {
                TickBoardOrder(deltaTime);
                return;
            }

            if (attackTarget != null)
            {
                TickAttackOrder(deltaTime);
                return;
            }

            if (hasAttackMoveDestination)
            {
                RtsEntity acquired = Damage > 0f && RtsGame.HasInstance ? RtsGame.Instance.FindClosestEnemy(Team, transform.position, Mathf.Max(AttackRange * 1.25f, 8f)) : null;
                if (acquired != null)
                {
                    attackTarget = acquired;
                    TickAttackOrder(deltaTime);
                    return;
                }

                if (MoveToward(attackMoveDestination, deltaTime, StopDistance))
                {
                    hasAttackMoveDestination = false;
                }

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

        private void TickBoardOrder(float deltaTime)
        {
            if (boardingTarget == null || !boardingTarget.IsAlive || !CanBoardMediumTank(boardingTarget))
            {
                boardingTarget = null;
                return;
            }

            float distance = PlanarDistance(transform.position, boardingTarget.transform.position);
            if (distance > 1.35f)
            {
                MoveToward(boardingTarget.transform.position, deltaTime, 1.1f);
                return;
            }

            MediumTankUnit target = boardingTarget;
            boardingTarget = null;
            target.TryLoadPassenger(this);
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

            float currentTime = GetSimulationTime();
            if (currentTime >= nextAttackTime)
            {
                nextAttackTime = currentTime + Mathf.Max(0.15f, AttackCooldown);
                attackTarget.TakeDamage(Damage, this);
                RtsGame.Instance.SpawnTracer(GroundPosition + Vector3.up * 0.8f, attackTarget.GroundPosition + Vector3.up * 0.8f, Team);
                OnPrimaryAttackFired(attackTarget);
            }
        }

        protected virtual void OnPrimaryAttackFired(RtsEntity target)
        {
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

        protected static float GetDeltaTime()
        {
            return RtsGame.HasInstance ? RtsGame.Instance.Clock.DeltaTime : Time.deltaTime;
        }

        protected static float GetSimulationTime()
        {
            return RtsGame.HasInstance ? RtsGame.Instance.Clock.SimulationTime : Time.time;
        }

        private static float GetSelectionRadius(UnitKind kind)
        {
            switch (RtsBalance.NormalizeUnitKind(kind))
            {
                case UnitKind.LightTank:
                    return 1.0f;
                case UnitKind.MediumTank:
                    return 1.15f;
                case UnitKind.HeavyTank:
                    return 1.45f;
                default:
                    return 0.72f;
            }
        }
    }

    public sealed class MediumTankUnit : RtsUnit
    {
        public const int PassengerCapacity = 1;
        public int LoadedRiflemen { get; private set; }
        public bool HasPassenger => LoadedRiflemen > 0;

        private Transform passengerIndicator;

        public bool CanLoadPassenger(RtsUnit passenger)
        {
            return passenger != null &&
                passenger.IsAlive &&
                passenger.Team == Team &&
                passenger.UnitKind == UnitKind.Rifleman &&
                LoadedRiflemen < PassengerCapacity;
        }

        public bool TryLoadPassenger(RtsUnit passenger)
        {
            if (!CanLoadPassenger(passenger))
            {
                return false;
            }

            LoadedRiflemen++;
            RefreshPassengerIndicator();

            if (RtsGame.HasInstance)
            {
                RtsGame.Instance.SpawnFloatingText("Rifleman loaded", GroundPosition + Vector3.up * 2.2f, new Color(0.55f, 0.95f, 1f));
                RtsGame.Instance.UnregisterEntity(passenger);
            }

            if (Application.isPlaying)
            {
                Destroy(passenger.gameObject);
            }
            else
            {
                DestroyImmediate(passenger.gameObject);
            }

            return true;
        }

        public void RestoreLoadedRiflemen(int count)
        {
            LoadedRiflemen = Mathf.Clamp(count, 0, PassengerCapacity);
            RefreshPassengerIndicator();
        }

        protected override void OnPrimaryAttackFired(RtsEntity target)
        {
            if (LoadedRiflemen <= 0 || target == null || !target.IsAlive)
            {
                return;
            }

            float passengerDamage = RtsBalance.GetUnit(UnitKind.Rifleman).Damage * LoadedRiflemen;
            target.TakeDamage(passengerDamage, this);

            if (RtsGame.HasInstance)
            {
                Vector3 muzzle = GroundPosition + transform.TransformDirection(new Vector3(0.62f, 1.15f, 0.35f));
                RtsGame.Instance.SpawnTracer(muzzle, target.GroundPosition + Vector3.up * 0.75f, Team);
            }
        }

        private void RefreshPassengerIndicator()
        {
            if (LoadedRiflemen <= 0)
            {
                if (passengerIndicator != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(passengerIndicator.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(passengerIndicator.gameObject);
                    }

                    passengerIndicator = null;
                }

                return;
            }

            if (passengerIndicator != null)
            {
                return;
            }

            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            indicator.name = "Loaded Rifleman Indicator";
            indicator.transform.SetParent(transform, false);
            indicator.transform.localPosition = new Vector3(0.62f, 1.24f, -0.18f);
            indicator.transform.localScale = new Vector3(0.18f, 0.34f, 0.18f);
            indicator.GetComponent<Renderer>().sharedMaterial = RtsGame.CreateMaterial(new Color(0.62f, 0.95f, 1f));

            Collider collider = indicator.GetComponent<Collider>();
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

            passengerIndicator = indicator.transform;
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

        public override void IssueAttackMove(Vector3 worldPosition)
        {
            state = HarvestState.Idle;
            targetNode = null;
            homeRefinery = null;
            base.IssueAttackMove(worldPosition);
        }

        public override void IssueStop()
        {
            state = HarvestState.Idle;
            targetNode = null;
            homeRefinery = null;
            harvestAccumulator = 0f;
            base.IssueStop();
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
            if (RtsGame.HasInstance && (RtsGame.Instance.IsMatchOver || RtsGame.Instance.Clock.IsPaused))
            {
                return;
            }

            float deltaTime = GetDeltaTime();
            switch (state)
            {
                case HarvestState.MovingToResource:
                    TickMovingToResource(deltaTime);
                    break;
                case HarvestState.Harvesting:
                    TickHarvesting(deltaTime);
                    break;
                case HarvestState.Returning:
                    TickReturning(deltaTime);
                    break;
                default:
                    base.Update();
                    break;
            }
        }

        public RtsHarvesterSaveData CaptureHarvesterState()
        {
            return new RtsHarvesterSaveData
            {
                state = (int)state,
                cargo = Cargo,
                targetResourceNodeId = targetNode != null ? targetNode.PersistentId : 0,
                homeRefineryEntityId = homeRefinery != null ? homeRefinery.PersistentId : 0,
                harvestAccumulator = harvestAccumulator
            };
        }

        public void RestoreHarvesterState(RtsHarvesterSaveData data, Dictionary<int, ResourceNode> resourceById, Dictionary<int, RtsEntity> entityById)
        {
            if (data == null)
            {
                return;
            }

            Cargo = Mathf.Clamp(data.cargo, 0, CargoCapacity);
            state = (HarvestState)Mathf.Clamp(data.state, 0, 3);
            harvestAccumulator = Mathf.Max(0f, data.harvestAccumulator);
            targetNode = resourceById != null && resourceById.TryGetValue(data.targetResourceNodeId, out ResourceNode node) ? node : null;
            homeRefinery = null;
            if (entityById != null && entityById.TryGetValue(data.homeRefineryEntityId, out RtsEntity entity))
            {
                homeRefinery = entity as RefineryStructure;
            }

            if (state != HarvestState.Idle)
            {
                hasDestination = false;
                attackTarget = null;
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
