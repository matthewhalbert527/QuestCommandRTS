using UnityEngine;
using UnityEngine.UI;

namespace QuestCommandRTS
{
    public sealed class QuestWorldHud : MonoBehaviour
    {
        private const float RefreshInterval = 0.15f;

        private RtsGame game;
        private Text statusText;
        private float nextRefreshTime;
        private int lastCredits = int.MinValue;
        private int lastPowerProvided = int.MinValue;
        private int lastPowerUsed = int.MinValue;
        private int lastSelectionCount = int.MinValue;
        private bool lastPaused;
        private string lastStatusMessage = string.Empty;

        public void Initialize(RtsGame owner, Transform rigRoot, QuestTabletopSettings settings)
        {
            game = owner;

            GameObject canvasObject = new GameObject("Quest World Status");
            canvasObject.transform.SetParent(rigRoot, false);
            canvasObject.transform.localPosition = settings.StatusPanelLocalPositionMeters;
            canvasObject.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            canvasObject.transform.localScale = Vector3.one * 0.001f;

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 5;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = settings.StatusPanelSizeMeters * 1000f;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            GraphicRaycaster raycaster = canvasObject.AddComponent<GraphicRaycaster>();
            raycaster.enabled = false;

            Image background = canvasObject.AddComponent<Image>();
            background.color = new Color(0.018f, 0.028f, 0.032f, 0.86f);
            background.raycastTarget = false;

            GameObject textObject = new GameObject("Status Text");
            textObject.transform.SetParent(canvasObject.transform, false);
            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18f, 14f);
            textRect.offsetMax = new Vector2(-18f, -14f);

            statusText = textObject.AddComponent<Text>();
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = 22;
            statusText.alignment = TextAnchor.MiddleLeft;
            statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            statusText.verticalOverflow = VerticalWrapMode.Truncate;
            statusText.color = new Color(0.86f, 0.96f, 1f, 1f);
            statusText.raycastTarget = false;
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
        public Text StatusTextForTests => statusText;

        public void RefreshForTests(bool force)
        {
            Refresh(force);
        }
#endif

        private void Refresh(bool force)
        {
            if (game == null || game.Resources == null || statusText == null)
            {
                return;
            }

            int credits = game.Resources.Credits;
            int powerProvided = game.Resources.PowerProvided;
            int powerUsed = game.Resources.PowerUsed;
            int selectionCount = game.Selection.Count;
            bool paused = game.IsUserPaused;
            string status = game.StatusMessage;

            if (!force &&
                credits == lastCredits &&
                powerProvided == lastPowerProvided &&
                powerUsed == lastPowerUsed &&
                selectionCount == lastSelectionCount &&
                paused == lastPaused &&
                status == lastStatusMessage)
            {
                return;
            }

            lastCredits = credits;
            lastPowerProvided = powerProvided;
            lastPowerUsed = powerUsed;
            lastSelectionCount = selectionCount;
            lastPaused = paused;
            lastStatusMessage = status;

            statusText.text =
                "Credits " + credits +
                "   Power " + powerUsed + "/" + powerProvided +
                "   Selected " + selectionCount +
                "\n" + (paused ? "Paused" : status) +
                "\nTrigger: Select" +
                "\nLeft Trigger + Trigger: Add/Area" +
                "\nA: Command   B: Cancel/Clear" +
                "\nX: Command Console";
        }
    }
}
