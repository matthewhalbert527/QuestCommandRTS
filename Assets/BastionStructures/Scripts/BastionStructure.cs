using UnityEngine;
using UnityEngine.Events;

namespace BastionStructures
{
    public enum BastionStructureRole
    {
        InfantryProduction,
        VehicleProduction,
        Economy,
        Vehicle,
        Power,
        Support,
        Medical,
        Technology,
        Defense
    }

    [DisallowMultipleComponent]
    public sealed class BastionStructure : MonoBehaviour
    {
        [SerializeField] private string displayName;
        [SerializeField] private BastionStructureRole role;
        [SerializeField] private Vector2 footprintMeters = Vector2.one;
        [SerializeField, Min(1f)] private float maxHealth = 1000f;
        [SerializeField] private int powerProduction;
        [SerializeField] private int powerConsumption;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform rallyPoint;
        [SerializeField] private Transform dockPoint;
        [SerializeField] private Transform servicePoint;
        [SerializeField] private UnityEvent onDestroyed = new UnityEvent();

        private float currentHealth;

        public string DisplayName => displayName;
        public BastionStructureRole Role => role;
        public Vector2 FootprintMeters => footprintMeters;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public int PowerProduction => powerProduction;
        public int PowerConsumption => powerConsumption;
        public int NetPower => powerProduction - powerConsumption;
        public Transform SpawnPoint => spawnPoint;
        public Transform RallyPoint => rallyPoint;
        public Transform DockPoint => dockPoint;
        public Transform ServicePoint => servicePoint;
        public UnityEvent OnDestroyed => onDestroyed;

        private void Awake()
        {
            if (currentHealth <= 0f)
            {
                currentHealth = maxHealth;
            }
        }

        public void Configure(string newDisplayName, BastionStructureRole newRole, Vector2 newFootprint,
            float newMaxHealth, int newPowerProduction, int newPowerConsumption,
            Transform newSpawnPoint, Transform newRallyPoint, Transform newDockPoint, Transform newServicePoint)
        {
            displayName = newDisplayName;
            role = newRole;
            footprintMeters = newFootprint;
            maxHealth = Mathf.Max(1f, newMaxHealth);
            powerProduction = newPowerProduction;
            powerConsumption = newPowerConsumption;
            spawnPoint = newSpawnPoint;
            rallyPoint = newRallyPoint;
            dockPoint = newDockPoint;
            servicePoint = newServicePoint;
            currentHealth = maxHealth;
        }

        public void ApplyDamage(float amount)
        {
            if (amount <= 0f || currentHealth <= 0f) return;
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            if (currentHealth <= 0f)
            {
                onDestroyed.Invoke();
            }
        }

        public void Repair(float amount)
        {
            if (amount <= 0f || currentHealth <= 0f) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }

        public void RestoreToFullHealth()
        {
            currentHealth = maxHealth;
        }
    }
}
