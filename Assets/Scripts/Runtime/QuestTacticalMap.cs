using UnityEngine;
using UnityEngine.UI;

namespace QuestCommandRTS
{
    [DisallowMultipleComponent]
    public sealed class QuestTacticalMap : MonoBehaviour
    {
        public const int MaxEntityPips = 128;
        public const int MaxResourcePips = 80;

        private const float RefreshInterval = 0.2f;
        private const float PanelPadding = 18f;
        private const float HeaderHeight = 42f;
        private const float FooterHeight = 28f;

        private RtsGame game;
        private GameObject canvasObject;
        private RectTransform panelRect;
        private RectTransform mapRect;
        private RectTransform entityLayer;
        private RectTransform resourceLayer;
        private RectTransform[] entityPips;
        private RectTransform[] resourcePips;
        private Image[] entityPipImages;
        private Image[] resourcePipImages;
        private Text footerText;
        private float nextRefreshTime;
        private int visibleEntityPips;
        private int visibleResourcePips;

        public void Initialize(RtsGame owner, Transform rigRoot, QuestTabletopSettings settings)
        {
            game = owner;
            BuildCanvas(rigRoot, settings);
            Refresh(true);
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextRefreshTime)
            {
                nextRefreshTime = Time.unscaledTime + RefreshInterval;
                Refresh(false);
            }
        }

#if UNITY_EDITOR
        public RectTransform PanelRectForTests => panelRect;
        public RectTransform MapRectForTests => mapRect;
        public int EntityPipCapacityForTests => entityPips != null ? entityPips.Length : 0;
        public int ResourcePipCapacityForTests => resourcePips != null ? resourcePips.Length : 0;
        public int VisibleEntityPipCountForTests => visibleEntityPips;
        public int VisibleResourcePipCountForTests => visibleResourcePips;

        public void RefreshForTests(bool force)
        {
            Refresh(force);
        }
#endif

        private void BuildCanvas(Transform rigRoot, QuestTabletopSettings settings)
        {
            canvasObject = new GameObject("Quest Tactical Map");
            canvasObject.transform.SetParent(rigRoot, false);
            canvasObject.transform.localPosition = settings.TacticalMapLocalPositionMeters;
            canvasObject.transform.localRotation = Quaternion.Euler(12f, -10f, 0f);
            canvasObject.transform.localScale = Vector3.one * 0.001f;

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 7;

            panelRect = canvasObject.GetComponent<RectTransform>();
            panelRect.sizeDelta = settings.TacticalMapSizeMeters * 1000f;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 12f;

            GraphicRaycaster raycaster = canvasObject.AddComponent<GraphicRaycaster>();
            raycaster.enabled = false;

            Image background = canvasObject.AddComponent<Image>();
            background.color = new Color(0.008f, 0.02f, 0.025f, 0.86f);
            background.raycastTarget = false;

            Vector2 panelSize = panelRect.sizeDelta;
            CreatePanelImage(panelRect, "Tactical Map Top Glow", new Color(0.32f, 0.86f, 1f, 0.72f), new Vector2(0f, 0f), new Vector2(panelSize.x, 3f));
            CreatePanelImage(panelRect, "Tactical Map Bottom Glow", new Color(0.18f, 0.62f, 0.82f, 0.52f), new Vector2(0f, -panelSize.y + 3f), new Vector2(panelSize.x, 3f));
            CreatePanelImage(panelRect, "Tactical Map Left Glow", new Color(0.24f, 0.78f, 0.96f, 0.46f), new Vector2(0f, 0f), new Vector2(3f, panelSize.y));
            CreatePanelImage(panelRect, "Tactical Map Right Glow", new Color(0.16f, 0.62f, 0.78f, 0.42f), new Vector2(panelSize.x - 3f, 0f), new Vector2(3f, panelSize.y));

            CreateText(panelRect, "Tactical Map Header", "TACTICAL MAP", 20, TextAnchor.MiddleLeft, new Vector2(18f, -9f), new Vector2(panelSize.x - 36f, 32f));

            float mapSize = Mathf.Max(120f, Mathf.Min(panelSize.x - PanelPadding * 2f, panelSize.y - HeaderHeight - FooterHeight - PanelPadding));
            float mapX = (panelSize.x - mapSize) * 0.5f;
            float mapY = -HeaderHeight;
            Image plotBackground = CreatePanelImage(panelRect, "Tactical Map Plot Area", new Color(0.018f, 0.042f, 0.045f, 0.9f), new Vector2(mapX, mapY), new Vector2(mapSize, mapSize));
            plotBackground.color = new Color(0.018f, 0.042f, 0.045f, 0.9f);
            mapRect = plotBackground.rectTransform;

            BuildGrid(mapSize);

            resourceLayer = CreateLayer("Tactical Map Resource Layer");
            entityLayer = CreateLayer("Tactical Map Entity Layer");

            footerText = CreateText(panelRect, "Tactical Map Footer", "ACTIVE BATTLE VIEW", 14, TextAnchor.MiddleCenter, new Vector2(18f, -HeaderHeight - mapSize - 6f), new Vector2(panelSize.x - 36f, 24f));

            CreatePipPools();
        }

