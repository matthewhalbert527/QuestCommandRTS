using UnityEngine;
using UnityEngine.XR;

namespace QuestCommandRTS
{
    public struct QuestRtsInputFrame
    {
        public readonly Ray PointerRay;
        public readonly bool LeftTriggerHeld;
        public readonly bool RightTriggerHeld;
        public readonly bool PrimaryButtonHeld;
        public readonly bool SecondaryButtonHeld;
        public readonly bool LeftPrimaryButtonHeld;

        public QuestRtsInputFrame(Ray pointerRay, bool leftTriggerHeld, bool rightTriggerHeld, bool primaryButtonHeld, bool secondaryButtonHeld, bool leftPrimaryButtonHeld)
        {
            PointerRay = pointerRay;
            LeftTriggerHeld = leftTriggerHeld;
            RightTriggerHeld = rightTriggerHeld;
            PrimaryButtonHeld = primaryButtonHeld;
            SecondaryButtonHeld = secondaryButtonHeld;
            LeftPrimaryButtonHeld = leftPrimaryButtonHeld;
        }
    }

    public sealed class QuestRtsInputController : MonoBehaviour
    {
        public const float DeviceRefreshIntervalSeconds = 0.5f;
        public const float AreaSelectionRadiusSimulationUnits = 7f;

        private RtsGame game;
        private RtsCommandDispatcher dispatcher;
        private QuestTabletopSettings settings;
        private Transform rightController;
        private Transform leftController;
        private LineRenderer pointerLine;
        private Transform reticle;
        private Renderer reticleRenderer;
        private Material reticleMaterial;
        private QuestCommandConsole commandConsole;
        private InputDevice rightDevice;
        private InputDevice leftDevice;
        private float nextRightDeviceRefreshTime;
        private float nextLeftDeviceRefreshTime;
        private bool previousRightTrigger;
        private bool previousPrimaryButton;
        private bool previousSecondaryButton;
        private bool previousLeftPrimaryButton;
        private bool hasPointerColor;
        private Color currentPointerColor;
        private readonly Color moveColor = new Color(0.3f, 0.88f, 1f, 0.95f);
        private readonly Color attackColor = new Color(1f, 0.32f, 0.22f, 0.95f);
        private readonly Color harvestColor = new Color(0.25f, 1f, 0.48f, 0.95f);
        private readonly Color rallyColor = new Color(0.55f, 0.95f, 1f, 0.95f);
        private readonly Color uiColor = new Color(0.72f, 0.92f, 1f, 0.95f);
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
            float now = Time.unscaledTime;
            RefreshDevice(XRNode.RightHand, ref rightDevice, now, ref nextRightDeviceRefreshTime);
            RefreshDevice(XRNode.LeftHand, ref leftDevice, now, ref nextLeftDeviceRefreshTime);
        }

        public void SetCommandConsole(QuestCommandConsole console)
        {
            commandConsole = console;
        }

        private void Update()
        {
            if (game == null || dispatcher == null || settings == null || rightController == null)
            {
                return;
            }

            if (!game.AcceptsSystemInput)
            {
                previousRightTrigger = false;
                previousPrimaryButton = false;
                previousSecondaryButton = false;
                previousLeftPrimaryButton = false;
                SetPointerVisible(false);
                return;
            }

            SetPointerVisible(true);

            float now = Time.unscaledTime;
            RefreshDeviceIfNeeded(XRNode.RightHand, ref rightDevice, now, ref nextRightDeviceRefreshTime);
            RefreshDeviceIfNeeded(XRNode.LeftHand, ref leftDevice, now, ref nextLeftDeviceRefreshTime);

            Ray ray = new Ray(rightController.position, rightController.forward);
            bool leftTriggerHeld = ReadButton(leftDevice, CommonUsages.triggerButton, CommonUsages.trigger);
            bool rightTrigger = ReadButton(rightDevice, CommonUsages.triggerButton, CommonUsages.trigger);
            bool primaryButton = ReadButton(rightDevice, CommonUsages.primaryButton);
            bool secondaryButton = ReadButton(rightDevice, CommonUsages.secondaryButton);
            bool leftPrimaryButton = ReadButton(leftDevice, CommonUsages.primaryButton);

            ProcessInputFrame(new QuestRtsInputFrame(ray, leftTriggerHeld, rightTrigger, primaryButton, secondaryButton, leftPrimaryButton), true);
        }

#if UNITY_EDITOR
        public LineRenderer PointerLineForTests => pointerLine;
        public Transform ReticleForTests => reticle;

