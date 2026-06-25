using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace QuestCommandRTS
{
    public enum QuestCommandConsoleTab
    {
        Build,
        Produce,
        Selected,
        System
    }

    public sealed class QuestCommandConsole : MonoBehaviour
    {
        private const float RefreshInterval = 0.18f;
        private const int QueueLineCount = 4;

        private sealed class ConsoleButton
        {
            public RectTransform Rect;
            public Image Image;
            public Text Label;
            public Action Clicked;
            public bool Interactable = true;
            public bool Selected;
            public float PressedUntilTime;

            public void SetVisual(bool hovered)
            {
                if (Image == null)
                {
                    return;
                }

                if (!Interactable)
                {
                    Image.color = new Color(0.045f, 0.055f, 0.06f, 0.82f);
                }
                else if (Time.unscaledTime < PressedUntilTime)
                {
                    Image.color = new Color(0.26f, 0.58f, 0.62f, 0.98f);
                }
                else if (Selected)
                {
                    Image.color = new Color(0.05f, 0.38f, 0.43f, 0.95f);
                }
                else if (hovered)
                {
                    Image.color = new Color(0.12f, 0.28f, 0.31f, 0.96f);
                }
                else
                {
                    Image.color = new Color(0.055f, 0.09f, 0.1f, 0.9f);
                }

                if (Label != null)
                {
                    Label.color = Interactable ? new Color(0.88f, 0.98f, 1f) : new Color(0.48f, 0.56f, 0.58f);
                }
            }
        }

        private sealed class ConsoleRow
        {
            public ConsoleButton Button;
            public Image Icon;
            public Text Title;
            public Text Detail;
            public Image StateStrip;
        }

        public bool IsOpen => canvasObject != null && canvasObject.activeSelf;
        public RectTransform PanelRect => panelRect;
        public Transform WristAnchor => wristAnchor;

        private readonly List<ConsoleButton> buttons = new List<ConsoleButton>(24);
        private readonly ConsoleRow[] buildRows = new ConsoleRow[RtsCommandConsoleModel.BuildKinds.Length];
        private readonly ConsoleRow[] productionRows = new ConsoleRow[RtsCommandConsoleModel.UnitKinds.Length];
        private readonly Text[] queueLines = new Text[QueueLineCount];

        private RtsGame game;
        private QuestTabletopSettings settings;
        private RtsCommandConsoleModel model;
        private GameObject canvasObject;
        private Transform wristAnchor;
        private RectTransform panelRect;
        private RectTransform buildRoot;
        private RectTransform produceRoot;
        private RectTransform selectedRoot;
        private RectTransform systemRoot;
        private Text headerText;
        private Text placementText;
        private Text selectedText;
        private Text systemText;
        private ConsoleButton buildTabButton;
        private ConsoleButton produceTabButton;
        private ConsoleButton selectedTabButton;
        private ConsoleButton systemTabButton;
        private ConsoleButton cancelQueueButton;
        private ConsoleButton selectedCancelQueueButton;
        private ConsoleButton repairButton;
        private ConsoleButton sellButton;
        private ConsoleButton rallyHintButton;
        private ConsoleButton stopUnitsButton;
        private ConsoleButton pauseButton;
        private ConsoleButton saveButton;
        private ConsoleButton loadButton;
        private ConsoleButton restartButton;
        private ConsoleButton hoveredButton;
        private Font font;
        private QuestCommandConsoleTab activeTab = QuestCommandConsoleTab.Build;
        private float nextRefreshTime;

        public void Initialize(RtsGame owner, Transform rigRoot, Transform leftController, QuestTabletopSettings tabletopSettings)
        {
            game = owner;
            settings = tabletopSettings;
            model = new RtsCommandConsoleModel();
            model.Initialize(game);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            BuildCanvas(rigRoot, leftController);
            SetOpen(false);
        }

        private void Update()
        {
            if (IsOpen && Time.unscaledTime >= nextRefreshTime)
            {
                nextRefreshTime = Time.unscaledTime + RefreshInterval;
                Refresh(false);
            }
        }

        public void ToggleOpen()
        {
            SetOpen(!IsOpen);
        }

        public void SetOpen(bool open)
        {
            if (canvasObject == null)
            {
                return;
            }

            canvasObject.SetActive(open);
            if (open)
            {
                Refresh(true);
            }
            else
            {
                SetHoveredButton(null);
            }
        }

        public bool TryHandlePointer(Ray ray, bool activateDown)
        {
            if (!IsOpen || panelRect == null)
            {
                return false;
            }

            Vector3 panelPoint;
            if (!TryGetPanelPoint(ray, out panelPoint))
            {
                SetHoveredButton(null);
                return false;
            }

            ConsoleButton hitButton = null;
            for (int i = 0; i < buttons.Count; i++)
            {
                ConsoleButton button = buttons[i];
                if (button.Rect != null && button.Rect.gameObject.activeInHierarchy && ContainsWorldPoint(button.Rect, panelPoint))
                {
                    hitButton = button;
                    break;
                }
            }

            SetHoveredButton(hitButton);
            if (activateDown && hitButton != null && hitButton.Interactable && hitButton.Clicked != null)
            {
                hitButton.PressedUntilTime = Time.unscaledTime + 0.12f;
                SetButtonStates();
                hitButton.Clicked.Invoke();
                Refresh(true);
            }

            return true;
        }

        public bool TryGetPanelHit(Ray ray, out Vector3 point)
        {
            if (!IsOpen || panelRect == null)
            {
                point = Vector3.zero;
                return false;
            }

            return TryGetPanelPoint(ray, out point);
        }

        private void BuildCanvas(Transform rigRoot, Transform leftController)
        {
            Transform canvasParent = CreateWristAnchor(rigRoot, leftController);
            canvasObject = new GameObject("Quest Command Console");
            canvasObject.transform.SetParent(canvasParent, false);
            canvasObject.transform.localPosition = Vector3.zero;
            canvasObject.transform.localRotation = Quaternion.identity;
            canvasObject.transform.localScale = Vector3.one * 0.001f;

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            panelRect = canvasObject.GetComponent<RectTransform>();
            panelRect.sizeDelta = settings.CommandConsoleSizeMeters * 1000f;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 12f;

            Image background = canvasObject.AddComponent<Image>();
            background.color = new Color(0.01f, 0.022f, 0.028f, 0.88f);

            CreatePanelImage(panelRect, "Console Top Glow", new Color(0.36f, 0.9f, 1f, 0.78f), new Vector2(0f, 0f), new Vector2(panelRect.sizeDelta.x, 3f));
            CreatePanelImage(panelRect, "Console Bottom Glow", new Color(0.18f, 0.72f, 0.9f, 0.58f), new Vector2(0f, -panelRect.sizeDelta.y + 3f), new Vector2(panelRect.sizeDelta.x, 3f));
            CreatePanelImage(panelRect, "Console Left Glow", new Color(0.25f, 0.82f, 1f, 0.58f), new Vector2(0f, 0f), new Vector2(3f, panelRect.sizeDelta.y));
            CreatePanelImage(panelRect, "Console Right Glow", new Color(0.16f, 0.68f, 0.84f, 0.48f), new Vector2(panelRect.sizeDelta.x - 3f, 0f), new Vector2(3f, panelRect.sizeDelta.y));
            CreatePanelImage(panelRect, "Console Header Underline", new Color(0.23f, 0.77f, 0.92f, 0.48f), new Vector2(18f, -54f), new Vector2(700f, 2f));
            CreatePanelImage(panelRect, "Console Tab Rail", new Color(0.08f, 0.26f, 0.32f, 0.62f), new Vector2(18f, -106f), new Vector2(700f, 2f));
            CreatePanelImage(panelRect, "Console Content Backplate", new Color(0.018f, 0.038f, 0.045f, 0.46f), new Vector2(14f, -112f), new Vector2(712f, 392f));

            headerText = CreateText(panelRect, "Console Header", "", 24, TextAnchor.MiddleLeft, new Vector2(18f, -16f), new Vector2(530f, 42f));
            placementText = CreateText(panelRect, "Placement Status", "", 16, TextAnchor.MiddleRight, new Vector2(548f, -17f), new Vector2(170f, 40f));

            buildTabButton = CreateButton(panelRect, "Build Tab", "Build", new Vector2(18f, -66f), new Vector2(110f, 38f), () => SetTab(QuestCommandConsoleTab.Build));
            produceTabButton = CreateButton(panelRect, "Produce Tab", "Produce", new Vector2(136f, -66f), new Vector2(128f, 38f), () => SetTab(QuestCommandConsoleTab.Produce));
            selectedTabButton = CreateButton(panelRect, "Selected Tab", "Selected", new Vector2(272f, -66f), new Vector2(132f, 38f), () => SetTab(QuestCommandConsoleTab.Selected));
            systemTabButton = CreateButton(panelRect, "System Tab", "System", new Vector2(412f, -66f), new Vector2(118f, 38f), () => SetTab(QuestCommandConsoleTab.System));

            buildRoot = CreateRoot("Build Content");
            produceRoot = CreateRoot("Produce Content");
            selectedRoot = CreateRoot("Selected Content");
            systemRoot = CreateRoot("System Content");

            BuildBuildTab();
            BuildProduceTab();
            BuildSelectedTab();
            BuildSystemTab();
            SetTab(QuestCommandConsoleTab.Build);
        }

        private Transform CreateWristAnchor(Transform rigRoot, Transform leftController)
        {
            Transform parent = leftController != null ? leftController : rigRoot;
            GameObject anchorObject = new GameObject("Left Wrist Build Menu Anchor");
            Transform anchor = anchorObject.transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = settings.CommandConsoleLocalPositionMeters;
            anchor.localRotation = Quaternion.Euler(settings.CommandConsoleLocalEulerDegrees);
            wristAnchor = anchor;
            return anchor;
        }

        private RectTransform CreateRoot(string name)
        {
            GameObject rootObject = new GameObject(name);
            rootObject.transform.SetParent(panelRect, false);
            RectTransform rect = rootObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(14f, 14f);
            rect.offsetMax = new Vector2(-14f, -112f);
            return rect;
        }

        private void BuildBuildTab()
        {
            for (int i = 0; i < buildRows.Length; i++)
            {
                int capturedIndex = i;
                StructureKind kind = RtsCommandConsoleModel.BuildKinds[i];
                ConsoleRow row = CreateSelectionTile(buildRoot, "Build Row " + i, i, new Vector2(220f, 160f), () => OnBuildClicked(kind));
                AddStructureGlyph(row.Icon.rectTransform, kind);
                buildRows[capturedIndex] = row;
            }
        }

        private void BuildProduceTab()
        {
            for (int i = 0; i < productionRows.Length; i++)
            {
                UnitKind kind = RtsCommandConsoleModel.UnitKinds[i];
                ConsoleRow row = CreateSelectionTile(produceRoot, "Produce Row " + i, i, new Vector2(220f, 146f), () => OnProduceClicked(kind));
                AddUnitGlyph(row.Icon.rectTransform, kind);
                productionRows[i] = row;
            }

            CreatePanelImage(produceRoot, "Production Queue Backplate", new Color(0.025f, 0.06f, 0.07f, 0.76f), new Vector2(0f, -178f), new Vector2(704f, 152f));
            CreateText(produceRoot, "Queue Label", "Queue", 18, TextAnchor.MiddleLeft, new Vector2(18f, -188f), new Vector2(260f, 28f));
            for (int i = 0; i < queueLines.Length; i++)
            {
                queueLines[i] = CreateText(produceRoot, "Queue " + i, "", 15, TextAnchor.MiddleLeft, new Vector2(18f, -222f - i * 28f), new Vector2(400f, 26f));
            }

            cancelQueueButton = CreateButton(produceRoot, "Cancel Queue Button", "Cancel Production", new Vector2(456f, -238f), new Vector2(230f, 46f), () => game.PlayerCommands.CancelProduction());
        }

        private void BuildSelectedTab()
        {
            selectedText = CreateText(selectedRoot, "Selected Details", "", 18, TextAnchor.UpperLeft, new Vector2(0f, -16f), new Vector2(460f, 230f));
            repairButton = CreateButton(selectedRoot, "Repair Button", "Repair", new Vector2(486f, -20f), new Vector2(218f, 44f), () => game.PlayerCommands.RepairSelectedStructures());
            sellButton = CreateButton(selectedRoot, "Sell Button", "Sell", new Vector2(486f, -74f), new Vector2(218f, 44f), () => game.PlayerCommands.SellSelectedStructures());
            selectedCancelQueueButton = CreateButton(selectedRoot, "Cancel Queue Button Selected", "Cancel Production", new Vector2(486f, -128f), new Vector2(218f, 44f), () => game.PlayerCommands.CancelProduction());
            rallyHintButton = CreateButton(selectedRoot, "Rally Hint Button", "Set Rally: A on terrain", new Vector2(486f, -182f), new Vector2(218f, 44f), OnRallyHint);
            stopUnitsButton = CreateButton(selectedRoot, "Stop Units Button", "Stop Units", new Vector2(486f, -236f), new Vector2(218f, 44f), () => game.CommandDispatcher.StopSelectedUnits());
        }

        private void BuildSystemTab()
        {
            systemText = CreateText(systemRoot, "System Details", "", 18, TextAnchor.UpperLeft, new Vector2(0f, -16f), new Vector2(460f, 230f));
            pauseButton = CreateButton(systemRoot, "Pause Button", "Pause", new Vector2(486f, -20f), new Vector2(218f, 44f), () => game.ToggleUserPause());
            saveButton = CreateButton(systemRoot, "Save Button", "Save", new Vector2(486f, -74f), new Vector2(218f, 44f), () => game.TryManualSave());
            loadButton = CreateButton(systemRoot, "Load Button", "Load", new Vector2(486f, -128f), new Vector2(218f, 44f), () => game.TryManualLoad());
            restartButton = CreateButton(systemRoot, "New Match Button", "New Match", new Vector2(486f, -182f), new Vector2(218f, 44f), () => game.TryRestartMatch());
        }

        private ConsoleRow CreateSelectionTile(RectTransform parent, string name, int index, Vector2 size, Action clicked)
        {
            const float columnGap = 18f;
            const float rowGap = 18f;
            int column = index % 3;
            int row = index / 3;
            Vector2 position = new Vector2(column * (size.x + columnGap), -12f - row * (size.y + rowGap));
            ConsoleButton button = CreateButton(parent, name, "", position, size, clicked);

            CreatePanelImage(button.Rect, name + " Tile Glow", new Color(0.2f, 0.84f, 1f, 0.22f), new Vector2(7f, -7f), new Vector2(size.x - 14f, 3f));
            Image icon = CreatePanelImage(button.Rect, name + " Icon", new Color(0.2f, 0.75f, 0.9f, 0.92f), new Vector2(48f, -20f), new Vector2(124f, 74f));
            CreatePanelImage(button.Rect, name + " Icon Shine", new Color(0.9f, 1f, 1f, 0.18f), new Vector2(56f, -28f), new Vector2(108f, 4f));
            Text title = CreateText(button.Rect, name + " Title", "", 17, TextAnchor.MiddleCenter, new Vector2(12f, -104f), new Vector2(size.x - 24f, 24f));
            Text detail = CreateText(button.Rect, name + " Detail", "", 13, TextAnchor.MiddleCenter, new Vector2(12f, -128f), new Vector2(size.x - 24f, 22f));

            GameObject stripObject = new GameObject(name + " State");
            stripObject.transform.SetParent(button.Rect, false);
            RectTransform stripRect = stripObject.AddComponent<RectTransform>();
            SetTopLeft(stripRect, new Vector2(0f, 0f), new Vector2(size.x, 5f));
            Image strip = stripObject.AddComponent<Image>();

            return new ConsoleRow
            {
                Button = button,
                Icon = icon,
                Title = title,
                Detail = detail,
                StateStrip = strip
            };
        }

        private ConsoleButton CreateButton(RectTransform parent, string name, string label, Vector2 position, Vector2 size, Action clicked)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            SetTopLeft(rect, position, size);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.055f, 0.09f, 0.1f, 0.9f);

            ConsoleButton button = new ConsoleButton
            {
                Rect = rect,
                Image = image,
                Clicked = clicked
            };

            if (!string.IsNullOrEmpty(label))
            {
                button.Label = CreateText(rect, name + " Text", label, 16, TextAnchor.MiddleCenter, Vector2.zero, size);
            }

            buttons.Add(button);
            return button;
        }

        private static void AddStructureGlyph(RectTransform parent, StructureKind kind)
        {
            Color glyph = new Color(0.88f, 0.98f, 1f, 0.92f);
            Color dim = new Color(0.45f, 0.85f, 0.95f, 0.5f);

            switch (kind)
            {
                case StructureKind.CommandCenter:
                    CreatePanelImage(parent, "Command Glyph Base", glyph, new Vector2(22f, -44f), new Vector2(82f, 20f));
                    CreatePanelImage(parent, "Command Glyph Tower", glyph, new Vector2(46f, -20f), new Vector2(34f, 28f));
                    CreatePanelImage(parent, "Command Glyph Dish", dim, new Vector2(82f, -28f), new Vector2(24f, 8f));
                    break;
                case StructureKind.PowerPlant:
                    CreatePanelImage(parent, "Power Glyph Base", glyph, new Vector2(22f, -50f), new Vector2(82f, 14f));
                    CreatePanelImage(parent, "Power Glyph Stack A", glyph, new Vector2(36f, -18f), new Vector2(14f, 32f));
                    CreatePanelImage(parent, "Power Glyph Stack B", glyph, new Vector2(70f, -12f), new Vector2(14f, 38f));
                    break;
                case StructureKind.Barracks:
                    CreatePanelImage(parent, "Barracks Glyph Body", glyph, new Vector2(22f, -38f), new Vector2(80f, 26f));
                    CreatePanelImage(parent, "Barracks Glyph Roof", dim, new Vector2(34f, -20f), new Vector2(56f, 12f));
                    CreatePanelImage(parent, "Barracks Glyph Door", new Color(0.02f, 0.08f, 0.09f, 0.8f), new Vector2(56f, -46f), new Vector2(14f, 18f));
                    break;
                case StructureKind.Refinery:
                    CreatePanelImage(parent, "Refinery Glyph Body", glyph, new Vector2(18f, -42f), new Vector2(58f, 22f));
                    CreatePanelImage(parent, "Refinery Glyph Silo A", dim, new Vector2(82f, -18f), new Vector2(14f, 42f));
                    CreatePanelImage(parent, "Refinery Glyph Silo B", dim, new Vector2(102f, -28f), new Vector2(10f, 32f));
                    break;
                case StructureKind.WarFactory:
                    CreatePanelImage(parent, "Factory Glyph Body", glyph, new Vector2(18f, -36f), new Vector2(88f, 28f));
                    CreatePanelImage(parent, "Factory Glyph Door", new Color(0.02f, 0.08f, 0.09f, 0.8f), new Vector2(52f, -44f), new Vector2(28f, 20f));
                    CreatePanelImage(parent, "Factory Glyph Rail", dim, new Vector2(28f, -20f), new Vector2(68f, 6f));
                    break;
                case StructureKind.Turret:
                    CreatePanelImage(parent, "Turret Glyph Base", glyph, new Vector2(42f, -50f), new Vector2(44f, 14f));
                    CreatePanelImage(parent, "Turret Glyph Neck", glyph, new Vector2(56f, -34f), new Vector2(18f, 16f));
                    CreatePanelImage(parent, "Turret Glyph Barrel", dim, new Vector2(72f, -24f), new Vector2(34f, 8f));
                    break;
            }
        }

        private static void AddUnitGlyph(RectTransform parent, UnitKind kind)
        {
            Color glyph = new Color(0.88f, 0.98f, 1f, 0.92f);
            Color dim = new Color(0.45f, 0.85f, 0.95f, 0.5f);

            switch (kind)
            {
                case UnitKind.Harvester:
                    CreatePanelImage(parent, "Harvester Glyph Body", glyph, new Vector2(28f, -34f), new Vector2(62f, 28f));
                    CreatePanelImage(parent, "Harvester Glyph Scoop", dim, new Vector2(90f, -44f), new Vector2(24f, 16f));
                    CreatePanelImage(parent, "Harvester Glyph Tread", new Color(0.02f, 0.08f, 0.09f, 0.75f), new Vector2(24f, -54f), new Vector2(76f, 10f));
                    break;
                case UnitKind.Tank:
                    CreatePanelImage(parent, "Tank Glyph Hull", glyph, new Vector2(26f, -42f), new Vector2(72f, 22f));
                    CreatePanelImage(parent, "Tank Glyph Turret", glyph, new Vector2(50f, -26f), new Vector2(24f, 16f));
                    CreatePanelImage(parent, "Tank Glyph Barrel", dim, new Vector2(72f, -22f), new Vector2(36f, 6f));
                    break;
                default:
                    CreatePanelImage(parent, "Rifleman Glyph Body", glyph, new Vector2(56f, -22f), new Vector2(16f, 34f));
                    CreatePanelImage(parent, "Rifleman Glyph Rifle", dim, new Vector2(74f, -28f), new Vector2(32f, 6f));
                    CreatePanelImage(parent, "Rifleman Glyph Feet", glyph, new Vector2(42f, -58f), new Vector2(44f, 8f));
                    break;
            }
        }

        private Text CreateText(RectTransform parent, string name, string value, int size, TextAnchor anchor, Vector2 position, Vector2 dimensions)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            SetTopLeft(rect, position, dimensions);

            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.color = new Color(0.88f, 0.95f, 0.96f);
            return text;
        }

        private static Image CreatePanelImage(RectTransform parent, string name, Color color, Vector2 position, Vector2 size)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.AddComponent<RectTransform>();
            SetTopLeft(rect, position, size);
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private void Refresh(bool force)
        {
            if (game == null || game.Resources == null)
            {
                return;
            }

            headerText.text = "RTS COMMAND   Credits " + game.Resources.Credits + "   Power " + game.Resources.PowerUsed + "/" + game.Resources.PowerProvided;
            placementText.text = game.BuildManager != null && game.BuildManager.IsPlacing ? game.BuildManager.GetPlacementStatusText() : "X: Close";

            RefreshBuildTab();
            RefreshProduceTab();
            RefreshSelectedTab();
            RefreshSystemTab();
            SetButtonStates();
        }

        private void RefreshBuildTab()
        {
            for (int i = 0; i < buildRows.Length; i++)
            {
                RtsBuildOptionView option = model.GetBuildOption(i);
                ConsoleRow row = buildRows[i];
                row.Title.text = option.Name + "  $" + option.Cost;

                string state = option.IsAvailable ? "Ready" : option.DisabledReason;
                if (option.WillCauseLowPower && option.IsAvailable)
                {
                    state = "Ready, low power";
                }

                row.Detail.text = option.PowerText + "   " + state;
                row.Button.Interactable = option.IsAvailable;
                SetIconColor(row.Icon, GetStructureIconColor(option.Kind), option.IsAvailable);
                row.StateStrip.color = option.IsAvailable ? new Color(0.25f, 0.95f, 0.55f) : new Color(0.95f, 0.35f, 0.25f);
            }
        }

        private void RefreshProduceTab()
        {
            for (int i = 0; i < productionRows.Length; i++)
            {
                RtsProductionOptionView option = model.GetProductionOption(i);
                ConsoleRow row = productionRows[i];
                row.Title.text = option.Name + "  $" + option.Cost;
                row.Detail.text = option.BuildTime.ToString("0.0") + "s   " + (option.IsAvailable ? "Queue" : option.DisabledReason);
                row.Button.Interactable = option.IsAvailable;
                SetIconColor(row.Icon, GetUnitIconColor(option.Kind), option.IsAvailable);
                row.StateStrip.color = option.IsAvailable ? new Color(0.25f, 0.8f, 1f) : new Color(0.95f, 0.35f, 0.25f);
            }

            for (int i = 0; i < queueLines.Length; i++)
            {
                queueLines[i].text = model.GetProductionQueueLine(i);
            }

            RtsSelectedEntityView selected = model.GetSelectedEntityView();
            if (cancelQueueButton != null && cancelQueueButton.Rect != null && cancelQueueButton.Rect.IsChildOf(produceRoot))
            {
                cancelQueueButton.Interactable = selected.CanCancelQueue;
            }
        }

        private void RefreshSelectedTab()
        {
            RtsSelectedEntityView selected = model.GetSelectedEntityView();
            if (selected.SelectedCount == 0)
            {
                selectedText.text = "No selection\n\nTrigger selects friendly units and structures.";
            }
            else if (selected.HasSingleEntity)
            {
                selectedText.text =
                    selected.Name + "\n" +
                    selected.EntityType + "\n" +
                    "Health " + selected.Health + "/" + selected.MaxHealth + "\n" +
                    selected.QueueSummary + "\n" +
                    selected.RallyText;
            }
            else
            {
                selectedText.text =
                    selected.Name + "\n" +
                    "Units " + selected.UnitCount + "\n" +
                    "Structures " + selected.StructureCount;
            }

            repairButton.Interactable = selected.CanRepair;
            sellButton.Interactable = selected.CanSell;
            selectedCancelQueueButton.Interactable = selected.CanCancelQueue;
            rallyHintButton.Interactable = selected.HasProduction;
            stopUnitsButton.Interactable = game.AcceptsPlayerInput && selected.UnitCount > 0;
        }

        private void RefreshSystemTab()
        {
            if (systemText == null)
            {
                return;
            }

            systemText.text =
                "Match " + FormatTime(game.MatchTime) + "\n" +
                (game.IsUserPaused ? "Paused" : "Running") + "\n" +
                "Manual save " + game.GetManualSaveSummary() + "\n\n" +
                game.StatusMessage;

            pauseButton.Label.text = game.IsUserPaused ? "Resume" : "Pause";
            pauseButton.Interactable = game.AcceptsSystemInput && !game.IsMatchOver;
            saveButton.Interactable = game.AcceptsSystemInput && !game.IsMatchOver;
            loadButton.Interactable = game.AcceptsSystemInput && game.CanLoadManualSave();
            restartButton.Interactable = game.AcceptsSystemInput;
        }

        private void SetTab(QuestCommandConsoleTab tab)
        {
            activeTab = tab;
            if (buildRoot != null)
            {
                buildRoot.gameObject.SetActive(tab == QuestCommandConsoleTab.Build);
                produceRoot.gameObject.SetActive(tab == QuestCommandConsoleTab.Produce);
                selectedRoot.gameObject.SetActive(tab == QuestCommandConsoleTab.Selected);
                systemRoot.gameObject.SetActive(tab == QuestCommandConsoleTab.System);
            }

            buildTabButton.Selected = tab == QuestCommandConsoleTab.Build;
            produceTabButton.Selected = tab == QuestCommandConsoleTab.Produce;
            selectedTabButton.Selected = tab == QuestCommandConsoleTab.Selected;
            systemTabButton.Selected = tab == QuestCommandConsoleTab.System;
            Refresh(true);
        }

        private void OnBuildClicked(StructureKind kind)
        {
            if (game.PlayerCommands.RequestConstruction(kind))
            {
                SetOpen(false);
            }
        }

        private void OnProduceClicked(UnitKind kind)
        {
            game.PlayerCommands.QueueProduction(kind);
        }

        private void OnRallyHint()
        {
            game.SpawnFloatingText("Aim terrain, press A", game.GetPlayerBaseCenter() + Vector3.up * 2.4f, new Color(0.55f, 0.95f, 1f));
        }

        private void SetHoveredButton(ConsoleButton button)
        {
            if (hoveredButton == button)
            {
                return;
            }

            hoveredButton = button;
            SetButtonStates();
        }

        private void SetButtonStates()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                ConsoleButton button = buttons[i];
                button.SetVisual(button == hoveredButton);
            }
        }

        private static void SetIconColor(Image icon, Color color, bool enabled)
        {
            if (icon == null)
            {
                return;
            }

            icon.color = enabled ? color : new Color(color.r * 0.42f, color.g * 0.42f, color.b * 0.42f, 0.72f);
        }

        private static Color GetStructureIconColor(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.PowerPlant:
                    return new Color(0.95f, 0.84f, 0.28f, 0.98f);
                case StructureKind.Barracks:
                    return new Color(0.35f, 0.72f, 1f, 0.98f);
                case StructureKind.Refinery:
                    return new Color(0.22f, 0.95f, 0.58f, 0.98f);
                case StructureKind.WarFactory:
                    return new Color(0.95f, 0.52f, 0.24f, 0.98f);
                case StructureKind.Turret:
                    return new Color(1f, 0.35f, 0.32f, 0.98f);
                default:
                    return new Color(0.65f, 0.86f, 0.95f, 0.98f);
            }
        }

        private static Color GetUnitIconColor(UnitKind kind)
        {
            switch (kind)
            {
                case UnitKind.Harvester:
                    return new Color(0.24f, 0.96f, 0.62f, 0.98f);
                case UnitKind.Tank:
                    return new Color(0.92f, 0.72f, 0.34f, 0.98f);
                default:
                    return new Color(0.45f, 0.88f, 1f, 0.98f);
            }
        }

        private bool TryGetPanelPoint(Ray ray, out Vector3 point)
        {
            Plane plane = new Plane(panelRect.forward, panelRect.position);
            float distance;
            if (!plane.Raycast(ray, out distance) || distance < 0f || distance > settings.RayLengthSimulationUnits)
            {
                point = Vector3.zero;
                return false;
            }

            point = ray.GetPoint(distance);
            Vector3 local = panelRect.InverseTransformPoint(point);
            return panelRect.rect.Contains(new Vector2(local.x, local.y));
        }

        private static bool ContainsWorldPoint(RectTransform rect, Vector3 worldPoint)
        {
            Vector3 local = rect.InverseTransformPoint(worldPoint);
            return rect.rect.Contains(new Vector2(local.x, local.y));
        }

        private static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static string FormatTime(float seconds)
        {
            int wholeSeconds = Mathf.FloorToInt(seconds);
            int minutes = wholeSeconds / 60;
            int remainder = wholeSeconds % 60;
            return minutes + ":" + remainder.ToString("00");
        }
    }
}
