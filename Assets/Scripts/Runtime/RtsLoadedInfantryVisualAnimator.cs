using System.Collections.Generic;
using UnityEngine;

namespace QuestCommandRTS
{
    [DisallowMultipleComponent]
    public sealed class RtsLoadedInfantryVisualAnimator : MonoBehaviour
    {
        private const float IdleFrequency = 1.45f;
        private const float IdleBobHeight = 0.018f;
        private const float FireKickRecoverSpeed = 7.5f;

        private readonly List<Transform> bodyParts = new List<Transform>();
        private readonly List<Vector3> bodyBasePositions = new List<Vector3>();
        private readonly List<Quaternion> bodyBaseRotations = new List<Quaternion>();
        private readonly List<Transform> armParts = new List<Transform>();
        private readonly List<Quaternion> armBaseRotations = new List<Quaternion>();
        private readonly List<Transform> weaponParts = new List<Transform>();
        private readonly List<Vector3> weaponBasePositions = new List<Vector3>();
        private readonly List<Quaternion> weaponBaseRotations = new List<Quaternion>();

        private float phase;
        private float fireKick;
        private bool captured;

        public Transform BodyForTests => bodyParts.Count > 0 ? bodyParts[0] : null;
        public Transform WeaponForTests => weaponParts.Count > 0 ? weaponParts[0] : null;

        private void Awake()
        {
            CaptureParts();
        }

        public void CaptureParts()
        {
            bodyParts.Clear();
            bodyBasePositions.Clear();
            bodyBaseRotations.Clear();
            armParts.Clear();
            armBaseRotations.Clear();
            weaponParts.Clear();
            weaponBasePositions.Clear();
            weaponBaseRotations.Clear();

            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == transform)
                {
                    continue;
                }

                string childName = child.name;
                if (childName.StartsWith("Loaded Passenger Body", System.StringComparison.Ordinal) ||
                    childName.StartsWith("Loaded Passenger Helmet", System.StringComparison.Ordinal) ||
                    childName.StartsWith("Loaded Passenger Pack", System.StringComparison.Ordinal))
                {
                    bodyParts.Add(child);
                    bodyBasePositions.Add(child.localPosition);
                    bodyBaseRotations.Add(child.localRotation);
                }
                else if (childName.StartsWith("Loaded Passenger Arm", System.StringComparison.Ordinal))
                {
                    armParts.Add(child);
                    armBaseRotations.Add(child.localRotation);
                }
                else if (childName.StartsWith("Loaded Passenger Weapon", System.StringComparison.Ordinal))
                {
                    weaponParts.Add(child);
                    weaponBasePositions.Add(child.localPosition);
                    weaponBaseRotations.Add(child.localRotation);
                }
            }

            captured = true;
        }

        private void Update()
        {
            if (RtsGame.HasInstance && RtsGame.Instance.Clock != null && RtsGame.Instance.Clock.IsPaused)
            {
                return;
            }

            Tick(Time.deltaTime);
        }

        public void PlayFirePulse()
        {
            fireKick = 1f;
        }

#if UNITY_EDITOR
        public void TickForTests(float deltaTime)
        {
            Tick(deltaTime);
        }
#endif

        private void Tick(float deltaTime)
        {
            if (!captured)
            {
                CaptureParts();
            }

            float safeDelta = Mathf.Max(0.0001f, deltaTime);
            phase += safeDelta * IdleFrequency;
            fireKick = Mathf.MoveTowards(fireKick, 0f, safeDelta * FireKickRecoverSpeed);

            float bob = Mathf.Sin(phase) * IdleBobHeight;
            float lean = Mathf.Sin(phase * 0.72f) * 2.1f;
            float scan = Mathf.Sin(phase * 0.43f) * 2.8f;

            for (int i = 0; i < bodyParts.Count; i++)
            {
                Transform part = bodyParts[i];
                if (part == null)
                {
                    continue;
                }

                float weight = part.name.IndexOf("Helmet", System.StringComparison.Ordinal) >= 0 ? 1.25f : 1f;
                part.localPosition = bodyBasePositions[i] + new Vector3(0f, bob * weight, 0f);
                part.localRotation = bodyBaseRotations[i] * Quaternion.Euler(lean * 0.35f * weight, scan * weight, 0f);
            }

            for (int i = 0; i < armParts.Count; i++)
            {
                Transform part = armParts[i];
                if (part == null)
                {
                    continue;
                }

                float side = i % 2 == 0 ? -1f : 1f;
                part.localRotation = armBaseRotations[i] * Quaternion.Euler(-fireKick * 12f, 0f, side * Mathf.Sin(phase * 1.3f) * 1.6f);
            }

            for (int i = 0; i < weaponParts.Count; i++)
            {
                Transform part = weaponParts[i];
                if (part == null)
                {
                    continue;
                }

                part.localPosition = weaponBasePositions[i] + new Vector3(0f, bob * 0.7f, -fireKick * 0.055f);
                part.localRotation = weaponBaseRotations[i] * Quaternion.Euler(Mathf.Sin(phase * 1.6f) * 1.6f - fireKick * 8f, 0f, 0f);
            }
        }
    }
}
