using System.Collections.Generic;
using UnityEngine;

namespace QuestCommandRTS
{
    [DisallowMultipleComponent]
    public sealed class RtsUnitVisualAnimator : MonoBehaviour
    {
        public const float TankTurretAimToleranceDegrees = 7.5f;

        private const float InfantryStrideFrequency = 4.8f;
        private const float InfantryLegSwingDegrees = 18f;
        private const float WheelRollDegreesPerUnit = 155f;
        private const float TrackScrollUnitsPerWorldUnit = 0.42f;
        private const float TurretYawDegreesPerSecond = 300f;

        private readonly List<Transform> legParts = new List<Transform>();
        private readonly List<Transform> wheelParts = new List<Transform>();
        private readonly List<Transform> trackPads = new List<Transform>();
        private readonly List<Transform> harvestParts = new List<Transform>();
        private readonly List<Transform> cargoFillParts = new List<Transform>();
        private readonly List<Quaternion> legBaseRotations = new List<Quaternion>();
        private readonly List<Vector3> legBasePositions = new List<Vector3>();
        private readonly List<Quaternion> wheelBaseRotations = new List<Quaternion>();
        private readonly List<Vector3> trackBasePositions = new List<Vector3>();
        private readonly List<Quaternion> harvestBaseRotations = new List<Quaternion>();
        private readonly List<Vector3> harvestBasePositions = new List<Vector3>();
        private readonly List<Vector3> cargoFillBaseScales = new List<Vector3>();
        private readonly List<Vector3> cargoFillBasePositions = new List<Vector3>();

        private RtsUnit owner;
        private UnitKind unitKind;
        private Transform turretPivot;
        private Transform turretMuzzle;
        private Quaternion turretBaseRotation;
        private Vector3 lastPosition;
        private float stridePhase;
        private float wheelRollDegrees;
        private float trackScroll;
        private float harvestMotionPhase;

        public bool HasLegRigForTests => legParts.Count > 0;
        public bool HasWheelRigForTests => wheelParts.Count > 0 || trackPads.Count > 0;
        public bool HasRoundWheelRigForTests => wheelParts.Count > 0;
        public bool HasTrackRigForTests => trackPads.Count > 0;
        public bool HasHarvestRigForTests => harvestParts.Count > 0;
        public bool HasCargoFillRigForTests => cargoFillParts.Count > 0;
        public bool HasTurretRigForTests => turretPivot != null;
        public Transform FirstLegForTests => legParts.Count > 0 ? legParts[0] : null;
        public Transform FirstWheelForTests => wheelParts.Count > 0 ? wheelParts[0] : FirstTrackPadForTests;
        public Transform FirstTrackPadForTests => trackPads.Count > 0 ? trackPads[0] : null;
        public Transform FirstHarvestPartForTests => harvestParts.Count > 0 ? harvestParts[0] : null;
        public Transform FirstCargoFillPartForTests => cargoFillParts.Count > 0 ? cargoFillParts[0] : null;
        public Transform TurretPivotForTests => turretPivot;

        public void Initialize(RtsUnit unit, UnitKind kind)
        {
            owner = unit;
            unitKind = RtsBalance.NormalizeUnitKind(kind);
            lastPosition = transform.position;
            CollectRigParts();
            UpdateHarvesterCargoFill(0f);
        }

        public bool AimTurretAt(Vector3 worldPosition, float deltaTime)
        {
            if (turretPivot == null)
            {
                return true;
            }

            Quaternion targetRotation = GetTurretTargetRotation(worldPosition);
            float safeDelta = Mathf.Max(0.0001f, deltaTime);
            turretPivot.localRotation = Quaternion.RotateTowards(turretPivot.localRotation, targetRotation, TurretYawDegreesPerSecond * safeDelta);
            return Quaternion.Angle(turretPivot.localRotation, targetRotation) <= TankTurretAimToleranceDegrees;
        }

        public bool IsTurretAimedAt(Vector3 worldPosition)
        {
            if (turretPivot == null)
            {
                return true;
            }

            return Quaternion.Angle(turretPivot.localRotation, GetTurretTargetRotation(worldPosition)) <= TankTurretAimToleranceDegrees;
        }

