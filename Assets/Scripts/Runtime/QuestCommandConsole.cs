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
                    Image.color = new Color(0.08f, 0.09f, 0.095f, 0.88f);
                }
                else if (Time.unscaledTime < PressedUntilTime)
                {
                    Image.color = new Color(0.32f, 0.52f, 0.54f, 0.98f);
                }
                else if (Selected)
                {
                    Image.color = new Color(0.08f, 0.34f, 0.38f, 0.94f);
                }
                else if (hovered)
                {
                    Image.color = new Color(0.18f, 0.26f, 0.27f, 0.96f);
                }
                else
                {
                    Image.color = new Color(0.11f, 0.145f, 0.15f, 0.93f);
                }

                if (Label != null)
                {
                    Label.color = Interactable ? new Color(0.9f, 0.97f, 0.98f) : new Color(0.52f, 0.58f, 0.6f);
                }
            }
        }

        private sealed class ConsoleRow
        {
            public ConsoleButton Button;
            public Text Title;
            public Text Detail;
            public Image StateStrip;
        }

        public bool IsOpen => canvasObject != null && canvasObject.activeSelf;
        public RectTransform PanelRect => panelRect;

        private readonly List<ConsoleButton> buttons = new List<ConsoleButton>(24);
        private readonly ConsoleRow[] buildRows = new ConsoleRow[RtsCommandConsoleModel.BuildKinds.Length];
        private readonly ConsoleRow[] productionRows = new ConsoleRow[RtsCommandConsoleModel.UnitKinds.Length];
        private readonly Text[] queueLines = new Text[QueueLineCount];

        private RtsGame game;
        private QuestTabletopSettings settings;
        private RtsCommandConsoleModel model;
        private GameObject canvasObject;
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

        public void Initialize(RtsGame owner, Transform rigRoot, QuestTabletopSettings tabletopSettings)
        {
            game = owner;
            settings = tabletopSettings;
            model = new RtsCommandConsoleModel();
            model.Initialize(game);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            BuildCanvas(rigRoot);
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

        private void BuildCanvas(Transform rigRoot)
        {
            canvasObject = new GameObject("Quest Command Console");
            canvasObject.transform.SetParent(rigRoot, false);
            canvasObject.transform.localPosition = settings.CommandConsoleLocalPositionMeters;
            canvasObject.transform.localRotation = Quaternion.Euler(10f, 8f, 0f);
            canvasObject.transform.localScale = Vector3.one * 0.001f;

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            panelRect = canvasObject.GetComponent<RectTransform>();
            panelRect.sizeDelta = settings.CommandConsoleSizeMeters * 1000f;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 12f;

            Image background = canvasObject.AddComponent<Image>();
            background.color = new Color(0.014f, 0.02f, 0.024f, 0.94f);

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
                ConsoleRow row = CreateRow(buildRoot, "Build Row " + i, new Vector2(0f, -12f - i * 58f), new Vector2(708f, 48f), () => OnBuildClicked(kind));
                buildRows[capturedIndex] = row;
            }
        }

        private void BuildProduceTab()
        {
            for (int i = 0; i < productionRows.Length; i++)
            {
                UnitKind kind = RtsCommandConsoleModel.UnitKinds[i];
                ConsoleRow row = CreateRow(produceRoot, "Produce Row " + i, new Vector2(0f, -12f - i * 58f), new Vector2(420f, 48f), () => OnProduceClicked(kind));
                productionRows[i] = row;
            }

            CreateText(produceRoot, "Queue Label", "Queue", 18, TextAnchor.MiddleLeft, new Vector2(440f, -12f), new Vector2(260f, 28f));
            for (int i = 0; i < queueLines.Length; i++)
            {
                queueLines[i] = CreateText(produceRoot, "Queue " + i, "", 15, TextAnchor.MiddleLeft, new Vector2(440f, -48f - i * 30f), new Vector2(260f, 28f));
            }

            cancelQueueButton = CreateButton(produceRoot, "Cancel Queue Button", "Cancel Last", new Vector2(440f, -186f), new Vector2(260f, 40f), () => game.PlayerCommands.CancelLastQueuedProduction());
        }

        private void BuildSelectedTab()
        {
            selectedText = CreateText(selectedRoot, "Selected Details", "", 18, TextAnchor.UpperLeft, new Vector2(0f, -16f), new Vector2(460f, 230f));
            repairButton = CreateButton(selectedRoot, "Repair Button", "Repair", new Vector2(486f, -20f), new Vector2(218f, 44f), () => game.PlayerCommands.RepairSelectedStructures());
            sellButton = CreateButton(selectedRoot, "Sell Button", "Sell", new Vector2(486f, -74f), new Vector2(218f, 44f), () => game.PlayerCommands.SellSelectedStructures());
            selectedCancelQueueButton = CreateButton(selectedRoot, "Cancel Queue Button Selected", "Cancel Last Queue", new Vector2(486f, -128f), new Vector2(218f, 44f), () => game.PlayerCommands.CancelLastQueuedProduction());
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

        private ConsoleRow CreateRow(RectTransform parent, string name, Vector2 position, Vector2 size, Action clicked)
        {
            ConsoleButton button = CreateButton(parent, name, "", position, size, clicked);

            Text title = CreateText(button.Rect, name + " Title", "", 17, TextAnchor.MiddleLeft, new Vector2(14f, -4f), new Vector2(size.x - 220f, 22f));
            Text detail = CreateText(button.Rect, name + " Detail", "", 14, TextAnchor.MiddleRight, new Vector2(size.x - 228f, -4f), new Vector2(208f, 22f));

            GameObject stripObject = new GameObject(name + " State");
            stripObject.transform.SetParent(button.Rect, false);
            RectTransform stripRect = stripObject.AddComponent<RectTransform>();
            SetTopLeft(stripRect, new Vector2(0f, 0f), new Vector2(6f, size.y));
            Image strip = stripObject.AddComponent<Image>();

            return new ConsoleRow
            {
                Button = button,
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
            image.color = new Color(0.11f, 0.145f, 0.15f, 0.93f);

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

        private void Refresh(bool force)
        {
            if (game == null || game.Resources == null)
            {
                return;
            }

            headerText.text = "Command Console   Credits " + game.Resources.Credits + "   Power " + game.Resources.PowerUsed + "/" + game.Resources.PowerProvided;
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
