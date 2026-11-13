using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Controls the animated insertion visualization on the animation panel.
/// Demonstrates the step-by-step insertion of a node into a linked list.
/// Redesigned to match reference image: dark translucent panel, compact layout, 7 buttons.
/// </summary>
public sealed class AnimationPanelController : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private Transform animationRoot;      // 3D nodes/arrows root (not a Canvas)
    [SerializeField] private GameObject nodePrefab;        // Optional: prefab matching table nodes
    [SerializeField] private GameObject arrowPrefab;       // Optional: prefab for arrows

    [Header("Animation Controls (auto-created if missing)")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button nextStepButton;
    [SerializeField] private Button prevStepButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button hintButton;
    [SerializeField] private TMP_Text stepDescriptionText;
    [SerializeField] private TMP_Text moduleLabelText;
    [SerializeField] private TMP_Text tutorialTitleText;
    [SerializeField] private TMP_Text explanationText;

    [Header("Visual Settings")]
    [SerializeField] private float nodeSpacing = 0.4f;  // Tight spacing for ultra-compact panel
    [SerializeField] private float animationDuration = 1.0f;
    [SerializeField] private Color nodeColor = new Color(0.12f, 0.55f, 0.95f);
    [SerializeField] private Color highlightColor = new Color(1f, 0.62f, 0.12f);
    [SerializeField] private Color arrowColor = Color.white;
    [SerializeField] private Color headerColor = new Color(0.9f, 0.9f, 0.95f);
    [SerializeField] private Color panelBackgroundColor = new Color(0.09f, 0.095f, 0.11f, 0.8f); // #17181D ~80% opacity

    [Header("Button Colors")]
    [SerializeField] private Color primaryButtonColor = new Color(0.08f, 0.12f, 0.25f, 0.95f);  // Dark navy blue
    [SerializeField] private Color primaryButtonHighlight = new Color(0.12f, 0.18f, 0.35f, 0.95f);
    [SerializeField] private Color primaryButtonPressed = new Color(0.05f, 0.08f, 0.18f, 0.95f);
    [SerializeField] private Color secondaryButtonColor = new Color(0.18f, 0.18f, 0.22f, 0.95f); // Dark grey/slate
    [SerializeField] private Color secondaryButtonHighlight = new Color(0.25f, 0.25f, 0.3f, 0.95f);
    [SerializeField] private Color secondaryButtonPressed = new Color(0.12f, 0.12f, 0.15f, 0.95f);
    [SerializeField] private Color buttonTextColor = Color.white;

    [Header("Panel Dimensions")]
    [SerializeField] private float panelWidth = 3f;   // Ultra-compact width
    [SerializeField] private float panelHeight = 2f;  // Compact height
    [SerializeField] private float panelDepth = 0.1f;

    [Header("Node Visual Settings")]
    [SerializeField] private float nodeRadius = 0.12f;    // Small nodes for compact panel
    [SerializeField] private float nodeHeight = 0.05f;    // Thin nodes
    [SerializeField] private float nodeLabelSize = 0.07f; // Readable label

    [Header("Auto-setup")]
    [SerializeField] private bool autoCreateUI = true;

    // Animation state - 5 steps matching the spec
    public enum AnimationStep
    {
        InitialState = 0,        // 1→2→4→5, node 3 hidden
        CreateSpace = 1,         // Nodes 4&5 shift right, gap created
        ShowNode3 = 2,           // Node 3 appears in gap (fade/scale in)
        CreateArrow3to4 = 3,     // Arrow 3→4 appears
        CreateArrow2to3 = 4,     // Arrow 2→3 appears, old 2→4 removed
        Count = 5
    }

    private AnimationStep currentStep = AnimationStep.InitialState;
    private bool isPlaying = false;
    private Coroutine animationCoroutine;

    // Visual objects (3D, under animationRoot)
    private readonly List<GameObject> animationNodes = new List<GameObject>();
    private readonly List<GameObject> animationArrows = new List<GameObject>();
    private readonly List<Vector3> initialNodePositions = new List<Vector3>();  // Original positions of 1,2,4,5
    private readonly List<Vector3> targetNodePositions = new List<Vector3>();   // Final positions of 1,2,3,4,5
    private GameObject detachedNode3;
    private GameObject arrow3to4;
    private GameObject arrow2to3;
    private GameObject oldArrow2to4;  // Reference to arrow between 2→4 for removal

    // UI objects (Canvas-based)
    private Canvas uiCanvas;
    private GameObject panelBackground;

    // Step descriptions matching spec - concise for compact layout
    private readonly string[] stepDescriptions = new string[]
    {
        "1 → 2 → 4 → 5",
        "Moving 4 & 5 to create space",
        "Node 3 appears in gap",
        "Connecting 3 → 4",
        "Connecting 2 → 3. Done: 1 → 2 → 3 → 4 → 5"
    };

    private void Awake()
    {
        if (autoCreateUI)
        {
            EnsurePanelSetup();
        }
        BindControls();
        SetupAnimation();
    }

    /// <summary>
    /// Set up the panel: create background mesh, World Space Canvas for UI, organize layout into 4 areas.
    /// The 3D visualization stays under animationRoot (not on Canvas).
    /// </summary>
    private void EnsurePanelSetup()
    {
        // Ensure animationRoot exists (for 3D nodes/arrows)
        if (animationRoot == null)
        {
            GameObject rootObj = new GameObject("AnimationRoot");
            rootObj.transform.SetParent(transform, false);
            rootObj.transform.localPosition = Vector3.zero;
            animationRoot = rootObj.transform;
        }

        // Create background panel mesh (not a Canvas - just a 3D quad for visual background)
        CreatePanelBackground();

        // Create World Space Canvas for UI (header, step desc, controls)
        if (uiCanvas == null)
        {
            uiCanvas = GetComponentInChildren<Canvas>();
        }
        if (uiCanvas == null)
        {
            GameObject canvasObj = new GameObject("AnimationUICanvas");
            canvasObj.transform.SetParent(transform, false);
            canvasObj.layer = LayerMask.NameToLayer("UI");

            uiCanvas = canvasObj.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.WorldSpace;
            // Canvas size in world units - matches panel background
            // Use higher resolution: 200 pixels per world unit for crisp text
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(panelWidth * 200f, panelHeight * 200f);
            canvasObj.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f); // 1 world unit = 200 pixels
            // Position canvas IN FRONT of background (positive z)
            canvasObj.transform.localPosition = new Vector3(0, 0, panelDepth * 0.5f + 0.01f);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Build the layout on the Canvas
        CreateLayout();
    }

    /// <summary>
    /// Create a 3D background panel mesh for the animation panel - dark charcoal translucent
    /// </summary>
    private void CreatePanelBackground()
    {
        if (panelBackground != null) return;

        panelBackground = GameObject.CreatePrimitive(PrimitiveType.Quad);
        panelBackground.name = "PanelBackground";
        panelBackground.transform.SetParent(transform, false);
        // Position BEHIND animationRoot (nodes at z=0) and use AlphaTest queue (2500)
        // so it renders AFTER opaque nodes (2000) but BEFORE transparent UI Canvas (3000+)
        panelBackground.transform.localPosition = new Vector3(0, 0, -0.1f);
        panelBackground.transform.localRotation = Quaternion.Euler(0, 0, 0);
        panelBackground.transform.localScale = new Vector3(panelWidth, panelHeight, 1f);
        panelBackground.layer = LayerMask.NameToLayer("Default");

        // Remove collider if present (we don't want it blocking rays)
        Collider col = panelBackground.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Set material - dark charcoal translucent
        Renderer renderer = panelBackground.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", panelBackgroundColor);
            mat.SetColor("_Color", panelBackgroundColor);
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 1);
            mat.SetFloat("_SrcBlend", 5); // SrcAlpha
            mat.SetFloat("_DstBlend", 10); // OneMinusSrcAlpha
            mat.SetFloat("_ZWrite", 0);
            mat.renderQueue = 2500; // Renders after opaque (2000), before transparent UI (3000+)
            renderer.sharedMaterial = mat;
        }
    }

    /// <summary>
    /// Create the layout: Header (top-left), Visualization Space, Step Description, Primary Buttons, Secondary Buttons
    /// </summary>
    private void CreateLayout()
    {
        if (uiCanvas == null) return;

        // Main container with vertical layout
        GameObject container = new GameObject("LayoutContainer");
        container.transform.SetParent(uiCanvas.transform, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = new Vector2(10, 8);
        containerRect.offsetMax = new Vector2(-10, -8);

        VerticalLayoutGroup vLayout = container.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 3;
        vLayout.padding = new RectOffset(6, 6, 6, 6);
        vLayout.childAlignment = TextAnchor.UpperLeft;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = true;
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;

        // ===== AREA A: HEADER (left-aligned) =====
        GameObject headerArea = new GameObject("HeaderArea");
        headerArea.transform.SetParent(container.transform, false);
        LayoutElement headerLayout = headerArea.AddComponent<LayoutElement>();
        headerLayout.minHeight = 28;
        headerLayout.preferredHeight = 28;
        headerLayout.flexibleWidth = 1;

        VerticalLayoutGroup headerLayoutGroup = headerArea.AddComponent<VerticalLayoutGroup>();
        headerLayoutGroup.spacing = 0;
        headerLayoutGroup.childAlignment = TextAnchor.UpperLeft;
        headerLayoutGroup.childControlWidth = true;
        headerLayoutGroup.childControlHeight = true;
        headerLayoutGroup.childForceExpandWidth = false;

        // MODULE 1 - small, light grey
        moduleLabelText = CreateText(headerArea.transform, "ModuleLabel", "MODULE 1", 8, FontStyles.Normal, new Color(0.7f, 0.7f, 0.8f));
        LayoutElement moduleLayout = moduleLabelText.gameObject.AddComponent<LayoutElement>();
        moduleLayout.minHeight = 10;
        moduleLayout.preferredHeight = 10;

        // Tutorial - bold, white
        tutorialTitleText = CreateText(headerArea.transform, "TutorialTitle", "Tutorial", 12, FontStyles.Bold, Color.white);
        LayoutElement titleLayout = tutorialTitleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.minHeight = 16;
        titleLayout.preferredHeight = 16;

        // Explanation - white/light grey, smaller
        explanationText = CreateText(headerArea.transform, "ExplanationText", "Insert node 3 between nodes 2 and 4.", 7, FontStyles.Normal, new Color(0.9f, 0.9f, 0.95f));
        LayoutElement explLayout = explanationText.gameObject.AddComponent<LayoutElement>();
        explLayout.minHeight = 10;
        explLayout.preferredHeight = 10;

        // ===== AREA B: VISUALIZATION SPACE (compact, ~20-25% panel height) =====
        // This is a placeholder area - the actual 3D nodes are under animationRoot (not on Canvas)
        // Keep it minimal - just enough vertical space for nodes to be visible
        GameObject vizSpace = new GameObject("VisualizationSpace");
        vizSpace.transform.SetParent(container.transform, false);
        LayoutElement vizLayout = vizSpace.AddComponent<LayoutElement>();
        vizLayout.minHeight = 20;   // Minimal space
        vizLayout.preferredHeight = 24;
        vizLayout.flexibleWidth = 1;
        vizLayout.flexibleHeight = 0;

        // ===== AREA C: STEP DESCRIPTION =====
        GameObject stepDescArea = new GameObject("StepDescriptionArea");
        stepDescArea.transform.SetParent(container.transform, false);
        LayoutElement descLayout = stepDescArea.AddComponent<LayoutElement>();
        descLayout.minHeight = 16;
        descLayout.preferredHeight = 18;
        descLayout.flexibleWidth = 1;

        GameObject textObj = new GameObject("StepDescriptionText");
        textObj.transform.SetParent(stepDescArea.transform, false);
        stepDescriptionText = textObj.AddComponent<TextMeshProUGUI>();
        stepDescriptionText.fontSize = 9;
        stepDescriptionText.alignment = TextAlignmentOptions.Center;
        stepDescriptionText.color = new Color(0.9f, 0.9f, 0.95f);
        stepDescriptionText.textWrappingMode = TextWrappingModes.Normal;
        stepDescriptionText.text = stepDescriptions[0];
        stepDescriptionText.overflowMode = TextOverflowModes.Overflow;
        stepDescriptionText.enableWordWrapping = true;

        LayoutElement textLayout = textObj.AddComponent<LayoutElement>();
        textLayout.minHeight = 20;
        textLayout.flexibleWidth = 1;

        // ===== AREA D: PRIMARY BUTTON ROW (Play, Restart, Previous, Next) =====
        GameObject primaryControlsArea = new GameObject("PrimaryControlsArea");
        primaryControlsArea.transform.SetParent(container.transform, false);
        LayoutElement primaryControlsLayout = primaryControlsArea.AddComponent<LayoutElement>();
        primaryControlsLayout.minHeight = 22;
        primaryControlsLayout.preferredHeight = 22;
        primaryControlsLayout.flexibleWidth = 1;

        HorizontalLayoutGroup primaryHLayout = primaryControlsArea.AddComponent<HorizontalLayoutGroup>();
        primaryHLayout.spacing = 4;
        primaryHLayout.padding = new RectOffset(4, 4, 2, 2);
        primaryHLayout.childAlignment = TextAnchor.MiddleCenter;
        primaryHLayout.childControlWidth = true;
        primaryHLayout.childControlHeight = true;
        primaryHLayout.childForceExpandWidth = true;

        // Order: Play, Restart, Previous, Next
        playButton = CreateButton(primaryControlsArea.transform, "Play", "Play", Play, true);
        restartButton = CreateButton(primaryControlsArea.transform, "Restart", "Restart", Restart, true);
        prevStepButton = CreateButton(primaryControlsArea.transform, "Previous", "Prev", PrevStep, true);
        nextStepButton = CreateButton(primaryControlsArea.transform, "Next", "Next", NextStep, true);

        // ===== AREA E: SECONDARY BUTTON ROW (Back, Replay, Hint) =====
        GameObject secondaryControlsArea = new GameObject("SecondaryControlsArea");
        secondaryControlsArea.transform.SetParent(container.transform, false);
        LayoutElement secondaryControlsLayout = secondaryControlsArea.AddComponent<LayoutElement>();
        secondaryControlsLayout.minHeight = 20;
        secondaryControlsLayout.preferredHeight = 20;
        secondaryControlsLayout.flexibleWidth = 1;

        HorizontalLayoutGroup secondaryHLayout = secondaryControlsArea.AddComponent<HorizontalLayoutGroup>();
        secondaryHLayout.spacing = 4;
        secondaryHLayout.padding = new RectOffset(4, 4, 2, 2);
        secondaryHLayout.childAlignment = TextAnchor.MiddleCenter;
        secondaryHLayout.childControlWidth = true;
        secondaryHLayout.childControlHeight = true;
        secondaryHLayout.childForceExpandWidth = true;

        backButton = CreateButton(secondaryControlsArea.transform, "Back", "Back", Back, false);
        replayButton = CreateButton(secondaryControlsArea.transform, "Replay", "Replay", Replay, false);
        hintButton = CreateButton(secondaryControlsArea.transform, "Hint", "Hint", ShowHint, false);
    }

    private TMP_Text CreateText(Transform parent, string name, string text, int fontSize, FontStyles style, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    private Button CreateButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick, bool isPrimary)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = isPrimary ? primaryButtonColor : secondaryButtonColor;
        btnImg.sprite = null;

        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
        btnText.text = label;
        btnText.fontSize = 7;  // Uniform smaller font
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = buttonTextColor;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;

        // Ultra-compact button sizing
        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        if (isPrimary)
        {
            le.minWidth = 40;
            le.minHeight = 18;
            le.preferredWidth = 44;
            le.preferredHeight = 18;
        }
        else
        {
            le.minWidth = 36;
            le.minHeight = 16;
            le.preferredWidth = 40;
            le.preferredHeight = 16;
        }

        // Hover/pressed colors via ColorBlock - unified slate theme
        ColorBlock cb = btn.colors;
        Color baseColor = isPrimary ? primaryButtonColor : secondaryButtonColor;
        Color highlightCol = isPrimary ? primaryButtonHighlight : secondaryButtonHighlight;
        Color pressedCol = isPrimary ? primaryButtonPressed : secondaryButtonPressed;
        cb.normalColor = baseColor;
        cb.highlightedColor = highlightCol;
        cb.pressedColor = pressedCol;
        cb.selectedColor = highlightCol;
        cb.disabledColor = new Color(0.15f, 0.15f, 0.18f, 0.5f);
        cb.colorMultiplier = 1f;
        btn.colors = cb;

        // Add collider for XR raycasting - match button size
        BoxCollider box = btnObj.AddComponent<BoxCollider>();
        float colliderWidth = isPrimary ? 50f : 46f;
        float colliderHeight = isPrimary ? 22f : 20f;
        box.size = new Vector3(colliderWidth, colliderHeight, 0.1f);

        return btn;
    }

    private void OnDestroy()
    {
        UnbindControls();
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        CleanupAnimationObjects();
    }

    private void BindControls()
    {
        if (playButton != null) playButton.onClick.AddListener(Play);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (nextStepButton != null) nextStepButton.onClick.AddListener(NextStep);
        if (prevStepButton != null) prevStepButton.onClick.AddListener(PrevStep);
        if (backButton != null) backButton.onClick.AddListener(Back);
        if (replayButton != null) replayButton.onClick.AddListener(Replay);
        if (hintButton != null) hintButton.onClick.AddListener(ShowHint);
    }

    private void UnbindControls()
    {
        if (playButton != null) playButton.onClick.RemoveListener(Play);
        if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
        if (nextStepButton != null) nextStepButton.onClick.RemoveListener(NextStep);
        if (prevStepButton != null) prevStepButton.onClick.RemoveListener(PrevStep);
        if (backButton != null) backButton.onClick.RemoveListener(Back);
        if (replayButton != null) replayButton.onClick.RemoveListener(Replay);
        if (hintButton != null) hintButton.onClick.RemoveListener(ShowHint);
    }

    /// <summary>
    /// Setup the 3D animation visual objects under animationRoot.
    /// Initial state: 1 → 2 → 4 → 5 with node 3 hidden (not created yet).
    /// </summary>
    private void SetupAnimation()
    {
        if (animationRoot == null)
        {
            Debug.LogError("AnimationPanelController: animationRoot not assigned!");
            return;
        }

        CleanupAnimationObjects();

        // Calculate positions for 5 nodes evenly spaced with compact spacing
        float totalWidth = nodeSpacing * 4;  // 4 gaps between 5 nodes
        float startX = -totalWidth * 0.5f;

        // Final target positions for all 5 nodes: 1, 2, 3, 4, 5
        targetNodePositions.Clear();
        for (int i = 0; i < 5; i++)
        {
            targetNodePositions.Add(new Vector3(startX + i * nodeSpacing, 0, 0));
        }

        // Initial positions for 1, 2, 4, 5 (node 3 not in list yet)
        // Nodes 1,2 at final positions 0,1. Nodes 4,5 initially at positions 2,3 (compact)
        initialNodePositions.Clear();
        initialNodePositions.Add(targetNodePositions[0]);  // Node 1 at index 0
        initialNodePositions.Add(targetNodePositions[1]);  // Node 2 at index 1
        initialNodePositions.Add(targetNodePositions[2]);  // Node 4 initially at index 2 (where 3 will be)
        initialNodePositions.Add(targetNodePositions[3]);  // Node 5 initially at index 3

        // Create initial nodes: 1, 2, 4, 5 at initial positions
        int[] initialValues = { 1, 2, 4, 5 };
        for (int i = 0; i < initialValues.Length; i++)
        {
            GameObject node = CreateNode(initialValues[i], initialNodePositions[i]);
            animationNodes.Add(node);
        }

        // Create arrows between initial nodes: 1→2, 2→4, 4→5
        for (int i = 0; i < 3; i++)
        {
            GameObject arrow = CreateArrow(initialNodePositions[i], initialNodePositions[i + 1]);
            animationArrows.Add(arrow);
        }
        oldArrow2to4 = animationArrows[1];  // Arrow from 2→4 (will be removed in step 5)

        // Node 3 is NOT created yet - it will be created and shown in Step 3
        detachedNode3 = null;

        UpdateStepDisplay();
        UpdateButtonStates();
    }

    private GameObject CreateNode(int value, Vector3 position)
    {
        GameObject node;

        if (nodePrefab != null)
        {
            node = Instantiate(nodePrefab, animationRoot);
        }
        else
        {
            // Create a cylinder node programmatically - smaller to match compact design
            node = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            node.transform.SetParent(animationRoot, false);

            // Scale to smaller size: radius 0.12, height 0.05
            node.transform.localScale = new Vector3(nodeRadius * 2f, nodeHeight, nodeRadius * 2f);

            // Add text label using 3D TextMeshPro (much sharper than legacy TextMesh)
            GameObject labelObj = new GameObject("ValueLabel");
            labelObj.transform.SetParent(node.transform, false);
            labelObj.transform.localPosition = new Vector3(0, nodeHeight + 0.03f, 0);
            // Face the viewer (assume panel is viewed from -Z direction, so label faces +Z)
            labelObj.transform.localRotation = Quaternion.Euler(0, 180, 0);

            var tmp = labelObj.AddComponent<TextMeshPro>();
            // Load default TMP font asset from TextMesh Pro package
            var fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF - Fallback");
            if (fontAsset != null) tmp.font = fontAsset;
            tmp.text = value.ToString();
            tmp.fontSize = 3f;        // World-space size
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.enableAutoSizing = false;
            tmp.isOverlay = false; // Render in world space

            // Set material color
            Renderer renderer = node.GetComponent<Renderer>();
            if (renderer != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", nodeColor);
                block.SetColor("_Color", nodeColor);
                renderer.SetPropertyBlock(block);
            }
        }

        node.transform.localPosition = position;
        node.name = $"AnimNode_{value}";

        // Update label if using prefab
        TextMesh label = node.GetComponentInChildren<TextMesh>();
        if (label != null)
        {
            label.text = value.ToString();
        }

        return node;
    }

    private GameObject CreateArrow(Vector3 start, Vector3 end)
    {
        GameObject arrow;

        if (arrowPrefab != null)
        {
            arrow = Instantiate(arrowPrefab, animationRoot);
        }
        else
        {
            arrow = new GameObject("AnimArrow");
            arrow.transform.SetParent(animationRoot, false);

            LineRenderer line = arrow.AddComponent<LineRenderer>();
            line.positionCount = 5;
            line.startWidth = 0.018f;  // Thinner arrows
            line.endWidth = 0.018f;
            line.useWorldSpace = false;

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", arrowColor);
            mat.SetColor("_Color", arrowColor);
            line.sharedMaterial = mat;
        }

        UpdateArrowGeometry(arrow.transform, start, end);
        return arrow;
    }

    private void UpdateArrowGeometry(Transform arrowTransform, Vector3 start, Vector3 end)
    {
        LineRenderer line = arrowTransform.GetComponent<LineRenderer>();
        if (line == null) return;

        Vector3 direction = (end - start).normalized;
        float inset = Mathf.Min(nodeRadius * 0.9f, Vector3.Distance(start, end) * 0.15f);
        Vector3 arrowStart = start + direction * inset;
        Vector3 arrowEnd = end - direction * inset;
        Vector3 side = Vector3.Cross(direction, Vector3.up).normalized;
        if (side.sqrMagnitude < 0.01f) side = Vector3.Cross(direction, Vector3.forward).normalized;
        float arrowHeadSize = Mathf.Min(0.07f, Vector3.Distance(arrowStart, arrowEnd) * 0.2f);

        line.SetPosition(0, arrowStart);
        line.SetPosition(1, arrowEnd);
        line.SetPosition(2, arrowEnd - direction * arrowHeadSize + side * arrowHeadSize * 0.5f);
        line.SetPosition(3, arrowEnd);
        line.SetPosition(4, arrowEnd - direction * arrowHeadSize - side * arrowHeadSize * 0.5f);
    }

    private void UpdateArrowGeometry(GameObject arrow, Vector3 start, Vector3 end)
    {
        UpdateArrowGeometry(arrow.transform, start, end);
    }

    // ===== ANIMATION CONTROL =====

    public void Play()
    {
        if (isPlaying)
        {
            Pause();
            return;
        }
        if (currentStep >= AnimationStep.Count - 1)
        {
            Restart();
            return;
        }
        isPlaying = true;
        animationCoroutine = StartCoroutine(PlayAnimation());
        UpdateButtonStates();
    }

    public void Pause()
    {
        isPlaying = false;
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
        UpdateButtonStates();
    }

    public void Restart()
    {
        Pause();
        GoToStep(AnimationStep.InitialState);
        UpdateButtonStates();
    }

    public void NextStep()
    {
        if (currentStep < AnimationStep.Count - 1 && !isPlaying)
        {
            GoToStep(currentStep + 1);
        }
        UpdateButtonStates();
    }

    public void PrevStep()
    {
        if (currentStep > AnimationStep.InitialState && !isPlaying)
        {
            GoToStep(currentStep - 1);
        }
        UpdateButtonStates();
    }

    public void Back()
    {
        // Use existing back-navigation behaviour if available
        // For now, restart the animation as a fallback
        Restart();
    }

    public void Replay()
    {
        // Replay the animation sequence from the beginning
        Restart();
        // Small delay then play
        if (!isPlaying)
        {
            isPlaying = true;
            animationCoroutine = StartCoroutine(PlayAnimation());
        }
    }

    public void ShowHint()
    {
        // Display a short contextual hint related to the current animation step
        string hint = GetHintForCurrentStep();
        if (stepDescriptionText != null)
        {
            string originalText = stepDescriptionText.text;
            stepDescriptionText.text = $"Hint: {hint}";
            StartCoroutine(RestoreHintText(originalText, 3f));
        }
    }

    private string GetHintForCurrentStep()
    {
        switch (currentStep)
        {
            case AnimationStep.InitialState:
                return "List: 1→2→4→5. Insert 3 between 2 and 4.";
            case AnimationStep.CreateSpace:
                return "Nodes 4 & 5 shift right to make room.";
            case AnimationStep.ShowNode3:
                return "Node 3 appears in the gap (disconnected).";
            case AnimationStep.CreateArrow3to4:
                return "Connect 3 → 4.";
            case AnimationStep.CreateArrow2to3:
                return "Connect 2 → 3. Remove old 2 → 4 link.";
            default:
                return "Use Prev/Next to step through.";
        }
    }

    private IEnumerator RestoreHintText(string originalText, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (stepDescriptionText != null && !isPlaying)
        {
            stepDescriptionText.text = originalText;
        }
    }

    public void GoToStep(AnimationStep step)
    {
        currentStep = step;

        switch (step)
        {
            case AnimationStep.InitialState:
                ShowInitialState();
                break;
            case AnimationStep.CreateSpace:
                ShowCreateSpace();
                break;
            case AnimationStep.ShowNode3:
                ShowNode3();
                break;
            case AnimationStep.CreateArrow3to4:
                ShowCreateArrow3to4();
                break;
            case AnimationStep.CreateArrow2to3:
                ShowCreateArrow2to3();
                break;
        }

        UpdateStepDisplay();
        UpdateButtonStates();
    }

    private IEnumerator PlayAnimation()
    {
        while (currentStep < AnimationStep.Count - 1 && isPlaying)
        {
            yield return StartCoroutine(AnimateToNextStep());
            if (!isPlaying) break;
            yield return new WaitForSeconds(0.4f);
        }
        isPlaying = false;
        UpdateButtonStates();
    }

    private IEnumerator AnimateToNextStep()
    {
        AnimationStep nextStep = currentStep + 1;
        float elapsed = 0f;

        switch (nextStep)
        {
            case AnimationStep.CreateSpace:
                // Animate nodes 4 and 5 moving RIGHT to their final positions
                // Node 4: from initialNodePositions[2] to targetNodePositions[3]
                // Node 5: from initialNodePositions[3] to targetNodePositions[4]
                GameObject node4 = animationNodes[2];
                GameObject node5 = animationNodes[3];
                Vector3 node4Start = node4.transform.localPosition;
                Vector3 node4Target = targetNodePositions[3];
                Vector3 node5Start = node5.transform.localPosition;
                Vector3 node5Target = targetNodePositions[4];

                while (elapsed < animationDuration && isPlaying)
                {
                    elapsed += Time.deltaTime;
                    float t = EaseOutCubic(Mathf.Clamp01(elapsed / animationDuration));

                    // Safety: check objects still exist
                    if (node4 == null || node5 == null || animationNodes[1] == null ||
                        animationArrows.Count < 3 || animationArrows[1] == null || animationArrows[2] == null)
                    {
                        yield break;
                    }

                    node4.transform.localPosition = Vector3.Lerp(node4Start, node4Target, t);
                    node5.transform.localPosition = Vector3.Lerp(node5Start, node5Target, t);

                    // Update arrows: 2→4 (stretching), 4→5 (moving with nodes)
                    UpdateArrowGeometry(animationArrows[1], animationNodes[1].transform.localPosition, node4.transform.localPosition);
                    UpdateArrowGeometry(animationArrows[2], node4.transform.localPosition, node5.transform.localPosition);

                    yield return null;
                }

                node4.transform.localPosition = node4Target;
                node5.transform.localPosition = node5Target;
                UpdateArrowGeometry(animationArrows[1], animationNodes[1].transform.localPosition, node4Target);
                UpdateArrowGeometry(animationArrows[2], node4Target, node5Target);
                break;

            case AnimationStep.ShowNode3:
                {
                    // Create node 3 at the gap position (targetNodePositions[2]) but start hidden/scaled down
                    Vector3 gapTarget = targetNodePositions[2];
                    GameObject node4Moved = animationNodes[2]; // Now at targetNodePositions[3]

                    // Create node 3 at the gap position
                    detachedNode3 = CreateNode(3, gapTarget);
                    detachedNode3.name = "AnimNode_3";

                    // Start with zero scale for scale-in animation
                    detachedNode3.transform.localScale = Vector3.zero;

                    // Create arrow 3→4 (initially from node 3 to node 4)
                    if (arrow3to4 == null)
                    {
                        arrow3to4 = CreateArrow(gapTarget, node4Moved.transform.localPosition);
                        arrow3to4.name = "Arrow_3_to_4";
                    }
                    arrow3to4.SetActive(false); // Will be shown in next step

                    // Scale in animation
                    float scaleElapsed = 0f;
                    float scaleDuration = animationDuration * 0.6f;
                    while (scaleElapsed < scaleDuration && isPlaying)
                    {
                        scaleElapsed += Time.deltaTime;
                        float t = EaseOutCubic(Mathf.Clamp01(scaleElapsed / scaleDuration));
                        if (detachedNode3 == null) yield break;
                        detachedNode3.transform.localScale = Vector3.Lerp(Vector3.zero, new Vector3(nodeRadius * 2f, nodeHeight, nodeRadius * 2f), t);
                        yield return null;
                    }
                    if (detachedNode3 != null)
                        detachedNode3.transform.localScale = new Vector3(nodeRadius * 2f, nodeHeight, nodeRadius * 2f);
                }
                break;

            case AnimationStep.CreateArrow3to4:
                {
                    // Show arrow 3→4 with grow animation
                    if (detachedNode3 == null || animationNodes.Count < 3 || animationNodes[2] == null || arrow3to4 == null) yield break;
                    GameObject node3 = detachedNode3;
                    GameObject node4Final = animationNodes[2];
                    arrow3to4.SetActive(true);

                    float arrowElapsed = 0f;
                    float arrowDuration = animationDuration * 0.7f;
                    Vector3 arrowStart = node3.transform.localPosition;
                    Vector3 arrowEnd = node4Final.transform.localPosition;

                    while (arrowElapsed < arrowDuration && isPlaying)
                    {
                        arrowElapsed += Time.deltaTime;
                        float t = EaseOutCubic(Mathf.Clamp01(arrowElapsed / arrowDuration));
                        if (arrow3to4 == null) yield break;
                        Vector3 currentEnd = Vector3.Lerp(arrowStart, arrowEnd, t);
                        UpdateArrowGeometry(arrow3to4, arrowStart, currentEnd);
                        yield return null;
                    }
                    if (arrow3to4 != null)
                        UpdateArrowGeometry(arrow3to4, arrowStart, arrowEnd);
                }
                break;

            case AnimationStep.CreateArrow2to3:
                // Phase 1: Ensure arrow 3→4 is correct
                if (detachedNode3 == null || animationNodes.Count < 3 || animationNodes[2] == null || animationNodes[1] == null || arrow3to4 == null) yield break;
                GameObject node3Final = detachedNode3;
                GameObject node4Final2 = animationNodes[2];
                UpdateArrowGeometry(arrow3to4, node3Final.transform.localPosition, node4Final2.transform.localPosition);

                yield return new WaitForSeconds(0.3f);
                if (!isPlaying) yield break;

                // Phase 2: Create arrow 2→3
                if (arrow2to3 == null)
                {
                    arrow2to3 = CreateArrow(animationNodes[1].transform.localPosition, node3Final.transform.localPosition);
                    arrow2to3.name = "Arrow_2_to_3";
                }
                arrow2to3.SetActive(true);

                // Animate arrow 2→3 growing
                float arrow2Elapsed = 0f;
                float arrow2Duration = animationDuration * 0.7f;
                while (arrow2Elapsed < arrow2Duration && isPlaying)
                {
                    arrow2Elapsed += Time.deltaTime;
                    float t = EaseOutCubic(Mathf.Clamp01(arrow2Elapsed / arrow2Duration));
                    if (arrow2to3 == null || animationNodes[1] == null || node3Final == null) yield break;
                    Vector3 currentEnd = Vector3.Lerp(animationNodes[1].transform.localPosition, node3Final.transform.localPosition, t);
                    UpdateArrowGeometry(arrow2to3, animationNodes[1].transform.localPosition, currentEnd);
                    yield return null;
                }
                if (arrow2to3 != null && animationNodes[1] != null && node3Final != null)
                    UpdateArrowGeometry(arrow2to3, animationNodes[1].transform.localPosition, node3Final.transform.localPosition);

                // Phase 3: Remove old arrow 2→4 (fade out)
                if (oldArrow2to4 != null)
                {
                    float fadeElapsed = 0f;
                    float fadeDuration = 0.5f;
                    LineRenderer oldLine = oldArrow2to4.GetComponent<LineRenderer>();
                    if (oldLine == null) yield break;
                    Color startColor = oldLine.startColor;
                    while (fadeElapsed < fadeDuration)
                    {
                        fadeElapsed += Time.deltaTime;
                        float t = Mathf.Clamp01(fadeElapsed / fadeDuration);
                        Color c = startColor;
                        c.a = Mathf.Lerp(1f, 0f, t);
                        oldLine.startColor = c;
                        oldLine.endColor = c;
                        yield return null;
                    }
                    oldArrow2to4.SetActive(false);
                }

                // Reorder lists to reflect final structure: 1, 2, 3, 4, 5
                animationNodes.Insert(2, node3Final);
                detachedNode3 = null;

                animationArrows[1] = arrow2to3;  // 2→3 at index 1
                animationArrows[2] = arrow3to4;  // 3→4 at index 2
                oldArrow2to4 = null;
                arrow2to3 = null;
                arrow3to4 = null;
                break;
        }

        currentStep = nextStep;
        UpdateStepDisplay();
        UpdateButtonStates();
    }

    private void ShowInitialState()
    {
        CleanupAnimationObjects();
        SetupAnimation();
    }

    private void ShowCreateSpace()
    {
        if (currentStep == AnimationStep.InitialState)
            StartCoroutine(AnimateToNextStep());
    }

    private void ShowNode3()
    {
        if (currentStep == AnimationStep.InitialState)
            StartCoroutine(AnimateToStep(AnimationStep.CreateSpace));
        else if (currentStep == AnimationStep.CreateSpace)
            StartCoroutine(AnimateToNextStep());
    }

    private void ShowCreateArrow3to4()
    {
        if (currentStep == AnimationStep.InitialState)
        {
            StartCoroutine(AnimateToStep(AnimationStep.CreateSpace));
            StartCoroutine(AnimateToStep(AnimationStep.ShowNode3));
        }
        else if (currentStep == AnimationStep.CreateSpace)
        {
            StartCoroutine(AnimateToStep(AnimationStep.ShowNode3));
        }
        else if (currentStep == AnimationStep.ShowNode3)
        {
            StartCoroutine(AnimateToNextStep());
        }
    }

    private void ShowCreateArrow2to3()
    {
        if (currentStep == AnimationStep.InitialState)
        {
            StartCoroutine(AnimateToStep(AnimationStep.CreateSpace));
            StartCoroutine(AnimateToStep(AnimationStep.ShowNode3));
            StartCoroutine(AnimateToStep(AnimationStep.CreateArrow3to4));
        }
        else if (currentStep == AnimationStep.CreateSpace)
        {
            StartCoroutine(AnimateToStep(AnimationStep.ShowNode3));
            StartCoroutine(AnimateToStep(AnimationStep.CreateArrow3to4));
        }
        else if (currentStep == AnimationStep.ShowNode3)
        {
            StartCoroutine(AnimateToStep(AnimationStep.CreateArrow3to4));
        }
        else if (currentStep == AnimationStep.CreateArrow3to4)
        {
            StartCoroutine(AnimateToNextStep());
        }
    }

    private IEnumerator AnimateToStep(AnimationStep targetStep)
    {
        while (currentStep < targetStep && isPlaying)
        {
            yield return StartCoroutine(AnimateToNextStep());
            if (!isPlaying) yield break;
            yield return new WaitForSeconds(0.2f);
        }
    }

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private void UpdateStepDisplay()
    {
        if (stepDescriptionText != null)
        {
            stepDescriptionText.text = stepDescriptions[(int)currentStep];
        }
    }

    private void UpdateButtonStates()
    {
        bool canPlay = currentStep < AnimationStep.Count - 1 && !isPlaying;
        bool canPause = isPlaying;
        bool canNext = currentStep < AnimationStep.Count - 1 && !isPlaying;
        bool canPrev = currentStep > AnimationStep.InitialState && !isPlaying;
        bool canRestart = currentStep > AnimationStep.InitialState;

        // Play button toggles between Play/Pause
        if (playButton != null)
        {
            playButton.interactable = canPlay || canPause;
            TextMeshProUGUI btnText = playButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = isPlaying ? "Pause" : "Play";
            }
        }
        if (nextStepButton != null) nextStepButton.interactable = canNext;
        if (prevStepButton != null) prevStepButton.interactable = canPrev;
        if (restartButton != null) restartButton.interactable = canRestart;
        // Back, Replay, Hint always interactable
        if (backButton != null) backButton.interactable = true;
        if (replayButton != null) replayButton.interactable = true;
        if (hintButton != null) hintButton.interactable = true;
    }

    private void CleanupAnimationObjects()
    {
        foreach (GameObject obj in animationNodes)
        {
            if (obj != null) Destroy(obj);
        }
        animationNodes.Clear();

        foreach (GameObject obj in animationArrows)
        {
            if (obj != null) Destroy(obj);
        }
        animationArrows.Clear();

        if (detachedNode3 != null) { Destroy(detachedNode3); detachedNode3 = null; }
        if (arrow3to4 != null) { Destroy(arrow3to4); arrow3to4 = null; }
        if (arrow2to3 != null) { Destroy(arrow2to3); arrow2to3 = null; }
        if (oldArrow2to4 != null) { Destroy(oldArrow2to4); oldArrow2to4 = null; }
    }

    public void SyncWithTableOperation(string operation, int[] beforeValues, int[] afterValues)
    {
        Restart();
    }
}