        private void BuildGrid(float mapSize)
        {
            for (int i = 1; i < 4; i++)
            {
                float offset = mapSize * i / 4f;
                CreatePanelImage(mapRect, "Tactical Map Grid V" + i, new Color(0.18f, 0.52f, 0.58f, 0.22f), new Vector2(offset, 0f), new Vector2(1f, mapSize));
                CreatePanelImage(mapRect, "Tactical Map Grid H" + i, new Color(0.18f, 0.52f, 0.58f, 0.22f), new Vector2(0f, -offset), new Vector2(mapSize, 1f));
            }

            CreatePanelImage(mapRect, "Tactical Map Center Line V", new Color(0.32f, 0.92f, 1f, 0.18f), new Vector2(mapSize * 0.5f, 0f), new Vector2(2f, mapSize));
            CreatePanelImage(mapRect, "Tactical Map Center Line H", new Color(0.32f, 0.92f, 1f, 0.18f), new Vector2(0f, -mapSize * 0.5f), new Vector2(mapSize, 2f));
        }

        private RectTransform CreateLayer(string layerName)
        {
            GameObject layerObject = new GameObject(layerName);
            layerObject.transform.SetParent(mapRect, false);
            RectTransform rect = layerObject.AddComponent<RectTransform>();
            SetTopLeft(rect, Vector2.zero, mapRect.sizeDelta);
            return rect;
        }

        private void CreatePipPools()
        {
            entityPips = new RectTransform[MaxEntityPips];
            entityPipImages = new Image[MaxEntityPips];
            for (int i = 0; i < entityPips.Length; i++)
            {
                Image image = CreatePip(entityLayer, "Entity Pip " + i, new Vector2(5f, 5f));
                entityPips[i] = image.rectTransform;
                entityPipImages[i] = image;
                image.gameObject.SetActive(false);
            }

            resourcePips = new RectTransform[MaxResourcePips];
            resourcePipImages = new Image[MaxResourcePips];
            for (int i = 0; i < resourcePips.Length; i++)
            {
                Image image = CreatePip(resourceLayer, "Resource Pip " + i, new Vector2(4f, 4f));
                resourcePips[i] = image.rectTransform;
                resourcePipImages[i] = image;
                image.gameObject.SetActive(false);
            }
        }

        private Image CreatePip(RectTransform parent, string pipName, Vector2 size)
        {
            GameObject pipObject = new GameObject(pipName);
            pipObject.transform.SetParent(parent, false);
            RectTransform rect = pipObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            Image image = pipObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = Color.white;
            return image;
        }

        private Text CreateText(RectTransform parent, string name, string value, int size, TextAnchor anchor, Vector2 position, Vector2 dimensions)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            SetTopLeft(rect, position, dimensions);

            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.color = new Color(0.84f, 0.96f, 1f, 0.95f);
            text.raycastTarget = false;
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
            image.raycastTarget = false;
            return image;
        }

        private void Refresh(bool force)
        {
            if (game == null || mapRect == null || entityPips == null || resourcePips == null)
            {
                return;
            }

            RefreshResourcePips();
            RefreshEntityPips();

            if (force && footerText != null)
            {
                footerText.text = "ACTIVE BATTLE VIEW";
            }
        }

        private void RefreshResourcePips()
        {
            visibleResourcePips = 0;
            for (int i = 0; i < game.ResourceNodes.Count && visibleResourcePips < resourcePips.Length; i++)
            {
                ResourceNode node = game.ResourceNodes[i];
                if (node == null || node.IsDepleted)
                {
                    continue;
                }

                RectTransform pip = resourcePips[visibleResourcePips];
                Image image = resourcePipImages[visibleResourcePips];
                pip.anchoredPosition = WorldToMapPosition(node.transform.position);
                pip.sizeDelta = new Vector2(4f, 4f);
                image.color = new Color(0.2f, 1f, 0.48f, 0.95f);
                pip.gameObject.SetActive(true);
                visibleResourcePips++;
            }

            HideRemaining(resourcePips, visibleResourcePips);
        }

        private void RefreshEntityPips()
        {
            visibleEntityPips = 0;
            for (int i = 0; i < game.Entities.Count && visibleEntityPips < entityPips.Length; i++)
            {
                RtsEntity entity = game.Entities[i];
                if (entity == null || !entity.IsAlive || entity.Team == RtsTeam.Neutral || !game.IsEntityVisible(entity))
                {
                    continue;
                }

                RectTransform pip = entityPips[visibleEntityPips];
                Image image = entityPipImages[visibleEntityPips];
                float size = entity is RtsStructure ? 7f : 5f;
                pip.anchoredPosition = WorldToMapPosition(entity.GroundPosition);
                pip.sizeDelta = new Vector2(size, size);
                image.color = GetPipColor(entity);
                pip.gameObject.SetActive(true);
                visibleEntityPips++;
            }

            HideRemaining(entityPips, visibleEntityPips);
        }

        private Vector2 WorldToMapPosition(Vector3 worldPosition)
        {
            Rect rect = mapRect.rect;
            float normalizedX = Mathf.InverseLerp(-RtsBalance.MapHalfSize, RtsBalance.MapHalfSize, worldPosition.x);
            float normalizedY = Mathf.InverseLerp(-RtsBalance.MapHalfSize, RtsBalance.MapHalfSize, worldPosition.z);
            float x = Mathf.Lerp(rect.xMin, rect.xMax, normalizedX);
            float y = Mathf.Lerp(rect.yMin, rect.yMax, normalizedY);
            return new Vector2(x, y);
        }

        private static Color GetPipColor(RtsEntity entity)
        {
            if (entity.IsSelected)
            {
                return new Color(0.65f, 1f, 0.86f, 1f);
            }

            Color teamColor = RtsBalance.TeamColor(entity.Team);
            teamColor.a = entity.Team == RtsTeam.Enemy ? 0.95f : 1f;
            return teamColor;
        }

        private static void HideRemaining(RectTransform[] pips, int startIndex)
        {
            for (int i = startIndex; i < pips.Length; i++)
            {
                if (pips[i] != null)
                {
                    pips[i].gameObject.SetActive(false);
                }
            }
        }

        private static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
