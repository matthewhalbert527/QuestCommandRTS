using UnityEngine;

namespace BastionHeavyTank
{
    [DisallowMultipleComponent]
    public sealed class BastionHT77Controller : MonoBehaviour
    {
        [Header("Main weapon articulation")]
        [SerializeField] private Transform turretYaw;
        [SerializeField] private Transform cannonPitch;
        [SerializeField] private Transform leftCannonMuzzle;
        [SerializeField] private Transform rightCannonMuzzle;

        [Header("Missile attachment articulation")]
        [SerializeField] private Transform missileYaw;
        [SerializeField] private Transform missilePitch;
        [SerializeField] private Transform[] missileMuzzles;

        [Header("Main weapon traverse")]
        [SerializeField, Min(0f)] private float turretSpeedDegrees = 42f;
        [SerializeField, Min(0f)] private float cannonSpeedDegrees = 34f;
        [SerializeField] private float minimumCannonElevation = -6f;
        [SerializeField] private float maximumCannonElevation = 24f;

        [Header("Missile traverse")]
        [SerializeField, Min(0f)] private float missileYawSpeedDegrees = 70f;
        [SerializeField, Min(0f)] private float missilePitchSpeedDegrees = 58f;
        [SerializeField] private float minimumMissileElevation = -5f;
        [SerializeField] private float maximumMissileElevation = 55f;

        private bool hasMainAimPoint;
        private bool hasMissileAimPoint;
        private Vector3 mainAimPoint;
        private Vector3 missileAimPoint;

        public Transform TurretYaw => turretYaw;
        public Transform CannonPitch => cannonPitch;
        public Transform LeftCannonMuzzle => leftCannonMuzzle;
        public Transform RightCannonMuzzle => rightCannonMuzzle;
        public Transform MissileYaw => missileYaw;
        public Transform MissilePitch => missilePitch;
        public Transform[] MissileMuzzles => missileMuzzles;

        private void Reset()
        {
            ResolveTransforms();
        }

        private void Awake()
        {
            ResolveTransforms();
        }

        private void LateUpdate()
        {
            if (hasMainAimPoint)
            {
                ApplyMainAim(mainAimPoint, Time.deltaTime, false);
            }
            if (hasMissileAimPoint)
            {
                ApplyMissileAim(missileAimPoint, Time.deltaTime, false);
            }
        }

        public void SetAimPoint(Vector3 worldPoint)
        {
            SetMainAimPoint(worldPoint);
            SetMissileAimPoint(worldPoint);
        }

        public void ClearAimPoint()
        {
            ClearMainAimPoint();
            ClearMissileAimPoint();
        }

        public void SetMainAimPoint(Vector3 worldPoint)
        {
            mainAimPoint = worldPoint;
            hasMainAimPoint = true;
        }

        public void ClearMainAimPoint()
        {
            hasMainAimPoint = false;
        }

        public void SetMissileAimPoint(Vector3 worldPoint)
        {
            missileAimPoint = worldPoint;
            hasMissileAimPoint = true;
        }

        public void ClearMissileAimPoint()
        {
            hasMissileAimPoint = false;
        }

        public void SnapAimAt(Vector3 worldPoint)
        {
            if (!ResolveTransforms())
            {
                return;
            }
            ApplyMainAim(worldPoint, 0f, true);
            ApplyMissileAim(worldPoint, 0f, true);
        }

