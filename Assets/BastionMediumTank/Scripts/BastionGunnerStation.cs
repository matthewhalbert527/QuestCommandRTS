using UnityEngine;

namespace BastionMediumTank
{
    [DisallowMultipleComponent]
    public sealed class BastionGunnerStation : MonoBehaviour
    {
        [Header("Weapon articulation")]
        [SerializeField] private Transform gunnerYaw;
        [SerializeField] private Transform minigunPitch;
        [SerializeField] private Transform barrelSpin;
        [SerializeField] private Transform muzzle;

        [Header("Crew anchors")]
        [SerializeField] private Transform occupantAnchor;
        [SerializeField] private Transform leftHandGrip;
        [SerializeField] private Transform rightHandGrip;
        [SerializeField] private Transform leftFootAnchor;
        [SerializeField] private Transform rightFootAnchor;
        [SerializeField] private Transform headLookAnchor;
        [SerializeField] private Transform boardingPoint;
        [SerializeField] private Transform dismountPoint;

        [Header("Traverse")]
        [SerializeField, Min(0f)] private float yawSpeedDegrees = 165f;
        [SerializeField, Min(0f)] private float pitchSpeedDegrees = 125f;
        [SerializeField] private float minimumElevation = -15f;
        [SerializeField] private float maximumElevation = 55f;

        [Header("Barrel spin")]
        [SerializeField, Min(0f)] private float maximumSpinDegreesPerSecond = 2400f;
        [SerializeField, Min(0f)] private float spinAccelerationDegreesPerSecond = 5000f;

        private Transform occupant;
        private Transform occupantOriginalParent;
        private bool hasAimPoint;
        private bool firing;
        private Vector3 aimPoint;
        private float currentSpinDegreesPerSecond;

        public bool IsOccupied => occupant != null;
        public Transform Occupant => occupant;
        public Transform OccupantAnchor => occupantAnchor;
        public Transform LeftHandGrip => leftHandGrip;
        public Transform RightHandGrip => rightHandGrip;
        public Transform LeftFootAnchor => leftFootAnchor;
        public Transform RightFootAnchor => rightFootAnchor;
        public Transform HeadLookAnchor => headLookAnchor;
        public Transform BoardingPoint => boardingPoint;
        public Transform DismountPoint => dismountPoint;
        public Transform Muzzle => muzzle;
        public bool IsFiring => firing;

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

            float targetSpin = firing ? maximumSpinDegreesPerSecond : 0f;
            currentSpinDegreesPerSecond = Mathf.MoveTowards(
                currentSpinDegreesPerSecond,
                targetSpin,
                spinAccelerationDegreesPerSecond * Time.deltaTime);

            if (barrelSpin != null && Mathf.Abs(currentSpinDegreesPerSecond) > 0.01f)
            {
                barrelSpin.Rotate(0f, 0f, currentSpinDegreesPerSecond * Time.deltaTime, Space.Self);
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

        public void SetFiring(bool value)
        {
            firing = value;
        }

        public bool Mount(Transform actorRoot)
        {
            if (actorRoot == null || occupant != null || !ResolveTransforms() || occupantAnchor == null)
            {
                return false;
            }

            occupant = actorRoot;
            occupantOriginalParent = actorRoot.parent;
            actorRoot.SetParent(occupantAnchor, false);
            actorRoot.localPosition = Vector3.zero;
            actorRoot.localRotation = Quaternion.identity;
            return true;
        }

        public Transform Dismount()
        {
            if (occupant == null)
            {
                return null;
            }

            Transform released = occupant;
            released.SetParent(occupantOriginalParent, true);
            if (dismountPoint != null)
            {
                released.SetPositionAndRotation(dismountPoint.position, dismountPoint.rotation);
            }

            occupant = null;
            occupantOriginalParent = null;
            firing = false;
            return released;
        }

        private void ApplyAim(Vector3 worldPoint, float deltaTime, bool snap)
        {
            Vector3 yawLocalDirection = gunnerYaw.parent.InverseTransformDirection(worldPoint - gunnerYaw.position);
            yawLocalDirection.y = 0f;
            if (yawLocalDirection.sqrMagnitude > 0.0001f)
            {
                float targetYaw = Mathf.Atan2(yawLocalDirection.x, yawLocalDirection.z) * Mathf.Rad2Deg;
                float currentYaw = NormalizeAngle(gunnerYaw.localEulerAngles.y);
                float nextYaw = snap
                    ? targetYaw
                    : Mathf.MoveTowardsAngle(currentYaw, targetYaw, yawSpeedDegrees * deltaTime);
                gunnerYaw.localRotation = Quaternion.Euler(0f, nextYaw, 0f);
            }

            Vector3 pitchLocalDirection = gunnerYaw.InverseTransformDirection(worldPoint - minigunPitch.position);
            float elevation = Mathf.Atan2(pitchLocalDirection.y, Mathf.Max(0.0001f, pitchLocalDirection.z)) * Mathf.Rad2Deg;
            elevation = Mathf.Clamp(elevation, minimumElevation, maximumElevation);
            float targetPitch = -elevation;
            float currentPitch = NormalizeAngle(minigunPitch.localEulerAngles.x);
            float nextPitch = snap
                ? targetPitch
                : Mathf.MoveTowardsAngle(currentPitch, targetPitch, pitchSpeedDegrees * deltaTime);
            minigunPitch.localRotation = Quaternion.Euler(nextPitch, 0f, 0f);
        }

        private bool ResolveTransforms()
        {
            if (gunnerYaw == null)
            {
                gunnerYaw = transform.Find("GunnerYaw");
            }
            if (minigunPitch == null && gunnerYaw != null)
            {
                minigunPitch = gunnerYaw.Find("MinigunPitch");
            }
            if (barrelSpin == null && minigunPitch != null)
            {
                barrelSpin = minigunPitch.Find("BarrelSpin");
            }
            if (muzzle == null && barrelSpin != null)
            {
                muzzle = barrelSpin.Find("MinigunMuzzle");
            }
            if (occupantAnchor == null)
            {
                occupantAnchor = transform.Find("OccupantAnchor");
            }
            if (leftHandGrip == null)
            {
                leftHandGrip = transform.Find("LeftHandGrip");
            }
            if (rightHandGrip == null)
            {
                rightHandGrip = transform.Find("RightHandGrip");
            }
            if (leftFootAnchor == null)
            {
                leftFootAnchor = transform.Find("LeftFootAnchor");
            }
            if (rightFootAnchor == null)
            {
                rightFootAnchor = transform.Find("RightFootAnchor");
            }
            if (headLookAnchor == null)
            {
                headLookAnchor = transform.Find("HeadLookAnchor");
            }
            return gunnerYaw != null && minigunPitch != null && barrelSpin != null;
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
