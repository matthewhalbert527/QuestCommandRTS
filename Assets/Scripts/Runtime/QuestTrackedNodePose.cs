using UnityEngine;
using UnityEngine.XR;

namespace QuestCommandRTS
{
    public sealed class QuestTrackedNodePose : MonoBehaviour
    {
        public XRNode Node;

        private InputDevice device;

        private void OnEnable()
        {
            RefreshDevice();
        }

        private void Update()
        {
            if (!device.isValid)
            {
                RefreshDevice();
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

        private void RefreshDevice()
        {
            device = InputDevices.GetDeviceAtXRNode(Node);
        }
    }
}