        public Vector3 GetTurretMuzzleWorldPosition()
        {
            if (turretMuzzle != null)
            {
                return turretMuzzle.position;
            }

            if (turretPivot != null)
            {
                return turretPivot.TransformPoint(new Vector3(0f, 0f, 0.75f));
            }

            return transform.position + transform.forward * 0.8f + Vector3.up;
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
            trackPads.Clear();
            harvestParts.Clear();
            cargoFillParts.Clear();
            legBaseRotations.Clear();
            legBasePositions.Clear();
            wheelBaseRotations.Clear();
            trackBasePositions.Clear();
            harvestBaseRotations.Clear();
            harvestBasePositions.Clear();
            cargoFillBaseScales.Clear();
            cargoFillBasePositions.Clear();
            turretPivot = null;
            turretMuzzle = null;

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
                else if (child.name.StartsWith("Track Tread", System.StringComparison.Ordinal))
                {
                    trackPads.Add(child);
                    trackBasePositions.Add(child.localPosition);
                }
                else if (child.name.StartsWith("Harvest Motion", System.StringComparison.Ordinal))
                {
                    harvestParts.Add(child);
                    harvestBaseRotations.Add(child.localRotation);
                    harvestBasePositions.Add(child.localPosition);
                }
                else if (child.name.StartsWith("Cargo Fill Ore", System.StringComparison.Ordinal))
                {
                    cargoFillParts.Add(child);
                    cargoFillBaseScales.Add(child.localScale);
                    cargoFillBasePositions.Add(child.localPosition);
                }
                else if (child.name == "Animated Turret Pivot")
                {
                    turretPivot = child;
                    turretBaseRotation = child.localRotation;
                }
                else if (child.name == "Animated Turret Muzzle")
                {
                    turretMuzzle = child;
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
                trackScroll += moved * TrackScrollUnitsPerWorldUnit;
            }

            AnimateLegs(movementBlend);
            AnimateWheels();
            AnimateTracks();
            AnimateHarvester(safeDelta);
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

        private void AnimateTracks()
        {
            if (trackPads.Count == 0)
            {
                return;
            }

            for (int i = 0; i < trackPads.Count; i++)
            {
                Transform pad = trackPads[i];
                if (pad == null)
                {
                    continue;
                }

                Vector3 basePosition = trackBasePositions[i];
                float sideDirection = basePosition.x < 0f ? -1f : 1f;
                float phase = Mathf.Repeat(trackScroll + i * 0.135f, 1f);
                float loopOffset = (phase - 0.5f) * 0.34f;
                pad.localPosition = basePosition + new Vector3(0f, Mathf.Sin(phase * Mathf.PI * 2f) * 0.015f, loopOffset * sideDirection);
            }
        }

        private void AnimateTurret(float deltaTime)
        {
            if (turretPivot == null || owner == null || !RtsBalance.IsTank(unitKind))
            {
                return;
            }

            RtsEntity target = owner.CurrentAttackTargetForVisuals;
            if (target != null)
            {
                AimTurretAt(target.GroundPosition, deltaTime);
                return;
            }

            turretPivot.localRotation = Quaternion.RotateTowards(turretPivot.localRotation, turretBaseRotation, TurretYawDegreesPerSecond * deltaTime);
        }

        private void AnimateHarvester(float deltaTime)
        {
            if (owner == null || unitKind != UnitKind.Harvester)
            {
                return;
            }

            UpdateHarvesterCargoFill(deltaTime);
            if (harvestParts.Count == 0)
            {
                return;
            }

            HarvesterUnit harvester = owner as HarvesterUnit;
            bool harvesting = harvester != null && harvester.IsHarvestingForVisuals;
            if (harvesting)
            {
                harvestMotionPhase += Mathf.Max(0.0001f, deltaTime) * 7.5f;
            }

            float blend = harvesting ? 1f : 0f;
            for (int i = 0; i < harvestParts.Count; i++)
            {
                Transform part = harvestParts[i];
                if (part == null)
                {
                    continue;
                }

                Quaternion baseRotation = harvestBaseRotations[i];
                Vector3 basePosition = harvestBasePositions[i];
                if (!harvesting)
                {
                    part.localRotation = Quaternion.RotateTowards(part.localRotation, baseRotation, 260f * deltaTime);
                    part.localPosition = Vector3.Lerp(part.localPosition, basePosition, Mathf.Clamp01(deltaTime * 8f));
                    continue;
                }

                float phase = harvestMotionPhase + i * 0.9f;
                if (part.name.IndexOf("Collector", System.StringComparison.Ordinal) >= 0)
                {
                    part.localRotation = baseRotation * Quaternion.Euler(0f, harvestMotionPhase * 180f, 0f);
                }
                else
                {
                    float pump = Mathf.Sin(phase) * 0.08f * blend;
                    part.localRotation = baseRotation * Quaternion.Euler(Mathf.Sin(phase) * 9f * blend, 0f, 0f);
                    part.localPosition = basePosition + new Vector3(0f, pump, 0f);
                }
            }
        }

        private void UpdateHarvesterCargoFill(float deltaTime)
        {
            if (cargoFillParts.Count == 0)
            {
                return;
            }

            HarvesterUnit harvester = owner as HarvesterUnit;
            float fill = harvester != null ? harvester.CargoFill01 : 0f;
            float interpolation = deltaTime <= 0f ? 1f : Mathf.Clamp01(deltaTime * 10f);
            for (int i = 0; i < cargoFillParts.Count; i++)
            {
                Transform part = cargoFillParts[i];
                if (part == null)
                {
                    continue;
                }

                Vector3 baseScale = cargoFillBaseScales[i];
                Vector3 targetScale = new Vector3(baseScale.x, Mathf.Max(0.001f, baseScale.y * fill), baseScale.z);
                Vector3 targetPosition = cargoFillBasePositions[i] + Vector3.up * (baseScale.y * (fill - 1f) * 0.5f);
                part.localScale = Vector3.Lerp(part.localScale, targetScale, interpolation);
                part.localPosition = Vector3.Lerp(part.localPosition, targetPosition, interpolation);
                part.gameObject.SetActive(fill > 0.015f);
            }
        }

        private Quaternion GetTurretTargetRotation(Vector3 worldPosition)
        {
            Vector3 direction = worldPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                return turretBaseRotation;
            }

            Vector3 localDirection = transform.InverseTransformDirection(direction.normalized);
            float desiredYaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
            return turretBaseRotation * Quaternion.Euler(0f, desiredYaw, 0f);
        }
    }
}
