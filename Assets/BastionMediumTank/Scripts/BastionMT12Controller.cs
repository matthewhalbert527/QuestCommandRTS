using UnityEngine;

namespace BastionMediumTank
{
    [DisallowMultipleComponent]
    public sealed class BastionMT12Controller : MonoBehaviour
    {
        [Header("Main weapon articulation")]
        [SerializeField] private Transform turretYaw;
        [SerializeField] private Transform cannonPitch;
        [SerializeField] private Transform mainMuzzle;

        [Header("Traverse")]
        [SerializeField, Min(0f)] private float turretSpeedDegrees = 82f;
        [SerializeField, Min(0f)] private float cannonSpeedDegrees = 58f;
        [SerializeField] private float minimumElevation = -7f;
        [SerializeField] private float maximumElevation = 28f;

        private bool hasAimPoint;
        private Vector3 aimPoint;

        public Transform TurretYaw => turretYaw;
        public Transform CannonPitch => cannonPitch;
        public Transform MainMuzzle => mainMuzzle;

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
            if (hasAimPoint)
            {
                ApplyAim(aimPoint, Time.deltaTime, false);
            }
        }

        public void SetAimPoint(Vector3 worldPoint)
        {
            aimPoint = worldPoint;
            hasAimPoint = true;
        }

        public void ClearAimPoint()
        {
            hasAimPoint = false;
        }

        public void SnapAimAt(Vector3 worldPoint)
        {
            if (ResolveTransforms())
            {
                ApplyAim(worldPoint, 0f, true);
            }
        }

        private void ApplyAim(Vector3 worldPoint, float deltaTime, bool snap)
        {
            Vector3 yawLocalDirection = turretYaw.parent.InverseTransformDirection(worldPoint - turretYaw.position);
            yawLocalDirection.y = 0f;
            if (yawLocalDirection.sqrMagnitude > 0.0001f)
            {
                float targetYaw = Mathf.Atan2(yawLocalDirection.x, yawLocalDirection.z) * Mathf.Rad2Deg;
                float currentYaw = NormalizeAngle(turretYaw.localEulerAngles.y);
                float nextYaw = snap
                    ? targetYaw
                    : Mathf.MoveTowardsAngle(currentYaw, targetYaw, turretSpeedDegrees * deltaTime);
                turretYaw.localRotation = Quaternion.Euler(0f, nextYaw, 0f);
            }

            Vector3 pitchLocalDirection = turretYaw.InverseTransformDirection(worldPoint - cannonPitch.position);
            float elevation = Mathf.Atan2(pitchLocalDirection.y, Mathf.Max(0.0001f, pitchLocalDirection.z)) * Mathf.Rad2Deg;
            elevation = Mathf.Clamp(elevation, minimumElevation, maximumElevation);
            float targetPitch = -elevation;
            float currentPitch = NormalizeAngle(cannonPitch.localEulerAngles.x);
            float nextPitch = snap
                ? targetPitch
                : Mathf.MoveTowardsAngle(currentPitch, targetPitch, cannonSpeedDegrees * deltaTime);
            cannonPitch.localRotation = Quaternion.Euler(nextPitch, 0f, 0f);
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
            if (mainMuzzle == null && cannonPitch != null)
            {
                mainMuzzle = cannonPitch.Find("MainMuzzle");
            }
            return turretYaw != null && cannonPitch != null;
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
