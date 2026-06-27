
using UnityEngine;
using UnityEngine.Events;

namespace BastionWarFactoryCMV2
{
    public class BastionWarFactoryCMV2 : MonoBehaviour
    {
        [Header("Gameplay sockets")]
        public Transform spawnPoint;
        public Transform rallyPoint;
        public Transform vehicleAssemblySocket;
        public Transform interiorPoint;
        public Transform doorSocket;
        public Transform conveyorSocket;
        public Transform[] smokeSockets;
        public Transform[] lightSockets;
        public Transform[] robotArmSockets;

        [Header("Production state")]
        public bool productionActive;
        [Range(0f, 1f)] public float productionProgress;
        public UnityEvent onProductionStarted;
        public UnityEvent onProductionCompleted;

        public void StartProduction()
        {
            productionActive = true;
            productionProgress = 0f;
            onProductionStarted?.Invoke();
        }

        public void SetProgress(float progress)
        {
            productionProgress = Mathf.Clamp01(progress);
            if (productionProgress >= 1f) CompleteProduction();
        }

        public void CompleteProduction()
        {
            productionProgress = 1f;
            productionActive = false;
            onProductionCompleted?.Invoke();
        }
    }
}
