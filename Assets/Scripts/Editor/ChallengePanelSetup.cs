using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Editor script to fix the Challenge Panel UI setup.
/// Run this via: Tools > LeetCode VR > Fix Challenge Panel
/// </summary>
public class ChallengePanelSetup : EditorWindow
{
    [MenuItem("Tools/LeetCode VR/Fix Challenge Panel")]
    public static void FixChallengePanel()
    {
        // Find the Challenge Panel Canvas
        Canvas challengeCanvas = FindChallengeCanvas();
        if (challengeCanvas == null)
        {
            Debug.LogError("ChallengePanelCanvas not found! Create a World Space Canvas named 'ChallengePanelCanvas' first.");
            return;
        }

        // Find or create PanelBackground
        Transform panelBackground = FindOrCreatePanelBackground(challengeCanvas.transform);

        // Get the TMP Font Asset and Material
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/LiberationSans SDF.asset");
        Material fontMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/TextMesh Pro/Fonts/LiberationSans SDF Material.mat");

        if (fontAsset == null)
        {
            // Try alternative paths
            fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            fontMaterial = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF Material");
        }

        if (fontAsset == null)
        {
            Debug.LogWarning("LiberationSans SDF font asset not found at expected paths. Using default.");
            fontAsset = TMP_Settings.defaultFontAsset;
        }

        if (fontMaterial == null && fontAsset != null)
        {
            fontMaterial = fontAsset.material;
        }

        // Fix the four missing UI elements
        FixTitleText(panelBackground, fontAsset, fontMaterial);
        FixPromptText(panelBackground, fontAsset, fontMaterial);
        FixStatusText(panelBackground, fontAsset, fontMaterial);
        FixValueInput(panelBackground, fontAsset, fontMaterial);

        // Configure Canvas settings
        ConfigureCanvas(challengeCanvas);

        // Wire up VRUIManager references
        WireVRUIManager(challengeCanvas);

        Debug.Log("Challenge Panel fix complete!");
        EditorUtility.SetDirty(challengeCanvas);
        SceneView.RepaintAll();
    }

    private static Canvas FindChallengeCanvas()
    {
        // Look for existing ChallengePanelCanvas
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            if (canvas.name == "ChallengePanelCanvas" || canvas.name == "ChallengePanel")
            {
                return canvas;
            }
        }

        // Look for ChallengePanelUIRoot
        Transform root = GameObject.Find("ChallengePanelUIRoot")?.transform;
        if (root != null)
        {
            return root.GetComponentInChildren<Canvas>();
        }

