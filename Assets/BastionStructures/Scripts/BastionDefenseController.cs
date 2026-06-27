using UnityEngine;

namespace BastionStructures
{
    [DisallowMultipleComponent]
    public sealed class BastionDefenseController : MonoBehaviour
    {
        [SerializeField] private Transform weaponYaw;
        [SerializeField] private Transform weaponPitch;
        [SerializeField] private Transform[] muzzles;
        [SerializeField, Min(0f)] private float yawSpeedDegrees = 80f;
        [SerializeField, Min(0f)] private float pitchSpeedDegrees = 60f;
        [SerializeField] private float minimumElevation = -8f;
        [SerializeField] private float maximumElevation = 40f;

        private bool hasAimPoint;
        private Vector3 aimPoint;

        public Transform WeaponYaw => weaponYaw;
        public Transform WeaponPitch => weaponPitch;
        public Transform[] Muzzles => muzzles;

        private void LateUpdate()
        {
            if (hasAimPoint)
            {
                ApplyAim(aimPoint, Time.deltaTime, false);
            }
        }

        public void Configure(Transform yaw, Transform pitch, Transform[] muzzleTransforms,
            float yawSpeed, float pitchSpeed, float minElevation, float maxElevation)
        {
            weaponYaw = yaw;
            weaponPitch = pitch;
            muzzles = muzzleTransforms;
            yawSpeedDegrees = Mathf.Max(0f, yawSpeed);
            pitchSpeedDegrees = Mathf.Max(0f, pitchSpeed);
            minimumElevation = minElevation;
            maximumElevation = maxElevation;
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
            ApplyAim(worldPoint, 0f, true);
        }

        private void ApplyAim(Vector3 worldPoint, float deltaTime, bool snap)
        {
            if (weaponYaw == null || weaponPitch == null) return;

            Vector3 yawDirection = weaponYaw.parent.InverseTransformDirection(worldPoint - weaponYaw.position);
            yawDirection.y = 0f;
            if (yawDirection.sqrMagnitude > 0.0001f)
            {
                float targetYaw = Mathf.Atan2(yawDirection.x, yawDirection.z) * Mathf.Rad2Deg;
                float currentYaw = NormalizeAngle(weaponYaw.localEulerAngles.y);
                float nextYaw = snap ? targetYaw : Mathf.MoveTowardsAngle(currentYaw, targetYaw, yawSpeedDegrees * deltaTime);
                weaponYaw.localRotation = Quaternion.Euler(0f, nextYaw, 0f);
            }

            Vector3 pitchDirection = weaponYaw.InverseTransformDirection(worldPoint - weaponPitch.position);
            float elevation = Mathf.Atan2(pitchDirection.y, Mathf.Max(0.0001f, pitchDirection.z)) * Mathf.Rad2Deg;
            elevation = Mathf.Clamp(elevation, minimumElevation, maximumElevation);
            float targetPitch = -elevation;
            float currentPitch = NormalizeAngle(weaponPitch.localEulerAngles.x);
            float nextPitch = snap ? targetPitch : Mathf.MoveTowardsAngle(currentPitch, targetPitch, pitchSpeedDegrees * deltaTime);
            weaponPitch.localRotation = Quaternion.Euler(nextPitch, 0f, 0f);
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
