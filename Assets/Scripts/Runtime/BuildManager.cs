using UnityEngine;

namespace QuestCommandRTS
{
    public sealed class BuildManager : MonoBehaviour
    {
        public bool IsPlacing => preview != null;
        public bool IsConstructing => activeKind.HasValue;
        public StructureKind PendingKind => pendingKind;

        private RtsGame game;
        private StructureKind pendingKind;
        private StructureKind? activeKind;
        private float activeRemaining;
        private float activeDuration;
        private GameObject preview;
        private Vector3 placementPoint;
        private bool placementValid;

        public void Initialize(RtsGame owner)
        {
            game = owner;
        }

        private void Update()
        {
            if (game == null || !activeKind.HasValue)
            {
                return;
            }

            float powerMultiplier = game.Resources != null && game.Resources.HasLowPower ? 0.45f : 1f;
            activeRemaining -= Time.deltaTime * powerMultiplier;
            if (activeRemaining > 0f)
            {
                return;
            }

            StructureKind completed = activeKind.Value;
            activeKind = null;
            BeginReadyPlacement(completed);
            StructureStats stats = RtsBalance.GetStructure(completed);
            game.SpawnFloatingText(stats.Name + " ready", game.GetPlayerBaseCenter() + Vector3.up * 2f, Color.white);
        }

        public bool BeginPlacement(StructureKind kind)
        {
            return QueueStructure(kind);
        }

        public bool QueueStructure(StructureKind kind)
        {
            StructureStats stats = RtsBalance.GetStructure(kind);
            if (IsPlacing)
            {
                game.SpawnFloatingText("Place current building", game.GetPlayerBaseCenter() + Vector3.up * 2f, Color.yellow);
                return false;
            }

            if (activeKind.HasValue)
            {
                game.SpawnFloatingText("Construction busy", game.GetPlayerBaseCenter() + Vector3.up * 2f, Color.yellow);
                return false;
            }

            if (!game.Resources.TrySpend(stats.Cost))
            {
                game.SpawnFloatingText("Need credits", game.GetPlayerBaseCenter() + Vector3.up * 2f, Color.yellow);
                return false;
            }

            activeKind = kind;
            activeDuration = Mathf.Max(0.1f, stats.BuildTime);
            activeRemaining = activeDuration;
            game.SpawnFloatingText(stats.Name, game.GetPlayerBaseCenter() + Vector3.up * 2f, Color.white);
            return true;
        }

        private void BeginReadyPlacement(StructureKind kind)
        {
            CancelPlacement();
            pendingKind = kind;
            preview = game.CreateStructurePreview(kind);
            placementValid = false;
        }

        public void CancelPlacement()
        {
            if (preview != null)
            {
                Destroy(preview);
            }

            preview = null;
            placementValid = false;
        }

        public void UpdatePlacement(Ray ray)
        {
            if (preview == null)
            {
                return;
            }

            if (!TryProjectToGround(ray, out placementPoint))
            {
                placementValid = false;
                game.SetPreviewValid(preview, false);
                return;
            }

            placementPoint = Snap(placementPoint);
            preview.transform.position = placementPoint;
            placementValid = CanPlaceAt(placementPoint, pendingKind);
            game.SetPreviewValid(preview, placementValid);
        }

        public bool TryConfirmPlacement()
        {
            if (preview == null)
            {
                return false;
            }

            if (!placementValid)
            {
                game.SpawnFloatingText("Blocked", placementPoint + Vector3.up * 2f, Color.yellow);
                return false;
            }

            game.CreateStructure(RtsTeam.Player, pendingKind, placementPoint);
            CancelPlacement();
            return true;
        }

        public bool CanStartStructure(StructureKind kind)
        {
            if (game == null || game.Resources == null || IsPlacing || activeKind.HasValue)
            {
                return false;
            }

            return game.Resources.CanAfford(RtsBalance.GetStructure(kind).Cost);
        }

        public bool IsStructureBuilding(StructureKind kind)
        {
            return activeKind.HasValue && activeKind.Value == kind;
        }

        public bool IsPlacementReady(StructureKind kind)
        {
            return IsPlacing && pendingKind == kind;
        }

        public float GetStructureBuildProgress(StructureKind kind)
        {
            if (!IsStructureBuilding(kind))
            {
                return 0f;
            }

            return activeDuration <= 0f ? 1f : 1f - Mathf.Clamp01(activeRemaining / activeDuration);
        }

        public int GetStructureQueuedCount(StructureKind kind)
        {
            return IsStructureBuilding(kind) || IsPlacementReady(kind) ? 1 : 0;
        }

        private bool CanPlaceAt(Vector3 point, StructureKind kind)
        {
            StructureStats stats = RtsBalance.GetStructure(kind);
            float footprint = stats.FootprintRadius;

            if (Mathf.Abs(point.x) + footprint > RtsBalance.MapHalfSize || Mathf.Abs(point.z) + footprint > RtsBalance.MapHalfSize)
            {
                return false;
            }

            bool nearAnchor = false;
            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsStructure structure = game.Entities[i] as RtsStructure;
                if (structure == null || structure.Team != RtsTeam.Player || !structure.IsAlive)
                {
                    continue;
                }

                float distance = PlanarDistance(point, structure.transform.position);
                if (distance <= RtsBalance.BuildRadius + structure.FootprintRadius)
                {
                    nearAnchor = true;
                    break;
                }
            }

            if (!nearAnchor)
            {
                return false;
            }

            Collider[] hits = Physics.OverlapSphere(point + Vector3.up * 0.8f, footprint * 0.92f);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null || hit.isTrigger)
                {
                    continue;
                }

                if (hit.GetComponentInParent<RtsEntity>() != null || hit.GetComponentInParent<ResourceNode>() != null)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryProjectToGround(Ray ray, out Vector3 point)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, 250f))
            {
                point = hit.point;
                point.y = 0f;
                return true;
            }

            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float distance))
            {
                point = ray.GetPoint(distance);
                point.y = 0f;
                return true;
            }

            point = Vector3.zero;
            return false;
        }

        private static Vector3 Snap(Vector3 point)
        {
            point.x = Mathf.Round(point.x);
            point.y = 0f;
            point.z = Mathf.Round(point.z);
            return point;
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
