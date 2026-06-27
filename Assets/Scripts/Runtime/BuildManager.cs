using System.Collections.Generic;
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
        public int ActiveConstructionCount => constructions.Count;

        private RtsGame game;
        private StructureKind pendingKind;
        private GameObject preview;
        private Vector3 placementPoint;
        private bool placementValid;
        private bool hasPlacementPoint;
        private BuildPlacementFailureReason lastFailureReason = BuildPlacementFailureReason.None;
        private bool placementSuspended;
        private readonly List<ActiveConstruction> constructions = new List<ActiveConstruction>();

        private sealed class ActiveConstruction
        {
            public RtsTeam Team;
            public StructureKind Kind;
            public Vector3 Position;
            public float Remaining;
            public float Duration;
            public GameObject Site;
        }

        public void Initialize(RtsGame owner)
        {
            game = owner;
        }

        private void Update()
        {
            if (game == null || game.IsMatchOver || game.Clock == null || game.Clock.IsPaused)
            {
                return;
            }

            TickConstructions(game.Clock.DeltaTime);
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
                DestroyObject(preview);
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

            StartConstruction(RtsTeam.Player, pendingKind, placementPoint, Mathf.Max(0f, stats.BuildTime), Mathf.Max(0f, stats.BuildTime));
            game.SpawnFloatingText(stats.Name + " constructing", placementPoint + Vector3.up * 2f, new Color(0.75f, 0.95f, 1f));
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
            RtsBuildPlacementSaveData data = new RtsBuildPlacementSaveData
            {
                isPlacing = preview != null,
                structureKind = preview != null ? pendingKind.ToString() : string.Empty,
                hasPlacementPoint = hasPlacementPoint,
                placementPoint = new Vector3Data(placementPoint)
            };

            for (int i = 0; i < constructions.Count; i++)
            {
                ActiveConstruction construction = constructions[i];
                if (construction == null)
                {
                    continue;
                }

                data.constructions.Add(new RtsPendingConstructionSaveData
                {
                    team = construction.Team.ToString(),
                    structureKind = construction.Kind.ToString(),
                    position = new Vector3Data(construction.Position),
                    remaining = construction.Remaining,
                    duration = construction.Duration
                });
            }

            return data;
        }

        public void RestorePlacementState(RtsBuildPlacementSaveData data)
        {
            CancelPlacement();
            ClearConstructions();
            if (data == null || !data.isPlacing || !System.Enum.TryParse(data.structureKind, out StructureKind kind))
            {
                RestoreConstructions(data);
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

            RestoreConstructions(data);
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

            for (int i = 0; i < constructions.Count; i++)
            {
                ActiveConstruction construction = constructions[i];
                if (construction == null)
                {
                    continue;
                }

                float constructionFootprint = RtsBalance.GetStructure(construction.Kind).FootprintRadius;
                if (PlanarDistance(point, construction.Position) < footprint + constructionFootprint)
                {
                    reason = BuildPlacementFailureReason.BlockedFootprint;
                    return false;
                }
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

        public void ClearConstructions()
        {
            for (int i = 0; i < constructions.Count; i++)
            {
                if (constructions[i] != null && constructions[i].Site != null)
                {
                    DestroyObject(constructions[i].Site);
                }
            }

            constructions.Clear();
        }

#if UNITY_EDITOR
        public void TickConstructionsForTests(float deltaTime)
        {
            TickConstructions(deltaTime);
        }

        public float GetConstructionProgressForTests(int index)
        {
            if (index < 0 || index >= constructions.Count)
            {
                return 0f;
            }

            ActiveConstruction construction = constructions[index];
            return construction.Duration <= 0f ? 1f : 1f - Mathf.Clamp01(construction.Remaining / construction.Duration);
        }
#endif

        private void TickConstructions(float deltaTime)
        {
            if (constructions.Count <= 0)
            {
                return;
            }

            float powerMultiplier = game.Resources != null && game.Resources.HasLowPower ? 0.45f : 1f;
            for (int i = constructions.Count - 1; i >= 0; i--)
            {
                ActiveConstruction construction = constructions[i];
                if (construction == null)
                {
                    constructions.RemoveAt(i);
                    continue;
                }

                construction.Remaining -= Mathf.Max(0f, deltaTime) * powerMultiplier;
                RefreshConstructionVisual(construction);
                if (construction.Remaining > 0f)
                {
                    continue;
                }

                CompleteConstruction(construction);
                constructions.RemoveAt(i);
            }
        }

        private void StartConstruction(RtsTeam team, StructureKind kind, Vector3 position, float remaining, float duration)
        {
            StructureStats stats = RtsBalance.GetStructure(kind);
            float safeDuration = Mathf.Max(0.01f, duration);
            float safeRemaining = Mathf.Clamp(remaining, 0f, safeDuration);
            if (safeRemaining <= 0.001f)
            {
                game.CreateStructure(team, kind, position);
                return;
            }

            ActiveConstruction construction = new ActiveConstruction
            {
                Team = team,
                Kind = kind,
                Position = Snap(position),
                Remaining = safeRemaining,
                Duration = safeDuration,
                Site = game.CreateStructurePreview(kind)
            };

            construction.Site.name = "Construction " + stats.Name;
            construction.Site.transform.position = construction.Position;
            constructions.Add(construction);
            RefreshConstructionVisual(construction);
        }

        private void RestoreConstructions(RtsBuildPlacementSaveData data)
        {
            if (data == null || data.constructions == null)
            {
                return;
            }

            for (int i = 0; i < data.constructions.Count; i++)
            {
                RtsPendingConstructionSaveData saved = data.constructions[i];
                if (saved == null ||
                    !System.Enum.TryParse(saved.team, out RtsTeam team) ||
                    !System.Enum.TryParse(saved.structureKind, out StructureKind kind))
                {
                    continue;
                }

                StartConstruction(team, kind, saved.position.ToVector3(), saved.remaining, saved.duration);
            }
        }

        private void CompleteConstruction(ActiveConstruction construction)
        {
            if (construction.Site != null)
            {
                DestroyObject(construction.Site);
                construction.Site = null;
            }

            RtsStructure structure = game.CreateStructure(construction.Team, construction.Kind, construction.Position);
            if (structure != null)
            {
                StructureStats stats = RtsBalance.GetStructure(construction.Kind);
                game.SpawnFloatingText(stats.Name + " complete", construction.Position + Vector3.up * 2.2f, new Color(0.55f, 1f, 0.65f));
            }

            Physics.SyncTransforms();
        }

        private static void RefreshConstructionVisual(ActiveConstruction construction)
        {
            if (construction == null || construction.Site == null)
            {
                return;
            }

            float progress = construction.Duration <= 0f ? 1f : 1f - Mathf.Clamp01(construction.Remaining / construction.Duration);
            float scale = Mathf.Lerp(0.42f, 1f, Mathf.SmoothStep(0f, 1f, progress));
            construction.Site.transform.localScale = new Vector3(scale, Mathf.Lerp(0.28f, 1f, progress), scale);
            Color color = Color.Lerp(new Color(1f, 0.78f, 0.18f, 0.48f), new Color(0.35f, 1f, 0.55f, 0.58f), progress);
            Renderer[] renderers = construction.Site.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Material material = renderers[i].sharedMaterial;
                if (material == null)
                {
                    continue;
                }

                material.color = color;
                material.SetColor("_Color", color);
                material.SetColor("_BaseColor", color);
            }
        }

        private static void DestroyObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
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
