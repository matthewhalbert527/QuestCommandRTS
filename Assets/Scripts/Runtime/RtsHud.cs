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

        private readonly List<HudButton> buttons = new List<HudButton>();
        private RtsGame game;
        private Text resourcesText;
        private Text selectionText;
        private Font font;
        private GUIStyle bannerStyle;
        private GUIStyle bannerSubStyle;

        public void Initialize(RtsGame owner)
        {
            game = owner;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            BuildCanvas();
        }

        private void Update()
        {
            if (game == null || game.Resources == null)
            {
                return;
            }

            string powerColor = game.Resources.HasLowPower ? "LOW" : "OK";
            string status = game.IsUserPaused ? "PAUSED" : game.StatusMessage;
            resourcesText.text = "Credits " + game.Resources.Credits + "    Power " + game.Resources.PowerUsed + "/" + game.Resources.PowerProvided + " " + powerColor + "    Time " + FormatTime(game.MatchTime) + "    " + status;
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
            }
        }

        private void OnGUI()
        {
            if (game == null)
            {
                return;
            }

            DrawMinimap();

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

            RectTransform topBar = CreatePanel(canvasObject.transform, "Resources", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, -10f), new Vector2(-280f, 42f), new Color(0.02f, 0.025f, 0.025f, 0.78f));
            resourcesText = CreateText(topBar, "Resources Text", "", 19, TextAnchor.MiddleLeft);
            resourcesText.rectTransform.offsetMin = new Vector2(18f, 0f);
            resourcesText.rectTransform.offsetMax = new Vector2(-18f, 0f);

            RectTransform commandPanel = CreatePanel(canvasObject.transform, "Commands", new Vector2(1f, 0.04f), new Vector2(1f, 0.96f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(230f, 0f), new Color(0.025f, 0.03f, 0.032f, 0.82f));
            CreateText(commandPanel, "Production Label", "Production", 17, TextAnchor.MiddleLeft, new Vector2(14f, -26f), new Vector2(198f, 26f));

            float y = -68f;
            AddCommandButton(commandPanel, "Rifleman", y, () => game.PlayerCommands.QueueProduction(UnitKind.Rifleman), () => CanQueue(UnitKind.Rifleman), () => "Rifleman  " + RtsBalance.GetUnit(UnitKind.Rifleman).Cost);
            y -= 48f;
            AddCommandButton(commandPanel, "Harvester", y, () => game.PlayerCommands.QueueProduction(UnitKind.Harvester), () => CanQueue(UnitKind.Harvester), () => "Harvester  " + RtsBalance.GetUnit(UnitKind.Harvester).Cost);
            y -= 48f;
            AddCommandButton(commandPanel, "Tank", y, () => game.PlayerCommands.QueueProduction(UnitKind.Tank), () => CanQueue(UnitKind.Tank), () => "Tank  " + RtsBalance.GetUnit(UnitKind.Tank).Cost);
            y -= 62f;

            CreateText(commandPanel, "Build Label", "Build", 17, TextAnchor.MiddleLeft, new Vector2(14f, y), new Vector2(198f, 26f));
            y -= 42f;

            AddBuildButton(commandPanel, StructureKind.PowerPlant, y);
            y -= 48f;
            AddBuildButton(commandPanel, StructureKind.Barracks, y);
            y -= 48f;
            AddBuildButton(commandPanel, StructureKind.Refinery, y);
            y -= 48f;
            AddBuildButton(commandPanel, StructureKind.WarFactory, y);
            y -= 48f;
            AddBuildButton(commandPanel, StructureKind.Turret, y);
            y -= 62f;

            AddCommandButton(commandPanel, "Army", y, () => game.SelectCombatUnits(), () => game.AcceptsPlayerInput, () => "Select Army");
            y -= 48f;
            AddCommandButton(commandPanel, "Repair", y, () => game.PlayerCommands.RepairSelectedStructures(), () => game.AcceptsPlayerInput && game.CanRepairSelectedStructures(), () => "Repair  Z");
            y -= 48f;
            AddCommandButton(commandPanel, "Sell", y, () => game.PlayerCommands.SellSelectedStructures(), () => game.AcceptsPlayerInput && game.CanSellSelectedStructures(), () => "Sell  X");
            y -= 62f;

            CreateText(commandPanel, "System Label", "System", 17, TextAnchor.MiddleLeft, new Vector2(14f, y), new Vector2(198f, 26f));
            y -= 42f;

            AddCommandButton(commandPanel, "Pause", y, () => game.ToggleUserPause(), () => game.AcceptsSystemInput && !game.IsMatchOver, () => game.IsUserPaused ? "Resume  P" : "Pause  P");
            y -= 48f;
            AddCommandButton(commandPanel, "Save", y, () => game.TryManualSave(), () => game.AcceptsSystemInput && !game.IsMatchOver, () => "Save  F5");
            y -= 48f;
            AddCommandButton(commandPanel, "Load", y, () => game.TryManualLoad(), () => game.AcceptsSystemInput && game.CanLoadManualSave(), () => "Load  F9");

            RectTransform selectionPanel = CreatePanel(canvasObject.transform, "Selection", new Vector2(0f, 0f), new Vector2(0.58f, 0f), new Vector2(0f, 0f), new Vector2(12f, 12f), new Vector2(0f, 118f), new Color(0.02f, 0.024f, 0.026f, 0.8f));
            selectionText = CreateText(selectionPanel, "Selection Text", "", 17, TextAnchor.UpperLeft);
            selectionText.rectTransform.offsetMin = new Vector2(16f, 10f);
            selectionText.rectTransform.offsetMax = new Vector2(-16f, -10f);
        }

        private void AddBuildButton(RectTransform parent, StructureKind kind, float y)
        {
            StructureStats stats = RtsBalance.GetStructure(kind);
            AddCommandButton(parent, stats.Name, y, () => game.PlayerCommands.RequestConstruction(kind), () => CanBuild(kind), () => GetBuildButtonText(kind, stats));
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