        return null;
    }

    private static Transform FindOrCreatePanelBackground(Transform canvasTransform)
    {
        Transform bg = canvasTransform.Find("PanelBackground");
        if (bg == null)
        {
            // Create PanelBackground
            GameObject bgObj = new GameObject("PanelBackground");
            bgObj.transform.SetParent(canvasTransform, false);
            bg = bgObj.transform;

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        }
        return bg;
    }

    private static void FixTitleText(Transform parent, TMP_FontAsset fontAsset, Material fontMaterial)
    {
        // Remove existing if misconfigured
        Transform existing = parent.Find("TitleText");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject obj = new GameObject("TitleText");
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, 210);
        rt.sizeDelta = new Vector2(1000, 60);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = "VR LEETCODE";
        tmp.fontSize = 42;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.enableWordWrapping = false;

        if (fontAsset != null) tmp.font = fontAsset;
        if (fontMaterial != null) tmp.fontSharedMaterial = fontMaterial;
    }

    private static void FixPromptText(Transform parent, TMP_FontAsset fontAsset, Material fontMaterial)
    {
        Transform existing = parent.Find("PromptText");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject obj = new GameObject("PromptText");
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, 130);
        rt.sizeDelta = new Vector2(1000, 110);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Insert Node in the Middle\n\nInput: 1 → 2 → 4 → 5\nInsert value: 3\nExpected output: 1 → 2 → 3 → 4 → 5";
        tmp.fontSize = 27;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;

        if (fontAsset != null) tmp.font = fontAsset;
        if (fontMaterial != null) tmp.fontSharedMaterial = fontMaterial;
    }

    private static void FixStatusText(Transform parent, TMP_FontAsset fontAsset, Material fontMaterial)
    {
        Transform existing = parent.Find("StatusText");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject obj = new GameObject("StatusText");
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, 65);
        rt.sizeDelta = new Vector2(1000, 40);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Status: Ready";
        tmp.fontSize = 23;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.enableWordWrapping = false;

        if (fontAsset != null) tmp.font = fontAsset;
        if (fontMaterial != null) tmp.fontSharedMaterial = fontMaterial;
    }

    private static void FixValueInput(Transform parent, TMP_FontAsset fontAsset, Material fontMaterial)
    {
        Transform existing = parent.Find("ValueInput");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject obj = new GameObject("ValueInput");
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, 0);
        rt.sizeDelta = new Vector2(250, 50);

        // Add Image for background
        Image bgImage = obj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        TMP_InputField inputField = obj.AddComponent<TMP_InputField>();
        inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.characterLimit = 6;
        inputField.interactable = true;

        // Create placeholder
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(obj.transform, false);
        TextMeshProUGUI placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholder.text = "Enter node value";
        placeholder.fontSize = 24;
        placeholder.alignment = TextAlignmentOptions.Center;
        placeholder.color = new Color(1, 1, 1, 0.5f);
        placeholder.enableWordWrapping = false;
        placeholder.raycastTarget = false;

        if (fontAsset != null) placeholder.font = fontAsset;
        if (fontMaterial != null) placeholder.fontSharedMaterial = fontMaterial;

        RectTransform phRect = placeholderObj.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = new Vector2(10, 0);
        phRect.offsetMax = new Vector2(-10, 0);

        inputField.placeholder = placeholder;

        // Create text component
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();
        textComp.text = "";
        textComp.fontSize = 24;
        textComp.alignment = TextAlignmentOptions.Center;
        textComp.color = Color.white;
        textComp.enableWordWrapping = false;
        textComp.raycastTarget = false;

        if (fontAsset != null) textComp.font = fontAsset;
        if (fontMaterial != null) textComp.fontSharedMaterial = fontMaterial;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-10, 0);

        inputField.textComponent = textComp;
    }

    private static void ConfigureCanvas(Canvas canvas)
    {
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform rt = canvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1100, 500);
        rt.localScale = new Vector3(0.0041f, 0.0041f, 0.0041f);

        canvas.sortingOrder = 5;

        // Find XR Camera for Event Camera
        Camera xrCamera = FindXRCamera();
        if (xrCamera != null)
        {
            canvas.worldCamera = xrCamera;
        }

        // Ensure GraphicRaycaster exists
        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private static Camera FindXRCamera()
    {
        // Try to find XR Camera
        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in cameras)
        {
            if (cam.name.Contains("XR") || cam.name.Contains("Main") || cam.name.Contains("CenterEye"))
            {
                return cam;
            }
        }
        return Camera.main;
    }

    private static void WireVRUIManager(Canvas challengeCanvas)
    {
        VRUIManager uiManager = Object.FindFirstObjectByType<VRUIManager>();
        if (uiManager == null)
        {
            Debug.LogWarning("VRUIManager not found in scene. Create a GameObject with VRUIManager component.");
            return;
        }

        // Use reflection to set private fields
        var type = typeof(VRUIManager);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        // Helper to find child by name
        Transform FindChild(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t != null) return t;
            foreach (Transform child in parent)
            {
                t = FindChild(child, name);
                if (t != null) return t;
            }
            return null;
        }

        // Find all buttons
        Transform panelBg = challengeCanvas.transform.Find("PanelBackground");
        if (panelBg == null) return;

        SetField(uiManager, "titleText", FindChild(panelBg, "TitleText")?.GetComponent<TextMeshProUGUI>());
        SetField(uiManager, "promptText", FindChild(panelBg, "PromptText")?.GetComponent<TextMeshProUGUI>());
        SetField(uiManager, "statusText", FindChild(panelBg, "StatusText")?.GetComponent<TextMeshProUGUI>());
        SetField(uiManager, "valueInput", FindChild(panelBg, "ValueInput")?.GetComponent<TMP_InputField>());
        SetField(uiManager, "insertButton", FindChild(panelBg, "InsertButton")?.GetComponent<Button>());
        SetField(uiManager, "deleteButton", FindChild(panelBg, "DeleteButton")?.GetComponent<Button>());
        SetField(uiManager, "editButton", FindChild(panelBg, "EditButton")?.GetComponent<Button>());
        SetField(uiManager, "connectButton", FindChild(panelBg, "ConnectButton")?.GetComponent<Button>());
        SetField(uiManager, "disconnectButton", FindChild(panelBg, "DisconnectButton")?.GetComponent<Button>());
        SetField(uiManager, "cancelConnectButton", FindChild(panelBg, "CancelConnectButton")?.GetComponent<Button>());
        SetField(uiManager, "validateButton", FindChild(panelBg, "ValidateButton")?.GetComponent<Button>());
        SetField(uiManager, "resetButton", FindChild(panelBg, "ResetButton")?.GetComponent<Button>());
        SetField(uiManager, "addButton", FindChild(panelBg, "AddButton")?.GetComponent<Button>());
        SetField(uiManager, "increaseButton", FindChild(panelBg, "IncreaseButton")?.GetComponent<Button>());
        SetField(uiManager, "decreaseButton", FindChild(panelBg, "DecreaseButton")?.GetComponent<Button>());
        SetField(uiManager, "challengeCanvas", challengeCanvas);

        // Legacy Runtime Font field: leave null — it expects UnityEngine.Font (legacy UI.Text)
        // Our UI uses TextMeshProUGUI which uses TMP_FontAsset directly, not legacy Font
        var legacyFontField = type.GetField("legacyRuntimeFont", flags);
        if (legacyFontField != null)
        {
            legacyFontField.SetValue(uiManager, null);
        }

        EditorUtility.SetDirty(uiManager);
        Debug.Log("VRUIManager references wired up (TMP components).");
    }

    private static void SetField(object obj, string fieldName, object value)
    {
        if (value == null) return;

        var type = obj.GetType();
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
        var field = type.GetField(fieldName, flags);

        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }
}