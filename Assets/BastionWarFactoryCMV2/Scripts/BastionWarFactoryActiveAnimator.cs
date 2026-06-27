
using UnityEngine;

namespace BastionWarFactoryCMV2
{
    public class BastionWarFactoryActiveAnimator : MonoBehaviour
    {
        [Header("Moving pieces")]
        public Transform door;
        public Vector3 doorOpenOffset = new Vector3(0f, 1.95f, 0f);
        public bool doorOpenOnStart = true;
        public float doorSpeed = 2.2f;
        public Transform conveyor;
        public float conveyorBobAmount = 0.04f;
        public float conveyorSpeed = 1.2f;
        public Transform gantryTrolley;
        public float gantryTravel = 2.3f;
        public float gantrySpeed = 0.35f;
        public Transform[] roofFans;
        public float fanDegreesPerSecond = 220f;
        public Transform[] robotArms;
        public float robotArmDegrees = 7.5f;
        public float robotArmSpeed = 0.7f;
        public Transform[] beacons;
        public float beaconDegreesPerSecond = 85f;

        [Header("Smoke")]
        public Transform[] smokeSockets;
        public bool createSmokeOnStart = true;
        public float smokeEmissionRate = 7f;
        public float smokeLifetime = 3.0f;
        public float smokeSpeed = 0.65f;

        private Vector3 doorClosed;
        private Vector3 conveyorHome;
        private Vector3 gantryHome;
        private Quaternion[] armHome;
        private bool doorIsOpen;

        private void Awake()
        {
            if (door != null) doorClosed = door.localPosition;
            if (conveyor != null) conveyorHome = conveyor.localPosition;
            if (gantryTrolley != null) gantryHome = gantryTrolley.localPosition;
            if (robotArms != null)
            {
                armHome = new Quaternion[robotArms.Length];
                for (int i = 0; i < robotArms.Length; i++)
                    armHome[i] = robotArms[i] != null ? robotArms[i].localRotation : Quaternion.identity;
            }
            doorIsOpen = doorOpenOnStart;
        }

        private void Start()
        {
            if (createSmokeOnStart) CreateSmokeEmitters();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (door != null)
            {
                Vector3 target = doorClosed + (doorIsOpen ? doorOpenOffset : Vector3.zero);
                door.localPosition = Vector3.MoveTowards(door.localPosition, target, doorSpeed * dt);
            }
            if (conveyor != null)
            {
                Vector3 p = conveyorHome;
                p.y += Mathf.Sin(Time.time * conveyorSpeed) * conveyorBobAmount;
                conveyor.localPosition = p;
            }
            if (gantryTrolley != null)
            {
                Vector3 p = gantryHome;
                p.x += Mathf.Sin(Time.time * gantrySpeed) * gantryTravel;
                gantryTrolley.localPosition = p;
            }
            if (roofFans != null)
            {
                foreach (Transform fan in roofFans)
                    if (fan != null) fan.Rotate(Vector3.up, fanDegreesPerSecond * dt, Space.Self);
            }
            if (beacons != null)
            {
                foreach (Transform b in beacons)
                    if (b != null) b.Rotate(Vector3.up, beaconDegreesPerSecond * dt, Space.Self);
            }
            if (robotArms != null && armHome != null)
            {
                float wave = Mathf.Sin(Time.time * robotArmSpeed) * robotArmDegrees;
                for (int i = 0; i < robotArms.Length; i++)
                {
                    if (robotArms[i] == null) continue;
                    float sign = (i % 2 == 0) ? 1f : -1f;
                    robotArms[i].localRotation = armHome[i] * Quaternion.Euler(0f, sign * wave, sign * wave * 0.35f);
                }
            }
        }

        public void SetDoorOpen(bool open) => doorIsOpen = open;
        public void OpenDoor() => SetDoorOpen(true);
        public void CloseDoor() => SetDoorOpen(false);

        private void CreateSmokeEmitters()
        {
            if (smokeSockets == null) return;
            foreach (Transform socket in smokeSockets)
            {
                if (socket == null) continue;
                GameObject go = new GameObject("LightweightSmokeFX");
                go.transform.SetParent(socket, false);
                QuestCommandRTS.RtsSmokeEmitter smoke = go.AddComponent<QuestCommandRTS.RtsSmokeEmitter>();
                smoke.Interval = Mathf.Max(0.08f, 1f / Mathf.Max(0.1f, smokeEmissionRate));
                smoke.Jitter = 0.12f;
                smoke.Radius = 0.16f;
                smoke.Lifetime = smokeLifetime;
                smoke.MaxScale = Mathf.Lerp(0.7f, 1.25f, Mathf.Clamp01(smokeSpeed));
                smoke.SmokeColor = new Color(0.32f, 0.33f, 0.31f, 0.38f);
            }
        }
    }
}
