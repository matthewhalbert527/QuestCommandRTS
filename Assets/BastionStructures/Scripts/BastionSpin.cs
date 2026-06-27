using UnityEngine;

namespace BastionStructures
{
    [DisallowMultipleComponent]
    public sealed class BastionSpin : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 localAxis = Vector3.up;
        [SerializeField] private float degreesPerSecond = 30f;
        [SerializeField] private bool spinOnStart = true;

        public bool IsSpinning { get; set; }

        private void Awake()
        {
            IsSpinning = spinOnStart;
            if (target == null) target = transform;
        }

        public void Configure(Transform newTarget, Vector3 newLocalAxis, float newDegreesPerSecond, bool startEnabled = true)
        {
            target = newTarget;
            localAxis = newLocalAxis.sqrMagnitude > 0.0001f ? newLocalAxis.normalized : Vector3.up;
            degreesPerSecond = newDegreesPerSecond;
            spinOnStart = startEnabled;
            IsSpinning = startEnabled;
        }

        private void Update()
        {
            if (!IsSpinning || target == null || Mathf.Approximately(degreesPerSecond, 0f)) return;
            target.Rotate(localAxis, degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
