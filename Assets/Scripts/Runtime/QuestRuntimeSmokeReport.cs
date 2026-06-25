using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;

namespace QuestCommandRTS
{
    public readonly struct QuestRuntimeSmokeItem
    {
        public readonly string Label;
        public readonly bool Passed;
        public readonly bool Manual;
        public readonly string Detail;

        public QuestRuntimeSmokeItem(string label, bool passed, bool manual, string detail)
        {
            Label = label;
            Passed = passed;
            Manual = manual;
            Detail = detail;
        }
    }

    public static class QuestRuntimeSmokeReport
    {
        public static List<QuestRuntimeSmokeItem> Build(RtsGame game)
        {
            List<QuestRuntimeSmokeItem> results = new List<QuestRuntimeSmokeItem>();

            if (game == null)
            {
                Add(results, "Runtime instance", false, "No RtsGame instance was provided.");
                return results;
            }

            QuestTabletopSettings settings = game.GetComponent<QuestTabletopSettings>();
            QuestTabletopRig rig = game.QuestRig;
            QuestRtsInputController input = game.GetComponent<QuestRtsInputController>();
            QuestWorldHud worldHud = game.GetComponent<QuestWorldHud>();
            QuestTacticalMap tacticalMap = game.GetComponent<QuestTacticalMap>();
            QuestCommandConsole console = game.GetComponent<QuestCommandConsole>();

            Add(results, "Runtime mode", game.RuntimeMode == RtsRuntimeMode.QuestVr, game.RuntimeMode.ToString());
            Add(results, "Desktop command camera absent", game.CommandCamera == null && GameObject.Find("Command Camera") == null, "Quest mode must not create the desktop Command Camera.");
            Add(results, "Desktop input absent", game.GetComponent<RtsInputController>() == null, "Quest mode must not install RtsInputController.");
            Add(results, "Desktop HUD absent", game.GetComponent<RtsHud>() == null, "Quest mode must not install the Screen Space Overlay desktop HUD.");
            string overlayDetail;
            Add(results, "Screen-space overlay canvases absent", HasNoScreenSpaceOverlayCanvases(out overlayDetail), overlayDetail);
            string eventSystemDetail;
            Add(results, "Desktop event system absent", HasNoDesktopEventSystem(out eventSystemDetail), eventSystemDetail);
            string locomotionDetail;
            Add(results, "Quest locomotion components absent", HasNoQuestLocomotionOrManipulationObjects(out locomotionDetail), locomotionDetail);
            Add(results, "Quest rig present", rig != null && rig.RigRoot != null && rig.HeadCamera != null, "QuestTabletopRig should own the tabletop root and XR head camera.");
            Add(results, "Quest settings present", settings != null, "QuestTabletopSettings should own tabletop scale, height, ray, reticle, clip-plane, and world-space UI values.");
            Add(results, "Quest input present", input != null, "QuestRtsInputController should translate controller state into shared dispatcher calls.");
            Add(results, "Quest input uses shared dispatcher", input != null && input.SharedDispatcher == game.CommandDispatcher, "Quest input should reference RtsGame.CommandDispatcher.");
            Add(results, "Quest world HUD present", worldHud != null && HasWorldSpaceCanvas("Quest World Status"), "Quest mode should expose a world-space status panel.");
            Add(results, "Quest world HUD control hints", HasWorldHudControlHints(), "World-space status panel should show trigger, A/B, and X command-console hints.");
            string worldHudDetail;
            Add(results, "Quest world HUD non-interactive", HasNonInteractiveWorldHud(out worldHudDetail), worldHudDetail);
            Add(results, "Quest tactical map present", tacticalMap != null && HasWorldSpaceCanvas("Quest Tactical Map"), "Quest mode should expose the battle map as world-space headset UI.");
            string tacticalMapDetail;
            Add(results, "Quest tactical map non-interactive", HasNonInteractiveTacticalMap(out tacticalMapDetail), tacticalMapDetail);
            Add(results, "Quest command console present", console != null && console.PanelRect != null, "Quest command console should exist under the tabletop rig.");
            Add(results, "Quest production progress meter", HasTransformNamed("Quest Queue Progress Fill") && HasTransformNamed("Quest Queue Progress Text"), "Wrist production tab should include an active unit build progress meter.");
            string uiAnchorDetail;
            Add(results, "Quest world UI anchored to rig", HasQuestWorldUiAnchoredToRig(rig, console, out uiAnchorDetail), uiAnchorDetail);
            string consoleDetail;
            Add(results, "Quest command console panel ray", HasCommandConsolePanelRay(console, out consoleDetail), consoleDetail);
            Add(results, "View camera uses XR head", rig != null && rig.HeadCamera != null && game.GetViewCameraTransform() == rig.HeadCamera.transform, "Game view camera should resolve to the Quest head camera.");

            if (settings != null)
            {
                float minSimulationUnitsPerMeter = 126f / RtsProfileSettingsData.MaxTabletopScale;
                float maxSimulationUnitsPerMeter = 126f / RtsProfileSettingsData.MinTabletopScale;
                bool scaleValid = settings.SimulationUnitsPerMeter >= minSimulationUnitsPerMeter - 0.01f && settings.SimulationUnitsPerMeter <= maxSimulationUnitsPerMeter + 0.01f;
                Add(results, "Tabletop scale", scaleValid, "SimulationUnitsPerMeter=" + settings.SimulationUnitsPerMeter.ToString("0.##") + ", supported=" + minSimulationUnitsPerMeter.ToString("0.##") + "-" + maxSimulationUnitsPerMeter.ToString("0.##"));

                float minBoardWidthMeters = RtsBalance.MapHalfSize * 2f / maxSimulationUnitsPerMeter;
                float maxBoardWidthMeters = RtsBalance.MapHalfSize * 2f / minSimulationUnitsPerMeter;
                bool boardWidthValid = settings.BattlefieldWidthMeters >= minBoardWidthMeters - 0.01f && settings.BattlefieldWidthMeters <= maxBoardWidthMeters + 0.01f;
                Add(results, "Board physical width", boardWidthValid, "Supported " + minBoardWidthMeters.ToString("0.##") + "m to " + maxBoardWidthMeters.ToString("0.##") + "m, actual " + settings.BattlefieldWidthMeters.ToString("0.##") + "m.");

                bool tabletopHeightValid = Mathf.Abs(settings.GetRigRootPosition().y + settings.BoardHeightSimulationUnits) <= 0.01f;
                Add(results, "Tabletop height offset", tabletopHeightValid, "BoardHeightMeters=" + settings.BoardHeightMeters.ToString("0.##"));

                if (rig != null && rig.RigRoot != null)
                {
                    bool rigScaleValid = Vector3.Distance(rig.RigRoot.localScale, Vector3.one * settings.SimulationUnitsPerMeter) <= 0.01f;
                    Add(results, "Rig scale applied", rigScaleValid, "RigRoot.localScale=" + rig.RigRoot.localScale);
                    string comfortViewDetail;
                    Add(results, "Fallback tabletop view", HasComfortableFallbackView(rig, settings, out comfortViewDetail), comfortViewDetail);
                }
            }
            if (rig != null)
            {
                string headNodeDetail;
                Add(results, "Head tracking node", HasTrackedPose(rig.Head, XRNode.Head, out headNodeDetail), headNodeDetail);
                string leftNodeDetail;
                Add(results, "Left controller node", HasTrackedPose(rig.LeftController, XRNode.LeftHand, out leftNodeDetail), leftNodeDetail);
                string rightNodeDetail;
                Add(results, "Right controller node", HasTrackedPose(rig.RightController, XRNode.RightHand, out rightNodeDetail), rightNodeDetail);
                string pointerDetail;
                Add(results, "Pointer visuals", HasPointerVisuals(rig, settings, out pointerDetail), pointerDetail);
            }

            AddManual(results, "Physical headset verification", "HMD pose, controller tracking, ray alignment, comfort, and performance still require Quest Link or device testing.");
            return results;
        }

