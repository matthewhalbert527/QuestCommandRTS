using UnityEngine;

namespace BastionStructures
{
    [DisallowMultipleComponent]
    public sealed class BastionRepairBayController : MonoBehaviour
    {
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private float inactiveAngle = 0f;
        [SerializeField] private float activeAngle = 18f;
        [SerializeField, Min(0f)] private float degreesPerSecond = 50f;
        [SerializeField] private bool serviceActive;

        public bool ServiceActive => serviceActive;

        public void Configure(Transform newLeftArm, Transform newRightArm, float newInactiveAngle, float newActiveAngle, float speed)
        {
            leftArm = newLeftArm;
            rightArm = newRightArm;
            inactiveAngle = newInactiveAngle;
            activeAngle = newActiveAngle;
            degreesPerSecond = Mathf.Max(0f, speed);
        }

        public void SetServiceActive(bool active)
        {
            serviceActive = active;
        }

        private void Update()
        {
            float target = serviceActive ? activeAngle : inactiveAngle;
            if (leftArm != null)
            {
                float z = MoveAngle(leftArm.localEulerAngles.z, -target);
                leftArm.localRotation = Quaternion.Euler(0f, 0f, z);
            }
            if (rightArm != null)
            {
                float z = MoveAngle(rightArm.localEulerAngles.z, target);
                rightArm.localRotation = Quaternion.Euler(0f, 0f, z);
            }
        }

        private float MoveAngle(float current, float target)
        {
            current = current > 180f ? current - 360f : current;
            return Mathf.MoveTowardsAngle(current, target, degreesPerSecond * Time.deltaTime);
        }
    }
}