        private void ApplyMainAim(Vector3 worldPoint, float deltaTime, bool snap)
        {
            Vector3 yawLocalDirection = turretYaw.parent.InverseTransformDirection(worldPoint - turretYaw.position);
            yawLocalDirection.y = 0f;
            if (yawLocalDirection.sqrMagnitude > 0.0001f)
            {
                float targetYaw = Mathf.Atan2(yawLocalDirection.x, yawLocalDirection.z) * Mathf.Rad2Deg;
                float currentYaw = NormalizeAngle(turretYaw.localEulerAngles.y);
                float nextYaw = snap ? targetYaw : Mathf.MoveTowardsAngle(currentYaw, targetYaw, turretSpeedDegrees * deltaTime);
                turretYaw.localRotation = Quaternion.Euler(0f, nextYaw, 0f);
            }

            Vector3 pitchLocalDirection = turretYaw.InverseTransformDirection(worldPoint - cannonPitch.position);
            float elevation = Mathf.Atan2(pitchLocalDirection.y, Mathf.Max(0.0001f, pitchLocalDirection.z)) * Mathf.Rad2Deg;
            elevation = Mathf.Clamp(elevation, minimumCannonElevation, maximumCannonElevation);
            float targetPitch = -elevation;
            float currentPitch = NormalizeAngle(cannonPitch.localEulerAngles.x);
            float nextPitch = snap ? targetPitch : Mathf.MoveTowardsAngle(currentPitch, targetPitch, cannonSpeedDegrees * deltaTime);
            cannonPitch.localRotation = Quaternion.Euler(nextPitch, 0f, 0f);
        }

        private void ApplyMissileAim(Vector3 worldPoint, float deltaTime, bool snap)
        {
            Vector3 yawLocalDirection = missileYaw.parent.InverseTransformDirection(worldPoint - missileYaw.position);
            yawLocalDirection.y = 0f;
            if (yawLocalDirection.sqrMagnitude > 0.0001f)
            {
                float targetYaw = Mathf.Atan2(yawLocalDirection.x, yawLocalDirection.z) * Mathf.Rad2Deg;
                float currentYaw = NormalizeAngle(missileYaw.localEulerAngles.y);
                float nextYaw = snap ? targetYaw : Mathf.MoveTowardsAngle(currentYaw, targetYaw, missileYawSpeedDegrees * deltaTime);
                missileYaw.localRotation = Quaternion.Euler(0f, nextYaw, 0f);
            }

            Vector3 pitchLocalDirection = missileYaw.InverseTransformDirection(worldPoint - missilePitch.position);
            float elevation = Mathf.Atan2(pitchLocalDirection.y, Mathf.Max(0.0001f, pitchLocalDirection.z)) * Mathf.Rad2Deg;
            elevation = Mathf.Clamp(elevation, minimumMissileElevation, maximumMissileElevation);
            float targetPitch = -elevation;
            float currentPitch = NormalizeAngle(missilePitch.localEulerAngles.x);
            float nextPitch = snap ? targetPitch : Mathf.MoveTowardsAngle(currentPitch, targetPitch, missilePitchSpeedDegrees * deltaTime);
            missilePitch.localRotation = Quaternion.Euler(nextPitch, 0f, 0f);
        }

        private bool ResolveTransforms()
        {
            if (turretYaw == null)
            {
                turretYaw = transform.Find("TurretYaw");
            }
            if (cannonPitch == null && turretYaw != null)
            {
                cannonPitch = turretYaw.Find("CannonPitch");
            }
            if (leftCannonMuzzle == null && cannonPitch != null)
            {
                leftCannonMuzzle = cannonPitch.Find("LeftCannonMuzzle");
            }
            if (rightCannonMuzzle == null && cannonPitch != null)
            {
                rightCannonMuzzle = cannonPitch.Find("RightCannonMuzzle");
            }
            if (missileYaw == null && turretYaw != null)
            {
                missileYaw = turretYaw.Find("MissileYaw");
            }
            if (missilePitch == null && missileYaw != null)
            {
                missilePitch = missileYaw.Find("MissilePitch");
            }
            if ((missileMuzzles == null || missileMuzzles.Length == 0) && missilePitch != null)
            {
                missileMuzzles = new Transform[8];
                for (int i = 0; i < missileMuzzles.Length; i++)
                {
                    missileMuzzles[i] = missilePitch.Find($"MissileMuzzle_{i + 1:00}");
                }
            }
            return turretYaw != null && cannonPitch != null && missileYaw != null && missilePitch != null;
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
