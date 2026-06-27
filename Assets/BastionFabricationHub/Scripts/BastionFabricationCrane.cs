using UnityEngine;

namespace BastionFabrication
{
    [DisallowMultipleComponent]
    public sealed class BastionFabricationCrane : MonoBehaviour
    {
        [SerializeField] private Transform yawPivot;
        [SerializeField] private Transform boomPitch;
        [SerializeField] private Transform trolley;
        [SerializeField] private Transform hookSocket;
        [SerializeField, Min(0f)] private float yawSpeedDegrees = 45f;
        [SerializeField, Min(0f)] private float boomSpeedDegrees = 25f;
        [SerializeField, Min(0f)] private float trolleySpeedMeters = 2.2f;
        [SerializeField] private float minimumBoomPitch = -18f;
        [SerializeField] private float maximumBoomPitch = 18f;
        [SerializeField] private float minimumTrolleyZ = 0.25f;
        [SerializeField] private float maximumTrolleyZ = 4.25f;

        private bool hasTarget;
        private Vector3 targetWorld;
        private float trolleyBaseY;
        private float trolleyBaseX;

        public Transform HookSocket => hookSocket;

        private void Awake()
        {
            if (trolley != null)
            {
                trolleyBaseX = trolley.localPosition.x;
                trolleyBaseY = trolley.localPosition.y;
            }
        }

        public void Configure(Transform newYawPivot, Transform newBoomPitch, Transform newTrolley,
            Transform newHookSocket, float yawSpeed, float boomSpeed, float trolleySpeed,
            float minPitch, float maxPitch, float minTrolley, float maxTrolley)
        {
            yawPivot = newYawPivot;
            boomPitch = newBoomPitch;
            trolley = newTrolley;
            hookSocket = newHookSocket;
            yawSpeedDegrees = Mathf.Max(0f, yawSpeed);
            boomSpeedDegrees = Mathf.Max(0f, boomSpeed);
            trolleySpeedMeters = Mathf.Max(0f, trolleySpeed);
            minimumBoomPitch = minPitch;
            maximumBoomPitch = maxPitch;
            minimumTrolleyZ = minTrolley;
            maximumTrolleyZ = maxTrolley;
            if (trolley != null)
            {
                trolleyBaseX = trolley.localPosition.x;
                trolleyBaseY = trolley.localPosition.y;
            }
        }

        public void SetWorkPoint(Vector3 worldPoint)
        {
            targetWorld = worldPoint;
            hasTarget = true;
        }

        public void ClearWorkPoint()
        {
            hasTarget = false;
        }

        private void LateUpdate()
        {
            if (!hasTarget || yawPivot == null || boomPitch == null || trolley == null) return;

            Transform parent = yawPivot.parent;
            Vector3 localDirection = parent != null
                ? parent.InverseTransformDirection(targetWorld - yawPivot.position)
                : targetWorld - yawPivot.position;
            localDirection.y = 0f;
            if (localDirection.sqrMagnitude > 0.0001f)
            {
                float targetYaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
                float currentYaw = NormalizeAngle(yawPivot.localEulerAngles.y);
                float nextYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, yawSpeedDegrees * Time.deltaTime);
                yawPivot.localRotation = Quaternion.Euler(0f, nextYaw, 0f);
            }

            Vector3 boomLocal = yawPivot.InverseTransformPoint(targetWorld);
            float horizontal = Mathf.Max(0.01f, new Vector2(boomLocal.x, boomLocal.z).magnitude);
            float requestedPitch = -Mathf.Atan2(boomLocal.y - boomPitch.localPosition.y, horizontal) * Mathf.Rad2Deg;
            requestedPitch = Mathf.Clamp(requestedPitch, minimumBoomPitch, maximumBoomPitch);
            float currentPitch = NormalizeAngle(boomPitch.localEulerAngles.x);
            float nextPitch = Mathf.MoveTowardsAngle(currentPitch, requestedPitch, boomSpeedDegrees * Time.deltaTime);
            boomPitch.localRotation = Quaternion.Euler(nextPitch, 0f, 0f);

            Vector3 targetInBoom = boomPitch.InverseTransformPoint(targetWorld);
            float requestedZ = Mathf.Clamp(targetInBoom.z, minimumTrolleyZ, maximumTrolleyZ);
            Vector3 desired = new Vector3(trolleyBaseX, trolleyBaseY, requestedZ);
            trolley.localPosition = Vector3.MoveTowards(trolley.localPosition, desired, trolleySpeedMeters * Time.deltaTime);
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
