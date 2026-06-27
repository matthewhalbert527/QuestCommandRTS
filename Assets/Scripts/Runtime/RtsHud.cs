using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuestCommandRTS
{
    public sealed class RtsHud : MonoBehaviour
    {
        private sealed class HudButton
        {
            public Button Button;
            public Text Label;
            public Func<bool> IsEnabled;
            public Func<string> GetText;
        }

        private enum SidebarTab
        {
            Buildings,
            Units,
            Commands
        }

        private sealed class MenuButtonSpec
        {
            public readonly string Name;
            public readonly UnityEngine.Events.UnityAction Action;
            public readonly Func<bool> IsEnabled;
            public readonly Func<string> GetText;

            public MenuButtonSpec(string name, UnityEngine.Events.UnityAction action, Func<bool> isEnabled, Func<string> getText)
            {
                Name = name;
                Action = action;
                IsEnabled = isEnabled;
                GetText = getText;
            }
        }

        private readonly List<HudButton> buttons = new List<HudButton>();
        private readonly List<Image> minimapPips = new List<Image>();
        private readonly List<ProductionTileMeter> productionTileMeters = new List<ProductionTileMeter>();
        private readonly Dictionary<UnitKind, Sprite> unitTileArtSprites = new Dictionary<UnitKind, Sprite>();
        private readonly Dictionary<StructureKind, Sprite> structureTileArtSprites = new Dictionary<StructureKind, Sprite>();
        private const float SidebarWidth = 326f;
        private const float SidebarCollapsedWidth = 46f;
        private const float SidebarMargin = 12f;
        private const float SidebarMinimapSize = 252f;
        private const string UnitTileArtResourceRoot = "HudArt/Units/";
        private const string StructureTileArtResourceRoot = "HudArt/Structures/";

        private RtsGame game;
        private Text resourcesText;
        private Text selectionText;
        private Text productionProgressText;
        private RectTransform productionProgressFill;
        private RectTransform commandPanel;
        private RectTransform sidebarContent;
        private RectTransform buildingsTabContent;
        private RectTransform unitsTabContent;
        private RectTransform commandsTabContent;
        private RectTransform minimapPlot;
        private RectTransform selectionPanel;
        private Font font;
        private GUIStyle bannerStyle;
        private GUIStyle bannerSubStyle;
        private RectTransform mainMenuPanel;
        private RectTransform pauseMenuPanel;
        private RtsSkirmishOptions menuOptions;
        private SidebarTab activeSidebarTab = SidebarTab.Buildings;
        private bool mainMenuVisible;
        private bool sidebarCollapsed;
        private Sprite hudFillSprite;

        private sealed class ProductionTileMeter
        {
            public UnitKind Kind;
            public Image ClockFill;
            public Text QueueBadge;
        }

        public void Initialize(RtsGame owner)
        {
            game = owner;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            menuOptions = game != null && game.SkirmishOptions != null ? game.SkirmishOptions.Clone() : RtsSkirmishOptions.CreateDefault();
            EnsureEventSystem(transform);
            BuildCanvas();

            if (Application.isPlaying)
            {
                ShowMainMenu();
            }
        }

        private void Update()
        {
            RefreshRuntimeHud();
        }

        private void OnGUI()
        {
            if (game == null)
            {
                return;
            }

            if (game.MatchState != RtsMatchState.Running)
            {
                DrawMatchBanner();
            }
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject("RTS HUD");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            commandPanel = CreatePanel(canvasObject.transform, "Commands", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-SidebarMargin, 0f), new Vector2(SidebarWidth, -24f), new Color(0.018f, 0.023f, 0.025f, 0.92f));
            sidebarContent = CreatePanel(commandPanel, "Command Sidebar Content", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-18f, -18f), new Color(0.03f, 0.04f, 0.042f, 0.36f));

            AddSidebarToggle(commandPanel);

            CreateText(sidebarContent, "Command Sidebar Header", "COMMAND", 20, TextAnchor.MiddleLeft, new Vector2(0f, -10f), new Vector2(-16f, 30f));

            RectTransform minimapFrame = CreatePanel(sidebarContent, "Sidebar Minimap", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(-28f, SidebarMinimapSize + 30f), new Color(0.008f, 0.016f, 0.018f, 0.94f));
            CreateText(minimapFrame, "Sidebar Minimap Label", "TACTICAL MAP", 13, TextAnchor.MiddleCenter, new Vector2(0f, -6f), new Vector2(-18f, 20f));
            minimapPlot = CreatePanel(minimapFrame, "Sidebar Minimap Plot", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(SidebarMinimapSize, SidebarMinimapSize), new Color(0.018f, 0.045f, 0.046f, 0.96f));
            BuildSidebarMinimapGrid();

            RectTransform economyPanel = CreatePanel(sidebarContent, "Sidebar Economy", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -342f), new Vector2(-28f, 72f), new Color(0.025f, 0.07f, 0.06f, 0.88f));
            resourcesText = CreateText(economyPanel, "Resources Text", "", 16, TextAnchor.MiddleLeft);
            resourcesText.rectTransform.offsetMin = new Vector2(14f, 0f);
            resourcesText.rectTransform.offsetMax = new Vector2(-10f, 0f);

            RectTransform tabRail = CreatePanel(sidebarContent, "Command Sidebar Tabs", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -424f), new Vector2(-28f, 34f), new Color(0.01f, 0.018f, 0.019f, 0.82f));
            AddTabButton(tabRail, "Buildings Tab", "BUILD", SidebarTab.Buildings, 0);
            AddTabButton(tabRail, "Units Tab", "UNITS", SidebarTab.Units, 1);
            AddTabButton(tabRail, "Commands Tab", "CMDS", SidebarTab.Commands, 2);

            RectTransform progressBackplate = CreatePanel(sidebarContent, "Production Progress Backplate", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -466f), new Vector2(-28f, 34f), new Color(0.018f, 0.05f, 0.056f, 0.88f));
            productionProgressFill = CreatePanel(progressBackplate, "Production Progress Fill", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.22f, 0.78f, 1f, 0.86f));
            productionProgressText = CreateText(progressBackplate, "Production Progress Text", "", 11, TextAnchor.MiddleCenter);

            RectTransform tileArea = CreatePanel(sidebarContent, "Command Sidebar Tile Area", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -162f), new Vector2(-28f, -518f), new Color(0f, 0f, 0f, 0f));
            tileArea.offsetMin = new Vector2(14f, 14f);
            tileArea.offsetMax = new Vector2(-14f, -516f);
            buildingsTabContent = CreateTabContent(tileArea, "Buildings Tile Content");
            unitsTabContent = CreateTabContent(tileArea, "Units Tile Content");
            commandsTabContent = CreateTabContent(tileArea, "Commands Tile Content");

            AddBuildTile(buildingsTabContent, StructureKind.PowerPlant, 0);
            AddBuildTile(buildingsTabContent, StructureKind.Barracks, 1);
            AddBuildTile(buildingsTabContent, StructureKind.Refinery, 2);
            AddBuildTile(buildingsTabContent, StructureKind.WarFactory, 3);
            AddBuildTile(buildingsTabContent, StructureKind.Turret, 4);
            AddBuildTile(buildingsTabContent, StructureKind.GunTower, 5);
            AddBuildTile(buildingsTabContent, StructureKind.AdvancedGunTower, 6);
            AddBuildTile(buildingsTabContent, StructureKind.DualHelipad, 7);

            AddUnitTile(unitsTabContent, UnitKind.Rifleman, 0);
            AddUnitTile(unitsTabContent, UnitKind.Grenadier, 1);
            AddUnitTile(unitsTabContent, UnitKind.RocketSoldier, 2);
            AddUnitTile(unitsTabContent, UnitKind.FlameTrooper, 3);
            AddUnitTile(unitsTabContent, UnitKind.Engineer, 4);
            AddUnitTile(unitsTabContent, UnitKind.Harvester, 5);
            AddUnitTile(unitsTabContent, UnitKind.Humvee, 6);
            AddUnitTile(unitsTabContent, UnitKind.Apc, 7);
            AddUnitTile(unitsTabContent, UnitKind.LightTank, 8);
            AddUnitTile(unitsTabContent, UnitKind.MediumTank, 9);
            AddUnitTile(unitsTabContent, UnitKind.HeavyTank, 10);
            AddUnitTile(unitsTabContent, UnitKind.Skyraider, 11);
            AddUnitTile(unitsTabContent, UnitKind.OrcaLifter, 12);

            AddActionTile(commandsTabContent, "Army", "ARMY", "Select", 0, () => game.SelectCombatUnits(), () => game.AcceptsPlayerInput, () => "All combat");
            AddActionTile(commandsTabContent, "Stop", "STOP", "S", 1, () => game.CommandDispatcher.StopSelectedUnits(), () => game.AcceptsPlayerInput && game.HasSelectedControllableUnits(), () => "Stop selected");
            AddActionTile(commandsTabContent, "Repair", "RPR", "Z", 2, () => game.PlayerCommands.RepairSelectedStructures(), () => game.AcceptsPlayerInput && game.CanRepairSelectedStructures(), () => "Repair");
            AddActionTile(commandsTabContent, "Sell", "SELL", "X", 3, () => game.PlayerCommands.SellSelectedStructures(), () => game.AcceptsPlayerInput && game.CanSellSelectedStructures(), () => "Sell");
            AddActionTile(commandsTabContent, "Pause", "PAUSE", "P", 4, () => game.ToggleUserPause(), () => game.AcceptsSystemInput && !game.IsMatchOver, () => game.IsUserPaused ? "Resume" : "Pause");
            AddActionTile(commandsTabContent, "Save", "SAVE", "F5", 5, () => game.TryManualSave(), () => game.AcceptsSystemInput && !game.IsMatchOver, () => "Manual save");
            AddActionTile(commandsTabContent, "Load", "LOAD", "F9", 6, () => game.TryManualLoad(), () => game.AcceptsSystemInput && game.CanLoadManualSave(), () => game.GetManualSaveSummary());
            AddActionTile(commandsTabContent, "New Match", "NEW", "Match", 7, () => game.TryRestartMatch(), () => game.AcceptsSystemInput, () => "Restart");

            selectionPanel = CreatePanel(canvasObject.transform, "Selection", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(12f, 12f), new Vector2(-SidebarWidth - 36f, 118f), new Color(0.02f, 0.024f, 0.026f, 0.8f));
            selectionPanel.offsetMin = new Vector2(12f, 12f);
            selectionPanel.offsetMax = new Vector2(-SidebarWidth - 36f, 130f);
            selectionText = CreateText(selectionPanel, "Selection Text", "", 17, TextAnchor.UpperLeft);
            selectionText.rectTransform.offsetMin = new Vector2(16f, 10f);
            selectionText.rectTransform.offsetMax = new Vector2(-16f, -10f);

            mainMenuPanel = BuildMenuPanel(
                canvasObject.transform,
                "Main Menu",
                "Quest Command RTS",
                "Desktop Skirmish",
                new[]
                {
                    new MenuButtonSpec("Difficulty", () => CycleDifficulty(), () => true, () => "AI: " + menuOptions.difficulty),
                    new MenuButtonSpec("Credits", () => CycleStartingCredits(), () => true, () => "Credits: " + menuOptions.StartingCreditsLabel),
                    new MenuButtonSpec("Peace Time", () => CyclePeaceTime(), () => true, () => "Peace Time: " + menuOptions.PeaceTimeLabel),
                    new MenuButtonSpec("Game Speed", () => CycleGameSpeed(), () => true, () => "Speed: " + menuOptions.GameSpeedLabel),
                    new MenuButtonSpec("Fog", () => CycleFog(), () => true, () => "Fog: " + menuOptions.FogLabel),
                    new MenuButtonSpec("Starting Forces", () => CycleStartingForces(), () => true, () => "Start: " + menuOptions.StartingForcesLabel),
                    new MenuButtonSpec("Start Skirmish", () => StartSkirmishFromMainMenu(), () => true, () => "Start Skirmish"),
                    new MenuButtonSpec("Load Game", () => LoadFromMainMenu(), () => game.CanLoadManualSave(), () => "Load Game  " + game.GetManualSaveSummary()),
                    new MenuButtonSpec("Quit", () => game.RequestQuit(), () => true, () => "Quit")
                });

            pauseMenuPanel = BuildMenuPanel(
                canvasObject.transform,
                "Pause Menu",
                "Paused",
                "Skirmish Controls",
                new[]
                {
                    new MenuButtonSpec("Resume", () => game.SetUserPaused(false), () => game.AcceptsSystemInput && !game.IsMatchOver, () => "Resume"),
                    new MenuButtonSpec("Restart", () => game.TryRestartMatch(), () => game.AcceptsSystemInput, () => "Restart Skirmish"),
                    new MenuButtonSpec("Save", () => game.TryManualSave(), () => game.AcceptsSystemInput && !game.IsMatchOver, () => "Save Game"),
                    new MenuButtonSpec("Load", () => game.TryManualLoad(), () => game.AcceptsSystemInput && game.CanLoadManualSave(), () => "Load Game  " + game.GetManualSaveSummary()),
                    new MenuButtonSpec("Quit", () => game.RequestQuit(), () => true, () => "Quit")
                });

            RefreshMenuPanels();
        }

        public void ShowMainMenu()
        {
            mainMenuVisible = true;
            menuOptions = game != null && game.SkirmishOptions != null ? game.SkirmishOptions.Clone() : RtsSkirmishOptions.CreateDefault();
            if (game != null && !game.IsMatchOver)
            {
                game.SetUserPaused(true);
            }

            RefreshMenuPanels();
        }

