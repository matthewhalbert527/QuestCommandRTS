#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace QuestCommandRTS.Editor
{
    public static class RtsSoakScenarioExporter
    {
        private const string ScenePath = "Assets/Scenes/Battlefield.unity";
        private const string ScenarioName = "Generated desktop soak baseline";
        private const string DefaultOutputPath = "C:/Users/matth/Documents/Codex/2026-06-24/i-s/outputs/quest-command-rts-soak-diagnostics.json";

        [MenuItem("Command RTS/Export Soak Diagnostics Snapshot")]
        public static void ExportMenuSnapshot()
        {
            Export(DefaultOutputPath);
        }

        public static void ExportForCodex()
        {
            Export(DefaultOutputPath);
        }

        private static void Export(string outputPath)
        {
            EditorSceneManager.OpenScene(ScenePath);

            RtsRuntimeDiagnosticsSnapshot snapshot = BuildPopulatedDesktopSnapshot();
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, snapshot.ToJson(true));
            AssetDatabase.Refresh();
            Debug.Log("Command RTS soak diagnostics snapshot exported to " + outputPath);
        }

        internal static RtsRuntimeDiagnosticsSnapshot BuildPopulatedDesktopSnapshot()
        {
            RtsRuntimeModeResolver.ForceModeForTests(RtsRuntimeMode.Desktop);
            GameObject root = new GameObject("Soak Diagnostics Runtime");
            RtsGame game = root.AddComponent<RtsGame>();
            try
            {
                game.Initialize();
                Physics.SyncTransforms();
                PopulateGeneratedMatchForSoak(game);
                Physics.SyncTransforms();

                RtsRuntimeDiagnosticsSnapshot snapshot = RtsRuntimeDiagnosticsSnapshot.Capture(game);
                snapshot.scenarioName = ScenarioName;
                return snapshot;
            }
            finally
            {
                RtsRuntimeModeResolver.ForceModeForTests(null);
                Object.DestroyImmediate(root);
            }
        }

        internal static void PopulateGeneratedMatchForSoak(RtsGame game)
        {
            if (game == null || game.Resources == null)
            {
                return;
            }

            game.Resources.Add(60000);
            EnsureMinimumPlayerProductionStructures(game, 4);
            SpawnAdditionalUnits(game);
            QueueProductionForAllPlayerProducers(game);
            IssueMixedArmyOrders(game);
            StartValidPlacementPreview(game);
        }

        private static void EnsureMinimumPlayerProductionStructures(RtsGame game, int minimumCount)
        {
            if (!game.HasPlayerStructure(StructureKind.WarFactory))
            {
                game.CreateStructure(RtsTeam.Player, StructureKind.WarFactory, new Vector3(-60f, 0f, -64f));
            }

            Vector3[] extraProductionPositions =
            {
                new Vector3(-52f, 0f, -68f),
                new Vector3(-86f, 0f, -50f),
                new Vector3(-64f, 0f, -52f)
            };

            int positionIndex = 0;
            while (CountPlayerProductionStructures(game) < minimumCount && positionIndex < extraProductionPositions.Length)
            {
                game.CreateStructure(RtsTeam.Player, StructureKind.WarFactory, extraProductionPositions[positionIndex]);
                positionIndex++;
            }

            game.RecalculatePower();
        }

        private static int CountPlayerProductionStructures(RtsGame game)
        {
            int count = 0;
            for (int i = 0; i < game.Entities.Count; i++)
            {
                ProductionStructure producer = game.Entities[i] as ProductionStructure;
                if (producer != null && producer.Team == RtsTeam.Player && producer.IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        private static void SpawnAdditionalUnits(RtsGame game)
        {
            for (int i = 0; i < 12; i++)
            {
                game.CreateUnit(RtsTeam.Player, UnitKind.Rifleman, FormationPoint(new Vector3(-48f, 0f, -46f), i, 6, 2.6f));
            }

            for (int i = 0; i < 8; i++)
            {
                game.CreateUnit(RtsTeam.Player, UnitKind.Tank, FormationPoint(new Vector3(-36f, 0f, -36f), i, 4, 3.4f));
            }

            for (int i = 0; i < 4; i++)
            {
                game.CreateUnit(RtsTeam.Player, UnitKind.Harvester, FormationPoint(new Vector3(-58f, 0f, -88f), i, 4, 3.2f));
            }

            for (int i = 0; i < 18; i++)
            {
                game.CreateUnit(RtsTeam.Enemy, UnitKind.Rifleman, FormationPoint(new Vector3(48f, 0f, 48f), i, 6, 2.6f));
            }

            for (int i = 0; i < 12; i++)
            {
                game.CreateUnit(RtsTeam.Enemy, UnitKind.Tank, FormationPoint(new Vector3(66f, 0f, 42f), i, 4, 3.5f));
            }
        }

        private static void QueueProductionForAllPlayerProducers(RtsGame game)
        {
            List<ProductionStructure> producers = new List<ProductionStructure>();
            for (int i = 0; i < game.Entities.Count; i++)
            {
                ProductionStructure producer = game.Entities[i] as ProductionStructure;
                if (producer != null && producer.Team == RtsTeam.Player && producer.IsAlive)
                {
                    producers.Add(producer);
                }
            }

            for (int i = 0; i < producers.Count; i++)
            {
                ProductionStructure producer = producers[i];
                QueueIfAvailable(producer, UnitKind.Rifleman);
                QueueIfAvailable(producer, UnitKind.Harvester);
                QueueIfAvailable(producer, UnitKind.Tank);
                producer.StartNextProductionForTests();
            }
        }

        private static void QueueIfAvailable(ProductionStructure producer, UnitKind kind)
        {
            if (producer.CanTrain(kind))
            {
                producer.QueueUnit(kind);
            }
        }

        private static void IssueMixedArmyOrders(RtsGame game)
        {
            game.SelectCombatUnits();
            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsUnit unit = game.Entities[i] as RtsUnit;
                if (unit == null || unit.Team != RtsTeam.Player || unit.UnitKind == UnitKind.Harvester)
                {
                    continue;
                }

                Vector3 target = new Vector3(24f + i % 5 * 2.2f, 0f, 18f + i % 4 * 2.4f);
                unit.IssueAttackMove(target);
            }
        }

        private static void StartValidPlacementPreview(RtsGame game)
        {
            if (game.BuildManager == null || game.PlayerCommands == null)
            {
                return;
            }

            if (!game.PlayerCommands.RequestConstruction(StructureKind.Turret))
            {
                return;
            }

            if (TryFindValidBuildPoint(game, StructureKind.Turret, out Vector3 point))
            {
                game.BuildManager.UpdatePlacementAtPoint(point);
            }
        }

        private static bool TryFindValidBuildPoint(RtsGame game, StructureKind kind, out Vector3 point)
        {
            BuildPlacementFailureReason reason;
            for (float z = -52f; z <= -20f; z += 4f)
            {
                for (float x = -104f; x <= -44f; x += 4f)
                {
                    point = new Vector3(x, 0f, z);
                    if (game.BuildManager.CanPlaceAt(point, kind, out reason))
                    {
                        return true;
                    }
                }
            }

            point = Vector3.zero;
            return false;
        }

        private static Vector3 FormationPoint(Vector3 origin, int index, int columns, float spacing)
        {
            int row = index / columns;
            int column = index % columns;
            return origin + new Vector3(column * spacing, 0f, row * spacing);
        }
    }
}
#endif
