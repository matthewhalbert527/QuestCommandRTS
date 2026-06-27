using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QuestCommandRTS
{
    public sealed class RtsHud : MonoBehaviour
    {
        private enum CommandCategory
        {
            Buildings,
            Vehicles,
            Infantry
        }

        private sealed class HudButton
        {
            public Button Button;
            public Text Label;
            public GameObject CountRoot;
            public Text CountLabel;
            public GameObject ProgressRoot;
            public Image ProgressImage;
            public Text StatusLabel;
            public Func<bool> IsEnabled;
            public Func<string> GetText;
            public Func<int> GetCount;
            public Func<bool> HasProgress;
            public Func<float> GetProgress;
            public Func<string> GetStatus;
        }

        private sealed class CategoryTab
        {
            public CommandCategory Category;
            public Button Button;
            public Image Image;
            public Text Label;
        }

        private sealed class TooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
        {
            public RtsHud Owner;
            public Func<string> GetTooltip;

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (Owner != null && GetTooltip != null)
                {
                    Owner.ShowTooltip(GetTooltip());
                }
            }

            public void OnPointerMove(PointerEventData eventData)
            {
                if (Owner != null)
                {
                    Owner.UpdateTooltipPosition();
                }
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (Owner != null)
                {
                    Owner.HideTooltip();
                }
            }
        }

        private const float CardSize = 72f;
        private const float CardGap = 7f;
        private const float CardLeft = 14f;
        private const float CommandContentHeight = 590f;
        private const float CommandTopInset = 252f;

        private readonly List<HudButton> buttons = new List<HudButton>();
        private readonly List<CategoryTab> categoryTabs = new List<CategoryTab>();
        private readonly List<RectTransform> minimapBlips = new List<RectTransform>();
        private readonly Dictionary<string, Sprite> cardSprites = new Dictionary<string, Sprite>();
        private RtsGame game;
        private Text resourcesText;
        private Text selectionText;
        private RectTransform minimapRect;
        private RectTransform buildingsGroup;
        private RectTransform vehiclesGroup;
        private RectTransform infantryGroup;
        private ScrollRect commandScroll;
        private RectTransform tooltipRect;
        private Text tooltipText;
        private GameObject menuOverlay;
        private Text menuTitleText;
        private Text menuPrimaryLabel;
        private Button menuPrimaryButton;
        private Font font;
        private CommandCategory activeCategory = CommandCategory.Buildings;
        private float nextMinimapUpdate;
        private bool gameStarted;
        private bool pauseMenuOpen;

        public static bool BlocksGameInput { get; private set; }

        public void Initialize(RtsGame owner)
        {
            game = owner;
            font = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");
            EnsureEventSystem();
            BuildCanvas();
            ShowLandingMenu();
        }

        private void Update()
        {
            if (gameStarted && Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePauseMenu();
            }

            if (game == null || game.Resources == null)
            {
                return;
            }

            string powerColor = game.Resources.HasLowPower ? "LOW" : "OK";
            resourcesText.text = "Credits " + game.Resources.Credits + "    Power " + game.Resources.PowerUsed + "/" + game.Resources.PowerProvided + " " + powerColor;
            selectionText.text = BuildSelectionText();

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

                if (hudButton.CountRoot != null && hudButton.CountLabel != null && hudButton.GetCount != null)
                {
                    int count = hudButton.GetCount();
                    hudButton.CountRoot.SetActive(count > 0);
                    hudButton.CountLabel.text = count.ToString();
                }

                if (hudButton.ProgressRoot != null && hudButton.ProgressImage != null && hudButton.HasProgress != null && hudButton.GetProgress != null)
                {
                    bool hasProgress = hudButton.HasProgress();
                    hudButton.ProgressRoot.SetActive(hasProgress);
                    if (hasProgress)
                    {
                        hudButton.ProgressImage.fillAmount = 1f - Mathf.Clamp01(hudButton.GetProgress());
                    }
                }

                if (hudButton.StatusLabel != null && hudButton.GetStatus != null)
                {
                    string status = hudButton.GetStatus();
                    bool hasStatus = !string.IsNullOrEmpty(status);
                    hudButton.StatusLabel.gameObject.SetActive(hasStatus);
                    if (hasStatus)
                    {
                        hudButton.StatusLabel.text = status;
                    }
                }
            }

            UpdateMinimap();
            UpdateTooltipPosition();
        }

        private void OnDestroy()
        {
            if (BlocksGameInput)
            {
                BlocksGameInput = false;
                Time.timeScale = 1f;
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

            RectTransform topBar = CreatePanel(canvasObject.transform, "Resources", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, -10f), new Vector2(-280f, 42f), new Color(0.02f, 0.025f, 0.025f, 0.78f));
            resourcesText = CreateText(topBar, "Resources Text", "", 19, TextAnchor.MiddleLeft);
            resourcesText.rectTransform.offsetMin = new Vector2(18f, 0f);
            resourcesText.rectTransform.offsetMax = new Vector2(-18f, 0f);

            RectTransform commandPanel = CreatePanel(canvasObject.transform, "Commands", new Vector2(1f, 0.04f), new Vector2(1f, 0.96f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(238f, 0f), new Color(0.025f, 0.03f, 0.032f, 0.82f));
            CreateMinimap(commandPanel);
            CreateCategoryTabs(commandPanel);
            RectTransform commandContent = CreateCommandScrollContent(commandPanel);
            buildingsGroup = CreateCategoryGroup(commandContent, "Buildings Page");
            vehiclesGroup = CreateCategoryGroup(commandContent, "Vehicles Page");
            infantryGroup = CreateCategoryGroup(commandContent, "Infantry Page");

            CreateText(buildingsGroup, "Buildings Label", "Buildings", 17, TextAnchor.MiddleLeft, new Vector2(14f, -26f), new Vector2(198f, 26f));
            float buildY = -58f;
            AddBuildCard(buildingsGroup, StructureKind.PowerPlant, 0, buildY, "PowerPlant");
            AddBuildCard(buildingsGroup, StructureKind.Barracks, 1, buildY, "Barracks");
            AddBuildCard(buildingsGroup, StructureKind.Refinery, 2, buildY, "Refinery");
            AddBuildCard(buildingsGroup, StructureKind.WarFactory, 3, buildY, "WarFactory");
            AddBuildCard(buildingsGroup, StructureKind.Turret, 4, buildY, "Turret");
            AddBuildCard(buildingsGroup, StructureKind.DualHelipad, 5, buildY, "DualHelipad");

            CreateText(vehiclesGroup, "Vehicles Label", "Vehicles", 17, TextAnchor.MiddleLeft, new Vector2(14f, -26f), new Vector2(198f, 26f));
            float vehicleY = -58f;
            AddUnitCard(vehiclesGroup, UnitKind.Harvester, 0, vehicleY, "Harvester");
            AddUnitCard(vehiclesGroup, UnitKind.Tank, 1, vehicleY, "Tank");
            AddUnitCard(vehiclesGroup, UnitKind.Skyraider, 2, vehicleY, "Skyraider");
            AddUnitCard(vehiclesGroup, UnitKind.OrcaLifter, 3, vehicleY, "OrcaLifter");
            AddCommandButton(vehiclesGroup, "Vehicle Army", vehicleY - 2f * (CardSize + CardGap) - 18f, () => game.SelectCombatUnits(), () => true, () => "Select Army");

            CreateText(infantryGroup, "Infantry Label", "Infantry", 17, TextAnchor.MiddleLeft, new Vector2(14f, -26f), new Vector2(198f, 26f));
            float infantryY = -58f;
            AddUnitCard(infantryGroup, UnitKind.Rifleman, 0, infantryY, "Rifleman");
            AddUnitCard(infantryGroup, UnitKind.RocketSoldier, 1, infantryY, "RocketSoldier");
            AddUnitCard(infantryGroup, UnitKind.Grenadier, 2, infantryY, "Grenadier");
            AddUnitCard(infantryGroup, UnitKind.FlameTrooper, 3, infantryY, "FlameTrooper");
            AddUnitCard(infantryGroup, UnitKind.Engineer, 4, infantryY, "Engineer");
            AddCommandButton(infantryGroup, "Infantry Army", infantryY - 3f * (CardSize + CardGap) - 18f, () => game.SelectCombatUnits(), () => true, () => "Select Army");
            SelectCategory(CommandCategory.Buildings);

            RectTransform selectionPanel = CreatePanel(canvasObject.transform, "Selection", new Vector2(0f, 0f), new Vector2(0.58f, 0f), new Vector2(0f, 0f), new Vector2(12f, 12f), new Vector2(0f, 118f), new Color(0.02f, 0.024f, 0.026f, 0.8f));
            selectionText = CreateText(selectionPanel, "Selection Text", "", 17, TextAnchor.UpperLeft);
            selectionText.rectTransform.offsetMin = new Vector2(16f, 10f);
            selectionText.rectTransform.offsetMax = new Vector2(-16f, -10f);

            CreateTooltip(canvasObject.transform);
            CreateMenuOverlay(canvasObject.transform);
        }

        private RectTransform CreateCommandScrollContent(RectTransform commandPanel)
        {
            GameObject viewportObject = new GameObject("Command Viewport");
            viewportObject.transform.SetParent(commandPanel, false);
            RectTransform viewport = viewportObject.AddComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(0f, 8f);
            viewport.offsetMax = new Vector2(0f, -CommandTopInset);

            Image viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            Mask mask = viewportObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject contentObject = new GameObject("Command Content");
            contentObject.transform.SetParent(viewport, false);
            RectTransform content = contentObject.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, CommandContentHeight);

            commandScroll = commandPanel.gameObject.AddComponent<ScrollRect>();
            commandScroll.horizontal = false;
            commandScroll.vertical = true;
            commandScroll.movementType = ScrollRect.MovementType.Clamped;
            commandScroll.scrollSensitivity = 28f;
            commandScroll.viewport = viewport;
            commandScroll.content = content;
            commandScroll.verticalNormalizedPosition = 1f;

            return content;
        }

        private void CreateMinimap(RectTransform commandPanel)
        {
            minimapRect = CreatePanel(
                commandPanel,
                "Radar Map",
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -12f),
                new Vector2(-24f, 174f),
                new Color(0.006f, 0.012f, 0.014f, 0.94f));

            Image image = minimapRect.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
            }

            CreateMinimapLine("Radar Vertical Axis", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-1f, 8f), new Vector2(1f, -8f), new Color(0.12f, 0.28f, 0.28f, 0.55f));
            CreateMinimapLine("Radar Horizontal Axis", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(8f, -1f), new Vector2(-8f, 1f), new Color(0.12f, 0.28f, 0.28f, 0.55f));
            CreateMinimapLine("Radar North Grid", new Vector2(0f, 0.75f), new Vector2(1f, 0.75f), new Vector2(8f, -1f), new Vector2(-8f, 1f), new Color(0.08f, 0.18f, 0.18f, 0.36f));
            CreateMinimapLine("Radar South Grid", new Vector2(0f, 0.25f), new Vector2(1f, 0.25f), new Vector2(8f, -1f), new Vector2(-8f, 1f), new Color(0.08f, 0.18f, 0.18f, 0.36f));
            CreateMinimapLine("Radar West Grid", new Vector2(0.25f, 0f), new Vector2(0.25f, 1f), new Vector2(-1f, 8f), new Vector2(1f, -8f), new Color(0.08f, 0.18f, 0.18f, 0.36f));
            CreateMinimapLine("Radar East Grid", new Vector2(0.75f, 0f), new Vector2(0.75f, 1f), new Vector2(-1f, 8f), new Vector2(1f, -8f), new Color(0.08f, 0.18f, 0.18f, 0.36f));
        }

        private void CreateMinimapLine(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            GameObject lineObject = new GameObject(name);
            lineObject.transform.SetParent(minimapRect, false);

            RectTransform rect = lineObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Image image = lineObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private void CreateCategoryTabs(RectTransform commandPanel)
        {
            RectTransform tabBar = CreatePanel(
                commandPanel,
                "Category Tabs",
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -198f),
                new Vector2(-24f, 34f),
                new Color(0f, 0f, 0f, 0f));

            Image image = tabBar.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
            }

            CreateCategoryTab(tabBar, CommandCategory.Buildings, "Buildings", 0);
            CreateCategoryTab(tabBar, CommandCategory.Vehicles, "Vehicles", 1);
            CreateCategoryTab(tabBar, CommandCategory.Infantry, "Infantry", 2);
            RefreshCategoryTabs();
        }

        private void CreateCategoryTab(RectTransform parent, CommandCategory category, string label, int index)
        {
            GameObject tabObject = new GameObject(label + " Tab");
            tabObject.transform.SetParent(parent, false);

            RectTransform rect = tabObject.AddComponent<RectTransform>();
            float min = index / 3f;
            float max = (index + 1f) / 3f;
            rect.anchorMin = new Vector2(min, 0f);
            rect.anchorMax = new Vector2(max, 1f);
            rect.offsetMin = new Vector2(index == 0 ? 0f : 3f, 0f);
            rect.offsetMax = new Vector2(index == 2 ? 0f : -3f, 0f);

            Image image = tabObject.AddComponent<Image>();
            image.color = new Color(0.075f, 0.09f, 0.09f, 0.98f);

            Button button = tabObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => SelectCategory(category));

            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.2f, 0.28f, 0.3f, 1f);
            colors.pressedColor = new Color(0.1f, 0.45f, 0.52f, 1f);
            colors.disabledColor = new Color(0.08f, 0.08f, 0.08f, 0.72f);
            button.colors = colors;

            Text text = CreateText(rect, label + " Tab Text", label, 11, TextAnchor.MiddleCenter);
            text.fontStyle = FontStyle.Bold;
            text.rectTransform.offsetMin = new Vector2(2f, 0f);
            text.rectTransform.offsetMax = new Vector2(-2f, 0f);

            categoryTabs.Add(new CategoryTab
            {
                Category = category,
                Button = button,
                Image = image,
                Label = text
            });
        }

        private RectTransform CreateCategoryGroup(RectTransform parent, string name)
        {
            GameObject groupObject = new GameObject(name);
            groupObject.transform.SetParent(parent, false);

            RectTransform rect = groupObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, CommandContentHeight);
            return rect;
        }

        private void SelectCategory(CommandCategory category)
        {
            activeCategory = category;
            SetGroupVisible(buildingsGroup, category == CommandCategory.Buildings);
            SetGroupVisible(vehiclesGroup, category == CommandCategory.Vehicles);
            SetGroupVisible(infantryGroup, category == CommandCategory.Infantry);

            if (commandScroll != null)
            {
                commandScroll.verticalNormalizedPosition = 1f;
            }

            HideTooltip();
            RefreshCategoryTabs();
        }

        private static void SetGroupVisible(RectTransform group, bool visible)
        {
            if (group != null)
            {
                group.gameObject.SetActive(visible);
            }
        }

        private void RefreshCategoryTabs()
        {
            for (int i = 0; i < categoryTabs.Count; i++)
            {
                CategoryTab tab = categoryTabs[i];
                bool active = tab.Category == activeCategory;
                if (tab.Image != null)
                {
                    tab.Image.color = active ? new Color(0.14f, 0.24f, 0.25f, 0.98f) : new Color(0.075f, 0.09f, 0.09f, 0.98f);
                }

                if (tab.Label != null)
                {
                    tab.Label.color = active ? Color.white : new Color(0.62f, 0.72f, 0.72f, 1f);
                }
            }
        }

        private void CreateMenuOverlay(Transform parent)
        {
            menuOverlay = new GameObject("Landing Menu");
            menuOverlay.transform.SetParent(parent, false);

            RectTransform overlayRect = menuOverlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image overlayImage = menuOverlay.AddComponent<Image>();
            overlayImage.color = new Color(0.015f, 0.018f, 0.02f, 0.76f);

            RectTransform panel = CreatePanel(
                menuOverlay.transform,
                "Landing Menu Panel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(460f, 270f),
                new Color(0.035f, 0.042f, 0.046f, 0.96f));

            menuTitleText = CreateText(panel, "Landing Menu Title", "Quest Command RTS", 31, TextAnchor.MiddleCenter, new Vector2(0f, -30f), new Vector2(-56f, 58f));
            menuTitleText.fontStyle = FontStyle.Bold;

            menuPrimaryButton = CreateMenuButton(panel, "Primary", "Start Battle", -112f, StartBattle);
            menuPrimaryLabel = menuPrimaryButton.GetComponentInChildren<Text>();
            CreateMenuButton(panel, "Restart", "Restart", -166f, RestartBattle);
            CreateMenuButton(panel, "Quit", "Quit", -220f, QuitGame);

            menuOverlay.SetActive(false);
        }

        private Button CreateMenuButton(RectTransform parent, string name, string label, float y, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = new GameObject(name + " Menu Button");
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(-96f, 42f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.12f, 0.17f, 0.18f, 0.98f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.2f, 0.28f, 0.3f, 1f);
            colors.pressedColor = new Color(0.1f, 0.45f, 0.52f, 1f);
            colors.disabledColor = new Color(0.18f, 0.18f, 0.18f, 0.72f);
            button.colors = colors;

            Text text = CreateText(rect, name + " Menu Button Text", label, 17, TextAnchor.MiddleCenter);
            text.fontStyle = FontStyle.Bold;
            text.rectTransform.offsetMin = new Vector2(8f, 0f);
            text.rectTransform.offsetMax = new Vector2(-8f, 0f);
            return button;
        }

        private void ShowLandingMenu()
        {
            gameStarted = false;
            pauseMenuOpen = false;
            ShowMenu("Quest Command RTS", "Start Battle", StartBattle);
        }

        private void TogglePauseMenu()
        {
            if (pauseMenuOpen)
            {
                ResumeBattle();
            }
            else
            {
                pauseMenuOpen = true;
                ShowMenu("Paused", "Resume", ResumeBattle);
            }
        }

        private void ShowMenu(string title, string primaryLabel, UnityEngine.Events.UnityAction primaryAction)
        {
            if (menuOverlay == null || menuPrimaryButton == null || menuPrimaryLabel == null || menuTitleText == null)
            {
                return;
            }

            BlocksGameInput = true;
            Time.timeScale = 0f;
            menuTitleText.text = title;
            menuPrimaryLabel.text = primaryLabel;
            menuPrimaryButton.onClick.RemoveAllListeners();
            menuPrimaryButton.onClick.AddListener(primaryAction);
            HideTooltip();
            menuOverlay.SetActive(true);
        }

        private void HideMenu()
        {
            if (menuOverlay != null)
            {
                menuOverlay.SetActive(false);
            }

            BlocksGameInput = false;
            Time.timeScale = 1f;
        }

        private void StartBattle()
        {
            gameStarted = true;
            pauseMenuOpen = false;
            HideMenu();
        }

        private void ResumeBattle()
        {
            pauseMenuOpen = false;
            HideMenu();
        }

        private void RestartBattle()
        {
            BlocksGameInput = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void QuitGame()
        {
            BlocksGameInput = false;
            Time.timeScale = 1f;
            Application.Quit();
        }

        private void AddUnitCard(RectTransform parent, UnitKind kind, int index, float startY, string cardResource)
        {
            UnitStats stats = RtsBalance.GetUnit(kind);
            AddCard(
                parent,
                stats.Name,
                index,
                startY,
                cardResource,
                () => game.TryQueueUnit(kind),
                () => CanAfford(stats.Cost),
                () => game != null ? game.GetQueuedUnitCount(kind) : 0,
                () => "Cost " + stats.Cost + " credits  Build " + FormatBuildTime(stats.BuildTime),
                () => IsUnitBuilding(kind),
                () => GetUnitBuildProgress(kind),
                null);
        }

        private void AddBuildCard(RectTransform parent, StructureKind kind, int index, float startY, string cardResource)
        {
            StructureStats stats = RtsBalance.GetStructure(kind);
            AddCard(
                parent,
                stats.Name,
                index,
                startY,
                cardResource,
                () => game.BuildManager.QueueStructure(kind),
                () => CanStartStructure(kind) || IsStructurePlacementReady(kind),
                () => GetStructureQueuedCount(kind),
                () => "Cost " + stats.Cost + " credits  Build " + FormatBuildTime(stats.BuildTime),
                () => IsStructureBuilding(kind),
                () => GetStructureBuildProgress(kind),
                () => IsStructurePlacementReady(kind) ? "READY" : null);
        }

        private void AddCard(RectTransform parent, string name, int index, float startY, string cardResource, UnityEngine.Events.UnityAction action, Func<bool> enabled, Func<int> count, Func<string> tooltip, Func<bool> hasProgress, Func<float> getProgress, Func<string> getStatus)
        {
            GameObject buttonObject = new GameObject(name + " Card");
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(CardLeft + (index % 2) * (CardSize + CardGap), startY - (index / 2) * (CardSize + CardGap));
            rect.sizeDelta = new Vector2(CardSize, CardSize);

            Image image = buttonObject.AddComponent<Image>();
            Sprite sprite = LoadCardSprite(cardResource);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
                image.preserveAspect = false;
            }
            else
            {
                image.color = new Color(0.11f, 0.135f, 0.13f, 0.96f);
            }

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.86f);
            colors.pressedColor = new Color(0.72f, 0.92f, 1f, 0.9f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.65f);
            button.colors = colors;

            Text label = null;
            if (sprite == null)
            {
                label = CreateText(rect, name + " Text", name, 12, TextAnchor.MiddleCenter);
                label.rectTransform.offsetMin = new Vector2(5f, 5f);
                label.rectTransform.offsetMax = new Vector2(-5f, -5f);
            }

            GameObject progressRoot = CreateProgressClock(rect, out Image progressImage);

            GameObject countRoot = null;
            Text countLabel = null;
            if (count != null)
            {
                countRoot = CreateCountBadge(rect, out countLabel);
            }

            Text statusLabel = CreateStatusLabel(rect);

            TooltipTarget tooltipTarget = buttonObject.AddComponent<TooltipTarget>();
            tooltipTarget.Owner = this;
            tooltipTarget.GetTooltip = tooltip;

            buttons.Add(new HudButton
            {
                Button = button,
                Label = label,
                CountRoot = countRoot,
                CountLabel = countLabel,
                ProgressRoot = progressRoot,
                ProgressImage = progressImage,
                StatusLabel = statusLabel,
                IsEnabled = enabled,
                GetText = sprite == null ? (() => name) : null,
                GetCount = count,
                HasProgress = hasProgress,
                GetProgress = getProgress,
                GetStatus = getStatus
            });
        }

        private GameObject CreateProgressClock(RectTransform parent, out Image progressImage)
        {
            GameObject progressObject = new GameObject("Build Clock");
            progressObject.transform.SetParent(parent, false);

            RectTransform rect = progressObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            progressImage = progressObject.AddComponent<Image>();
            progressImage.color = new Color(0f, 0f, 0f, 0.62f);
            progressImage.type = Image.Type.Filled;
            progressImage.fillMethod = Image.FillMethod.Radial360;
            progressImage.fillOrigin = (int)Image.Origin360.Top;
            progressImage.fillClockwise = false;
            progressImage.fillAmount = 1f;
            progressImage.raycastTarget = false;

            progressObject.SetActive(false);
            return progressObject;
        }

        private Text CreateStatusLabel(RectTransform parent)
        {
            GameObject statusObject = new GameObject("Ready Status");
            statusObject.transform.SetParent(parent, false);

            RectTransform rect = statusObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 3f);
            rect.sizeDelta = new Vector2(-8f, 15f);

            Text label = statusObject.AddComponent<Text>();
            label.font = font;
            label.text = "";
            label.fontSize = 10;
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(0.35f, 1f, 0.95f, 1f);
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            statusObject.SetActive(false);
            return label;
        }

        private GameObject CreateCountBadge(RectTransform parent, out Text label)
        {
            GameObject badge = new GameObject("Queue Count");
            badge.transform.SetParent(parent, false);

            RectTransform rect = badge.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(6f, 6f);
            rect.sizeDelta = new Vector2(30f, 30f);

            Image image = badge.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.78f);

            GameObject textObject = new GameObject("Queue Count Text");
            textObject.transform.SetParent(badge.transform, false);
            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            label = textObject.AddComponent<Text>();
            label.font = font;
            label.text = "0";
            label.fontSize = 18;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;

            badge.SetActive(false);
            return badge;
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

        private bool CanAfford(int cost)
        {
            return game != null && game.Resources != null && game.Resources.CanAfford(cost);
        }

        private bool CanStartStructure(StructureKind kind)
        {
            return game != null && game.BuildManager != null && game.BuildManager.CanStartStructure(kind);
        }

        private bool IsUnitBuilding(UnitKind kind)
        {
            return game != null && game.TryGetUnitBuildProgress(kind, out float _);
        }

        private float GetUnitBuildProgress(UnitKind kind)
        {
            if (game != null && game.TryGetUnitBuildProgress(kind, out float progress))
            {
                return progress;
            }

            return 0f;
        }

        private bool IsStructureBuilding(StructureKind kind)
        {
            return game != null && game.BuildManager != null && game.BuildManager.IsStructureBuilding(kind);
        }

        private bool IsStructurePlacementReady(StructureKind kind)
        {
            return game != null && game.BuildManager != null && game.BuildManager.IsPlacementReady(kind);
        }

        private float GetStructureBuildProgress(StructureKind kind)
        {
            return game != null && game.BuildManager != null ? game.BuildManager.GetStructureBuildProgress(kind) : 0f;
        }

        private int GetStructureQueuedCount(StructureKind kind)
        {
            return game != null && game.BuildManager != null ? game.BuildManager.GetStructureQueuedCount(kind) : 0;
        }

        private static string FormatBuildTime(float seconds)
        {
            return Mathf.CeilToInt(seconds) + "s";
        }

        private void UpdateMinimap()
        {
            if (minimapRect == null || game == null || Time.unscaledTime < nextMinimapUpdate)
            {
                return;
            }

            nextMinimapUpdate = Time.unscaledTime + 0.12f;
            int blipIndex = 0;
            float halfSize = RtsBalance.MapHalfSize;

            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsEntity entity = game.Entities[i];
                if (entity == null || !entity.IsAlive)
                {
                    continue;
                }

                RectTransform blip = GetMinimapBlip(blipIndex);
                blipIndex++;
                blip.gameObject.SetActive(true);

                Vector3 point = entity.GroundPosition;
                float x = Mathf.InverseLerp(-halfSize, halfSize, Mathf.Clamp(point.x, -halfSize, halfSize));
                float y = Mathf.InverseLerp(-halfSize, halfSize, Mathf.Clamp(point.z, -halfSize, halfSize));
                blip.anchorMin = new Vector2(x, y);
                blip.anchorMax = new Vector2(x, y);
                blip.anchoredPosition = Vector2.zero;

                bool isStructure = entity is RtsStructure;
                float size = isStructure ? 6f : 4f;
                blip.sizeDelta = new Vector2(size, size);

                Image image = blip.GetComponent<Image>();
                if (image != null)
                {
                    image.color = GetMinimapColor(entity.Team, isStructure);
                }
            }

            for (int i = blipIndex; i < minimapBlips.Count; i++)
            {
                minimapBlips[i].gameObject.SetActive(false);
            }
        }

        private RectTransform GetMinimapBlip(int index)
        {
            while (minimapBlips.Count <= index)
            {
                GameObject blipObject = new GameObject("Radar Blip");
                blipObject.transform.SetParent(minimapRect, false);

                RectTransform rect = blipObject.AddComponent<RectTransform>();
                rect.pivot = new Vector2(0.5f, 0.5f);

                Image image = blipObject.AddComponent<Image>();
                image.color = Color.white;
                image.raycastTarget = false;

                minimapBlips.Add(rect);
            }

            return minimapBlips[index];
        }

        private static Color GetMinimapColor(RtsTeam team, bool isStructure)
        {
            switch (team)
            {
                case RtsTeam.Player:
                    return isStructure ? new Color(0.2f, 0.92f, 1f, 0.98f) : new Color(0.28f, 1f, 0.82f, 0.95f);
                case RtsTeam.Enemy:
                    return isStructure ? new Color(1f, 0.25f, 0.16f, 0.98f) : new Color(1f, 0.45f, 0.34f, 0.95f);
                default:
                    return new Color(0.92f, 0.78f, 0.34f, 0.9f);
            }
        }

        private Sprite LoadCardSprite(string cardResource)
        {
            if (string.IsNullOrEmpty(cardResource))
            {
                return null;
            }

            if (cardSprites.TryGetValue(cardResource, out Sprite cached))
            {
                return cached;
            }

            Texture2D texture = UnityEngine.Resources.Load<Texture2D>("Cards/" + cardResource);
            if (texture == null)
            {
                return null;
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            cardSprites[cardResource] = sprite;
            return sprite;
        }

        private void CreateTooltip(Transform parent)
        {
            GameObject tooltipObject = new GameObject("Price Tooltip");
            tooltipObject.transform.SetParent(parent, false);

            tooltipRect = tooltipObject.AddComponent<RectTransform>();
            tooltipRect.anchorMin = new Vector2(0f, 0f);
            tooltipRect.anchorMax = new Vector2(0f, 0f);
            tooltipRect.pivot = new Vector2(0f, 0f);
            tooltipRect.sizeDelta = new Vector2(156f, 34f);

            Image image = tooltipObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.86f);

            tooltipText = CreateText(tooltipRect, "Price Tooltip Text", "", 15, TextAnchor.MiddleCenter);
            tooltipText.fontStyle = FontStyle.Bold;
            tooltipText.rectTransform.offsetMin = new Vector2(8f, 0f);
            tooltipText.rectTransform.offsetMax = new Vector2(-8f, 0f);

            tooltipObject.SetActive(false);
        }

        private void ShowTooltip(string message)
        {
            if (tooltipRect == null || tooltipText == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            tooltipText.text = message;
            tooltipRect.gameObject.SetActive(true);
            UpdateTooltipPosition();
        }

        private void HideTooltip()
        {
            if (tooltipRect != null)
            {
                tooltipRect.gameObject.SetActive(false);
            }
        }

        private void UpdateTooltipPosition()
        {
            if (tooltipRect == null || !tooltipRect.gameObject.activeSelf)
            {
                return;
            }

            Vector3 position = Input.mousePosition + new Vector3(18f, 24f, 0f);
            position.x = Mathf.Min(position.x, Screen.width - tooltipRect.sizeDelta.x - 8f);
            position.y = Mathf.Min(position.y, Screen.height - tooltipRect.sizeDelta.y - 8f);
            tooltipRect.position = position;
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
            }

            return detail;
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

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }
}
