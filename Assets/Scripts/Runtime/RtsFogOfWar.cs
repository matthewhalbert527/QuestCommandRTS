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
        private Renderer fogRenderer;
        private Texture2D fogTexture;
        private Color[] fogPixels;
        private Material fogMaterial;
        private bool fogTextureDirty;

        private sealed class FogCell
        {
            public int X;
            public int Z;
            public Vector3 Center;
            public bool Explored;
            public bool Visible;
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

        private void OnDestroy()
        {
            DestroyRuntimeObject(fogTexture);
            DestroyRuntimeObject(fogMaterial);
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

        public RtsFogCoverageSnapshot CaptureCoverageSnapshot()
        {
            RtsFogCoverageSnapshot snapshot = new RtsFogCoverageSnapshot
            {
                totalCells = GridSize * GridSize
            };

            if (cells == null)
            {
                return snapshot;
            }

            for (int x = 0; x < GridSize; x++)
            {
                for (int z = 0; z < GridSize; z++)
                {
                    FogCell cell = cells[x, z];
                    if (cell.Explored)
                    {
                        snapshot.exploredCells++;
                    }

                    if (cell.Visible)
                    {
                        snapshot.visibleCells++;
                    }
                }
            }

            if (snapshot.totalCells > 0)
            {
                snapshot.exploredPercent = snapshot.exploredCells / (float)snapshot.totalCells;
                snapshot.visiblePercent = snapshot.visibleCells / (float)snapshot.totalCells;
            }

            return snapshot;
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

        public void ResetExploration()
        {
            if (cells == null)
            {
                return;
            }

            for (int x = 0; x < GridSize; x++)
            {
                for (int z = 0; z < GridSize; z++)
                {
                    FogCell cell = cells[x, z];
                    cell.Explored = false;
                    cell.Visible = false;
                    ApplyCellVisual(cell);
                }
            }

            ApplyFogTextureIfNeeded();
            RefreshFog(true);
        }

#if UNITY_EDITOR
        public void RefreshNowForTests()
        {
            RefreshFog(true);
        }

        public int FogRendererCountForTests => fogRoot != null ? fogRoot.GetComponentsInChildren<Renderer>(true).Length : 0;
        public bool HasFogTextureForTests => fogTexture != null;

        public Color GetFogTextureColorForTests(Vector3 point)
        {
            FogCell cell = GetCell(point);
            if (cell == null || fogTexture == null)
            {
                return Color.clear;
            }

            GetTextureCoordinates(cell, out int textureX, out int textureY);
            return fogTexture.GetPixel(textureX, textureY);
        }
#endif

        private void BuildGrid()
        {
            cells = new FogCell[GridSize, GridSize];
            fogPixels = new Color[GridSize * GridSize];
            float start = -RtsBalance.MapHalfSize + cellSize * 0.5f;

            fogTexture = new Texture2D(GridSize, GridSize, TextureFormat.RGBA32, false);
            fogTexture.name = "Fog Alpha Texture";
            fogTexture.filterMode = FilterMode.Point;
            fogTexture.wrapMode = TextureWrapMode.Clamp;

            GameObject overlay = GameObject.CreatePrimitive(PrimitiveType.Plane);
            overlay.name = "Fog Overlay";
            overlay.transform.SetParent(fogRoot, false);
            overlay.transform.position = new Vector3(0f, 0.08f, 0f);
            overlay.transform.localScale = Vector3.one * (RtsBalance.MapHalfSize * 2f / 10f);

            Collider overlayCollider = overlay.GetComponent<Collider>();
            if (overlayCollider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(overlayCollider);
                }
                else
                {
                    DestroyImmediate(overlayCollider);
                }
            }

            fogRenderer = overlay.GetComponent<Renderer>();
            fogMaterial = CreateFogMaterial(fogTexture);
            fogRenderer.sharedMaterial = fogMaterial;

            for (int x = 0; x < GridSize; x++)
            {
                for (int z = 0; z < GridSize; z++)
                {
                    Vector3 center = new Vector3(start + x * cellSize, 0.08f, start + z * cellSize);
                    cells[x, z] = new FogCell
                    {
                        X = x,
                        Z = z,
                        Center = center
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

            ApplyFogTextureIfNeeded();
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
            if (cell == null || fogPixels == null)
            {
                return;
            }

            float alpha = cell.Visible ? 0f : cell.Explored ? 0.34f : 0.78f;
            GetTextureCoordinates(cell, out int textureX, out int textureY);
            fogPixels[textureY * GridSize + textureX] = new Color(0.005f, 0.006f, 0.008f, alpha);
            fogTextureDirty = true;
        }

        private static void GetTextureCoordinates(FogCell cell, out int textureX, out int textureY)
        {
            textureX = GridSize - 1 - cell.X;
            textureY = GridSize - 1 - cell.Z;
        }

        private void ApplyFogTextureIfNeeded()
        {
            if (!fogTextureDirty || fogTexture == null || fogPixels == null)
            {
                return;
            }

            fogTexture.SetPixels(fogPixels);
            fogTexture.Apply(false);
            fogTextureDirty = false;
        }

        private static void DestroyRuntimeObject(UnityEngine.Object target)
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

                entity.RefreshVisibilityDependentVisuals();
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
                if (structure.StructureKind == StructureKind.AdvancedGunTower)
                {
                    return 30f;
                }

                if (structure.StructureKind == StructureKind.Turret || structure.StructureKind == StructureKind.GunTower)
                {
                    return 26f;
                }

                return 20f;
            }

            RtsUnit unit = entity as RtsUnit;
            if (unit != null)
            {
                switch (unit.UnitKind)
                {
                    case UnitKind.Harvester:
                        return 17f;
                    case UnitKind.RocketSoldier:
                        return 18f;
                    case UnitKind.Engineer:
                        return 15f;
                    case UnitKind.Tank:
                    case UnitKind.LightTank:
                    case UnitKind.MediumTank:
                        return 20f;
                    case UnitKind.HeavyTank:
                        return 23f;
                    default:
                        return 16f;
                }
            }

            return 8f;
        }

        private static Material CreateFogMaterial(Texture2D texture)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Transparent");
            }

            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            Material material = shader != null ? new Material(shader) : RtsGame.CreateMaterial(Color.white);
            material.color = Color.white;
            material.mainTexture = texture;
            material.SetTexture("_MainTex", texture);
            material.SetTexture("_BaseMap", texture);
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
