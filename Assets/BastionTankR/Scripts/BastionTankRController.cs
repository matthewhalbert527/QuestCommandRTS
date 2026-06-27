using UnityEngine;

namespace BastionTankR
{
    [DisallowMultipleComponent]
    public sealed class BastionTankRController : MonoBehaviour
    {
        [Header("Articulation")]
        [SerializeField] private Transform turretYaw;
        [SerializeField] private Transform cannonPitch;

        [Header("Traverse")]
        [SerializeField, Min(0f)] private float turretSpeedDegrees = 100f;
        [SerializeField, Min(0f)] private float cannonSpeedDegrees = 70f;
        [SerializeField] private float minimumElevation = -7f;
        [SerializeField] private float maximumElevation = 32f;

        private bool hasAimPoint;
        private Vector3 aimPoint;

        public Transform TurretYaw => turretYaw;
        public Transform CannonPitch => cannonPitch;

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
                RotateToward(aimPoint, Time.deltaTime);
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
            if (!ResolveTransforms())
            {
                return;
            }

            ApplyAim(worldPoint, 1f, true);
        }

        private void RotateToward(Vector3 worldPoint, float deltaTime)
        {
            if (!ResolveTransforms())
            {
                return;
            }

            ApplyAim(worldPoint, deltaTime, false);
        }

        private void ApplyAim(Vector3 worldPoint, float deltaTime, bool snap)
        {
            Vector3 flatDirection = worldPoint - turretYaw.position;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion desiredWorldYaw = Quaternion.LookRotation(flatDirection.normalized, transform.up);
                Quaternion desiredLocalYaw = Quaternion.Inverse(turretYaw.parent.rotation) * desiredWorldYaw;
                float targetYaw = desiredLocalYaw.eulerAngles.y;
                float currentYaw = turretYaw.localEulerAngles.y;
                float nextYaw = snap
                    ? targetYaw
                    : Mathf.MoveTowardsAngle(currentYaw, targetYaw, turretSpeedDegrees * deltaTime);
                turretYaw.localRotation = Quaternion.Euler(0f, nextYaw, 0f);
            }

            Vector3 localDirection = turretYaw.InverseTransformDirection(worldPoint - cannonPitch.position);
            float elevation = Mathf.Atan2(localDirection.y, Mathf.Max(0.0001f, localDirection.z)) * Mathf.Rad2Deg;
            elevation = Mathf.Clamp(elevation, minimumElevation, maximumElevation);

            // Unity's positive X rotation tips +Z downward, so elevation uses a negative local X angle.
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

            return turretYaw != null && cannonPitch != null;
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
