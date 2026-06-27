using UnityEngine;

namespace BastionInfantry
{
    [DisallowMultipleComponent]
    public sealed class BastionInfantryProceduralAnimator : MonoBehaviour
    {
        [SerializeField] private Transform pelvisPivot;
        [SerializeField] private Transform leftLegPivot;
        [SerializeField] private Transform rightLegPivot;
        [SerializeField] private Transform weaponRecoil;
        [SerializeField] private Transform headYaw;
        [SerializeField, Min(0f)] private float walkFrequency = 6.2f;
        [SerializeField, Min(0f)] private float runFrequency = 10.8f;
        [SerializeField, Range(0f, 70f)] private float legSwingDegrees = 34f;
        [SerializeField, Range(0f, 40f)] private float turnLegSwingDegrees = 18f;
        [SerializeField, Min(0f)] private float pelvisBob = 0.052f;
        [SerializeField, Range(0f, 18f)] private float forwardLeanDegrees = 8f;
        [SerializeField, Range(0f, 18f)] private float sideLeanDegrees = 6f;
        [SerializeField, Min(0f)] private float footLift = 0.065f;
        [SerializeField, Min(0f)] private float idleBreathing = 0.016f;
        [SerializeField, Min(0f)] private float weaponRunSway = 0.034f;
        [SerializeField, Min(0f)] private float recoilDistance = 0.075f;
        [SerializeField, Min(0f)] private float recoilRecovery = 9f;

        private float movementAmount;
        private float smoothedMovementAmount;
        private float turnAmount;
        private float smoothedTurnAmount;
        private Vector2 localMoveVector;
        private Vector2 smoothedLocalMoveVector;
        private float stridePhase;
        private float idlePhase;
        private float recoil;
        private Vector3 pelvisBasePosition;
        private Quaternion pelvisBaseRotation;
        private Vector3 leftLegBasePosition;
        private Vector3 rightLegBasePosition;
        private Quaternion leftLegBaseRotation;
        private Quaternion rightLegBaseRotation;
        private Vector3 weaponBasePosition;
        private Quaternion weaponBaseRotation;
        private Quaternion headBaseRotation;

        private void Awake()
        {
            if (headYaw == null)
            {
                headYaw = FindDeepChild(transform, "HeadYaw");
            }

            if (pelvisPivot != null)
            {
                pelvisBasePosition = pelvisPivot.localPosition;
                pelvisBaseRotation = pelvisPivot.localRotation;
            }
            if (leftLegPivot != null)
            {
                leftLegBasePosition = leftLegPivot.localPosition;
                leftLegBaseRotation = leftLegPivot.localRotation;
            }
            if (rightLegPivot != null)
            {
                rightLegBasePosition = rightLegPivot.localPosition;
                rightLegBaseRotation = rightLegPivot.localRotation;
            }
            if (weaponRecoil != null)
            {
                weaponBasePosition = weaponRecoil.localPosition;
                weaponBaseRotation = weaponRecoil.localRotation;
            }
            if (headYaw != null)
            {
                headBaseRotation = headYaw.localRotation;
            }
        }

