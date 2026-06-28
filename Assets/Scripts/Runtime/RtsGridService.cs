using System;
using UnityEngine;

namespace QuestCommandRTS
{
    [Flags]
    public enum RtsCellFlags
    {
        None = 0,
        Buildable = 1 << 0,
        Blocked = 1 << 1,
        Resource = 1 << 2,
        Explored = 1 << 3,
        Visible = 1 << 4,
        Reserved = 1 << 5
    }

    public enum RtsGridQueryFailureReason
    {
        None,
        OutsideMap,
        NotBuildable,
        Blocked,
        Reserved,
        Occupied
    }

    public struct RtsBuildFootprint
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        public RtsBuildFootprint(int width, int height)
        {
            Width = Mathf.Max(1, width);
            Height = Mathf.Max(1, height);
        }

        public static RtsBuildFootprint Square(int size)
        {
            return new RtsBuildFootprint(size, size);
        }
    }

    public struct RtsGridQueryResult
    {
        public bool Success { get; private set; }
        public RtsGridQueryFailureReason FailureReason { get; private set; }
        public Vector2Int FailedCell { get; private set; }

        public static RtsGridQueryResult Pass()
        {
            return new RtsGridQueryResult
            {
                Success = true,
                FailureReason = RtsGridQueryFailureReason.None,
                FailedCell = new Vector2Int(-1, -1)
            };
        }

        public static RtsGridQueryResult Fail(RtsGridQueryFailureReason reason, Vector2Int cell)
        {
            return new RtsGridQueryResult
            {
                Success = false,
                FailureReason = reason,
                FailedCell = cell
            };
        }
    }

    public struct RtsGridCell
    {
        public Vector2Int Coord;
        public RtsCellFlags Flags;
        public int ResourceAmount;
        public ResourceNode ResourceNode;
        public int OccupancyCount;

        public bool IsBuildable => (Flags & RtsCellFlags.Buildable) != 0;
        public bool IsBlocked => (Flags & RtsCellFlags.Blocked) != 0;
        public bool IsReserved => (Flags & RtsCellFlags.Reserved) != 0;
        public bool HasResource => (Flags & RtsCellFlags.Resource) != 0;
        public bool IsVisible => (Flags & RtsCellFlags.Visible) != 0;
        public bool IsExplored => (Flags & RtsCellFlags.Explored) != 0;
    }

    public sealed class RtsGridService
    {
        private readonly RtsGridCell[] cells;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public float CellSize { get; private set; }
        public Vector3 Origin { get; private set; }

        public RtsGridService(int width, int height, float cellSize, Vector3 origin)
        {
            Width = Mathf.Max(1, width);
            Height = Mathf.Max(1, height);
            CellSize = Mathf.Max(0.01f, cellSize);
            Origin = origin;
            cells = new RtsGridCell[Width * Height];

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int index = ToIndex(new Vector2Int(x, y));
                    cells[index] = new RtsGridCell
                    {
                        Coord = new Vector2Int(x, y),
                        Flags = RtsCellFlags.Buildable
                    };
                }
            }
        }

        public static RtsGridService CreateCentered(float halfSize, float cellSize)
        {
            float safeCellSize = Mathf.Max(0.01f, cellSize);
            int dimension = Mathf.CeilToInt((halfSize * 2f) / safeCellSize);
            return new RtsGridService(dimension, dimension, safeCellSize, new Vector3(-halfSize, 0f, -halfSize));
        }

        public Vector2Int WorldToCell(Vector3 worldPosition)
        {
            int x = Mathf.FloorToInt((worldPosition.x - Origin.x) / CellSize);
            int y = Mathf.FloorToInt((worldPosition.z - Origin.z) / CellSize);
            return new Vector2Int(x, y);
        }

        public Vector3 CellToWorldCenter(Vector2Int cell)
        {
            return new Vector3(
                Origin.x + (cell.x + 0.5f) * CellSize,
                0f,
                Origin.z + (cell.y + 0.5f) * CellSize);
        }

        public bool IsInsideMap(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Height;
        }

        public bool IsWorldInsideMap(Vector3 worldPosition)
        {
            return IsInsideMap(WorldToCell(worldPosition));
        }

        public bool TryGetCell(Vector2Int coord, out RtsGridCell cell)
        {
            if (!IsInsideMap(coord))
            {
                cell = default;
                return false;
            }

            cell = cells[ToIndex(coord)];
            return true;
        }

        public RtsGridCell GetCell(Vector2Int coord)
        {
            return IsInsideMap(coord) ? cells[ToIndex(coord)] : default;
        }

        public void SetBuildable(Vector2Int coord, bool buildable)
        {
            SetFlag(coord, RtsCellFlags.Buildable, buildable);
        }

        public void SetBlocked(Vector2Int coord, bool blocked)
        {
            SetFlag(coord, RtsCellFlags.Blocked, blocked);
        }

        public void SetExplored(Vector2Int coord, bool explored)
        {
            SetFlag(coord, RtsCellFlags.Explored, explored);
        }

        public void SetVisible(Vector2Int coord, bool visible)
        {
            SetFlag(coord, RtsCellFlags.Visible, visible);
            if (visible)
            {
                SetFlag(coord, RtsCellFlags.Explored, true);
            }
        }

        public void SetOccupancy(Vector2Int coord, int occupancyCount)
        {
            if (!IsInsideMap(coord))
            {
                return;
            }

            int index = ToIndex(coord);
            RtsGridCell cell = cells[index];
            cell.OccupancyCount = Mathf.Max(0, occupancyCount);
            cells[index] = cell;
        }

        public void SetResource(Vector2Int coord, int amount, ResourceNode node)
        {
            if (!IsInsideMap(coord))
            {
                return;
            }

            int index = ToIndex(coord);
            RtsGridCell cell = cells[index];
            cell.ResourceAmount = Mathf.Max(0, amount);
            cell.ResourceNode = node;
            SetFlag(ref cell, RtsCellFlags.Resource, amount > 0 || node != null);
            SetFlag(ref cell, RtsCellFlags.Blocked, amount > 0 || node != null);
            cells[index] = cell;
        }

        public void RegisterResourceNode(ResourceNode node)
        {
            if (node == null)
            {
                return;
            }

            SetResource(WorldToCell(node.transform.position), node.Amount, node);
        }

        public void ReserveCells(Vector2Int originCell, RtsBuildFootprint footprint)
        {
            SetReservation(originCell, footprint, true);
        }

        public void ReleaseReservation(Vector2Int originCell, RtsBuildFootprint footprint)
        {
            SetReservation(originCell, footprint, false);
        }

        public RtsGridQueryResult CanPlaceFootprint(Vector2Int originCell, int width, int height)
        {
            return CanPlaceFootprint(originCell, new RtsBuildFootprint(width, height), RtsTeam.Player);
        }

        public RtsGridQueryResult CanPlaceFootprint(Vector2Int originCell, RtsBuildFootprint footprint, RtsTeam team)
        {
            for (int y = 0; y < footprint.Height; y++)
            {
                for (int x = 0; x < footprint.Width; x++)
                {
                    Vector2Int coord = new Vector2Int(originCell.x + x, originCell.y + y);
                    if (!IsInsideMap(coord))
                    {
                        return RtsGridQueryResult.Fail(RtsGridQueryFailureReason.OutsideMap, coord);
                    }

                    RtsGridCell cell = cells[ToIndex(coord)];
                    if (!cell.IsBuildable)
                    {
                        return RtsGridQueryResult.Fail(RtsGridQueryFailureReason.NotBuildable, coord);
                    }

                    if (cell.IsBlocked)
                    {
                        return RtsGridQueryResult.Fail(RtsGridQueryFailureReason.Blocked, coord);
                    }

                    if (cell.IsReserved)
                    {
                        return RtsGridQueryResult.Fail(RtsGridQueryFailureReason.Reserved, coord);
                    }

                    if (cell.OccupancyCount > 0)
                    {
                        return RtsGridQueryResult.Fail(RtsGridQueryFailureReason.Occupied, coord);
                    }
                }
            }

            return RtsGridQueryResult.Pass();
        }

        public void ClearDynamicState()
        {
            for (int i = 0; i < cells.Length; i++)
            {
                RtsGridCell cell = cells[i];
                cell.Flags &= ~(RtsCellFlags.Blocked | RtsCellFlags.Resource | RtsCellFlags.Explored | RtsCellFlags.Visible | RtsCellFlags.Reserved);
                cell.ResourceAmount = 0;
                cell.ResourceNode = null;
                cell.OccupancyCount = 0;
                cells[i] = cell;
            }
        }

        private void SetReservation(Vector2Int originCell, RtsBuildFootprint footprint, bool reserved)
        {
            for (int y = 0; y < footprint.Height; y++)
            {
                for (int x = 0; x < footprint.Width; x++)
                {
                    SetFlag(new Vector2Int(originCell.x + x, originCell.y + y), RtsCellFlags.Reserved, reserved);
                }
            }
        }

        private void SetFlag(Vector2Int coord, RtsCellFlags flag, bool enabled)
        {
            if (!IsInsideMap(coord))
            {
                return;
            }

            int index = ToIndex(coord);
            RtsGridCell cell = cells[index];
            SetFlag(ref cell, flag, enabled);
            cells[index] = cell;
        }

        private static void SetFlag(ref RtsGridCell cell, RtsCellFlags flag, bool enabled)
        {
            if (enabled)
            {
                cell.Flags |= flag;
                return;
            }

            cell.Flags &= ~flag;
        }

        private int ToIndex(Vector2Int coord)
        {
            return coord.y * Width + coord.x;
        }
    }
}
