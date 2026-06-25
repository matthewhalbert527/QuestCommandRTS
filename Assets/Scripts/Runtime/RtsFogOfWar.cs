using System.Collections.Generic;
using UnityEngine;

namespace QuestCommandRTS
{
    public sealed class RtsFogOfWar : MonoBehaviour
    {
        private const int GridSize = 56;
        private const float UpdateInterval = 0.22f;

        private readonly List<Renderer> rendererBuffer = new List<Renderer>();
        private RtsGame game;
        private FogCell[,] cells;
        private float cellSize;
        private float nextUpdateTime;
        private Transform fogRoot;

        private sealed class FogCell
        {
            public Vector3 Center;
            public bool Explored;
            public bool Visible;
            public Renderer Renderer;
            public Material Material;
        }

        public void Initialize(RtsGame owner)
        {
            game = owner;
            cellSize = (RtsBalance.MapHalfSize * 2f) / GridSize;
            fogRoot = new GameObject("Fog of War").transform;
            fogRoot.SetParent(transform, false);
            BuildGrid();
            RefreshFog(true);
        }

        private void Update()
        {
            if (game == null || game.Clock.IsPaused || game.Clock.SimulationTime < nextUpdateTime)
            {
                return;
            }

            nextUpdateTime = game.Clock.SimulationTime + UpdateInterval;
            using (RtsProfilerMarkers.FogUpdate.Auto())
            {
                RefreshFog(false);
            }
        }

        public bool IsVisible(Vector3 point)
        {
            FogCell cell = GetCell(point);
            return cell == null || cell.Visible;
        }

        public bool IsExplored(Vector3 point)
        {
            FogCell cell = GetCell(point);
            return cell == null || cell.Explored;
        }

        public RtsFogSaveData CaptureState()
        {
            RtsFogSaveData data = new RtsFogSaveData
            {
                gridSize = GridSize,
                explored = new bool[GridSize * GridSize]
            };

            if (cells == null)
            {
                return data;
            }

            int index = 0;
            for (int z = 0; z < GridSize; z++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    data.explored[index++] = cells[x, z].Explored;
                }
            }

            return data;
        }

        public void RestoreState(RtsFogSaveData data)
        {
            if (data == null || data.explored == null || cells == null || data.gridSize != GridSize)
            {
                RefreshFog(true);
                return;
            }

            int index = 0;
            for (int z = 0; z < GridSize; z++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    FogCell cell = cells[x, z];
                    cell.Explored = index < data.explored.Length && data.explored[index];
                    cell.Visible = false;
                    index++;
                    ApplyCellVisual(cell);
                }
            }

            RefreshFog(true);
        }

        private void BuildGrid()
        {
            cells = new FogCell[GridSize, GridSize];
            float start = -RtsBalance.MapHalfSize + cellSize * 0.5f;

            for (int x = 0; x < GridSize; x++)
            {
                for (int z = 0; z < GridSize; z++)
                {
                    Vector3 center = new Vector3(start + x * cellSize, 0.08f, start + z * cellSize);
                    GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = "Fog Cell";
                    tile.transform.SetParent(fogRoot, false);
                    tile.transform.position = center;
                    tile.transform.localScale = new Vector3(cellSize * 1.02f, 0.025f, cellSize * 1.02f);

                    Collider collider = tile.GetComponent<Collider>();
                    if (collider != null)
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(collider);
                        }
                        else
                        {
                            DestroyImmediate(collider);
                        }
                    }

                    Renderer renderer = tile.GetComponent<Renderer>();
                    Material material = CreateFogMaterial(0.78f);
                    renderer.sharedMaterial = material;

                    cells[x, z] = new FogCell
                    {
                        Center = center,
                        Renderer = renderer,
                        Material = material
                    };
                }
            }
        }

        private void RefreshFog(bool force)
        {
            if (cells == null)
            {
                return;
            }

            for (int x = 0; x < GridSize; x++)
            {
                for (int z = 0; z < GridSize; z++)
                {
                    cells[x, z].Visible = false;
                }
            }

            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsEntity entity = game.Entities[i];
                if (entity == null || !entity.IsAlive || entity.Team != RtsTeam.Player)
                {
                    continue;
                }

                Reveal(entity.GroundPosition, GetRevealRadius(entity));
            }

            for (int x = 0; x < GridSize; x++)
            {
                for (int z = 0; z < GridSize; z++)
                {
                    FogCell cell = cells[x, z];
                    cell.Explored = cell.Explored || cell.Visible;
                    ApplyCellVisual(cell);
                }
            }

            ApplyEnemyVisibility();
        }

        private void Reveal(Vector3 position, float radius)
        {
            float radiusSqr = radius * radius;
            for (int x = 0; x < GridSize; x++)
            {
                for (int z = 0; z < GridSize; z++)
                {
                    FogCell cell = cells[x, z];
                    float dx = cell.Center.x - position.x;
                    float dz = cell.Center.z - position.z;
                    if (dx * dx + dz * dz <= radiusSqr)
                    {
                        cell.Visible = true;
                    }
                }
            }
        }

        private void ApplyCellVisual(FogCell cell)
        {
            if (cell.Renderer == null)
            {
                return;
            }

            if (cell.Visible)
            {
                cell.Renderer.enabled = false;
                return;
            }

            cell.Renderer.enabled = true;
            float alpha = cell.Explored ? 0.34f : 0.78f;
            Color color = new Color(0.005f, 0.006f, 0.008f, alpha);
            cell.Material.color = color;
            cell.Material.SetColor("_Color", color);
            cell.Material.SetColor("_BaseColor", color);
        }

        private void ApplyEnemyVisibility()
        {
            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsEntity entity = game.Entities[i];
                if (entity == null || entity.Team != RtsTeam.Enemy)
                {
                    continue;
                }

                bool visible = entity.IsAlive && IsVisible(entity.GroundPosition);
                rendererBuffer.Clear();
                entity.GetComponentsInChildren(rendererBuffer);

                for (int r = 0; r < rendererBuffer.Count; r++)
                {
                    if (rendererBuffer[r] is LineRenderer)
                    {
                        continue;
                    }

                    rendererBuffer[r].enabled = visible;
                }
            }
        }

        private FogCell GetCell(Vector3 point)
        {
            if (cells == null)
            {
                return null;
            }

            int x = Mathf.FloorToInt((point.x + RtsBalance.MapHalfSize) / cellSize);
            int z = Mathf.FloorToInt((point.z + RtsBalance.MapHalfSize) / cellSize);

            if (x < 0 || x >= GridSize || z < 0 || z >= GridSize)
            {
                return null;
            }

            return cells[x, z];
        }

        private static float GetRevealRadius(RtsEntity entity)
        {
            RtsStructure structure = entity as RtsStructure;
            if (structure != null)
            {
                return structure.StructureKind == StructureKind.Turret ? 26f : 20f;
            }

            RtsUnit unit = entity as RtsUnit;
            if (unit != null)
            {
                switch (unit.UnitKind)
                {
                    case UnitKind.Harvester:
                        return 17f;
                    case UnitKind.Tank:
                        return 20f;
                    default:
                        return 16f;
                }
            }

            return 8f;
        }

        private static Material CreateFogMaterial(float alpha)
        {
            Material material = RtsGame.CreateMaterial(new Color(0.005f, 0.006f, 0.008f, alpha));
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 2990;
            return material;
        }
    }
}