        private void LateUpdate()
        {
            smoothedMovementAmount = Mathf.MoveTowards(
                smoothedMovementAmount,
                movementAmount,
                (movementAmount > smoothedMovementAmount ? 8.5f : 6f) * Time.deltaTime);
            smoothedTurnAmount = Mathf.MoveTowards(smoothedTurnAmount, turnAmount, 7.5f * Time.deltaTime);
            smoothedLocalMoveVector = Vector2.MoveTowards(smoothedLocalMoveVector, localMoveVector, 7.5f * Time.deltaTime);

            float turnOnlyAmount = Mathf.Clamp01(Mathf.Abs(smoothedTurnAmount) * 0.75f);
            float locomotionAmount = Mathf.Clamp01(Mathf.Max(smoothedMovementAmount, turnOnlyAmount));
            float frequency = Mathf.Lerp(walkFrequency, runFrequency, smoothedMovementAmount);
            stridePhase += Time.deltaTime * Mathf.Lerp(2.2f, frequency, locomotionAmount);
            idlePhase += Time.deltaTime * 2.6f;

            float stride = Mathf.Sin(stridePhase);
            float oppositeStride = Mathf.Sin(stridePhase + Mathf.PI);
            float leftPlant = Mathf.Clamp01(Mathf.Sin(stridePhase + Mathf.PI * 0.5f));
            float rightPlant = Mathf.Clamp01(Mathf.Sin(stridePhase + Mathf.PI * 1.5f));
            float forward = Mathf.Clamp(smoothedLocalMoveVector.y, -1f, 1f);
            float lateral = Mathf.Clamp(smoothedLocalMoveVector.x, -1f, 1f);
            float turn = Mathf.Clamp(smoothedTurnAmount, -1f, 1f);

            float turnStep = turn * turnLegSwingDegrees * Mathf.Max(turnOnlyAmount, smoothedMovementAmount * 0.35f);
            float leftSwing = stride * legSwingDegrees * locomotionAmount + turnStep;
            float rightSwing = oppositeStride * legSwingDegrees * locomotionAmount - turnStep;
            float leftSideSwing = lateral * 9f * locomotionAmount - turn * 4f;
            float rightSideSwing = lateral * 9f * locomotionAmount + turn * 4f;

            if (leftLegPivot != null)
            {
                leftLegPivot.localPosition = leftLegBasePosition +
                    Vector3.up * (leftPlant * footLift * locomotionAmount) +
                    Vector3.forward * (stride * 0.028f * locomotionAmount);
                leftLegPivot.localRotation = leftLegBaseRotation * Quaternion.Euler(leftSwing, leftSideSwing, -lateral * 7f * locomotionAmount);
            }
            if (rightLegPivot != null)
            {
                rightLegPivot.localPosition = rightLegBasePosition +
                    Vector3.up * (rightPlant * footLift * locomotionAmount) +
                    Vector3.forward * (oppositeStride * 0.028f * locomotionAmount);
                rightLegPivot.localRotation = rightLegBaseRotation * Quaternion.Euler(rightSwing, rightSideSwing, -lateral * 7f * locomotionAmount);
            }
            if (pelvisPivot != null)
            {
                float runBob = Mathf.Abs(Mathf.Sin(stridePhase * 2f)) * pelvisBob * locomotionAmount;
                float idleBob = Mathf.Sin(idlePhase) * idleBreathing * (1f - locomotionAmount);
                float sideSway = Mathf.Sin(stridePhase) * 0.026f * locomotionAmount + lateral * 0.018f;
                float forwardPush = Mathf.Cos(stridePhase) * 0.014f * locomotionAmount - recoil * 0.018f;
                pelvisPivot.localPosition = pelvisBasePosition +
                    Vector3.up * (runBob + idleBob) +
                    Vector3.right * sideSway +
                    Vector3.forward * forwardPush;

                float lean = Mathf.Lerp(0f, forwardLeanDegrees, Mathf.Abs(forward) * locomotionAmount);
                float roll = -sideSway * sideLeanDegrees * 18f - lateral * sideLeanDegrees;
                float yaw = turn * 5.5f + Mathf.Sin(stridePhase) * 1.5f * locomotionAmount;
                pelvisPivot.localRotation = pelvisBaseRotation * Quaternion.Euler(lean, yaw, roll);
            }

            if (headYaw != null)
            {
                float idleLook = Mathf.Sin(idlePhase * 0.55f) * 2.2f * (1f - locomotionAmount);
                float runCounterSway = -Mathf.Sin(stridePhase) * 2.6f * locomotionAmount;
                headYaw.localRotation = headBaseRotation * Quaternion.Euler(
                    Mathf.Sin(idlePhase * 0.9f) * 1.2f * (1f - locomotionAmount),
                    idleLook - turn * 4.5f,
                    runCounterSway);
            }

            recoil = Mathf.MoveTowards(recoil, 0f, recoilRecovery * Time.deltaTime);
            if (weaponRecoil != null)
            {
                float weaponBob = Mathf.Sin(stridePhase * 2f) * weaponRunSway * locomotionAmount;
                float weaponSide = Mathf.Sin(stridePhase) * weaponRunSway * 0.7f * locomotionAmount + lateral * weaponRunSway;
                weaponRecoil.localPosition = weaponBasePosition +
                    Vector3.back * (recoilDistance * recoil + Mathf.Abs(stride) * weaponRunSway * 0.45f * locomotionAmount) +
                    Vector3.up * weaponBob +
                    Vector3.right * weaponSide;
                weaponRecoil.localRotation = weaponBaseRotation * Quaternion.Euler(
                    -recoil * 3.5f + Mathf.Sin(stridePhase) * 1.8f * locomotionAmount,
                    turn * 2.5f,
                    -weaponSide * 55f);
            }
        }

        public void SetMovementAmount(float normalizedSpeed)
        {
            movementAmount = Mathf.Clamp01(normalizedSpeed);
        }

        public void SetTurnAmount(float normalizedTurnSpeed)
        {
            turnAmount = Mathf.Clamp(normalizedTurnSpeed, -1f, 1f);
        }

        public void SetLocalMoveVector(Vector2 normalizedLocalVelocity)
        {
            localMoveVector = Vector2.ClampMagnitude(normalizedLocalVelocity, 1f);
        }

        public void TriggerRecoil()
        {
            recoil = 1f;
        }

        private static Transform FindDeepChild(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == childName)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                Transform match = FindDeepChild(child, childName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
