using UnityEngine;
using UnityEngine.UI;

/// Creates two warm, player-facing world-space information screens.
public sealed class ThemeScreens : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Vector3 leftPosition = new Vector3(-3.35f, 1.75f, 0.85f);
    [SerializeField] private Vector3 rightPosition = new Vector3(3.35f, 1.75f, 0.85f);
    [SerializeField, Min(0.0005f)] private float worldScale = 0.0024f;
    private Transform leftScreen;
    private Transform rightScreen;

    private void Start()
    {
        if (player == null && Camera.main != null) player = Camera.main.transform;
        leftScreen = CreateScreen("Challenge Screen 1", leftPosition, -45f, true);
        rightScreen = CreateScreen("Challenge Screen 2", rightPosition, 45f, false);
    }

    private void LateUpdate()
    {
        if (player == null && Camera.main != null) player = Camera.main.transform;
        if (player == null) return;
        FacePlayer(leftScreen, -45f);
        FacePlayer(rightScreen, 45f);
    }

    private Transform CreateScreen(string name, Vector3 position, float tilt, bool showChallenge)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.position = position;
        root.transform.localScale = Vector3.one * worldScale;
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 5;
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(760f, 440f);
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 8f;

        Image panel = CreateImage(root.transform, "Warm Panel", new Color(0.055f, 0.025f, 0.014f, 0.96f));
        Stretch(panel.rectTransform, 0f, 0f, 0f, 0f);
        panel.raycastTarget = false;
        Image accent = CreateImage(root.transform, "Sunset Accent", new Color(1f, 0.48f, 0.20f, 0.95f));
        SetRect(accent.rectTransform, 24f, -40f, -24f, -28f, true);
        accent.raycastTarget = false;

        if (showChallenge)
        {
            Text title = CreateText(root.transform, "Title", "VR LEETCODE  •  LINKED LIST", 27, new Color(1f, 0.78f, 0.48f));
            SetRect(title.rectTransform, 44f, -80f, -42f, -28f, true);
            title.fontStyle = FontStyle.Bold;

            Text body = CreateText(root.transform, "Question", 
                "INSERT A NODE IN THE MIDDLE\n\n" +
                "Input:  [1, 2, 4, 5]\n" +
                "Value:  3\n" +
                "Position:  2\n\n" +
                "Expected output:  [1, 2, 3, 4, 5]",
                22, new Color(1f, 0.92f, 0.78f));
            SetRect(body.rectTransform, 48f, -400f, -48f, -105f, false);
            body.fontStyle = FontStyle.Normal;
        }

        FacePlayer(root.transform, tilt);
        return root.transform;
    }

    private void FacePlayer(Transform screen, float tilt)
    {
        if (screen == null || player == null) return;
        Vector3 direction = screen.position - player.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;
        screen.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up) * Quaternion.Euler(0f, tilt, 0f);
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(Transform parent, string name, string content, int size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }

    private static void SetRect(RectTransform rect, float left, float bottom, float right, float top, bool anchoredTop)
    {
        rect.anchorMin = anchoredTop ? new Vector2(0f, 1f) : new Vector2(0f, 1f);
        rect.anchorMax = anchoredTop ? new Vector2(1f, 1f) : new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }
}
