using UnityEngine;
using UnityEngine.XR;

namespace QuestCommandRTS
{
    public sealed class QuestTrackedNodePose : MonoBehaviour
    {
        public const float DeviceRefreshIntervalSeconds = 0.5f;

        public XRNode Node;

        private InputDevice device;
        private float nextDeviceRefreshTime;

        private void OnEnable()
        {
            RefreshDevice(Time.unscaledTime);
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            if (ShouldRefreshDevice(device.isValid, now, nextDeviceRefreshTime))
            {
                RefreshDevice(now);
            }

            if (!device.isValid)
            {
                return;
            }

            ApplyDevicePose(transform, device);
        }

        private static void ApplyDevicePose(Transform target, InputDevice source)
        {
            Vector3 position;
            bool hasPosition = source.TryGetFeatureValue(CommonUsages.devicePosition, out position);
            Quaternion rotation;
            bool hasRotation = source.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation);
            ApplyPose(target, hasPosition, position, hasRotation, rotation);
        }

        private static void ApplyPose(Transform target, bool hasPosition, Vector3 position, bool hasRotation, Quaternion rotation)
        {
            if (target == null)
            {
                return;
            }

            if (hasPosition)
            {
                target.localPosition = position;
            }

            if (hasRotation)
            {
                target.localRotation = rotation;
            }
        }

        private void RefreshDevice(float now)
        {
            device = InputDevices.GetDeviceAtXRNode(Node);
            nextDeviceRefreshTime = now + DeviceRefreshIntervalSeconds;
        }

        private static bool ShouldRefreshDevice(bool isDeviceValid, float now, float nextRefreshTime)
        {
            return !isDeviceValid && now >= nextRefreshTime;
        }

#if UNITY_EDITOR
        public static bool ShouldRefreshDeviceForTests(bool isDeviceValid, float now, float nextRefreshTime)
        {
            return ShouldRefreshDevice(isDeviceValid, now, nextRefreshTime);
        }

        public static void ApplyPoseForTests(Transform target, bool hasPosition, Vector3 position, bool hasRotation, Quaternion rotation)
        {
            ApplyPose(target, hasPosition, position, hasRotation, rotation);
        }
#endif
    }
}
