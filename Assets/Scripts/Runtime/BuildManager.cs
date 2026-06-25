using UnityEngine;

namespace QuestCommandRTS
{
    public enum BuildPlacementFailureReason
    {
        None,
        NoGroundHit,
        OutsideMap,
        OutsideBuildRadius,
        BlockedFootprint,
        MissingPrerequisite,
        InsufficientCredits,
        MatchOver
    }

    public sealed class BuildManager : MonoBehaviour
    {
        private const float DefaultProjectionDistance = 250f;

        public bool IsPlacing => preview != null;
        public StructureKind PendingKind => pendingKind;
        public bool PlacementValid => placementValid;
        public bool HasPlacementPoint => hasPlacementPoint;
        public Vector3 PlacementPoint => placementPoint;
        public BuildPlacementFailureReason LastFailureReason => lastFailureReason;

        private RtsGame game;
        private StructureKind pendingKind;
        private GameObject preview;
        private Vector3 placementPoint;
        private bool placementValid;
        private bool hasPlacementPoint;
        private BuildPlacementFailureReason lastFailureReason = BuildPlacementFailureReason.None;
        private bool placementSuspended;

        public void Initialize(RtsGame owner)
        {
            game = owner;
        }

        public bool BeginPlacement(StructureKind kind)
        {
            StructureStats stats = RtsBalance.GetStructure(kind);
            if (game.IsMatchOver)
            {
                game.SpawnFloatingText(GetFailureText(BuildPlacementFailureReason.MatchOver), game.GetPlayerBaseCenter() + Vector3.up * 2f, Color.yellow);
                return false;
            }

            if (!game.CanBuildStructure(kind))
            {
                lastFailureReason = BuildPlacementFailureReason.MissingPrerequisite;
                game.SpawnFloatingText(game.GetStructureRequirement(kind), game.GetPlayerBaseCenter() + Vector3.up * 2f, Color.yellow);
                return false;
            }

            if (!game.Resources.CanAfford(stats.Cost))
            {
                lastFailureReason = BuildPlacementFailureReason.InsufficientCredits;
                game.SpawnFloatingText(GetFailureText(lastFailureReason), game.GetPlayerBaseCenter() + Vector3.up * 2f, Color.yellow);
                return false;
            }

            CancelPlacement();
            pendingKind = kind;
            preview = game.CreateStructurePreview(kind);
            placementValid = false;
            hasPlacementPoint = false;
            lastFailureReason = BuildPlacementFailureReason.NoGroundHit;
            game.SpawnFloatingText(stats.Name + " ready", game.GetPlayerBaseCenter() + Vector3.up * 2f, new Color(0.55f, 0.9f, 1f));
            return true;
        }

        public void CancelPlacement()
        {
            if (preview != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(preview);
                }
                else
                {
                    DestroyImmediate(preview);
                }
            }

            preview = null;
            placementValid = false;
            hasPlacementPoint = false;
            placementSuspended = false;
            lastFailureReason = BuildPlacementFailureReason.None;
        }

        public void UpdatePlacement(Ray ray)
        {
            UpdatePlacement(ray, DefaultProjectionDistance);
        }

        public void UpdatePlacement(Ray ray, float maxDistance)
        {
            if (preview == null || placementSuspended)
            {
                return;
            }

            if (!TryProjectToGround(ray, Mathf.Max(0.01f, maxDistance), out placementPoint))
            {
                placementValid = false;
                hasPlacementPoint = false;
                lastFailureReason = BuildPlacementFailureReason.NoGroundHit;
                game.SetPreviewValid(preview, false);
                return;
            }

            UpdatePlacementAtPoint(placementPoint);
        }

        public void UpdatePlacementAtPoint(Vector3 point)
        {
            if (preview == null || placementSuspended)
            {
                return;
            }

            placementPoint = Snap(point);
            hasPlacementPoint = true;
            preview.transform.position = placementPoint;
            placementValid = CanPlaceAt(placementPoint, pendingKind, out lastFailureReason);
            game.SetPreviewValid(preview, placementValid);
        }