#if UNITY_EDITOR
        public bool IsMainMenuVisibleForTests => mainMenuVisible && mainMenuPanel != null && mainMenuPanel.gameObject.activeSelf;
        public bool IsPauseMenuVisibleForTests => pauseMenuPanel != null && pauseMenuPanel.gameObject.activeSelf;

        public void ShowMainMenuForTests()
        {
            ShowMainMenu();
        }

        public void StartSkirmishFromMainMenuForTests()
        {
            StartSkirmishFromMainMenu();
        }

        public void RefreshMenuPanelsForTests()
        {
            RefreshMenuPanels();
        }

        public void RefreshForScreenshot()
        {
            RefreshRuntimeHud();
            Canvas.ForceUpdateCanvases();
            RefreshSidebarLayout();
            Canvas.ForceUpdateCanvases();
            RefreshMinimap();
            Canvas.ForceUpdateCanvases();
        }

        public void ShowUnitsTabForScreenshot()
        {
            activeSidebarTab = SidebarTab.Units;
            RefreshForScreenshot();
        }

        public void CycleSkirmishDifficultyForTests()
        {
            CycleDifficulty();
        }

        public void CycleSkirmishCreditsForTests()
        {
            CycleStartingCredits();
        }

        public void CycleSkirmishPeaceTimeForTests()
        {
            CyclePeaceTime();
        }

        public void CycleSkirmishGameSpeedForTests()
        {
            CycleGameSpeed();
        }

        public void CycleSkirmishFogForTests()
        {
            CycleFog();
        }

        public void CycleSkirmishStartingForcesForTests()
        {
            CycleStartingForces();
        }
