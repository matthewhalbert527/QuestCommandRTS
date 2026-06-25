using UnityEngine;
using UnityEngine.XR;

namespace QuestCommandRTS
{
    public sealed class QuestRtsInputController : MonoBehaviour
    {
        private RtsGame game;
        private RtsCommandDispatcher dispatcher;
        private QuestTabletopSettings settings;
        private Transform rightController;
        private Transform leftController;
        private LineRenderer pointerLine;
        private Transform reticle;
        private Renderer reticleRenderer;
        private Material reticleMaterial;
        private InputDevice rightDevice;
        private InputDevice leftDevice;
        private bool previousRightTrigger;
        private bool previousPrimaryButton;
        private bool previousSecondaryButton;
        private readonly Color moveColor = new Color(0.3f, 0.88f, 1f, 0.95f);
        private readonly Color attackColor = new Color(1f, 0.32f, 0.22f, 0.95f);
        private readonly Color harvestColor = new Color(0.25f, 1f, 0.48f, 0.95f);
        private readonly Color rallyColor = new Color(0.55f, 0.95f, 1f, 0.95f);
        private readonly Color invalidColor = new Color(0.55f, 0.6f, 0.62f, 0.65f);

        public void Initialize(RtsGame owner, RtsCommandDispatcher commandDispatcher, QuestTabletopSettings tabletopSettings, Transform rightHand, Transform leftHand, LineRenderer line, Transform hitReticle)
        {
            game = owner;
            dispatcher = commandDispatcher;
            settings = tabletopSettings;
            rightController = rightHand;
            leftController = leftHand;
            pointerLine = line;
            reticle = hitReticle;
            reticleRenderer = reticle != null ? reticle.GetComponent<Renderer>() : null;
            reticleMaterial = reticleRenderer != null ? reticleRenderer.sharedMaterial : null;
            rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        }

        private void Update()
        {
            if (game == null || dispatcher == null || settings == null || rightController == null)
            {
                return;
            }

            if (!rightDevice.isValid)
            {
                rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            }

            if (!leftDevice.isValid)
            {
                leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            }

            Ray ray = new Ray(rightController.position, rightController.forward);
            RaycastHit hit;
            bool hasHit = dispatcher.TryGetPointerHit(ray, settings.RayLengthSimulationUnits, out hit);
            UpdatePointer(ray, hasHit, hit);

            if (game.BuildManager != null && game.BuildManager.IsPlacing)
            {
                dispatcher.UpdatePlacement(ray);
            }

            bool leftTriggerHeld = ReadButton(leftDevice, CommonUsages.triggerButton, CommonUsages.trigger);
            bool rightTrigger = ReadButton(rightDevice, CommonUsages.triggerButton, CommonUsages.trigger);
            bool primaryButton = ReadButton(rightDevice, CommonUsages.primaryButton);
            bool secondaryButton = ReadButton(rightDevice, CommonUsages.secondaryButton);

            bool rightTriggerDown = rightTrigger && !previousRightTrigger;
            bool primaryDown = primaryButton && !previousPrimaryButton;
            bool secondaryDown = secondaryButton && !previousSecondaryButton;

            previousRightTrigger = rightTrigger;
            previousPrimaryButton = primaryButton;
            previousSecondaryButton = secondaryButton;

            if (game.IsMatchOver)
            {
                return;
            }

            if (secondaryDown)
            {
                dispatcher.CancelPlacementOrClearSelection();
                return;
            }

            if (rightTriggerDown)
            {
                dispatcher.SelectFromRay(ray, leftTriggerHeld, settings.RayLengthSimulationUnits);
            }

            if (primaryDown)
            {
                dispatcher.CommandFromRay(ray, settings.RayLengthSimulationUnits);
            }
        }

        private void UpdatePointer(Ray ray, bool hasHit, RaycastHit hit)
        {
            if (pointerLine == null)
            {
                return;
            }

            Vector3 end = hasHit ? hit.point : ray.GetPoint(settings.RayLengthSimulationUnits);
            pointerLine.SetPosition(0, ray.origin);
            pointerLine.SetPosition(1, end);

            if (reticle == null)
            {
                return;
            }

            reticle.gameObject.SetActive(hasHit);
            if (!hasHit)
            {
                SetPointerColor(invalidColor);
                return;
            }

            reticle.position = hit.point;
            reticle.localScale = Vector3.one * settings.ReticleSizeMeters;
            SetPointerColor(GetPointerColor(dispatcher.ResolveContextCommand(hit)));
        }

        private Color GetPointerColor(RtsContextCommandKind command)
        {
            switch (command)
            {
                case RtsContextCommandKind.Attack:
                    return attackColor;
                case RtsContextCommandKind.Harvest:
                    return harvestColor;
                case RtsContextCommandKind.Rally:
                    return rallyColor;
                case RtsContextCommandKind.Move:
                    return moveColor;
                default:
                    return invalidColor;
            }
        }

        private void SetPointerColor(Color color)
        {
            if (pointerLine != null)
            {
                pointerLine.startColor = color;
                pointerLine.endColor = color;
            }

            if (reticleMaterial != null)
            {
                reticleMaterial.color = color;
                reticleMaterial.SetColor("_Color", color);
                reticleMaterial.SetColor("_BaseColor", color);
            }
        }

        private static bool ReadButton(InputDevice device, InputFeatureUsage<bool> usage)
        {
            bool pressed;
            return device.isValid && device.TryGetFeatureValue(usage, out pressed) && pressed;
        }

        private static bool ReadButton(InputDevice device, InputFeatureUsage<bool> buttonUsage, InputFeatureUsage<float> analogUsage)
        {
            bool pressed;
            if (device.isValid && device.TryGetFeatureValue(buttonUsage, out pressed) && pressed)
            {
                return true;
            }

            float value;
            return device.isValid && device.TryGetFeatureValue(analogUsage, out value) && value >= 0.65f;
        }
    }
}
