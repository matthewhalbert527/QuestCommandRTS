using UnityEngine;
using UnityEngine.Events;

namespace BastionFabrication
{
    [DisallowMultipleComponent]
    public sealed class BastionFabricationHub : MonoBehaviour
    {
        [SerializeField] private string displayName = "Bastion Fabrication Hub";
        [SerializeField] private Vector2 footprintMeters = new Vector2(12f, 10.4f);
        [SerializeField, Min(1f)] private float maxHealth = 3200f;
        [SerializeField] private int powerConsumption = 45;
        [SerializeField, Min(1f)] private float buildRadiusMeters = 16f;
        [SerializeField] private Transform buildOrigin;
        [SerializeField] private Transform rallyPoint;
        [SerializeField] private Transform vehicleExit;
        [SerializeField] private Transform blueprintEmitter;
        [SerializeField] private Transform constructionFxSocket;
        [SerializeField] private UnityEvent onConstructionStarted = new UnityEvent();
        [SerializeField] private UnityEvent onConstructionStopped = new UnityEvent();
        [SerializeField] private UnityEvent onDestroyed = new UnityEvent();

        private float currentHealth;
        private bool constructionActive;

        public string DisplayName => displayName;
        public Vector2 FootprintMeters => footprintMeters;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public int PowerConsumption => powerConsumption;
        public float BuildRadiusMeters => buildRadiusMeters;
        public Transform BuildOrigin => buildOrigin;
        public Transform RallyPoint => rallyPoint;
        public Transform VehicleExit => vehicleExit;
        public Transform BlueprintEmitter => blueprintEmitter;
        public Transform ConstructionFxSocket => constructionFxSocket;
        public bool ConstructionActive => constructionActive;

        private void Awake()
        {
            if (currentHealth <= 0f) currentHealth = maxHealth;
        }

        public void Configure(string newDisplayName, Vector2 newFootprint, float newMaxHealth,
            int newPowerConsumption, float newBuildRadius, Transform newBuildOrigin,
            Transform newRallyPoint, Transform newVehicleExit, Transform newBlueprintEmitter,
            Transform newConstructionFxSocket)
        {
            displayName = newDisplayName;
            footprintMeters = newFootprint;
            maxHealth = Mathf.Max(1f, newMaxHealth);
            powerConsumption = newPowerConsumption;
            buildRadiusMeters = Mathf.Max(1f, newBuildRadius);
            buildOrigin = newBuildOrigin;
            rallyPoint = newRallyPoint;
            vehicleExit = newVehicleExit;
            blueprintEmitter = newBlueprintEmitter;
            constructionFxSocket = newConstructionFxSocket;
            currentHealth = maxHealth;
        }

        public void SetConstructionActive(bool active)
        {
            if (constructionActive == active) return;
            constructionActive = active;
            if (active) onConstructionStarted.Invoke();
            else onConstructionStopped.Invoke();
        }

        public void ApplyDamage(float amount)
        {
            if (amount <= 0f || currentHealth <= 0f) return;
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            if (currentHealth <= 0f)
            {
                SetConstructionActive(false);
                onDestroyed.Invoke();
            }
        }

        public void Repair(float amount)
        {
            if (amount <= 0f || currentHealth <= 0f) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }
    }
}