        private static bool HasTrackedPose(Transform target, XRNode expectedNode, out string detail)
        {
            if (target == null)
            {
                detail = "Tracked transform is missing; expected XRNode." + expectedNode;
                return false;
            }

            QuestTrackedNodePose trackedPose = target.GetComponent<QuestTrackedNodePose>();
            if (trackedPose == null)
            {
                detail = target.name + " is missing QuestTrackedNodePose; expected XRNode." + expectedNode;
                return false;
            }

            detail = target.name + " node=" + trackedPose.Node + ", expected=" + expectedNode;
            return trackedPose.Node == expectedNode;
        }

        private static bool HasTransformNamed(string objectName)
        {
            Transform[] transforms = Object.FindObjectsOfType<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].name == objectName)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasNoScreenSpaceOverlayCanvases(out string detail)
        {
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
            int overlayCount = 0;
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    overlayCount++;
                }
            }

            detail = "overlayCanvases=" + overlayCount + ", totalCanvases=" + canvases.Length;
            return overlayCount == 0;
        }

        private static bool HasNoDesktopEventSystem(out string detail)
        {
            EventSystem[] eventSystems = Object.FindObjectsOfType<EventSystem>(true);
            int standaloneModules = 0;
            for (int i = 0; i < eventSystems.Length; i++)
            {
                EventSystem eventSystem = eventSystems[i];
                if (eventSystem != null && eventSystem.GetComponent<StandaloneInputModule>() != null)
                {
                    standaloneModules++;
                }
            }

            detail = "eventSystems=" + eventSystems.Length + ", standaloneInputModules=" + standaloneModules;
            return eventSystems.Length == 0 && standaloneModules == 0;
        }

        private static bool HasNoQuestLocomotionOrManipulationObjects(out string detail)
        {
            int offendingComponents = 0;
            int offendingObjects = 0;
            string firstOffender = string.Empty;

            MonoBehaviour[] components = Object.FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < components.Length; i++)
            {
                MonoBehaviour component = components[i];
                if (component == null)
                {
                    continue;
                }

                string typeName = component.GetType().FullName;
                if (ContainsStationaryQuestForbiddenPattern(typeName))
                {
                    offendingComponents++;
                    if (firstOffender.Length == 0)
                    {
                        firstOffender = typeName;
                    }
                }
            }

            Transform[] transforms = Object.FindObjectsOfType<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                if (transform == null)
                {
                    continue;
                }

                if (ContainsStationaryQuestForbiddenPattern(transform.name))
                {
                    offendingObjects++;
                    if (firstOffender.Length == 0)
                    {
                        firstOffender = transform.name;
                    }
                }
            }

            detail = "offendingComponents=" + offendingComponents +
                ", offendingObjects=" + offendingObjects +
                (firstOffender.Length > 0 ? ", first=" + firstOffender : string.Empty);
            return offendingComponents == 0 && offendingObjects == 0;
        }

        private static bool ContainsStationaryQuestForbiddenPattern(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return value.IndexOf("Locomotion", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("Teleport", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("SnapTurn", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("ContinuousTurn", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("TurnProvider", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("GrabInteractable", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("XRGrab", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("Climb", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasComfortableFallbackView(QuestTabletopRig rig, QuestTabletopSettings settings, out string detail)
        {
            if (rig == null || rig.HeadCamera == null || settings == null)
            {
                detail = "Quest rig, head camera, and settings are required.";
                return false;
            }

            Transform head = rig.HeadCamera.transform;
            Vector3 headPosition = head.position;
            float boardHalfWidth = RtsBalance.MapHalfSize;
            float distanceOutsideNearEdgeMeters = (-boardHalfWidth - headPosition.z) / settings.SimulationUnitsPerMeter;
            float eyeHeightAboveBoardMeters = headPosition.y / settings.SimulationUnitsPerMeter;
            Vector3 toCenter = (Vector3.zero - headPosition).normalized;
            float facingCenterDot = Vector3.Dot(head.forward.normalized, toCenter);

            bool outsideNearEdge = headPosition.z < -boardHalfWidth - settings.SimulationUnitsPerMeter * 0.15f;
            bool comfortableEyeHeight = eyeHeightAboveBoardMeters >= 0.4f && eyeHeightAboveBoardMeters <= 1.4f;
            bool facingCenter = facingCenterDot >= 0.75f;
            detail = "outsideNearEdgeMeters=" + distanceOutsideNearEdgeMeters.ToString("0.##") +
                ", eyeHeightAboveBoardMeters=" + eyeHeightAboveBoardMeters.ToString("0.##") +
                ", facingCenterDot=" + facingCenterDot.ToString("0.##");
            return outsideNearEdge && comfortableEyeHeight && facingCenter;
        }

        private static bool HasWorldSpaceCanvas(string objectName)
        {
            GameObject canvasObject = GameObject.Find(objectName);
            if (canvasObject == null)
            {
                return false;
            }

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            return canvas != null && canvas.renderMode == RenderMode.WorldSpace;
        }

        private static bool HasWorldHudControlHints()
        {
            GameObject canvasObject = GameObject.Find("Quest World Status");
            if (canvasObject == null)
            {
                return false;
            }

            Text[] textBlocks = canvasObject.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < textBlocks.Length; i++)
            {
                Text text = textBlocks[i];
                if (text == null || string.IsNullOrEmpty(text.text))
                {
                    continue;
                }

                string value = text.text;
                if (value.Contains("Trigger: Select") &&
                    value.Contains("A: Command") &&
                    value.Contains("B: Cancel/Clear") &&
                    value.Contains("X: Command Console"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasNonInteractiveWorldHud(out string detail)
        {
            return HasNonInteractiveCanvas("Quest World Status", out detail);
        }

        private static bool HasNonInteractiveTacticalMap(out string detail)
        {
            return HasNonInteractiveCanvas("Quest Tactical Map", out detail);
        }

        private static bool HasNonInteractiveCanvas(string objectName, out string detail)
        {
            GameObject canvasObject = GameObject.Find(objectName);
            if (canvasObject == null)
            {
                detail = objectName + " object is missing.";
                return false;
            }

            GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
            Graphic[] graphics = canvasObject.GetComponentsInChildren<Graphic>(true);
            int raycastTargets = 0;
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic != null && graphic.raycastTarget)
                {
                    raycastTargets++;
                }
            }

            bool raycasterDisabled = raycaster != null && !raycaster.enabled;
            detail = "raycaster=" + (raycasterDisabled ? "disabled" : "enabled-or-missing") +
                ", raycastTargets=" + raycastTargets +
                ", graphics=" + graphics.Length;
            return raycasterDisabled && raycastTargets == 0;
        }

        private static bool HasQuestWorldUiAnchoredToRig(QuestTabletopRig rig, QuestCommandConsole console, out string detail)
        {
            if (rig == null || rig.RigRoot == null)
            {
                detail = "Quest rig root is missing.";
                return false;
            }

            Transform status = FindDescendant(rig.RigRoot, "Quest World Status");
            Transform tacticalMap = FindDescendant(rig.RigRoot, "Quest Tactical Map");
            Transform commandConsole = console != null && console.PanelRect != null ? console.PanelRect.transform : FindDescendant(rig.RigRoot, "Quest Command Console");

            bool statusAnchored = status != null && status.IsChildOf(rig.RigRoot);
            bool tacticalMapAnchored = tacticalMap != null && tacticalMap.IsChildOf(rig.RigRoot);
            bool commandConsoleAnchored = commandConsole != null && commandConsole.IsChildOf(rig.RigRoot);
            detail = "status=" + (statusAnchored ? "anchored" : "missing-or-detached") +
                ", tacticalMap=" + (tacticalMapAnchored ? "anchored" : "missing-or-detached") +
                ", commandConsole=" + (commandConsoleAnchored ? "anchored" : "missing-or-detached");
            return statusAnchored && tacticalMapAnchored && commandConsoleAnchored;
        }

        private static bool HasCommandConsolePanelRay(QuestCommandConsole console, out string detail)
        {
            if (console == null || console.PanelRect == null)
            {
                detail = "Quest command console or panel rect is missing.";
                return false;
            }

            bool wasOpen = console.IsOpen;
            try
            {
                console.SetOpen(true);
                Rect panelRect = console.PanelRect.rect;
                Vector3 panelCenter = console.PanelRect.TransformPoint(new Vector3(panelRect.center.x, panelRect.center.y, 0f));
                Ray panelRay = new Ray(panelCenter - console.PanelRect.forward * 8f, console.PanelRect.forward);
                bool hit = console.TryGetPanelHit(panelRay, out Vector3 hitPoint);
                bool captured = console.TryHandlePointer(panelRay, false);
                bool centerHit = hit && Vector3.Distance(hitPoint, panelCenter) <= 0.001f;

                detail = "initialOpen=" + wasOpen +
                    ", panelHit=" + hit +
                    ", centerHit=" + centerHit +
                    ", hoverCaptured=" + captured;
                return centerHit && captured;
            }
            finally
            {
                console.SetOpen(wasOpen);
            }
        }

        private static bool HasPointerVisuals(QuestTabletopRig rig, QuestTabletopSettings settings, out string detail)
        {
            if (rig == null || rig.RigRoot == null || settings == null)
            {
                detail = "Quest rig, rig root, and settings are required before pointer visuals can be checked.";
                return false;
            }

            Transform lineTransform = FindDescendant(rig.RigRoot, "Right Controller RTS Ray");
            Transform reticleTransform = FindDescendant(rig.RigRoot, "RTS Pointer Reticle");
            LineRenderer line = lineTransform != null ? lineTransform.GetComponent<LineRenderer>() : null;
            Renderer reticleRenderer = reticleTransform != null ? reticleTransform.GetComponent<Renderer>() : null;
            Collider reticleCollider = reticleTransform != null ? reticleTransform.GetComponent<Collider>() : null;

            bool lineValid = line != null &&
                line.useWorldSpace &&
                line.positionCount == 2 &&
                line.sharedMaterial != null &&
                Mathf.Abs(line.widthMultiplier - settings.RayWidthSimulationUnits) <= 0.001f;
            bool reticleValid = reticleTransform != null &&
                reticleRenderer != null &&
                reticleRenderer.sharedMaterial != null &&
                reticleCollider == null &&
                Vector3.Distance(reticleTransform.localScale, Vector3.one * settings.ReticleSizeMeters) <= 0.001f;

            detail = "line=" + (lineValid ? "ok" : "invalid") +
                ", reticle=" + (reticleValid ? "ok" : "invalid") +
                ", expectedWidth=" + settings.RayWidthSimulationUnits.ToString("0.###") +
                ", expectedReticleMeters=" + settings.ReticleSizeMeters.ToString("0.###");
            return lineValid && reticleValid;
        }

        private static Transform FindDescendant(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == objectName)
                {
                    return children[i];
                }
            }

            return null;
        }

        private static void Add(List<QuestRuntimeSmokeItem> results, string label, bool passed, string detail)
        {
            results.Add(new QuestRuntimeSmokeItem(label, passed, false, detail));
        }

        private static void AddManual(List<QuestRuntimeSmokeItem> results, string label, string detail)
        {
            results.Add(new QuestRuntimeSmokeItem(label, false, true, detail));
        }
    }
}
