using UnityEngine;

namespace BastionFabrication
{
    [DisallowMultipleComponent]
    public sealed class BastionFabricationSpin : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 localAxis = Vector3.up;
        [SerializeField] private float degreesPerSecond = 35f;
        [SerializeField] private bool activeOnStart = true;

        public bool Active { get; set; }

        private void Awake()
        {
            if (target == null) target = transform;
            Active = activeOnStart;
        }

        public void Configure(Transform newTarget, Vector3 axis, float speed, bool startActive = true)
        {
            target = newTarget;
            localAxis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;
            degreesPerSecond = speed;
            activeOnStart = startActive;
            Active = startActive;
        }

        private void Update()
        {
            if (!Active || target == null) return;
            target.Rotate(localAxis, degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