        public RtsCommandResult ProcessInputFrameForTests(QuestRtsInputFrame frame, bool updatePointerFeedback)
        {
            return ProcessInputFrame(frame, updatePointerFeedback);
        }

        public static bool ShouldRefreshDeviceForTests(bool isDeviceValid, float now, float nextRefreshTime)
        {
            return ShouldRefreshDevice(isDeviceValid, now, nextRefreshTime);
        }
#endif

        private RtsCommandResult ProcessInputFrame(QuestRtsInputFrame frame, bool updatePointerFeedback)
        {
            if (game == null || dispatcher == null || settings == null)
            {
                return RtsCommandResult.None;
            }

            if (!game.AcceptsSystemInput)
            {
                ResetButtonState();
                SetPointerVisible(false);
                return RtsCommandResult.None;
            }

            if (updatePointerFeedback)
            {
                SetPointerVisible(true);
            }

            bool rightTriggerDown = frame.RightTriggerHeld && !previousRightTrigger;
            bool primaryDown = frame.PrimaryButtonHeld && !previousPrimaryButton;
            bool secondaryDown = frame.SecondaryButtonHeld && !previousSecondaryButton;
            bool leftPrimaryDown = frame.LeftPrimaryButtonHeld && !previousLeftPrimaryButton;

            previousRightTrigger = frame.RightTriggerHeld;
            previousPrimaryButton = frame.PrimaryButtonHeld;
            previousSecondaryButton = frame.SecondaryButtonHeld;
            previousLeftPrimaryButton = frame.LeftPrimaryButtonHeld;

            if (leftPrimaryDown && commandConsole != null)
            {
                commandConsole.ToggleOpen();
            }

            bool uiCaptured = commandConsole != null && commandConsole.TryHandlePointer(frame.PointerRay, rightTriggerDown);
            Vector3 panelPoint = Vector3.zero;
            bool panelHit = commandConsole != null && commandConsole.TryGetPanelHit(frame.PointerRay, out panelPoint);
            if (updatePointerFeedback)
            {
                if (panelHit)
                {
                    UpdatePointer(frame.PointerRay, true, panelPoint, uiColor);
                }
                else
                {
                    RaycastHit hit;
                    bool hasHit = dispatcher.TryGetPointerHit(frame.PointerRay, settings.RayLengthSimulationUnits, out hit);
                    UpdatePointer(frame.PointerRay, hasHit, hit);
                }
            }

            if (!game.AcceptsPlayerInput)
            {
                return RtsCommandResult.None;
            }

            if (game.IsMatchOver)
            {
                return RtsCommandResult.None;
            }

            if (game.BuildManager != null && game.BuildManager.IsPlacing)
            {
                if (!uiCaptured)
                {
                    dispatcher.UpdatePlacement(frame.PointerRay, settings.RayLengthSimulationUnits);
                }

                RtsCommandResult placementResult = RtsCommandResult.None;
                if (primaryDown && !uiCaptured)
                {
                    placementResult = dispatcher.ConfirmPlacement();
                }

                if (secondaryDown)
                {
                    placementResult = dispatcher.CancelPlacement();
                }

                return placementResult;
            }

            if (secondaryDown)
            {
                if (frame.LeftTriggerHeld)
                {
                    return dispatcher.StopSelectedUnits();
                }

                return dispatcher.CancelPlacementOrClearSelection();
            }

            RtsCommandResult result = RtsCommandResult.None;
            if (rightTriggerDown && !uiCaptured)
            {
                result = dispatcher.SelectFromRay(frame.PointerRay, frame.LeftTriggerHeld, settings.RayLengthSimulationUnits);
                if (result == RtsCommandResult.None && frame.LeftTriggerHeld)
                {
                    result = dispatcher.SelectPlayerUnitsNearRay(frame.PointerRay, settings.RayLengthSimulationUnits, AreaSelectionRadiusSimulationUnits, true);
                }
            }

            if (primaryDown && !uiCaptured)
            {
                if (frame.LeftTriggerHeld)
                {
                    result = dispatcher.AttackMoveFromRay(frame.PointerRay, settings.RayLengthSimulationUnits);
                }
                else
                {
                    result = dispatcher.CommandFromRay(frame.PointerRay, settings.RayLengthSimulationUnits);
                }
            }

            return result;
        }

