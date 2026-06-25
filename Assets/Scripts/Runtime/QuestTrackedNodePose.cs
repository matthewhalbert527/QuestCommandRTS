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

            Vector3 position;
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out position))
            {
                transform.localPosition = position;
            }

            Quaternion rotation;
            if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation))
            {
                transform.localRotation = rotation;
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
#endif
    }
}
