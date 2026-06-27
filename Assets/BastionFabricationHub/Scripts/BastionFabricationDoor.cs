using UnityEngine;

namespace BastionFabrication
{
    [DisallowMultipleComponent]
    public sealed class BastionFabricationDoor : MonoBehaviour
    {
        [SerializeField] private Transform door;
        [SerializeField] private Vector3 openLocalOffset = new Vector3(0f, 2.7f, 0f);
        [SerializeField, Min(0.01f)] private float speed = 3.1f;
        [SerializeField] private bool startsOpen;

        private Vector3 closedPosition;
        private bool open;

        public bool IsOpen => open;

        private void Awake()
        {
            if (door == null) return;
            closedPosition = door.localPosition;
            open = startsOpen;
            door.localPosition = closedPosition + (open ? openLocalOffset : Vector3.zero);
        }

        public void Configure(Transform newDoor, Vector3 newOpenOffset, float newSpeed, bool newStartsOpen = false)
        {
            door = newDoor;
            openLocalOffset = newOpenOffset;
            speed = Mathf.Max(0.01f, newSpeed);
            startsOpen = newStartsOpen;
            if (door != null) closedPosition = door.localPosition;
        }

        public void SetOpen(bool value) => open = value;
        public void Toggle() => open = !open;

        private void Update()
        {
            if (door == null) return;
            Vector3 target = closedPosition + (open ? openLocalOffset : Vector3.zero);
            door.localPosition = Vector3.MoveTowards(door.localPosition, target, speed * Time.deltaTime);
        }
    }
}