        private void ResetButtonState()
        {
            previousRightTrigger = false;
            previousPrimaryButton = false;
            previousSecondaryButton = false;
            previousLeftPrimaryButton = false;
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
                if (!hasHit)
                {
                    SetPointerColor(invalidColor);
                }

                return;
            }

            SetReticleVisible(hasHit);
            if (!hasHit)
            {
                SetPointerColor(invalidColor);
                return;
            }

            reticle.position = hit.point;
            reticle.localScale = Vector3.one * settings.ReticleSizeMeters;
            SetPointerColor(GetPointerColor(dispatcher.ResolveContextCommand(hit)));
        }

        private void UpdatePointer(Ray ray, bool hasHit, Vector3 hitPoint, Color color)
        {
            if (pointerLine == null)
            {
                return;
            }

            Vector3 end = hasHit ? hitPoint : ray.GetPoint(settings.RayLengthSimulationUnits);
            pointerLine.SetPosition(0, ray.origin);
            pointerLine.SetPosition(1, end);

            if (reticle == null)
            {
                return;
            }

            SetReticleVisible(hasHit);
            if (hasHit)
            {
                reticle.position = hitPoint;
                reticle.localScale = Vector3.one * settings.ReticleSizeMeters;
            }

            SetPointerColor(color);
        }

        private void SetPointerVisible(bool visible)
        {
            if (pointerLine != null && pointerLine.enabled != visible)
            {
                pointerLine.enabled = visible;
            }

            if (!visible)
            {
                SetReticleVisible(false);
            }
        }

        private Color GetPointerColor(RtsContextCommandKind command)
        {
            switch (command)
            {
                case RtsContextCommandKind.Attack:
                    return attackColor;
                case RtsContextCommandKind.Harvest:
                    return harvestColor;
                case RtsContextCommandKind.Repair:
                    return new Color(0.5f, 1f, 0.78f, 0.96f);
                case RtsContextCommandKind.Board:
                    return new Color(0.55f, 0.95f, 1f, 0.96f);
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
            if (hasPointerColor && currentPointerColor == color)
            {
                return;
            }

            hasPointerColor = true;
            currentPointerColor = color;

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

        private void SetReticleVisible(bool visible)
        {
            if (reticle != null && reticle.gameObject.activeSelf != visible)
            {
                reticle.gameObject.SetActive(visible);
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

        private static void RefreshDeviceIfNeeded(XRNode node, ref InputDevice device, float now, ref float nextRefreshTime)
        {
            if (ShouldRefreshDevice(device.isValid, now, nextRefreshTime))
            {
                RefreshDevice(node, ref device, now, ref nextRefreshTime);
            }
        }

        private static void RefreshDevice(XRNode node, ref InputDevice device, float now, ref float nextRefreshTime)
        {
            device = InputDevices.GetDeviceAtXRNode(node);
            nextRefreshTime = now + DeviceRefreshIntervalSeconds;
        }

        private static bool ShouldRefreshDevice(bool isDeviceValid, float now, float nextRefreshTime)
        {
            return !isDeviceValid && now >= nextRefreshTime;
        }
    }
}
