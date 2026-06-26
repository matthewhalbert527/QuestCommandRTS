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
        public float SightRange = 10f;

        protected Vector3 destination;
        protected Vector3 attackMoveDestination;
        protected bool hasDestination;
        protected bool hasAttackMoveDestination;
        protected RtsEntity attackTarget;
        protected MediumTankUnit boardingTarget;
        protected RtsEntity repairTarget;
        public RtsEntity CurrentAttackTargetForVisuals => attackTarget != null && attackTarget.IsAlive ? attackTarget : null;
        public float BlockingRadius => GetBlockingRadius(UnitKind);

        private RtsUnitVisualAnimator visualAnimator;
        private float nextAttackTime;
        private float nextAwarenessScanTime;
        private const float AwarenessScanInterval = 0.35f;

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
            SightRange = GetSightRange(UnitKind, AttackRange, Damage);
            visualAnimator = GetComponent<RtsUnitVisualAnimator>();

            float radius = GetSelectionRadius(UnitKind);
            Initialize(team, stats.Name, stats.Health, radius);
        }

        public virtual void IssueMove(Vector3 worldPosition)
        {
            repairTarget = null;
            boardingTarget = null;
            if (!CanMoveWhileAttacking())
            {
                attackTarget = null;
            }

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

            repairTarget = null;
            boardingTarget = null;
            attackTarget = target;
            hasDestination = false;
            hasAttackMoveDestination = false;
        }

        public virtual void IssueAttackMove(Vector3 worldPosition)
        {
            repairTarget = null;
            boardingTarget = null;
            attackTarget = null;
            hasDestination = false;
            hasAttackMoveDestination = true;
            attackMoveDestination = ClampToMap(worldPosition);
        }

        public virtual bool CanBoardMediumTank(MediumTankUnit target)
        {
            return RtsBalance.IsInfantry(UnitKind) && target != null && target.Team == Team && target.CanLoadPassenger(this);
        }

        public virtual void IssueBoardMediumTank(MediumTankUnit target)
        {
            if (!CanBoardMediumTank(target))
            {
                return;
            }

            boardingTarget = target;
            repairTarget = null;
            attackTarget = null;
            hasDestination = false;
            hasAttackMoveDestination = false;
        }

        public virtual bool CanRepairTarget(RtsEntity target)
        {
            return false;
        }

        public virtual void IssueRepair(RtsEntity target)
        {
            if (!CanRepairTarget(target))
            {
                return;
            }

            repairTarget = target;
            boardingTarget = null;
            attackTarget = null;
            hasDestination = false;
            hasAttackMoveDestination = false;
        }

        public virtual void IssueStop()
        {
            repairTarget = null;
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

            if (repairTarget != null && repairTarget.IsAlive)
            {
                data.orderType = "Repair";
                data.targetEntityId = repairTarget.PersistentId;
                data.destination = new Vector3Data(repairTarget.transform.position);
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
            repairTarget = null;
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

            if (data.orderType == "Repair" && entityById != null && entityById.TryGetValue(data.targetEntityId, out RtsEntity repairEntity))
            {
                IssueRepair(repairEntity);
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

            if (repairTarget != null)
            {
                TickRepairOrder(deltaTime);
                return;
            }

            if (boardingTarget != null)
            {
                TickBoardOrder(deltaTime);
                return;
            }

            if (attackTarget != null)
            {
                if (CanMoveWhileAttacking() && (hasDestination || hasAttackMoveDestination))
                {
                    TickAttackOrder(deltaTime, true);
                    if (hasDestination && MoveToward(destination, deltaTime, StopDistance))
                    {
                        hasDestination = false;
                    }
                    else if (hasAttackMoveDestination && MoveToward(attackMoveDestination, deltaTime, StopDistance))
                    {
                        hasAttackMoveDestination = false;
                    }

                    return;
                }

                TickAttackOrder(deltaTime, false);
                return;
            }

            if (hasAttackMoveDestination)
            {
                RtsEntity acquired = Damage > 0f && RtsGame.HasInstance ? RtsGame.Instance.FindClosestEnemy(Team, transform.position, Mathf.Max(AttackRange * 1.25f, 8f)) : null;
                if (acquired != null)
                {
                    attackTarget = acquired;
                    if (CanMoveWhileAttacking())
                    {
                        TickAttackOrder(deltaTime, true);
                        if (MoveToward(attackMoveDestination, deltaTime, StopDistance))
                        {
                            hasAttackMoveDestination = false;
                        }

                        return;
                    }

                    TickAttackOrder(deltaTime, false);
                    return;
                }

                if (MoveToward(attackMoveDestination, deltaTime, StopDistance))
                {
                    hasAttackMoveDestination = false;
                }

                return;
            }

            if (!hasDestination && TryAcquireNearbyEnemy())
            {
                TickAttackOrder(deltaTime, false);
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

        protected override void OnDamaged(float amount, RtsEntity attacker)
        {
            if (!CanAutoEngage(attacker))
            {
                return;
            }

            if (attackTarget != null && attackTarget.IsAlive)
            {
                return;
            }

            attackTarget = attacker;
        }

        private bool TryAcquireNearbyEnemy()
        {
            if (!CanAutoEngage())
            {
                return false;
            }

            float currentTime = GetSimulationTime();
            if (currentTime < nextAwarenessScanTime)
            {
                return false;
            }

            nextAwarenessScanTime = currentTime + AwarenessScanInterval;
            RtsEntity acquired = RtsGame.Instance.FindClosestEnemy(Team, transform.position, SightRange);
            if (acquired == null)
            {
                return false;
            }

            attackTarget = acquired;
            return true;
        }

        private bool CanAutoEngage()
        {
            return Damage > 0f &&
                RtsGame.HasInstance &&
                IsAlive &&
                Team != RtsTeam.Neutral &&
                UnitKind != UnitKind.Harvester &&
                !RtsBalance.IsEngineer(UnitKind);
        }

        private bool CanAutoEngage(RtsEntity target)
        {
            return CanAutoEngage() &&
                target != null &&
                target.IsAlive &&
                target.Team != Team &&
                target.Team != RtsTeam.Neutral;
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

        private void TickRepairOrder(float deltaTime)
        {
            if (repairTarget == null || !CanRepairTarget(repairTarget))
            {
                repairTarget = null;
                return;
            }

            float distance = PlanarDistance(transform.position, repairTarget.transform.position);
            if (distance > 1.8f)
            {
                MoveToward(repairTarget.transform.position, deltaTime, 1.45f);
                return;
            }

            FacePoint(repairTarget.transform.position, deltaTime);
            OnRepairTick(repairTarget, deltaTime);

            if (repairTarget == null || repairTarget.Health >= repairTarget.MaxHealth - 0.01f)
            {
                repairTarget = null;
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
            Vector3 desiredPosition = current + direction * Mathf.Max(0f, step);
            transform.position = ResolveUnitBlockedPosition(current, desiredPosition, direction);
            return false;
        }

        private Vector3 ResolveUnitBlockedPosition(Vector3 current, Vector3 desired, Vector3 moveDirection)
        {
            desired = ClampToMap(desired);
            if (!RtsGame.HasInstance)
            {
                return desired;
            }

            float selfRadius = BlockingRadius;
            Vector3 sidestep = Vector3.zero;
            bool forwardBlocked = false;

            IReadOnlyList<RtsEntity> entities = RtsGame.Instance.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                RtsUnit other = entities[i] as RtsUnit;
                if (!IsBlockingUnit(other))
                {
                    continue;
                }

                float minimumDistance = selfRadius + other.BlockingRadius;
                if (WouldCrossBlockingRadius(current, desired, other.GroundPosition, minimumDistance))
                {
                    forwardBlocked = true;
                    sidestep += GetSidestepDirection(other, moveDirection);
                }
            }

            if (forwardBlocked)
            {
                if (sidestep.sqrMagnitude > 0.001f)
                {
                    float sideStepDistance = Mathf.Min(Vector3.Distance(current, desired) * 0.85f, selfRadius * 0.75f);
                    Vector3 sideCandidate = ClampToMap(current + sidestep.normalized * sideStepDistance);
                    if (IsUnitPositionClear(sideCandidate, selfRadius))
                    {
                        return sideCandidate;
                    }
                }

                return PushOutOfUnitOverlaps(current, selfRadius);
            }

            return PushOutOfUnitOverlaps(desired, selfRadius);
        }

        private Vector3 PushOutOfUnitOverlaps(Vector3 candidate, float selfRadius)
        {
            IReadOnlyList<RtsEntity> entities = RtsGame.Instance.Entities;
            for (int iteration = 0; iteration < 2; iteration++)
            {
                for (int i = 0; i < entities.Count; i++)
                {
                    RtsUnit other = entities[i] as RtsUnit;
                    if (!IsBlockingUnit(other))
                    {
                        continue;
                    }

                    float minimumDistance = selfRadius + other.BlockingRadius;
                    Vector3 delta = candidate - other.GroundPosition;
                    delta.y = 0f;
                    float distance = delta.magnitude;
                    if (distance >= minimumDistance || minimumDistance <= 0f)
                    {
                        continue;
                    }

                    Vector3 away = distance > 0.001f ? delta / distance : GetSidestepDirection(other, transform.forward);
                    candidate += away * (minimumDistance - distance + 0.015f);
                    candidate = ClampToMap(candidate);
                }
            }

            return candidate;
        }

        private bool IsUnitPositionClear(Vector3 candidate, float selfRadius)
        {
            IReadOnlyList<RtsEntity> entities = RtsGame.Instance.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                RtsUnit other = entities[i] as RtsUnit;
                if (!IsBlockingUnit(other))
                {
                    continue;
                }

                float minimumDistance = selfRadius + other.BlockingRadius;
                if (PlanarDistance(candidate, other.GroundPosition) < minimumDistance - 0.02f)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsBlockingUnit(RtsUnit other)
        {
            return other != null && other != this && other.IsAlive && other.gameObject.activeInHierarchy;
        }

        private static bool WouldCrossBlockingRadius(Vector3 from, Vector3 to, Vector3 blockerPosition, float minimumDistance)
        {
            Vector3 start = new Vector3(from.x, 0f, from.z);
            Vector3 end = new Vector3(to.x, 0f, to.z);
            Vector3 blocker = new Vector3(blockerPosition.x, 0f, blockerPosition.z);
            Vector3 segment = end - start;
            float lengthSqr = segment.sqrMagnitude;
            if (lengthSqr < 0.0001f)
            {
                return false;
            }

            float startDistance = Vector3.Distance(start, blocker);
            if (startDistance < minimumDistance * 0.92f)
            {
                return false;
            }

            float t = Mathf.Clamp01(Vector3.Dot(blocker - start, segment) / lengthSqr);
            if (t <= 0.01f || t >= 0.99f)
            {
                return false;
            }

            Vector3 closest = start + segment * t;
            return Vector3.Distance(closest, blocker) < minimumDistance;
        }

        private Vector3 GetSidestepDirection(RtsUnit other, Vector3 moveDirection)
        {
            Vector3 tangent = Vector3.Cross(Vector3.up, moveDirection);
            if (tangent.sqrMagnitude < 0.001f)
            {
                tangent = transform.right;
            }

            int selfId = PersistentId != 0 ? PersistentId : GetInstanceID();
            int otherId = other != null && other.PersistentId != 0 ? other.PersistentId : other != null ? other.GetInstanceID() : 0;
            return tangent.normalized * (((selfId + otherId) & 1) == 0 ? 1f : -1f);
        }

        private void TickAttackOrder(float deltaTime, bool allowIndependentMovement)
        {
            float distance = PlanarDistance(transform.position, attackTarget.transform.position);
            float desiredRange = Mathf.Max(1.2f, AttackRange * (RtsBalance.IsTank(UnitKind) ? 0.98f : 0.88f));

            if (distance > desiredRange)
            {
                if (!allowIndependentMovement)
                {
                    MoveToward(attackTarget.transform.position, deltaTime, desiredRange);
                }

                return;
            }

            if (RtsBalance.IsTank(UnitKind))
            {
                if (!IsTurretReadyToFireAt(attackTarget, deltaTime))
                {
                    return;
                }
            }
            else
            {
                FacePoint(attackTarget.transform.position, deltaTime);
            }

            float currentTime = GetSimulationTime();
            if (currentTime >= nextAttackTime)
            {
                nextAttackTime = currentTime + Mathf.Max(0.15f, AttackCooldown);
                FirePrimaryWeapon(attackTarget);
                OnPrimaryAttackFired(attackTarget);
            }
        }

        protected virtual void OnPrimaryAttackFired(RtsEntity target)
        {
        }

        private void FirePrimaryWeapon(RtsEntity target)
        {
            if (target == null || !RtsGame.HasInstance)
            {
                return;
            }

            RtsGame.Instance.SpawnProjectile(
                GetProjectileKind(),
                Team,
                this,
                target,
                GetWeaponMuzzlePoint(),
                GetProjectileDirectDamage(target),
                GetProjectileSplashRadius(),
                GetProjectileSplashDamage());
        }

        private bool IsTurretReadyToFireAt(RtsEntity target, float deltaTime)
        {
            if (visualAnimator == null)
            {
                visualAnimator = GetComponent<RtsUnitVisualAnimator>();
            }

            return visualAnimator == null || visualAnimator.AimTurretAt(target.GroundPosition, deltaTime);
        }

        private bool CanMoveWhileAttacking()
        {
            return RtsBalance.IsTank(UnitKind);
        }

        private Vector3 GetWeaponMuzzlePoint()
        {
            if (RtsBalance.IsTank(UnitKind))
            {
                if (visualAnimator == null)
                {
                    visualAnimator = GetComponent<RtsUnitVisualAnimator>();
                }

                if (visualAnimator != null)
                {
                    return visualAnimator.GetTurretMuzzleWorldPosition();
                }
            }

            switch (UnitKind)
            {
                case UnitKind.Grenadier:
                    return transform.TransformPoint(new Vector3(0.34f, 1.12f, 0.24f));
                case UnitKind.RocketSoldier:
                    return transform.TransformPoint(new Vector3(0.38f, 1.18f, 0.58f));
                case UnitKind.FlameTrooper:
                    return transform.TransformPoint(new Vector3(0.36f, 1.02f, 0.46f));
                default:
                    return transform.TransformPoint(new Vector3(0.34f, 1.05f, 0.48f));
            }
        }

        private RtsProjectileKind GetProjectileKind()
        {
            switch (UnitKind)
            {
                case UnitKind.Grenadier:
                    return RtsProjectileKind.Grenade;
                case UnitKind.RocketSoldier:
                    return RtsProjectileKind.Rocket;
                case UnitKind.FlameTrooper:
                    return RtsProjectileKind.FlameBolt;
                case UnitKind.LightTank:
                case UnitKind.MediumTank:
                case UnitKind.HeavyTank:
                case UnitKind.Tank:
                    return RtsProjectileKind.TankShell;
                default:
                    return RtsProjectileKind.RifleRound;
            }
        }

        private float GetProjectileDirectDamage(RtsEntity target)
        {
            if (UnitKind == UnitKind.RocketSoldier && target != null && (target is RtsStructure || IsArmoredTarget(target)))
            {
                return Damage * 1.45f;
            }

            return Damage;
        }

        private float GetProjectileSplashRadius()
        {
            switch (UnitKind)
            {
                case UnitKind.Grenadier:
                    return 2.35f;
                case UnitKind.FlameTrooper:
                    return 1.8f;
                default:
                    return 0f;
            }
        }

        private float GetProjectileSplashDamage()
        {
            switch (UnitKind)
            {
                case UnitKind.Grenadier:
                    return Damage * 0.55f;
                case UnitKind.FlameTrooper:
                    return Damage * 0.45f;
                default:
                    return 0f;
            }
        }

        protected virtual void OnRepairTick(RtsEntity target, float deltaTime)
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

        private static float GetBlockingRadius(UnitKind kind)
        {
            switch (RtsBalance.NormalizeUnitKind(kind))
            {
                case UnitKind.LightTank:
                    return 0.88f;
                case UnitKind.MediumTank:
                case UnitKind.Tank:
                    return 1.02f;
                case UnitKind.HeavyTank:
                    return 1.28f;
                case UnitKind.Harvester:
                    return 1.08f;
                default:
                    return 0.54f;
            }
        }

        private static float GetSightRange(UnitKind kind, float attackRange, float damage)
        {
            if (damage <= 0f || kind == UnitKind.Harvester || RtsBalance.IsEngineer(kind))
            {
                return 0f;
            }

            switch (RtsBalance.NormalizeUnitKind(kind))
            {
                case UnitKind.RocketSoldier:
                    return Mathf.Max(15f, attackRange * 1.35f);
                case UnitKind.HeavyTank:
                    return Mathf.Max(14f, attackRange * 1.3f);
                case UnitKind.MediumTank:
                case UnitKind.Tank:
                    return Mathf.Max(13f, attackRange * 1.35f);
                case UnitKind.LightTank:
                    return Mathf.Max(12f, attackRange * 1.45f);
                default:
                    return Mathf.Max(10f, attackRange * 1.45f);
            }
        }

        private static bool IsArmoredTarget(RtsEntity target)
        {
            RtsUnit unit = target as RtsUnit;
            return unit != null && RtsBalance.IsTank(unit.UnitKind);
        }
    }

    public sealed class EngineerUnit : RtsUnit
    {
        private const float RepairRatePerSecond = 34f;
        private float nextRepairTextTime;

        public override bool CanRepairTarget(RtsEntity target)
        {
            return UnitKind == UnitKind.Engineer &&
                target != null &&
                target != this &&
                target.IsAlive &&
                target.Team == Team &&
                target.Health < target.MaxHealth - 0.01f;
        }

        protected override void OnRepairTick(RtsEntity target, float deltaTime)
        {
            target.Repair(RepairRatePerSecond * deltaTime);

            if (RtsGame.HasInstance && GetSimulationTime() >= nextRepairTextTime)
            {
                nextRepairTextTime = GetSimulationTime() + 1.2f;
                RtsGame.Instance.SpawnFloatingText("Repair", target.GroundPosition + Vector3.up * 2.1f, new Color(0.5f, 1f, 0.78f));
            }
        }
    }

    public sealed class MediumTankUnit : RtsUnit
    {
        public const int PassengerCapacity = 1;
        public int LoadedRiflemen { get; private set; }
        public bool HasPassenger => LoadedRiflemen > 0;
        public UnitKind LoadedPassengerKind { get; private set; } = UnitKind.Rifleman;

        private Transform passengerIndicator;

        public bool CanLoadPassenger(RtsUnit passenger)
        {
            return passenger != null &&
                passenger.IsAlive &&
                passenger.Team == Team &&
                RtsBalance.IsInfantry(passenger.UnitKind) &&
                LoadedRiflemen < PassengerCapacity;
        }

        public bool TryLoadPassenger(RtsUnit passenger)
        {
            if (!CanLoadPassenger(passenger))
            {
                return false;
            }

            LoadedRiflemen++;
            LoadedPassengerKind = passenger.UnitKind;
            RefreshPassengerIndicator();

            if (RtsGame.HasInstance)
            {
                RtsGame.Instance.SpawnFloatingText(RtsBalance.GetUnit(LoadedPassengerKind).Name + " loaded", GroundPosition + Vector3.up * 2.2f, new Color(0.55f, 0.95f, 1f));
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
            RestorePassenger(UnitKind.Rifleman, count);
        }

        public void RestorePassenger(UnitKind kind, int count)
        {
            LoadedPassengerKind = RtsBalance.IsInfantry(kind) ? kind : UnitKind.Rifleman;
            LoadedRiflemen = Mathf.Clamp(count, 0, PassengerCapacity);
            RefreshPassengerIndicator();
        }

        protected override void OnPrimaryAttackFired(RtsEntity target)
        {
            if (LoadedRiflemen <= 0 || target == null || !target.IsAlive || !RtsGame.HasInstance)
            {
                return;
            }

            float passengerDamage = RtsBalance.GetUnit(LoadedPassengerKind).Damage * LoadedRiflemen;
            if (passengerDamage <= 0f)
            {
                return;
            }

            Vector3 muzzle = GroundPosition + transform.TransformDirection(new Vector3(0.62f, 1.15f, 0.35f));
            RtsGame.Instance.SpawnProjectile(RtsProjectileKind.RifleRound, Team, this, target, muzzle, passengerDamage, 0f, 0f);
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
            indicator.name = "Loaded Infantry Indicator";
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
        public float CargoFill01 => CargoCapacity <= 0 ? 0f : Mathf.Clamp01((float)Cargo / CargoCapacity);
        public bool IsHarvestingForVisuals => state == HarvestState.Harvesting && targetNode != null && !targetNode.IsDepleted && Cargo < CargoCapacity;

        private enum HarvestState
        {
            Idle,
            MovingToResource,
            Harvesting,
            Returning,
            ExitingProduction
        }

        private HarvestState state;
        private ResourceNode targetNode;
        private RefineryStructure homeRefinery;
        private Vector3 productionExitPoint;
        private float harvestAccumulator;

        public override void IssueMove(Vector3 worldPosition)
        {
            state = HarvestState.Idle;
            targetNode = null;
            productionExitPoint = transform.position;
            base.IssueMove(worldPosition);
        }

        public override void IssueAttackMove(Vector3 worldPosition)
        {
            state = HarvestState.Idle;
            targetNode = null;
            homeRefinery = null;
            productionExitPoint = transform.position;
            base.IssueAttackMove(worldPosition);
        }

        public override void IssueStop()
        {
            state = HarvestState.Idle;
            targetNode = null;
            homeRefinery = null;
            productionExitPoint = transform.position;
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
            productionExitPoint = transform.position;
            harvestAccumulator = 0f;
            state = HarvestState.MovingToResource;
            hasDestination = false;
            hasAttackMoveDestination = false;
            attackTarget = null;
        }

        public void IssueHarvestAfterExit(ResourceNode node, RefineryStructure refinery, Vector3 exitPoint)
        {
            if (node == null || refinery == null)
            {
                return;
            }

            targetNode = node;
            homeRefinery = refinery;
            productionExitPoint = ClampToMap(exitPoint);
            harvestAccumulator = 0f;
            state = HarvestState.ExitingProduction;
            hasDestination = false;
            hasAttackMoveDestination = false;
            attackTarget = null;
        }

        protected override void Update()
        {
            if (RtsGame.HasInstance && (RtsGame.Instance.IsMatchOver || RtsGame.Instance.Clock.IsPaused))
            {
                return;
            }

            TickHarvestOrders(GetDeltaTime());
        }

        private void TickHarvestOrders(float deltaTime)
        {
            switch (state)
            {
                case HarvestState.ExitingProduction:
                    TickExitingProduction(deltaTime);
                    break;
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
                    TickOrders(deltaTime);
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
                productionExitPoint = new Vector3Data(productionExitPoint),
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
            state = (HarvestState)Mathf.Clamp(data.state, 0, 4);
            harvestAccumulator = Mathf.Max(0f, data.harvestAccumulator);
            productionExitPoint = ClampToMap(data.productionExitPoint.ToVector3());
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

            if (state == HarvestState.ExitingProduction && productionExitPoint.sqrMagnitude < 0.001f)
            {
                state = HarvestState.MovingToResource;
            }
        }

#if UNITY_EDITOR
        public bool IsAutoHarvestExitingProductionForTests => state == HarvestState.ExitingProduction;
        public ResourceNode TargetResourceNodeForTests => targetNode;
        public RefineryStructure HomeRefineryForTests => homeRefinery;
        public float CargoFillForTests => CargoFill01;

        public void TickHarvesterForTests(float deltaTime)
        {
            TickHarvestOrders(deltaTime);
        }

        public void SetCargoForTests(int cargo)
        {
            Cargo = Mathf.Clamp(cargo, 0, CargoCapacity);
        }

        public void SetHarvestingForVisualsForTests(ResourceNode node, RefineryStructure refinery)
        {
            targetNode = node;
            homeRefinery = refinery;
            state = HarvestState.Harvesting;
            harvestAccumulator = 0f;
            hasDestination = false;
            attackTarget = null;
        }
#endif

        private void TickExitingProduction(float deltaTime)
        {
            if (targetNode == null || targetNode.IsDepleted)
            {
                state = HarvestState.Idle;
                return;
            }

            if (homeRefinery == null || !homeRefinery.IsAlive)
            {
                homeRefinery = RtsGame.HasInstance ? RtsGame.Instance.FindNearestRefinery(Team, transform.position) : null;
                if (homeRefinery == null)
                {
                    state = HarvestState.Idle;
                    return;
                }
            }

            if (MoveToward(productionExitPoint, deltaTime, 0.45f) || PlanarDistance(transform.position, productionExitPoint) <= 0.48f)
            {
                state = HarvestState.MovingToResource;
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
                homeRefinery = RtsGame.Instance.FindNearestRefinery(Team, transform.position);
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
