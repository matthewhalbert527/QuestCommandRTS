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
        protected Vector3 guardPosition;
        protected Vector3 guardOffset;
        protected RtsEntity guardTarget;
        protected bool hasGuardOrder;

        private readonly Collider[] avoidanceHits = new Collider[18];
        private RtsVisualMotionDriver visualMotionDriver;
        private Vector3 previousPosition;
        private float previousYaw;
        private float currentMoveSpeed;
        private float smoothedMovementAmount;
        private float smoothedTurnAmount;
        private float nextAttackTime;
        private float nextGuardScanTime;
        private bool IsAirborne => UnitKind == UnitKind.Skyraider || UnitKind == UnitKind.OrcaLifter;
        private bool IsHumanUnit => UnitKind != UnitKind.Tank && UnitKind != UnitKind.Harvester && !IsAirborne;
        public override Vector3 AimPoint => GroundPosition + Vector3.up * (IsAirborne ? 1.9f : 0.8f);

        protected override void Awake()
        {
            base.Awake();
            destination = transform.position;
            previousPosition = transform.position;
            previousYaw = transform.eulerAngles.y;
        }

        protected virtual void Update()
        {
            TickOrders(Time.deltaTime);
            TickMovementFeedback(Time.deltaTime);
        }

        public virtual void Initialize(RtsTeam team, UnitKind kind)
        {
            UnitKind = kind;
            UnitStats stats = RtsBalance.GetUnit(kind);
            MoveSpeed = stats.MoveSpeed;
            AttackRange = stats.AttackRange;
            Damage = stats.Damage;
            AttackCooldown = stats.AttackCooldown;

            float radius;
            switch (kind)
            {
                case UnitKind.Tank:
                    radius = 1.1f;
                    break;
                case UnitKind.Skyraider:
                    radius = 1.55f;
                    break;
                case UnitKind.OrcaLifter:
                    radius = 1.8f;
                    break;
                default:
                    radius = 0.72f;
                    break;
            }

            Initialize(team, stats.Name, stats.Health, radius);
            previousPosition = transform.position;
            previousYaw = transform.eulerAngles.y;
            currentMoveSpeed = 0f;
            visualMotionDriver = GetComponent<RtsVisualMotionDriver>();
            if (visualMotionDriver == null)
            {
                visualMotionDriver = gameObject.AddComponent<RtsVisualMotionDriver>();
            }

            visualMotionDriver.Initialize(kind);
        }

        public virtual void IssueMove(Vector3 worldPosition)
        {
            attackTarget = null;
            ClearGuardOrder();
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
            ClearGuardOrder();
            hasDestination = false;
        }

        public virtual void IssueGuard(Vector3 worldPosition, RtsEntity target)
        {
            attackTarget = null;
            hasDestination = false;
            hasGuardOrder = true;
            guardTarget = target != null && target.IsAlive ? target : null;
            guardPosition = ClampToMap(worldPosition);
            guardOffset = guardTarget != null ? guardPosition - guardTarget.GroundPosition : Vector3.zero;
            guardOffset.y = 0f;
            if (guardTarget != null)
            {
                float minimumOffset = Mathf.Max(1.5f, guardTarget.SelectionRadius + SelectionRadius + 0.75f);
                if (guardOffset.magnitude < minimumOffset)
                {
                    if (guardOffset.sqrMagnitude < 0.001f)
                    {
                        guardOffset = transform.position - guardTarget.GroundPosition;
                        guardOffset.y = 0f;
                    }

                    if (guardOffset.sqrMagnitude < 0.001f)
                    {
                        guardOffset = Vector3.back;
                    }

                    guardOffset = guardOffset.normalized * minimumOffset;
                    guardPosition = ClampToMap(guardTarget.GroundPosition + guardOffset);
                }
            }

            nextGuardScanTime = 0f;
        }

        public bool IsIdle()
        {
            return !hasDestination && attackTarget == null && !hasGuardOrder;
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

            if (hasGuardOrder)
            {
                TickGuardOrder(deltaTime);
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

        private void TickGuardOrder(float deltaTime)
        {
            Vector3 anchor = GetGuardAnchorPosition();
            if (Time.time >= nextGuardScanTime)
            {
                nextGuardScanTime = Time.time + 0.45f;
                RtsEntity threat = FindGuardThreat(anchor);
                if (threat != null)
                {
                    attackTarget = threat;
                    return;
                }
            }

            float holdRadius = Mathf.Max(1.2f, SelectionRadius + 0.8f);
            if (PlanarDistance(transform.position, anchor) > holdRadius)
            {
                MoveToward(anchor, deltaTime, holdRadius * 0.55f);
            }
            else if (guardTarget != null && guardTarget.IsAlive)
            {
                FacePoint(guardTarget.transform.position, deltaTime);
            }
        }

        private RtsEntity FindGuardThreat(Vector3 anchor)
        {
            if (!RtsGame.HasInstance || Damage <= 0f)
            {
                return null;
            }

            return RtsGame.Instance.FindClosestEnemy(Team, anchor, GetGuardScanRange());
        }

        private Vector3 GetGuardAnchorPosition()
        {
            if (guardTarget != null && guardTarget.IsAlive)
            {
                Vector3 targetPosition = guardTarget.GroundPosition;
                Vector3 offset = guardOffset;
                offset.y = 0f;
                if (offset.magnitude > GetGuardScanRange() * 0.65f)
                {
                    offset = offset.normalized * Mathf.Max(1.3f, guardTarget.SelectionRadius + SelectionRadius + 0.8f);
                }

                guardPosition = ClampToMap(targetPosition + offset);
                return guardPosition;
            }

            guardTarget = null;
            return guardPosition;
        }

        private float GetGuardScanRange()
        {
            return Mathf.Max(8f, AttackRange + 3f);
        }

        protected void ClearGuardOrder()
        {
            guardTarget = null;
            hasGuardOrder = false;
        }

        private void TickMovementFeedback(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            Vector3 delta = transform.position - previousPosition;
            delta.y = 0f;
            previousPosition = transform.position;

            float movementAmount = MoveSpeed > 0.001f
                ? Mathf.Clamp01(delta.magnitude / (MoveSpeed * deltaTime))
                : 0f;
            Vector3 localDelta = transform.InverseTransformDirection(delta);
            Vector2 localMove = MoveSpeed > 0.001f
                ? Vector2.ClampMagnitude(new Vector2(localDelta.x, localDelta.z) / (MoveSpeed * deltaTime), 1f)
                : Vector2.zero;
            float currentYaw = transform.eulerAngles.y;
            float yawDelta = Mathf.DeltaAngle(previousYaw, currentYaw);
            previousYaw = currentYaw;
            float turnAmount = TurnSpeed > 0.001f
                ? Mathf.Clamp(yawDelta / (TurnSpeed * deltaTime), -1f, 1f)
                : 0f;

            float response = movementAmount > smoothedMovementAmount ? 8f : 5f;
            smoothedMovementAmount = Mathf.MoveTowards(smoothedMovementAmount, movementAmount, response * deltaTime);
            smoothedTurnAmount = Mathf.MoveTowards(smoothedTurnAmount, turnAmount, 9f * deltaTime);

            BroadcastMessage("SetMovementAmount", smoothedMovementAmount, SendMessageOptions.DontRequireReceiver);
            BroadcastMessage("SetLocalMoveVector", localMove, SendMessageOptions.DontRequireReceiver);
            BroadcastMessage("SetTurnAmount", smoothedTurnAmount, SendMessageOptions.DontRequireReceiver);
        }

        protected bool MoveToward(Vector3 targetPosition, float deltaTime, float stoppingDistance)
        {
            Vector3 target = ClampToMap(targetPosition);
            Vector3 current = transform.position;
            Vector3 flatDelta = new Vector3(target.x - current.x, 0f, target.z - current.z);
            float distance = flatDelta.magnitude;

            if (distance <= Mathf.Max(0.05f, stoppingDistance))
            {
                currentMoveSpeed = 0f;
                return true;
            }

            Vector3 desiredDirection = flatDelta / distance;
            Vector3 moveDirection = GetAvoidedMoveDirection(desiredDirection, target, stoppingDistance);
            Quaternion lookRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, TurnSpeed * deltaTime);

            float targetMoveSpeed = GetTargetMoveSpeed(distance, stoppingDistance);
            float acceleration = IsHumanUnit
                ? (targetMoveSpeed > currentMoveSpeed ? 11.5f : 17f)
                : 120f;
            currentMoveSpeed = Mathf.MoveTowards(currentMoveSpeed, targetMoveSpeed, acceleration * deltaTime);

            float step = Mathf.Min(distance - stoppingDistance, currentMoveSpeed * deltaTime);
            transform.position = ClampToMap(current + moveDirection * Mathf.Max(0f, step));
            return false;
        }

        private float GetTargetMoveSpeed(float distance, float stoppingDistance)
        {
            if (!IsHumanUnit)
            {
                return MoveSpeed;
            }

            float remaining = Mathf.Max(0f, distance - stoppingDistance);
            float brakingFactor = Mathf.Clamp01(remaining / 1.35f);
            float combatCaution = attackTarget != null ? 0.88f : 1f;
            return MoveSpeed * Mathf.Lerp(0.35f, 1f, brakingFactor) * combatCaution;
        }

        private Vector3 GetAvoidedMoveDirection(Vector3 desiredDirection, Vector3 targetPosition, float stoppingDistance)
        {
            if (!RtsGame.HasInstance || IsAirborne)
            {
                return desiredDirection;
            }

            float scanRadius = Mathf.Max(1.6f, SelectionRadius + 1.35f);
            Vector3 scanCenter = transform.position + desiredDirection * Mathf.Min(0.9f, scanRadius * 0.45f) + Vector3.up * 0.6f;
            int hitCount = Physics.OverlapSphereNonAlloc(scanCenter, scanRadius, avoidanceHits);
            Vector3 avoidance = Vector3.zero;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = avoidanceHits[i];
                avoidanceHits[i] = null;

                if (hit == null || hit.isTrigger || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (!TryGetAvoidanceObstacle(hit, targetPosition, stoppingDistance, out Vector3 obstaclePosition, out float obstacleRadius))
                {
                    continue;
                }

                Vector3 away = transform.position - obstaclePosition;
                away.y = 0f;
                float distanceSqr = away.sqrMagnitude;
                if (distanceSqr < 0.0001f)
                {
                    away = Vector3.Cross(Vector3.up, desiredDirection);
                    distanceSqr = away.sqrMagnitude;
                }

                Vector3 toObstacle = obstaclePosition - transform.position;
                toObstacle.y = 0f;
                if (toObstacle.sqrMagnitude > 0.0001f && Vector3.Dot(desiredDirection, toObstacle.normalized) < -0.2f)
                {
                    continue;
                }

                float desiredClearance = SelectionRadius + obstacleRadius + 0.35f;
                float distance = Mathf.Sqrt(distanceSqr);
                if (distance >= desiredClearance)
                {
                    continue;
                }

                float strength = 1f - Mathf.Clamp01(distance / desiredClearance);
                avoidance += away.normalized * strength;
            }

            if (avoidance.sqrMagnitude < 0.0001f)
            {
                return desiredDirection;
            }

            Vector3 blended = desiredDirection + avoidance.normalized * 1.15f;
            if (Vector3.Dot(blended.normalized, desiredDirection) < 0.2f)
            {
                Vector3 side = Vector3.Cross(Vector3.up, desiredDirection).normalized;
                blended = desiredDirection + side * Mathf.Sign(Vector3.Dot(side, avoidance));
            }

            return blended.normalized;
        }

        private bool TryGetAvoidanceObstacle(Collider hit, Vector3 targetPosition, float stoppingDistance, out Vector3 obstaclePosition, out float obstacleRadius)
        {
            RtsEntity entity = hit.GetComponentInParent<RtsEntity>();
            if (entity != null)
            {
                if (entity == this || entity == attackTarget || !entity.IsAlive)
                {
                    obstaclePosition = Vector3.zero;
                    obstacleRadius = 0f;
                    return false;
                }

                obstaclePosition = entity.GroundPosition;
                obstacleRadius = Mathf.Max(0.55f, entity.SelectionRadius);
                return stoppingDistance <= 1f || PlanarDistance(targetPosition, obstaclePosition) > stoppingDistance + obstacleRadius * 0.45f;
            }

            ResourceNode resource = hit.GetComponentInParent<ResourceNode>();
            if (resource != null && !resource.IsDepleted)
            {
                obstaclePosition = resource.transform.position;
                obstaclePosition.y = 0f;
                obstacleRadius = 2.4f;
                return stoppingDistance <= 1f || PlanarDistance(targetPosition, obstaclePosition) > stoppingDistance + 0.9f;
            }

            obstaclePosition = Vector3.zero;
            obstacleRadius = 0f;
            return false;
        }

        private void TickAttackOrder(float deltaTime)
        {
            if (hasGuardOrder && PlanarDistance(attackTarget.transform.position, GetGuardAnchorPosition()) > GetGuardScanRange() + 2f)
            {
                attackTarget = null;
                return;
            }

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
                BroadcastMessage("TriggerRecoil", SendMessageOptions.DontRequireReceiver);
                RtsGame.Instance.SpawnTracer(AimPoint, attackTarget.AimPoint, Team, UnitKind);
            }
        }

        protected void FacePoint(Vector3 point, float deltaTime)
        {
            currentMoveSpeed = Mathf.MoveTowards(
                currentMoveSpeed,
                0f,
                (IsHumanUnit ? 17f : 120f) * deltaTime);

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

    [DisallowMultipleComponent]
    public sealed class RtsVisualMotionDriver : MonoBehaviour
    {
        private Transform[] visualRoots = new Transform[0];
        private Vector3[] basePositions = new Vector3[0];
        private Quaternion[] baseRotations = new Quaternion[0];
        private UnitKind kind;
        private float movementAmount;
        private float stridePhase;
        private float recoil;
        private bool initialized;

        public void Initialize(UnitKind unitKind)
        {
            kind = unitKind;
            CacheVisualRoots();
            initialized = true;
        }

        public void SetMovementAmount(float normalizedSpeed)
        {
            movementAmount = Mathf.Clamp01(normalizedSpeed);
        }

        public void TriggerRecoil()
        {
            recoil = 1f;
        }

        private void LateUpdate()
        {
            if (!initialized || visualRoots.Length == 0)
            {
                return;
            }

            stridePhase += Time.deltaTime * Mathf.Lerp(4.5f, IsInfantry(kind) ? 10.5f : 6.6f, movementAmount);
            recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 8f);

            float sin = Mathf.Sin(stridePhase);
            float cos = Mathf.Cos(stridePhase);
            Vector3 offset;
            Quaternion rotation;

            if (kind == UnitKind.Skyraider || kind == UnitKind.OrcaLifter)
            {
                offset = new Vector3(0f, Mathf.Sin(Time.time * 2.2f) * 0.045f, -recoil * 0.035f);
                rotation = Quaternion.Euler(cos * movementAmount * 1.4f, 0f, sin * 1.2f);
            }
            else if (kind == UnitKind.Tank || kind == UnitKind.Harvester)
            {
                offset = new Vector3(0f, Mathf.Abs(sin) * movementAmount * 0.018f, -recoil * 0.025f);
                rotation = Quaternion.Euler(sin * movementAmount * 0.9f, 0f, cos * movementAmount * 0.45f);
            }
            else
            {
                offset = new Vector3(0f, Mathf.Abs(sin) * movementAmount * 0.055f, -recoil * 0.035f);
                rotation = Quaternion.Euler(sin * movementAmount * 2.4f, 0f, cos * movementAmount * 1.3f);
            }

            for (int i = 0; i < visualRoots.Length; i++)
            {
                Transform visualRoot = visualRoots[i];
                if (visualRoot == null)
                {
                    continue;
                }

                visualRoot.localPosition = basePositions[i] + offset;
                visualRoot.localRotation = baseRotations[i] * rotation;
            }
        }

        private void CacheVisualRoots()
        {
            int count = 0;
            foreach (Transform child in transform)
            {
                if (ShouldAnimateChild(child))
                {
                    count++;
                }
            }

            visualRoots = new Transform[count];
            basePositions = new Vector3[count];
            baseRotations = new Quaternion[count];

            int index = 0;
            foreach (Transform child in transform)
            {
                if (!ShouldAnimateChild(child))
                {
                    continue;
                }

                visualRoots[index] = child;
                basePositions[index] = child.localPosition;
                baseRotations[index] = child.localRotation;
                index++;
            }
        }

        private static bool ShouldAnimateChild(Transform child)
        {
            if (child == null)
            {
                return false;
            }

            string childName = child.name.ToLowerInvariant();
            return !childName.Contains("selection ring") && !childName.Contains("health") && !childName.Contains("ui");
        }

        private static bool IsInfantry(UnitKind unitKind)
        {
            return unitKind != UnitKind.Tank &&
                unitKind != UnitKind.Harvester &&
                unitKind != UnitKind.Skyraider &&
                unitKind != UnitKind.OrcaLifter;
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

        public override void IssueGuard(Vector3 worldPosition, RtsEntity target)
        {
            state = HarvestState.Idle;
            targetNode = null;
            base.IssueGuard(worldPosition, target);
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
            ClearGuardOrder();
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
