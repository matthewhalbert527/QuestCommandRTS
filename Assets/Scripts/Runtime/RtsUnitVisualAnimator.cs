using System.Collections.Generic;
using UnityEngine;

namespace QuestCommandRTS
{
    [DisallowMultipleComponent]
    public sealed class RtsUnitVisualAnimator : MonoBehaviour
    {
        private const float InfantryStrideFrequency = 8.8f;
        private const float InfantryLegSwingDegrees = 18f;
        private const float WheelRollDegreesPerUnit = 155f;
        private const float TurretYawDegreesPerSecond = 260f;

        private readonly List<Transform> legParts = new List<Transform>();
        private readonly List<Transform> wheelParts = new List<Transform>();
        private readonly List<Quaternion> legBaseRotations = new List<Quaternion>();
        private readonly List<Vector3> legBasePositions = new List<Vector3>();
        private readonly List<Quaternion> wheelBaseRotations = new List<Quaternion>();

        private RtsUnit owner;
        private UnitKind unitKind;
        private Transform turretPivot;
        private Quaternion turretBaseRotation;
        private Vector3 lastPosition;
        private float stridePhase;
        private float wheelRollDegrees;

        public bool HasLegRigForTests => legParts.Count > 0;
        public bool HasWheelRigForTests => wheelParts.Count > 0;
        public bool HasTurretRigForTests => turretPivot != null;
        public Transform FirstLegForTests => legParts.Count > 0 ? legParts[0] : null;
        public Transform FirstWheelForTests => wheelParts.Count > 0 ? wheelParts[0] : null;
        public Transform TurretPivotForTests => turretPivot;

        public void Initialize(RtsUnit unit, UnitKind kind)
        {
            owner = unit;
            unitKind = RtsBalance.NormalizeUnitKind(kind);
            lastPosition = transform.position;
            CollectRigParts();
        }

        private void LateUpdate()
        {
            if (owner == null || !owner.IsAlive)
            {
                lastPosition = transform.position;
                return;
            }

            if (RtsGame.HasInstance && RtsGame.Instance.Clock != null && RtsGame.Instance.Clock.IsPaused)
            {
                lastPosition = transform.position;
                return;
            }

            TickVisuals(RtsGame.HasInstance && RtsGame.Instance.Clock != null ? RtsGame.Instance.Clock.DeltaTime : Time.deltaTime);
        }

#if UNITY_EDITOR
        public void TickVisualsForTests(float deltaTime)
        {
            TickVisuals(deltaTime);
        }
#endif

        private void CollectRigParts()
        {
            legParts.Clear();
            wheelParts.Clear();
            legBaseRotations.Clear();
            legBasePositions.Clear();
            wheelBaseRotations.Clear();
            turretPivot = null;

            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == transform)
                {
                    continue;
                }

                if (child.name.StartsWith("Walk Leg", System.StringComparison.Ordinal))
                {
                    legParts.Add(child);
                    legBaseRotations.Add(child.localRotation);
                    legBasePositions.Add(child.localPosition);
                }
                else if (child.name.StartsWith("Roll Wheel", System.StringComparison.Ordinal))
                {
                    wheelParts.Add(child);
                    wheelBaseRotations.Add(child.localRotation);
                }
                else if (child.name == "Animated Turret Pivot")
                {
                    turretPivot = child;
                    turretBaseRotation = child.localRotation;
                }
            }
        }

        private void TickVisuals(float deltaTime)
        {
            float safeDelta = Mathf.Max(0.0001f, deltaTime);
            Vector3 currentPosition = transform.position;
            Vector3 delta = currentPosition - lastPosition;
            delta.y = 0f;

            float moved = delta.magnitude;
            float speed = moved / safeDelta;
            float movementBlend = Mathf.Clamp01(speed / 0.35f);

            if (movementBlend > 0.01f)
            {
                stridePhase += moved * InfantryStrideFrequency;
                wheelRollDegrees += moved * WheelRollDegreesPerUnit;
            }

            AnimateLegs(movementBlend);
            AnimateWheels();
            AnimateTurret(safeDelta);
            lastPosition = currentPosition;
        }

        private void AnimateLegs(float movementBlend)
        {
            if (legParts.Count == 0)
            {
                return;
            }

            float bodyBob = Mathf.Sin(stridePhase * 2f) * 0.025f * movementBlend;
            for (int i = 0; i < legParts.Count; i++)
            {
                Transform leg = legParts[i];
                if (leg == null)
                {
                    continue;
                }

                float sidePhase = i % 2 == 0 ? 0f : Mathf.PI;
                float swing = Mathf.Sin(stridePhase + sidePhase) * InfantryLegSwingDegrees * movementBlend;
                leg.localRotation = legBaseRotations[i] * Quaternion.Euler(swing, 0f, 0f);
                leg.localPosition = legBasePositions[i] + new Vector3(0f, bodyBob, 0f);
            }
        }

        private void AnimateWheels()
        {
            for (int i = 0; i < wheelParts.Count; i++)
            {
                Transform wheel = wheelParts[i];
                if (wheel == null)
                {
                    continue;
                }

                wheel.localRotation = wheelBaseRotations[i] * Quaternion.Euler(wheelRollDegrees, 0f, 0f);
            }
        }

        private void AnimateTurret(float deltaTime)
        {
            if (turretPivot == null || owner == null || !RtsBalance.IsTank(unitKind))
            {
                return;
            }

            float desiredYaw = 0f;
            RtsEntity target = owner.CurrentAttackTargetForVisuals;
            if (target != null)
            {
                Vector3 direction = target.GroundPosition - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    Vector3 localDirection = transform.InverseTransformDirection(direction.normalized);
                    desiredYaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
                }
            }

            Quaternion targetRotation = turretBaseRotation * Quaternion.Euler(0f, desiredYaw, 0f);
            turretPivot.localRotation = Quaternion.RotateTowards(turretPivot.localRotation, targetRotation, TurretYawDegreesPerSecond * deltaTime);
        }
    }
}