        public bool TryConfirmPlacement()
        {
            if (preview == null || placementSuspended)
            {
                return false;
            }

            if (!placementValid)
            {
                game.SpawnFloatingText(GetFailureText(lastFailureReason), GetFeedbackPoint(), Color.yellow);
                return false;
            }

            StructureStats stats = RtsBalance.GetStructure(pendingKind);
            if (!game.Resources.TrySpend(stats.Cost))
            {
                lastFailureReason = BuildPlacementFailureReason.InsufficientCredits;
                game.SpawnFloatingText(GetFailureText(lastFailureReason), placementPoint + Vector3.up * 2f, Color.yellow);
                return false;
            }

            game.CreateStructure(RtsTeam.Player, pendingKind, placementPoint);
            game.SpawnFloatingText(stats.Name + " built", placementPoint + Vector3.up * 2f, new Color(0.55f, 1f, 0.65f));
            CancelPlacement();
            return true;
        }

        public string GetPlacementStatusText()
        {
            if (preview == null)
            {
                return string.Empty;
            }

            return placementValid ? "Valid placement" : GetFailureText(lastFailureReason);
        }

        public RtsBuildPlacementSaveData CapturePlacementState()
        {
            return new RtsBuildPlacementSaveData
            {
                isPlacing = preview != null,
                structureKind = preview != null ? pendingKind.ToString() : string.Empty,
                hasPlacementPoint = hasPlacementPoint,
                placementPoint = new Vector3Data(placementPoint)
            };
        }

        public void RestorePlacementState(RtsBuildPlacementSaveData data)
        {
            CancelPlacement();
            if (data == null || !data.isPlacing || !System.Enum.TryParse(data.structureKind, out StructureKind kind))
            {
                return;
            }

            pendingKind = kind;
            preview = game.CreateStructurePreview(kind);
            placementSuspended = false;
            hasPlacementPoint = data.hasPlacementPoint;
            placementPoint = data.placementPoint.ToVector3();
            if (hasPlacementPoint)
            {
                UpdatePlacementAtPoint(placementPoint);
            }
            else
            {
                placementValid = false;
                lastFailureReason = BuildPlacementFailureReason.NoGroundHit;
            }
        }

        public void SetPlacementSuspended(bool suspended)
        {
            placementSuspended = suspended;
            if (preview != null)
            {
                preview.SetActive(!suspended);
            }
        }

        public bool CanPlaceAt(Vector3 point, StructureKind kind, out BuildPlacementFailureReason reason)
        {
            StructureStats stats = RtsBalance.GetStructure(kind);
            float footprint = stats.FootprintRadius;

            if (Mathf.Abs(point.x) + footprint > RtsBalance.MapHalfSize || Mathf.Abs(point.z) + footprint > RtsBalance.MapHalfSize)
            {
                reason = BuildPlacementFailureReason.OutsideMap;
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
                reason = BuildPlacementFailureReason.OutsideBuildRadius;
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
                    reason = BuildPlacementFailureReason.BlockedFootprint;
                    return false;
                }
            }

            reason = BuildPlacementFailureReason.None;
            return true;
        }

        public static string GetFailureText(BuildPlacementFailureReason reason)
        {
            switch (reason)
            {
                case BuildPlacementFailureReason.NoGroundHit:
                    return "Aim at battlefield";
                case BuildPlacementFailureReason.OutsideMap:
                    return "Outside map";
                case BuildPlacementFailureReason.OutsideBuildRadius:
                    return "Outside build radius";
                case BuildPlacementFailureReason.BlockedFootprint:
                    return "Blocked footprint";
                case BuildPlacementFailureReason.MissingPrerequisite:
                    return "Missing prerequisite";
                case BuildPlacementFailureReason.InsufficientCredits:
                    return "Need credits";
                case BuildPlacementFailureReason.MatchOver:
                    return "Match complete";
                default:
                    return "Cannot place";
            }
        }

        private Vector3 GetFeedbackPoint()
        {
            if (hasPlacementPoint)
            {
                return placementPoint + Vector3.up * 2f;
            }

            return game.GetPlayerBaseCenter() + Vector3.up * 2f;
        }

        private static bool TryProjectToGround(Ray ray, float maxDistance, out Vector3 point)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                point = hit.point;
                point.y = 0f;
                return true;
            }

            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float distance) && distance >= 0f && distance <= maxDistance)
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
