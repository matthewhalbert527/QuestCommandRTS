using UnityEngine;
using UnityEngine.XR;

namespace QuestCommandRTS
{
    public sealed class QuestTabletopRig : MonoBehaviour
    {
        public Camera HeadCamera { get; private set; }
        public Transform RigRoot { get; private set; }
        public Transform Head { get; private set; }
        public Transform LeftController { get; private set; }
        public Transform RightController { get; private set; }
        public QuestCommandConsole CommandConsole { get; private set; }

        private QuestTabletopSettings settings;

        public void Initialize(RtsGame game, RtsCommandDispatcher dispatcher)
        {
            settings = gameObject.AddComponent<QuestTabletopSettings>();
            settings.ApplyProfile(game != null && game.ProfileSettings != null ? game.ProfileSettings.Data : null);

            GameObject rigObject = new GameObject("Quest Tabletop Rig");
            RigRoot = rigObject.transform;
            RigRoot.SetParent(transform, false);
            RigRoot.position = settings.GetRigRootPosition();
            RigRoot.rotation = Quaternion.Euler(0f, settings.InitialYawDegrees, 0f);
            RigRoot.localScale = Vector3.one * settings.SimulationUnitsPerMeter;

            Head = CreateTrackedNode("XR Head", XRNode.Head, settings.FallbackHeadLocalPositionMeters, settings.FallbackHeadEulerDegrees);
            HeadCamera = Head.gameObject.AddComponent<Camera>();
            HeadCamera.clearFlags = CameraClearFlags.SolidColor;
            HeadCamera.backgroundColor = new Color(0.035f, 0.04f, 0.05f);
            HeadCamera.nearClipPlane = settings.CameraNearClipSimulationUnits;
            HeadCamera.farClipPlane = settings.CameraFarClipSimulationUnits;
            HeadCamera.stereoTargetEye = StereoTargetEyeMask.Both;
            HeadCamera.gameObject.tag = "MainCamera";
            DisableCompetingCameras(HeadCamera);

            if (Head.GetComponent<AudioListener>() == null)
            {
                Head.gameObject.AddComponent<AudioListener>();
            }

            LeftController = CreateTrackedNode("Left Controller", XRNode.LeftHand, settings.FallbackLeftControllerLocalPositionMeters, settings.FallbackControllerEulerDegrees);
            RightController = CreateTrackedNode("Right Controller", XRNode.RightHand, settings.FallbackRightControllerLocalPositionMeters, settings.FallbackControllerEulerDegrees);

            LineRenderer pointerLine = CreatePointerLine();
            GameObject reticle = CreateReticle();

            QuestRtsInputController input = gameObject.AddComponent<QuestRtsInputController>();
            input.Initialize(game, dispatcher, settings, RightController, LeftController, pointerLine, reticle.transform);

            QuestWorldHud hud = gameObject.AddComponent<QuestWorldHud>();
            hud.Initialize(game, RigRoot, settings);

            CommandConsole = gameObject.AddComponent<QuestCommandConsole>();
            CommandConsole.Initialize(game, RigRoot, settings);

            input.SetCommandConsole(CommandConsole);
        }

        private Transform CreateTrackedNode(string nodeName, XRNode node, Vector3 fallbackLocalPosition, Vector3 fallbackEulerDegrees)
        {
            GameObject nodeObject = new GameObject(nodeName);
            Transform nodeTransform = nodeObject.transform;
            nodeTransform.SetParent(RigRoot, false);
            nodeTransform.localPosition = fallbackLocalPosition;
            nodeTransform.localRotation = Quaternion.Euler(fallbackEulerDegrees);

            QuestTrackedNodePose trackedPose = nodeObject.AddComponent<QuestTrackedNodePose>();
            trackedPose.Node = node;
            return nodeTransform;
        }

        private LineRenderer CreatePointerLine()
        {
            GameObject lineObject = new GameObject("Right Controller RTS Ray");
            lineObject.transform.SetParent(RigRoot, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.widthMultiplier = settings.RayWidthSimulationUnits;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.material = RtsGame.CreateMaterial(new Color(0.28f, 0.88f, 1f, 0.85f));
            return line;
        }

        private GameObject CreateReticle()
        {
            GameObject reticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reticle.name = "RTS Pointer Reticle";
            reticle.transform.SetParent(RigRoot, true);
            reticle.transform.localScale = Vector3.one * settings.ReticleSizeMeters;

            Collider collider = reticle.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(collider);
                }
                else
                {
                    DestroyImmediate(collider);
                }
            }

            Renderer renderer = reticle.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = RtsGame.CreateMaterial(new Color(0.3f, 0.95f, 1f, 0.9f));
            }

            reticle.SetActive(false);
            return reticle;
        }

        private static void DisableCompetingCameras(Camera activeCamera)
        {
            Camera[] cameras = Object.FindObjectsOfType<Camera>();
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera == null || camera == activeCamera)
                {
                    continue;
                }

                camera.enabled = false;
                if (camera.CompareTag("MainCamera"))
                {
                    camera.gameObject.tag = "Untagged";
                }
            }
        }
    }
}
