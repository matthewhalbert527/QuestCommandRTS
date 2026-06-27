using UnityEngine;
using UnityEngine.Events;

namespace BastionInfantry
{
    public enum BastionInfantryRole
    {
        Gunner,
        Grenadier,
        RocketSoldier,
        FlameTrooper,
        Engineer
    }

    public enum BastionWeaponDelivery
    {
        Hitscan,
        LobbedProjectile,
        Rocket,
        FlameStream,
        RepairTool
    }

    [DisallowMultipleComponent]
    public sealed class BastionInfantryUnit : MonoBehaviour
    {
        [Header("Role")]
        [SerializeField] private BastionInfantryRole role;
        [SerializeField] private BastionWeaponDelivery weaponDelivery;

        [Header("Rig")]
        [SerializeField] private Transform upperBodyYaw;
        [SerializeField] private Transform weaponPitch;
        [SerializeField] private Transform weaponRecoil;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform healthBarAnchor;
        [SerializeField] private Transform selectionAnchor;

        [Header("Combat")]
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float moveSpeed = 3.5f;
        [SerializeField, Min(0f)] private float turnSpeed = 360f;
        [SerializeField, Min(0f)] private float attackRange = 18f;
        [SerializeField, Min(0.01f)] private float actionCooldown = 0.5f;
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Min(0f)] private float splashRadius;
        [SerializeField, Min(0f)] private float projectileSpeed;
        [SerializeField, Min(0f)] private float utilityPower;
        [SerializeField] private Vector2 pitchLimits = new Vector2(-25f, 45f);

        [Header("Events")]
        [SerializeField] private UnityEvent onPrimaryAction = new UnityEvent();
        [SerializeField] private UnityEvent onUtilityAction = new UnityEvent();
        [SerializeField] private UnityEvent onDamaged = new UnityEvent();
        [SerializeField] private UnityEvent onDeath = new UnityEvent();

        private BastionInfantryProceduralAnimator proceduralAnimator;
        private float currentHealth;
        private float nextActionTime;
        private bool hasAimPoint;
        private Vector3 aimPoint;

        public BastionInfantryRole Role => role;
        public BastionWeaponDelivery WeaponDelivery => weaponDelivery;
        public Transform Muzzle => muzzle;
        public Transform HealthBarAnchor => healthBarAnchor;
        public Transform SelectionAnchor => selectionAnchor;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float MoveSpeed => moveSpeed;
        public float AttackRange => attackRange;
        public float Damage => damage;
        public float SplashRadius => splashRadius;
        public float ProjectileSpeed => projectileSpeed;
        public float UtilityPower => utilityPower;
        public bool IsAlive => currentHealth > 0f;
        public bool IsEngineer => role == BastionInfantryRole.Engineer;

        private void Awake()
        {
            currentHealth = maxHealth;
            proceduralAnimator = GetComponent<BastionInfantryProceduralAnimator>();
        }

        private void LateUpdate()
        {
            if (!hasAimPoint || upperBodyYaw == null || weaponPitch == null)
            {
                return;
            }

            Vector3 localDirection = transform.InverseTransformDirection(aimPoint - upperBodyYaw.position);
            if (localDirection.sqrMagnitude > 0.0001f)
            {
                float yaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
                Quaternion targetYaw = Quaternion.Euler(0f, yaw, 0f);
                upperBodyYaw.localRotation = Quaternion.RotateTowards(
                    upperBodyYaw.localRotation,
                    targetYaw,
                    turnSpeed * Time.deltaTime);
            }

            Vector3 pitchDirection = upperBodyYaw.InverseTransformDirection(aimPoint - weaponPitch.position);
            float horizontal = new Vector2(pitchDirection.x, pitchDirection.z).magnitude;
            float pitch = -Mathf.Atan2(pitchDirection.y, Mathf.Max(0.0001f, horizontal)) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);
            Quaternion targetPitch = Quaternion.Euler(pitch, 0f, 0f);
            weaponPitch.localRotation = Quaternion.RotateTowards(
                weaponPitch.localRotation,
                targetPitch,
                turnSpeed * Time.deltaTime);
        }

        public void SetAimPoint(Vector3 worldPoint)
        {
            aimPoint = worldPoint;
            hasAimPoint = true;
        }

        public void ClearAimPoint()
        {
            hasAimPoint = false;
        }

        public void SetMovementAmount(float normalizedSpeed)
        {
            if (proceduralAnimator != null)
            {
                proceduralAnimator.SetMovementAmount(normalizedSpeed);
            }
        }

        public bool TryPrimaryAction()
        {
            if (!IsAlive || Time.time < nextActionTime)
            {
                return false;
            }

            nextActionTime = Time.time + actionCooldown;
            if (proceduralAnimator != null)
            {
                proceduralAnimator.TriggerRecoil();
            }
            onPrimaryAction.Invoke();
            return true;
        }

        public void InvokeUtilityAction()
        {
            if (IsAlive)
            {
                onUtilityAction.Invoke();
            }
        }

        public void ApplyDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            onDamaged.Invoke();
            if (currentHealth <= 0f)
            {
                onDeath.Invoke();
            }
        }

        public void RestoreHealth(float amount)
        {
            if (amount > 0f && IsAlive)
            {
                currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            }
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;
        }
    }
}