#endif

        private void RefreshRuntimeHud()
        {
            if (game == null || game.Resources == null)
            {
                return;
            }

            string powerColor = game.Resources.HasLowPower ? "LOW" : "OK";
            string status = game.IsUserPaused ? "PAUSED" : game.StatusMessage;
            resourcesText.text = "CREDITS $" + game.Resources.Credits.ToString("N0") +
                "\nPOWER " + game.Resources.PowerUsed + "/" + game.Resources.PowerProvided + " " + powerColor +
                "\nTIME " + FormatTime(game.MatchTime) + "    " + status;
            selectionText.text = BuildSelectionText();
            RefreshProductionProgress();
            RefreshProductionTileMeters();
            RefreshSidebarLayout();
            RefreshMinimap();

            for (int i = 0; i < buttons.Count; i++)
            {
                HudButton hudButton = buttons[i];
                if (hudButton.Button != null && hudButton.IsEnabled != null)
                {
                    hudButton.Button.interactable = hudButton.IsEnabled();
                }

                if (hudButton.Label != null && hudButton.GetText != null)
                {
                    hudButton.Label.text = hudButton.GetText();
                }
            }

            RefreshMenuPanels();
        }

        private void StartSkirmishFromMainMenu()
        {
            mainMenuVisible = false;
            game.SetSkirmishOptions(menuOptions);
            if (!game.TryRestartMatch())
            {
                game.SetUserPaused(false);
            }

            RefreshMenuPanels();
        }

        private void LoadFromMainMenu()
        {
            if (game.TryManualLoad())
            {
                mainMenuVisible = false;
                game.SetUserPaused(false);
            }

            RefreshMenuPanels();
        }

        private void CycleDifficulty()
        {
            EnsureMenuOptions();
            switch (menuOptions.difficulty)
            {
                case RtsAiDifficulty.Recruit:
                    menuOptions.difficulty = RtsAiDifficulty.Standard;
                    break;
                case RtsAiDifficulty.Standard:
                    menuOptions.difficulty = RtsAiDifficulty.Veteran;
                    break;
                case RtsAiDifficulty.Veteran:
                    menuOptions.difficulty = RtsAiDifficulty.Brutal;
                    break;
                default:
                    menuOptions.difficulty = RtsAiDifficulty.Recruit;
                    break;
            }
        }

        private void CycleStartingCredits()
        {
            EnsureMenuOptions();
            switch (menuOptions.startingCredits)
            {
                case RtsStartingCreditsPreset.Low:
                    menuOptions.startingCredits = RtsStartingCreditsPreset.Standard;
                    break;
                case RtsStartingCreditsPreset.Standard:
                    menuOptions.startingCredits = RtsStartingCreditsPreset.High;
                    break;
                case RtsStartingCreditsPreset.High:
                    menuOptions.startingCredits = RtsStartingCreditsPreset.Massive;
                    break;
                default:
                    menuOptions.startingCredits = RtsStartingCreditsPreset.Low;
                    break;
            }
        }

        private void CyclePeaceTime()
        {
            EnsureMenuOptions();
            switch (menuOptions.peaceTime)
            {
                case RtsPeaceTimePreset.None:
                    menuOptions.peaceTime = RtsPeaceTimePreset.TwoMinutes;
                    break;
                case RtsPeaceTimePreset.TwoMinutes:
                    menuOptions.peaceTime = RtsPeaceTimePreset.ThreeMinutes;
                    break;
                case RtsPeaceTimePreset.ThreeMinutes:
                    menuOptions.peaceTime = RtsPeaceTimePreset.FiveMinutes;
                    break;
                default:
                    menuOptions.peaceTime = RtsPeaceTimePreset.None;
                    break;
            }
        }

        private void CycleGameSpeed()
        {
            EnsureMenuOptions();
            switch (menuOptions.gameSpeed)
            {
                case RtsGameSpeedPreset.Slow:
                    menuOptions.gameSpeed = RtsGameSpeedPreset.Normal;
                    break;
                case RtsGameSpeedPreset.Normal:
                    menuOptions.gameSpeed = RtsGameSpeedPreset.Fast;
                    break;
                default:
                    menuOptions.gameSpeed = RtsGameSpeedPreset.Slow;
                    break;
            }
        }

        private void CycleFog()
        {
            EnsureMenuOptions();
            menuOptions.fog = menuOptions.fog == RtsFogPreset.Enabled ? RtsFogPreset.Revealed : RtsFogPreset.Enabled;
        }

        private void CycleStartingForces()
        {
            EnsureMenuOptions();
            switch (menuOptions.startingForces)
            {
                case RtsStartingForcesPreset.FabricationOnly:
                    menuOptions.startingForces = RtsStartingForcesPreset.ScoutTeam;
                    break;
                case RtsStartingForcesPreset.ScoutTeam:
                    menuOptions.startingForces = RtsStartingForcesPreset.StrikeTeam;
                    break;
                default:
                    menuOptions.startingForces = RtsStartingForcesPreset.FabricationOnly;
                    break;
            }
        }

        private void EnsureMenuOptions()
        {
            if (menuOptions == null)
            {
                menuOptions = game != null && game.SkirmishOptions != null ? game.SkirmishOptions.Clone() : RtsSkirmishOptions.CreateDefault();
            }

            menuOptions.Normalize();
        }

        private void RefreshMenuPanels()
        {
            if (game == null)
            {
                return;
            }

            if (mainMenuVisible && !game.IsMatchOver && !game.IsUserPaused)
            {
                game.SetUserPaused(true);
            }

            if (mainMenuPanel != null)
            {
                mainMenuPanel.gameObject.SetActive(mainMenuVisible);
            }

            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.gameObject.SetActive(!mainMenuVisible && game.IsUserPaused);
            }
        }

        private void RefreshSidebarLayout()
        {
            float width = sidebarCollapsed ? SidebarCollapsedWidth : SidebarWidth;
            if (commandPanel != null)
            {
                commandPanel.sizeDelta = new Vector2(width, -24f);
            }

            if (sidebarContent != null)
            {
                sidebarContent.gameObject.SetActive(!sidebarCollapsed);
            }

            if (selectionPanel != null)
            {
                selectionPanel.offsetMax = new Vector2(-width - 36f, 130f);
            }

            if (buildingsTabContent != null)
            {
                buildingsTabContent.gameObject.SetActive(!sidebarCollapsed && activeSidebarTab == SidebarTab.Buildings);
            }

            if (unitsTabContent != null)
            {
                unitsTabContent.gameObject.SetActive(!sidebarCollapsed && activeSidebarTab == SidebarTab.Units);
            }

            if (commandsTabContent != null)
            {
                commandsTabContent.gameObject.SetActive(!sidebarCollapsed && activeSidebarTab == SidebarTab.Commands);
            }
        }

        private void AddSidebarToggle(RectTransform parent)
        {
            GameObject buttonObject = new GameObject("Sidebar Collapse Button");
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(7f, -7f);
            rect.sizeDelta = new Vector2(32f, 32f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.1f, 0.16f, 0.17f, 0.96f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => sidebarCollapsed = !sidebarCollapsed);

            Text label = CreateText(rect, "Sidebar Collapse Text", sidebarCollapsed ? ">" : "<", 18, TextAnchor.MiddleCenter);
            buttons.Add(new HudButton
            {
                Button = button,
                Label = label,
                IsEnabled = () => game != null && game.AcceptsSystemInput,
                GetText = () => sidebarCollapsed ? ">" : "<"
            });
        }

        private void AddTabButton(RectTransform parent, string objectName, string labelText, SidebarTab tab, int index)
        {
            GameObject buttonObject = new GameObject(objectName);
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(index / 3f, 0f);
            rect.anchorMax = new Vector2((index + 1f) / 3f, 1f);
            rect.offsetMin = new Vector2(index == 0 ? 0f : 3f, 0f);
            rect.offsetMax = new Vector2(index == 2 ? 0f : -3f, 0f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.09f, 0.13f, 0.14f, 0.96f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => activeSidebarTab = tab);

            Text label = CreateText(rect, objectName + " Text", labelText, 12, TextAnchor.MiddleCenter);
            buttons.Add(new HudButton
            {
                Button = button,
                Label = label,
                IsEnabled = () => game != null && game.AcceptsSystemInput,
                GetText = () => activeSidebarTab == tab ? "[" + labelText + "]" : labelText
            });
        }

        private RectTransform CreateTabContent(RectTransform parent, string objectName)
        {
            GameObject contentObject = new GameObject(objectName);
            contentObject.transform.SetParent(parent, false);
            RectTransform rect = contentObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private void AddBuildTile(RectTransform parent, StructureKind kind, int index)
        {
            StructureStats stats = RtsBalance.GetStructure(kind);
            RectTransform artRoot = AddBuildSidebarTile(
                parent,
                stats.Name,
                stats,
                kind,
                index,
                () => game.PlayerCommands.RequestConstruction(kind),
                () => CanBuild(kind),
                GetStructureAccent(kind));
            CreateStructureBuildTileArt(artRoot, kind, GetStructureAccent(kind));
        }

        private void AddUnitTile(RectTransform parent, UnitKind kind, int index)
        {
            UnitStats stats = RtsBalance.GetUnit(kind);
            RectTransform artRoot = AddSidebarTile(
                parent,
                stats.Name,
                "$" + stats.Cost.ToString("N0"),
                index,
                () => game.PlayerCommands.QueueProduction(kind),
                () => CanQueue(kind),
                () => GetUnitTileStatus(kind),
                GetUnitAccent(kind));
            CreateUnitTileArt(artRoot, kind, GetUnitAccent(kind));
            AddUnitProductionMeter(artRoot, kind, GetUnitAccent(kind));
        }

        private void AddActionTile(RectTransform parent, string name, string title, string costText, int index, UnityEngine.Events.UnityAction action, Func<bool> enabled, Func<string> status)
        {
            RectTransform artRoot = AddSidebarTile(parent, title, costText, index, action, enabled, status, new Color(0.42f, 0.72f, 0.78f, 1f));
            CreateActionTileArt(artRoot, title);
        }

        private RectTransform AddSidebarTile(RectTransform parent, string title, string costText, int index, UnityEngine.Events.UnityAction action, Func<bool> enabled, Func<string> status, Color accent)
        {
            const int columns = 3;
            const float tileWidth = 86f;
            const float tileHeight = 100f;
            const float gap = 9f;

            int column = index % columns;
            int row = index / columns;

            GameObject buttonObject = new GameObject(title + " Tile");
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(column * (tileWidth + gap), -row * (tileHeight + gap));
            rect.sizeDelta = new Vector2(tileWidth, tileHeight);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.075f, 0.092f, 0.095f, 0.98f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            RectTransform glow = CreatePanel(rect, title + " Tile Accent", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 4f), new Color(accent.r, accent.g, accent.b, 0.86f));
            glow.offsetMin = new Vector2(0f, -4f);
            glow.offsetMax = Vector2.zero;

            Text cost = CreateText(rect, title + " Tile Cost", costText, 10, TextAnchor.MiddleRight, new Vector2(0f, -9f), new Vector2(-8f, 16f));
            cost.color = new Color(0.66f, 1f, 0.72f, 0.96f);

            RectTransform artRoot = CreatePanel(rect, title + " Tile Art", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -21f), new Vector2(-12f, 58f), new Color(0.01f, 0.018f, 0.019f, 0.62f));

            Text titleLabel = CreateText(rect, title + " Tile Title", title, 9, TextAnchor.MiddleCenter, new Vector2(0f, -79f), new Vector2(-8f, 15f));
            titleLabel.color = new Color(0.9f, 0.96f, 0.96f, 1f);

            Text statusLabel = CreateText(rect, title + " Tile Status", status(), 8, TextAnchor.MiddleCenter, new Vector2(0f, -92f), new Vector2(-8f, 12f));
            statusLabel.color = new Color(0.66f, 0.84f, 0.86f, 0.96f);

            buttons.Add(new HudButton
            {
                Button = button,
                Label = statusLabel,
                IsEnabled = enabled,
                GetText = status
            });

            return artRoot;
        }

        private RectTransform AddBuildSidebarTile(RectTransform parent, string title, StructureStats stats, StructureKind kind, int index, UnityEngine.Events.UnityAction action, Func<bool> enabled, Color accent)
        {
            const int columns = 3;
            const float tileWidth = 298f / columns;
            const float tileHeight = 104f;
            const float photoHeight = 76f;

            int column = index % columns;
            int row = index / columns;
            bool hovered = false;

            GameObject buttonObject = new GameObject(title + " Build Tile");
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(column * tileWidth, -row * tileHeight);
            rect.sizeDelta = new Vector2(tileWidth, tileHeight);

            Image divider = buttonObject.AddComponent<Image>();
            divider.color = new Color(0.64f, 0.7f, 0.7f, 0.82f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = divider;
            button.onClick.AddListener(action);
            AddHoverCallbacks(buttonObject, value => hovered = value);

            RectTransform body = CreatePanel(rect, title + " Build Tile Body", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.045f, 0.055f, 0.056f, 0.98f));
            body.offsetMin = Vector2.one;
            body.offsetMax = -Vector2.one;

            RectTransform artRoot = CreatePanel(body, title + " Build Tile Photo", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, photoHeight), new Color(0.008f, 0.012f, 0.013f, 1f));

            Text titleLabel = CreateText(body, title + " Build Tile Title", title, 8, TextAnchor.MiddleCenter, new Vector2(0f, -photoHeight - 1f), new Vector2(-4f, 20f));
            titleLabel.color = new Color(0.92f, 0.95f, 0.95f, 1f);

            Text statusLabel = CreateText(body, title + " Build Tile Status", GetBuildGridTileStatus(kind, stats, false), 7, TextAnchor.MiddleCenter, new Vector2(0f, -photoHeight - 20f), new Vector2(-4f, 9f));
            statusLabel.color = new Color(0.72f, 0.82f, 0.82f, 0.98f);

            buttons.Add(new HudButton
            {
                Button = button,
                Label = statusLabel,
                IsEnabled = enabled,
                GetText = () => GetBuildGridTileStatus(kind, stats, hovered)
            });

            return artRoot;
        }

        private void AddHoverCallbacks(GameObject target, Action<bool> onHoverChanged)
        {
            if (target == null || onHoverChanged == null)
            {
                return;
            }

            EventTrigger trigger = target.AddComponent<EventTrigger>();
            EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => onHoverChanged(true));
            trigger.triggers.Add(enter);

            EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => onHoverChanged(false));
            trigger.triggers.Add(exit);
        }

        private string GetBuildTileStatus(StructureKind kind, StructureStats stats)
        {
            if (!CanAfford(stats.Cost))
            {
                return "Need credits";
            }

            string requirement = game.GetStructureRequirement(kind);
            return string.IsNullOrEmpty(requirement) ? "Ready" : requirement;
        }

        private string GetBuildGridTileStatus(StructureKind kind, StructureStats stats, bool hovered)
        {
            if (hovered)
            {
                string power = string.Empty;
                if (stats.PowerProvided > 0)
                {
                    power = "  PWR +" + stats.PowerProvided;
                }
                else if (stats.PowerUsed > 0)
                {
                    power = "  PWR -" + stats.PowerUsed;
                }

                return "$" + stats.Cost.ToString("N0") + power;
            }

            string status = GetBuildTileStatus(kind, stats);
            return status == "Ready" ? string.Empty : status;
        }

        private string GetUnitTileStatus(UnitKind kind)
        {
            string reason = "Unavailable";
            ProductionStructure producer = FindProducerForUnitHud(kind);
            if (producer != null)
            {
                int count = producer.CountProductionItems(kind);
                if (producer.IsActivelyProducing(kind))
                {
                    return "Building " + Mathf.RoundToInt(producer.ActiveProductionProgress * 100f) + "%";
                }

                if (count > 0)
                {
                    return "Queued x" + count;
                }
            }

            return game != null && game.PlayerCommands != null && game.PlayerCommands.CanQueueProduction(kind, out reason) ? "Queue" : reason;
        }

        private void BuildSidebarMinimapGrid()
        {
            if (minimapPlot == null)
            {
                return;
            }

            for (int i = 1; i < 4; i++)
            {
                float offset = SidebarMinimapSize * i / 4f;
                RectTransform vertical = CreatePanel(minimapPlot, "Sidebar Minimap Grid V" + i, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(offset, 0f), new Vector2(1f, SidebarMinimapSize), new Color(0.18f, 0.52f, 0.58f, 0.22f));
                RectTransform horizontal = CreatePanel(minimapPlot, "Sidebar Minimap Grid H" + i, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, -offset), new Vector2(SidebarMinimapSize, 1f), new Color(0.18f, 0.52f, 0.58f, 0.22f));
                vertical.SetAsFirstSibling();
                horizontal.SetAsFirstSibling();
            }
        }

        private void RefreshMinimap()
        {
            if (minimapPlot == null || sidebarCollapsed || game == null)
            {
                SetMinimapPipCount(0);
                return;
            }

            int index = 0;
            for (int i = 0; i < game.ResourceNodes.Count; i++)
            {
                ResourceNode node = game.ResourceNodes[i];
                if (node == null || node.IsDepleted)
                {
                    continue;
                }

                SetMinimapPip(index, node.transform.position, new Color(0.25f, 1f, 0.55f, 0.95f), 4f);
                index++;
            }

            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsEntity entity = game.Entities[i];
                if (entity == null || !entity.IsAlive || entity.Team == RtsTeam.Neutral)
                {
                    continue;
                }

                if (entity.Team == RtsTeam.Enemy && game.FogOfWar != null && !game.FogOfWar.IsVisible(entity.GroundPosition))
                {
                    continue;
                }

                SetMinimapPip(index, entity.GroundPosition, RtsBalance.TeamColor(entity.Team), entity is RtsStructure ? 6f : 4f);
                index++;
            }

            SetMinimapPipCount(index);
        }

        private void SetMinimapPip(int index, Vector3 worldPosition, Color color, float size)
        {
            while (minimapPips.Count <= index)
            {
                GameObject pipObject = new GameObject("Sidebar Minimap Pip");
                pipObject.transform.SetParent(minimapPlot, false);
                RectTransform rect = pipObject.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                Image image = pipObject.AddComponent<Image>();
                minimapPips.Add(image);
            }

            Image pip = minimapPips[index];
            pip.gameObject.SetActive(true);
            pip.color = color;
            RectTransform pipRect = pip.rectTransform;
            float mapSize = minimapPlot.rect.width > 1f ? minimapPlot.rect.width : SidebarMinimapSize;
            float normalizedX = Mathf.InverseLerp(-RtsBalance.MapHalfSize, RtsBalance.MapHalfSize, worldPosition.x);
            float normalizedY = Mathf.InverseLerp(-RtsBalance.MapHalfSize, RtsBalance.MapHalfSize, worldPosition.z);
            pipRect.anchoredPosition = new Vector2(normalizedX * mapSize, -((1f - normalizedY) * mapSize));
            pipRect.sizeDelta = new Vector2(size, size);
        }

        private void SetMinimapPipCount(int activeCount)
        {
            for (int i = activeCount; i < minimapPips.Count; i++)
            {
                if (minimapPips[i] != null)
                {
                    minimapPips[i].gameObject.SetActive(false);
                }
            }
        }

        private static Color GetStructureAccent(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.PowerPlant:
                    return new Color(0.95f, 0.74f, 0.24f, 1f);
                case StructureKind.Refinery:
                    return new Color(0.35f, 0.94f, 0.48f, 1f);
                case StructureKind.WarFactory:
                    return new Color(0.54f, 0.78f, 0.94f, 1f);
                case StructureKind.Turret:
                case StructureKind.GunTower:
                case StructureKind.AdvancedGunTower:
                    return new Color(0.95f, 0.28f, 0.2f, 1f);
                case StructureKind.DualHelipad:
                    return new Color(0.95f, 0.78f, 0.26f, 1f);
                default:
                    return new Color(0.44f, 0.82f, 0.92f, 1f);
            }
        }

        private static Color GetUnitAccent(UnitKind kind)
        {
            if (RtsBalance.IsInfantry(kind))
            {
                return new Color(0.34f, 0.75f, 0.82f, 1f);
            }

            if (kind == UnitKind.Harvester)
            {
                return new Color(0.42f, 0.9f, 0.45f, 1f);
            }

            if (RtsBalance.IsWheeledCombatVehicle(kind))
            {
                return new Color(0.86f, 0.62f, 0.32f, 1f);
            }

            if (RtsBalance.IsAircraft(kind))
            {
                return new Color(0.95f, 0.8f, 0.28f, 1f);
            }

            return new Color(0.78f, 0.88f, 0.92f, 1f);
        }

        private void CreateStructureTileArt(RectTransform root, StructureKind kind, Color accent)
        {
            if (TryCreateStructureTileCardArt(root, kind, accent))
            {
                return;
            }

            CreatePanel(root, "Structure Icon Base", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(48f, 8f), new Color(0.08f, 0.1f, 0.085f, 1f));
            CreatePanel(root, "Structure Icon Body", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(38f, 22f), new Color(0.22f, 0.28f, 0.21f, 1f));
            CreatePanel(root, "Structure Icon Highlight", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 31f), new Vector2(42f, 4f), accent);

            if (kind == StructureKind.PowerPlant || kind == StructureKind.Refinery)
            {
                CreatePanel(root, "Structure Icon Stack A", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-12f, 21f), new Vector2(7f, 24f), new Color(0.16f, 0.2f, 0.18f, 1f));
                CreatePanel(root, "Structure Icon Stack B", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(12f, 21f), new Vector2(7f, 24f), new Color(0.16f, 0.2f, 0.18f, 1f));
            }
            else if (kind == StructureKind.Turret || kind == StructureKind.GunTower || kind == StructureKind.AdvancedGunTower)
            {
                CreatePanel(root, "Structure Icon Tower", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(18f, 26f), new Color(0.18f, 0.23f, 0.19f, 1f));
                CreatePanel(root, "Structure Icon Barrel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0.5f), new Vector2(9f, 31f), new Vector2(28f, 5f), accent);
            }
            else
            {
                CreatePanel(root, "Structure Icon Door", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(13f, 16f), new Color(0.03f, 0.045f, 0.043f, 1f));
            }
        }

        private void CreateStructureBuildTileArt(RectTransform root, StructureKind kind, Color accent)
        {
            Sprite sprite = GetStructureTileArtSprite(kind);
            if (sprite == null)
            {
                CreateStructureTileArt(root, kind, accent);
                return;
            }

            Image rootImage = root.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.color = new Color(0.008f, 0.012f, 0.013f, 1f);
            }

            GameObject imageObject = new GameObject("Structure Build Photo");
            imageObject.transform.SetParent(root, false);
            RectTransform imageRect = imageObject.AddComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            Image image = imageObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = false;
            image.color = Color.white;

            RectTransform wash = CreatePanel(root, "Structure Build Photo Wash", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(accent.r, accent.g, accent.b, 0.06f));
            wash.SetAsLastSibling();
        }

        private bool TryCreateStructureTileCardArt(RectTransform root, StructureKind kind, Color accent)
        {
            Sprite sprite = GetStructureTileArtSprite(kind);
            if (sprite == null)
            {
                return false;
            }

            Image rootImage = root.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.color = new Color(0.01f, 0.014f, 0.01f, 0.94f);
            }

            RectTransform glow = CreatePanel(root, "Structure Card Glow", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(accent.r, accent.g, accent.b, 0.16f));
            glow.offsetMin = new Vector2(1f, 1f);
            glow.offsetMax = new Vector2(-1f, -1f);

            GameObject imageObject = new GameObject("Structure Card Art");
            imageObject.transform.SetParent(root, false);
            RectTransform imageRect = imageObject.AddComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = new Vector2(2f, 2f);
            imageRect.offsetMax = new Vector2(-2f, -2f);
            Image image = imageObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = Color.white;

            RectTransform trim = CreatePanel(root, "Structure Card Trim", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 3f), new Color(accent.r, accent.g, accent.b, 0.78f));
            trim.offsetMin = new Vector2(2f, -3f);
            trim.offsetMax = new Vector2(-2f, 0f);
            return true;
        }

        private void CreateUnitTileArt(RectTransform root, UnitKind kind, Color accent)
        {
            if (TryCreateUnitTileCardArt(root, kind, accent))
            {
                return;
            }

            if (RtsBalance.IsInfantry(kind))
            {
                CreatePanel(root, "Infantry Icon Body", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(18f, 24f), new Color(0.2f, 0.26f, 0.2f, 1f));
                CreatePanel(root, "Infantry Icon Head", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(15f, 10f), new Color(0.24f, 0.2f, 0.16f, 1f));
                CreatePanel(root, "Infantry Icon Weapon", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0.5f), new Vector2(9f, 25f), new Vector2(28f, 5f), accent);
                return;
            }

            CreatePanel(root, "Vehicle Icon Hull", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(48f, 18f), new Color(0.2f, 0.25f, 0.21f, 1f));
            CreatePanel(root, "Vehicle Icon Cabin", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-4f, 29f), new Vector2(25f, 13f), new Color(0.16f, 0.2f, 0.18f, 1f));
            CreatePanel(root, "Vehicle Icon Accent", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 31f), new Vector2(44f, 4f), accent);

            if (kind != UnitKind.Harvester)
            {
                CreatePanel(root, "Vehicle Icon Gun", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0.5f), new Vector2(7f, 39f), new Vector2(29f, 4f), new Color(0.08f, 0.1f, 0.1f, 1f));
            }
        }

        private bool TryCreateUnitTileCardArt(RectTransform root, UnitKind kind, Color accent)
        {
            Sprite sprite = GetUnitTileArtSprite(kind);
            if (sprite == null)
            {
                return false;
            }

            Image rootImage = root.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.color = new Color(0.01f, 0.014f, 0.01f, 0.94f);
            }

            RectTransform glow = CreatePanel(root, "Unit Card Glow", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(accent.r, accent.g, accent.b, 0.18f));
            glow.offsetMin = new Vector2(1f, 1f);
            glow.offsetMax = new Vector2(-1f, -1f);

            GameObject imageObject = new GameObject("Unit Card Art");
            imageObject.transform.SetParent(root, false);
            RectTransform imageRect = imageObject.AddComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = new Vector2(2f, 2f);
            imageRect.offsetMax = new Vector2(-2f, -2f);
            Image image = imageObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = Color.white;

            RectTransform trim = CreatePanel(root, "Unit Card Trim", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 3f), new Color(accent.r, accent.g, accent.b, 0.78f));
            trim.offsetMin = new Vector2(2f, -3f);
            trim.offsetMax = new Vector2(-2f, 0f);
            return true;
        }

        private void AddUnitProductionMeter(RectTransform root, UnitKind kind, Color accent)
        {
            GameObject fillObject = new GameObject("Production Clock Fill");
            fillObject.transform.SetParent(root, false);
            RectTransform fillRect = fillObject.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(1f, 1f);
            fillRect.offsetMax = new Vector2(-1f, -1f);
            Image fill = fillObject.AddComponent<Image>();
            fill.sprite = GetHudFillSprite();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = true;
            fill.fillAmount = 0f;
            fill.color = new Color(accent.r, accent.g, accent.b, 0.54f);
            fill.gameObject.SetActive(false);

            RectTransform badge = CreatePanel(root, "Production Queue Badge", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-2f, -2f), new Vector2(24f, 16f), new Color(0.005f, 0.01f, 0.012f, 0.92f));
            Text badgeText = CreateText(badge, "Production Queue Badge Text", "", 9, TextAnchor.MiddleCenter);
            badgeText.color = new Color(0.76f, 1f, 0.86f, 1f);
            badge.gameObject.SetActive(false);

            productionTileMeters.Add(new ProductionTileMeter
            {
                Kind = kind,
                ClockFill = fill,
                QueueBadge = badgeText
            });
        }

        private Sprite GetUnitTileArtSprite(UnitKind kind)
        {
            kind = RtsBalance.NormalizeUnitKind(kind);
            if (unitTileArtSprites.TryGetValue(kind, out Sprite existing))
            {
                return existing;
            }

            string resourceName = GetUnitTileArtResourceName(kind);
            if (string.IsNullOrEmpty(resourceName))
            {
                unitTileArtSprites[kind] = null;
                return null;
            }

            Texture2D texture = Resources.Load<Texture2D>(UnitTileArtResourceRoot + resourceName);
            if (texture == null)
            {
                unitTileArtSprites[kind] = null;
                return null;
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = resourceName + " HUD Sprite";
            unitTileArtSprites[kind] = sprite;
            return sprite;
        }

        private Sprite GetStructureTileArtSprite(StructureKind kind)
        {
            if (structureTileArtSprites.TryGetValue(kind, out Sprite existing))
            {
                return existing;
            }

            string resourceName = GetStructureTileArtResourceName(kind);
            if (string.IsNullOrEmpty(resourceName))
            {
                structureTileArtSprites[kind] = null;
                return null;
            }

            Texture2D texture = Resources.Load<Texture2D>(StructureTileArtResourceRoot + resourceName);
            if (texture == null)
            {
                structureTileArtSprites[kind] = null;
                return null;
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = resourceName + " HUD Sprite";
            structureTileArtSprites[kind] = sprite;
            return sprite;
        }

        private Sprite GetHudFillSprite()
        {
            if (hudFillSprite == null)
            {
                hudFillSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f), 100f);
                hudFillSprite.name = "HUD Radial Fill Sprite";
            }

            return hudFillSprite;
        }

        private static string GetUnitTileArtResourceName(UnitKind kind)
        {
            switch (RtsBalance.NormalizeUnitKind(kind))
            {
                case UnitKind.Rifleman:
                    return "Rifleman";
                case UnitKind.Grenadier:
                    return "Grenadier";
                case UnitKind.RocketSoldier:
                    return "RocketSoldier";
                case UnitKind.FlameTrooper:
                    return "FlameTrooper";
                case UnitKind.Engineer:
                    return "Engineer";
                case UnitKind.Harvester:
                    return "Harvester";
                case UnitKind.LightTank:
                    return "LightTank";
                case UnitKind.MediumTank:
                    return "MediumTank";
                case UnitKind.HeavyTank:
                    return "HeavyTank";
                case UnitKind.Skyraider:
                    return "Skyraider";
                case UnitKind.OrcaLifter:
                    return "OrcaLifter";
                default:
                    return string.Empty;
            }
        }

        private static string GetStructureTileArtResourceName(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.CommandCenter:
                    return "CommandCenter";
                case StructureKind.Refinery:
                    return "Refinery";
                case StructureKind.Barracks:
                    return "Barracks";
                case StructureKind.WarFactory:
                    return "WarFactory";
                case StructureKind.PowerPlant:
                    return "PowerPlant";
                case StructureKind.Turret:
                    return "Turret";
                case StructureKind.GunTower:
                    return "GunTower";
                case StructureKind.AdvancedGunTower:
                    return "AdvancedGunTower";
                case StructureKind.DualHelipad:
                    return "DualHelipad";
                default:
                    return string.Empty;
            }
        }

        private void CreateActionTileArt(RectTransform root, string title)
        {
            CreatePanel(root, "Action Icon Back", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 24f), new Color(0.1f, 0.16f, 0.17f, 1f));
            Text text = CreateText(root, "Action Icon Text", title.Length > 4 ? title.Substring(0, 4) : title, 12, TextAnchor.MiddleCenter);
            text.color = new Color(0.78f, 0.96f, 1f, 1f);
        }

        private void AddBuildButton(RectTransform parent, StructureKind kind, float y)
        {
            StructureStats stats = RtsBalance.GetStructure(kind);
            AddCommandButton(parent, stats.Name, y, () => game.PlayerCommands.RequestConstruction(kind), () => CanBuild(kind), () => GetBuildButtonText(kind, stats));
        }

        private RectTransform BuildMenuPanel(Transform parent, string name, string title, string subtitle, MenuButtonSpec[] specs)
        {
            float panelHeight = Mathf.Clamp(170f + specs.Length * 54f, 420f, 720f);
            float panelWidth = specs.Length > 6 ? 580f : 520f;
            RectTransform overlay = CreatePanel(
                parent,
                name,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.005f, 0.008f, 0.01f, 0.72f));

            RectTransform panel = CreatePanel(
                overlay,
                name + " Panel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(panelWidth, panelHeight),
                new Color(0.02f, 0.028f, 0.032f, 0.96f));

            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(0.025f, 0.034f, 0.038f, 0.96f);
            }

            CreateText(panel, name + " Title", title, 34, TextAnchor.MiddleCenter, new Vector2(0f, -34f), new Vector2(-48f, 54f));
            Text subtitleText = CreateText(panel, name + " Subtitle", subtitle, 17, TextAnchor.MiddleCenter, new Vector2(0f, -86f), new Vector2(-64f, 28f));
            subtitleText.color = new Color(0.58f, 0.86f, 0.92f);

            float y = -142f;
            for (int i = 0; i < specs.Length; i++)
            {
                AddMenuButton(panel, specs[i], y);
                y -= 54f;
            }

            overlay.gameObject.SetActive(false);
            return overlay;
        }

        private void AddCommandButton(RectTransform parent, string name, float y, UnityEngine.Events.UnityAction action, Func<bool> enabled, Func<string> text)
        {
            GameObject buttonObject = new GameObject(name + " Button");
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(-28f, 38f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.12f, 0.145f, 0.145f, 0.95f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            Text label = CreateText(rect, name + " Text", text(), 15, TextAnchor.MiddleCenter);
            label.rectTransform.offsetMin = new Vector2(8f, 0f);
            label.rectTransform.offsetMax = new Vector2(-8f, 0f);

            buttons.Add(new HudButton
            {
                Button = button,
                Label = label,
                IsEnabled = enabled,
                GetText = text
            });
        }

        private void AddMenuButton(RectTransform parent, MenuButtonSpec spec, float y)
        {
            GameObject buttonObject = new GameObject(spec.Name + " Button");
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(420f, 42f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.13f, 0.17f, 0.18f, 0.98f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(spec.Action);

            Text label = CreateText(rect, spec.Name + " Text", spec.GetText(), 16, TextAnchor.MiddleCenter);
            label.rectTransform.offsetMin = new Vector2(10f, 0f);
            label.rectTransform.offsetMax = new Vector2(-10f, 0f);

            buttons.Add(new HudButton
            {
                Button = button,
                Label = label,
                IsEnabled = spec.IsEnabled,
                GetText = spec.GetText
            });
        }

        private bool CanAfford(int cost)
        {
            return game != null && game.Resources != null && game.Resources.CanAfford(cost);
        }

        private bool CanBuild(StructureKind kind)
        {
            string reason;
            return game != null && game.PlayerCommands != null && game.PlayerCommands.CanRequestConstruction(kind, out reason);
        }

        private bool CanQueue(UnitKind kind)
        {
            string reason;
            return game != null && game.PlayerCommands != null && game.PlayerCommands.CanQueueProduction(kind, out reason);
        }

        private string GetBuildButtonText(StructureKind kind, StructureStats stats)
        {
            string requirement = game.GetStructureRequirement(kind);
            if (!string.IsNullOrEmpty(requirement))
            {
                return stats.Name + "  " + requirement;
            }

            return stats.Name + "  " + stats.Cost;
        }

        private string BuildSelectionText()
        {
            if (game.Selection.Count == 0)
            {
                return "No selection";
            }

            if (game.Selection.Count > 1)
            {
                int units = 0;
                int structures = 0;
                for (int i = 0; i < game.Selection.Count; i++)
                {
                    if (game.Selection[i] is RtsUnit)
                    {
                        units++;
                    }
                    else if (game.Selection[i] is RtsStructure)
                    {
                        structures++;
                    }
                }

                return game.Selection.Count + " selected    Units " + units + "    Structures " + structures;
            }

            RtsEntity entity = game.Selection[0];
            if (entity == null)
            {
                return "No selection";
            }

            string detail = entity.DisplayName + "    HP " + Mathf.RoundToInt(entity.Health) + "/" + Mathf.RoundToInt(entity.MaxHealth);

            HarvesterUnit harvester = entity as HarvesterUnit;
            if (harvester != null)
            {
                detail += "    Cargo " + harvester.Cargo + "/" + harvester.CargoCapacity;
            }

            ProductionStructure producer = entity as ProductionStructure;
            if (producer != null)
            {
                detail += "    " + producer.GetQueueSummary();
                if (producer.HasRallyPoint)
                {
                    detail += "    Rally set";
                }
            }

            return detail;
        }

        private void RefreshProductionProgress()
        {
            if (productionProgressFill == null || productionProgressText == null)
            {
                return;
            }

            ProductionStructure producer = FindProductionForHud();
            float progress = 0f;
            string label = "Production idle";
            if (producer == null)
            {
                label = "Select producer";
            }
            else if (producer.HasActiveProduction)
            {
                progress = Mathf.Clamp01(producer.ActiveProductionProgress);
                UnitStats stats = RtsBalance.GetUnit(producer.ActiveProductionKind);
                label = "Building " + stats.Name + " " + Mathf.RoundToInt(progress * 100f) + "%";
                string queuePreview = BuildProductionQueuePreview(producer);
                if (!string.IsNullOrEmpty(queuePreview))
                {
                    label += "    Next: " + queuePreview;
                }
            }
            else if (producer.PendingQueueCount > 0)
            {
                label = "Queued: " + BuildProductionQueuePreview(producer);
            }

            productionProgressFill.anchorMax = new Vector2(progress, 1f);
            productionProgressFill.gameObject.SetActive(progress > 0.001f);
            productionProgressText.text = label;
        }

        private void RefreshProductionTileMeters()
        {
            for (int i = 0; i < productionTileMeters.Count; i++)
            {
                ProductionTileMeter meter = productionTileMeters[i];
                if (meter == null)
                {
                    continue;
                }

                ProductionStructure producer = FindProducerForUnitHud(meter.Kind);
                int count = producer != null ? producer.CountProductionItems(meter.Kind) : 0;
                float progress = producer != null && producer.IsActivelyProducing(meter.Kind) ? Mathf.Clamp01(producer.ActiveProductionProgress) : 0f;

                if (meter.ClockFill != null)
                {
                    meter.ClockFill.fillAmount = progress;
                    meter.ClockFill.gameObject.SetActive(progress > 0.001f);
                }

                if (meter.QueueBadge != null)
                {
                    meter.QueueBadge.text = count > 0 ? "x" + count : string.Empty;
                    meter.QueueBadge.transform.parent.gameObject.SetActive(count > 0);
                }
            }
        }

        private string BuildProductionQueuePreview(ProductionStructure producer)
        {
            if (producer == null || producer.PendingQueueCount <= 0)
            {
                return string.Empty;
            }

            string preview = string.Empty;
            int displayCount = Mathf.Min(3, producer.PendingQueueCount);
            for (int i = 0; i < displayCount; i++)
            {
                UnitKind queuedKind;
                if (!producer.TryGetQueuedUnit(i, out queuedKind))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(preview))
                {
                    preview += ", ";
                }

                preview += RtsBalance.GetUnit(queuedKind).Name;
            }

            int remaining = producer.PendingQueueCount - displayCount;
            if (remaining > 0)
            {
                preview += " +" + remaining;
            }

            return preview;
        }

        private ProductionStructure FindProductionForHud()
        {
            if (game == null)
            {
                return null;
            }

            ProductionStructure selected = game.PlayerCommands != null ? game.PlayerCommands.FindSelectedProductionStructure() : null;
            if (selected != null)
            {
                return selected;
            }

            ProductionStructure fallback = null;
            for (int i = 0; i < game.Entities.Count; i++)
            {
                ProductionStructure producer = game.Entities[i] as ProductionStructure;
                if (producer == null || producer.Team != RtsTeam.Player || !producer.IsAlive)
                {
                    continue;
                }

                if (producer.QueueCount > 0)
                {
                    return producer;
                }

                if (fallback == null)
                {
                    fallback = producer;
                }
            }

            return fallback;
        }

        private ProductionStructure FindProducerForUnitHud(UnitKind kind)
        {
            if (game == null)
            {
                return null;
            }

            ProductionStructure selected = game.PlayerCommands != null ? game.PlayerCommands.FindSelectedProductionStructure() : null;
            if (selected != null && selected.CanTrain(kind))
            {
                return selected;
            }

            ProductionStructure fallback = null;
            for (int i = 0; i < game.Entities.Count; i++)
            {
                ProductionStructure producer = game.Entities[i] as ProductionStructure;
                if (producer == null || producer.Team != RtsTeam.Player || !producer.IsAlive || !producer.CanTrain(kind))
                {
                    continue;
                }

                if (producer.CountProductionItems(kind) > 0)
                {
                    return producer;
                }

                if (fallback == null)
                {
                    fallback = producer;
                }
            }

            return fallback;
        }

        private void DrawMinimap()
        {
            float size = Mathf.Clamp(Screen.height * 0.22f, 138f, 190f);
            Rect rect = new Rect(14f, Screen.height - size - 142f, size, size);
            Color previousColor = GUI.color;

            GUI.color = new Color(0.01f, 0.012f, 0.014f, 0.82f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.22f, 0.28f, 0.26f, 0.95f);
            DrawRectOutline(rect, 2f);

            for (int i = 0; i < game.ResourceNodes.Count; i++)
            {
                ResourceNode node = game.ResourceNodes[i];
                if (node == null || node.IsDepleted)
                {
                    continue;
                }

                DrawMapPip(rect, node.transform.position, new Color(0.25f, 1f, 0.55f, 0.95f), 4f);
            }

            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsEntity entity = game.Entities[i];
                if (entity == null || !entity.IsAlive || entity.Team == RtsTeam.Neutral)
                {
                    continue;
                }

                if (entity.Team == RtsTeam.Enemy && game.FogOfWar != null && !game.FogOfWar.IsVisible(entity.GroundPosition))
                {
                    continue;
                }

                float pipSize = entity is RtsStructure ? 6f : 4f;
                DrawMapPip(rect, entity.GroundPosition, RtsBalance.TeamColor(entity.Team), pipSize);
            }

            GUI.color = previousColor;
        }

        private void DrawMapPip(Rect mapRect, Vector3 worldPosition, Color color, float size)
        {
            float normalizedX = Mathf.InverseLerp(-RtsBalance.MapHalfSize, RtsBalance.MapHalfSize, worldPosition.x);
            float normalizedY = Mathf.InverseLerp(-RtsBalance.MapHalfSize, RtsBalance.MapHalfSize, worldPosition.z);
            float x = Mathf.Lerp(mapRect.xMin, mapRect.xMax, normalizedX) - size * 0.5f;
            float y = Mathf.Lerp(mapRect.yMax, mapRect.yMin, normalizedY) - size * 0.5f;

            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y, size, size), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void DrawMatchBanner()
        {
            if (bannerStyle == null)
            {
                bannerStyle = new GUIStyle(GUI.skin.label);
                bannerStyle.alignment = TextAnchor.MiddleCenter;
                bannerStyle.fontSize = 44;
                bannerStyle.fontStyle = FontStyle.Bold;
                bannerStyle.normal.textColor = Color.white;

                bannerSubStyle = new GUIStyle(GUI.skin.label);
                bannerSubStyle.alignment = TextAnchor.MiddleCenter;
                bannerSubStyle.fontSize = 20;
                bannerSubStyle.normal.textColor = new Color(0.86f, 0.94f, 0.96f);
            }

            Rect background = new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.5f - 86f, 520f, 150f);
            Color previousColor = GUI.color;
            GUI.color = new Color(0.01f, 0.015f, 0.018f, 0.88f);
            GUI.DrawTexture(background, Texture2D.whiteTexture);
            GUI.color = game.MatchState == RtsMatchState.Victory ? new Color(0.25f, 0.95f, 1f, 1f) : new Color(1f, 0.25f, 0.2f, 1f);
            DrawRectOutline(background, 3f);
            GUI.color = previousColor;

            GUI.Label(new Rect(background.x, background.y + 22f, background.width, 58f), game.MatchState == RtsMatchState.Victory ? "VICTORY" : "DEFEAT", bannerStyle);
            GUI.Label(new Rect(background.x + 24f, background.y + 86f, background.width - 48f, 44f), game.StatusMessage, bannerSubStyle);
        }

        private RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            GameObject panelObject = new GameObject(name);
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            Image image = panelObject.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private Text CreateText(RectTransform parent, string name, string value, int size, TextAnchor anchor)
        {
            return CreateText(parent, name, value, size, anchor, Vector2.zero, Vector2.zero);
        }

        private Text CreateText(RectTransform parent, string name, string value, int size, TextAnchor anchor, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            if (sizeDelta != Vector2.zero)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = sizeDelta;
            }

            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = new Color(0.9f, 0.94f, 0.94f);
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void EnsureEventSystem(Transform owner)
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            if (owner != null)
            {
                eventSystemObject.transform.SetParent(owner, false);
            }

            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static string FormatTime(float seconds)
        {
            int wholeSeconds = Mathf.FloorToInt(seconds);
            int minutes = wholeSeconds / 60;
            int remainder = wholeSeconds % 60;
            return minutes + ":" + remainder.ToString("00");
        }

        private static void DrawRectOutline(Rect rect, float thickness)
        {
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), Texture2D.whiteTexture);
        }
    }
}
