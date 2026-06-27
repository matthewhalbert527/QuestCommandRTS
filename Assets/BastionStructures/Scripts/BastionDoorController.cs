using UnityEngine;

namespace BastionStructures
{
    [DisallowMultipleComponent]
    public sealed class BastionDoorController : MonoBehaviour
    {
        [SerializeField] private Transform door;
        [SerializeField] private Vector3 openLocalOffset = new Vector3(0f, 3f, 0f);
        [SerializeField, Min(0.01f)] private float travelSpeed = 3f;
        [SerializeField] private bool startsOpen;

        private Vector3 closedLocalPosition;
        private bool open;

        public bool IsOpen => open;

        private void Awake()
        {
            if (door == null) return;
            closedLocalPosition = door.localPosition;
            open = startsOpen;
            door.localPosition = closedLocalPosition + (open ? openLocalOffset : Vector3.zero);
        }

        public void Configure(Transform newDoor, Vector3 newOpenOffset, float newTravelSpeed, bool newStartsOpen = false)
        {
            door = newDoor;
            openLocalOffset = newOpenOffset;
            travelSpeed = Mathf.Max(0.01f, newTravelSpeed);
            startsOpen = newStartsOpen;
            if (door != null) closedLocalPosition = door.localPosition;
        }

        public void SetOpen(bool shouldOpen)
        {
            open = shouldOpen;
        }

        public void Toggle()
        {
            open = !open;
        }

        private void Update()
        {
            if (door == null) return;
            Vector3 target = closedLocalPosition + (open ? openLocalOffset : Vector3.zero);
            door.localPosition = Vector3.MoveTowards(door.localPosition, target, travelSpeed * Time.deltaTime);
        }
    }
}
