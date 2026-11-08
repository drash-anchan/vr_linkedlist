using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Controls the animated insertion visualization on the animation panel.
/// Demonstrates the step-by-step insertion of a node into a linked list.
/// Fixed layout, animation sequence, node sizes, and removed obstructing panel.
/// </summary>
public sealed class AnimationPanelController : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private Transform animationRoot;      // 3D nodes/arrows root (not a Canvas)
    [SerializeField] private GameObject nodePrefab;        // Optional: prefab matching table nodes
    [SerializeField] private GameObject arrowPrefab;       // Optional: prefab for arrows

    [Header("Animation Controls (auto-created if missing)")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button nextStepButton;
    [SerializeField] private Button prevStepButton;
    [SerializeField] private TMP_Text stepDescriptionText;
    [SerializeField] private Slider progressSlider;

    [Header("Visual Settings")]
    [SerializeField] private float nodeSpacing = 1.8f;  // Increased for better visibility
    [SerializeField] private float animationDuration = 1.2f;
    [SerializeField] private Color nodeColor = new Color(0.12f, 0.55f, 0.95f);
    [SerializeField] private Color highlightColor = new Color(1f, 0.62f, 0.12f);
    [SerializeField] private Color arrowColor = Color.white;
    [SerializeField] private Color headerColor = new Color(0.12f, 0.55f, 0.95f);
    [SerializeField] private Color panelBackgroundColor = new Color(0.05f, 0.1f, 0.2f, 0.6f);

    [Header("Panel Dimensions")]
    [SerializeField] private float panelWidth = 14f;  // World units - wide enough for all 5 nodes + margins
    [SerializeField] private float panelHeight = 7f;  // World units - enough for header, viz, description, controls
    [SerializeField] private float panelDepth = 0.1f;

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
    private GameObject headerTitle;
    private GameObject headerSubtitle;

    // Step descriptions matching spec
    private readonly string[] stepDescriptions = new string[]
    {
        "Step 1 / 5: Initial linked list: 1 → 2 → 4 → 5",
        "Step 2 / 5: Moving nodes 4 and 5 to create space for node 3",
        "Step 3 / 5: Node 3 appears in the gap between nodes 2 and 4",
        "Step 4 / 5: Connecting node 3 → node 4",
        "Step 5 / 5: Connecting node 2 → node 3. Insertion complete! Final: 1 → 2 → 3 → 4 → 5"
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
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(panelWidth * 100f, panelHeight * 100f); // 100 pixels per world unit
            canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f); // 1 world unit = 100 pixels
            canvasObj.transform.localPosition = new Vector3(0, 0, -panelDepth * 0.5f - 0.01f);  // In front of background

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Build the 4-area layout on the Canvas
        CreateLayout();
    }

    /// <summary>
    /// Create a 3D background panel mesh for the animation panel
    /// </summary>
    private void CreatePanelBackground()
    {
        if (panelBackground != null) return;

        panelBackground = GameObject.CreatePrimitive(PrimitiveType.Quad);
        panelBackground.name = "PanelBackground";
        panelBackground.transform.SetParent(transform, false);
        panelBackground.transform.localPosition = new Vector3(0, 0, 0);
        panelBackground.transform.localRotation = Quaternion.Euler(0, 0, 0);
        panelBackground.transform.localScale = new Vector3(panelWidth, panelHeight, 1f);
        panelBackground.layer = LayerMask.NameToLayer("Default");

        // Remove collider if present (we don't want it blocking rays)
        Collider col = panelBackground.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Set material
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
            mat.renderQueue = 3000;
            renderer.sharedMaterial = mat;
        }
    }

    /// <summary>
    /// Create the 4-area layout: Header (top), Visualization (center - 3D), Step Description (below viz), Controls (bottom)
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
        containerRect.offsetMin = new Vector2(40, 40);
        containerRect.offsetMax = new Vector2(-40, -40);

        VerticalLayoutGroup vLayout = container.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 15;
        vLayout.padding = new RectOffset(20, 20, 20, 20);
        vLayout.childAlignment = TextAnchor.UpperCenter;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = true;
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;

        // ===== AREA A: HEADER =====
        GameObject headerArea = new GameObject("HeaderArea");
        headerArea.transform.SetParent(container.transform, false);
        LayoutElement headerLayout = headerArea.AddComponent<LayoutElement>();
        headerLayout.minHeight = 80;
        headerLayout.preferredHeight = 80;
        headerLayout.flexibleWidth = 1;

        VerticalLayoutGroup headerLayoutGroup = headerArea.AddComponent<VerticalLayoutGroup>();
        headerLayoutGroup.spacing = 4;
        headerLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
        headerLayoutGroup.childControlWidth = true;
        headerLayoutGroup.childControlHeight = true;

        // Title
        headerTitle = CreateText(headerArea.transform, "HeaderTitle", "LINKED LIST INSERTION", 36, FontStyles.Bold, headerColor);
        LayoutElement titleLayout = headerTitle.AddComponent<LayoutElement>();
        titleLayout.minHeight = 50;

        // Subtitle
        headerSubtitle = CreateText(headerArea.transform, "HeaderSubtitle", "Insert Node 3 Between Node 2 and Node 4", 22, FontStyles.Normal, Color.white);
        LayoutElement subtitleLayout = headerSubtitle.AddComponent<LayoutElement>();
        subtitleLayout.minHeight = 30;

        // ===== AREA B: VISUALIZATION SPACE =====
        // This is a placeholder area - the actual 3D nodes are under animationRoot (not on Canvas)
        // We create an empty space to reserve room in the layout
        GameObject vizSpace = new GameObject("VisualizationSpace");
        vizSpace.transform.SetParent(container.transform, false);
        LayoutElement vizLayout = vizSpace.AddComponent<LayoutElement>();
        vizLayout.minHeight = 220;
        vizLayout.preferredHeight = 240;
        vizLayout.flexibleWidth = 1;

        // ===== AREA C: STEP DESCRIPTION =====
        GameObject stepDescArea = new GameObject("StepDescriptionArea");
        stepDescArea.transform.SetParent(container.transform, false);
        LayoutElement descLayout = stepDescArea.AddComponent<LayoutElement>();
        descLayout.minHeight = 80;
        descLayout.preferredHeight = 100;
        descLayout.flexibleWidth = 1;

        GameObject textObj = new GameObject("StepDescriptionText");
        textObj.transform.SetParent(stepDescArea.transform, false);
        stepDescriptionText = textObj.AddComponent<TextMeshProUGUI>();
        stepDescriptionText.fontSize = 22;
        stepDescriptionText.alignment = TextAlignmentOptions.Center;
        stepDescriptionText.color = Color.white;
        stepDescriptionText.textWrappingMode = TextWrappingModes.Normal;
        stepDescriptionText.text = stepDescriptions[0];
        stepDescriptionText.overflowMode = TextOverflowModes.Overflow;

        LayoutElement textLayout = textObj.AddComponent<LayoutElement>();
        textLayout.minHeight = 80;
        textLayout.flexibleWidth = 1;

        // ===== AREA D: ANIMATION CONTROLS =====
        GameObject controlsArea = new GameObject("ControlsArea");
        controlsArea.transform.SetParent(container.transform, false);
        LayoutElement controlsLayout = controlsArea.AddComponent<LayoutElement>();
        controlsLayout.minHeight = 70;
        controlsLayout.preferredHeight = 80;
        controlsLayout.flexibleWidth = 1;

        HorizontalLayoutGroup hLayout = controlsArea.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 15;
        hLayout.padding = new RectOffset(10, 10, 5, 5);
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.childControlWidth = true;
        hLayout.childControlHeight = true;
        hLayout.childForceExpandWidth = true;

        // Create buttons in spec order: Previous | Play | Pause | Restart | Next
        prevStepButton = CreateButton(controlsArea.transform, "PrevStep", "◀ Previous", PrevStep);
        playButton = CreateButton(controlsArea.transform, "Play", "▶ Play", Play);
        pauseButton = CreateButton(controlsArea.transform, "Pause", "⏸ Pause", Pause);
        restartButton = CreateButton(controlsArea.transform, "Restart", "⟲ Restart", Restart);
        nextStepButton = CreateButton(controlsArea.transform, "NextStep", "Next ▶", NextStep);

        // Progress slider below controls (optional, small)
        GameObject sliderObj = new GameObject("ProgressSlider");
        sliderObj.transform.SetParent(container.transform, false);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(0, 30);

        LayoutElement sliderLayout = sliderObj.AddComponent<LayoutElement>();
        sliderLayout.minHeight = 30;
        sliderLayout.preferredHeight = 30;
        sliderLayout.flexibleWidth = 1;

        progressSlider = sliderObj.AddComponent<Slider>();
        progressSlider.minValue = 0;
        progressSlider.maxValue = 1;
        progressSlider.wholeNumbers = false;
        progressSlider.interactable = true;

        // Slider visuals
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;

        GameObject fillObj = new GameObject("Fill Area");
        fillObj.transform.SetParent(sliderObj.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero; fillRect.offsetMax = Vector2.zero;

        GameObject fillImageObj = new GameObject("Fill");
        fillImageObj.transform.SetParent(fillObj.transform, false);
        Image fillImg = fillImageObj.AddComponent<Image>();
        fillImg.color = headerColor;
        RectTransform fillImageRect = fillImageObj.GetComponent<RectTransform>();
        fillImageRect.anchorMin = Vector2.zero; fillImageRect.anchorMax = new Vector2(1, 1);
        fillImageRect.offsetMin = Vector2.zero; fillImageRect.offsetMax = Vector2.zero;

        progressSlider.targetGraphic = fillImg;
        progressSlider.fillRect = fillImageRect;
        progressSlider.handleRect = null;
    }

    private GameObject CreateText(Transform parent, string name, string text, int fontSize, FontStyles style, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return obj;
    }

    private Button CreateButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.12f, 0.35f, 0.75f, 0.95f);
        btnImg.sprite = null;

        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
        btnText.text = label;
        btnText.fontSize = 18;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;

        // Consistent sizing
        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.minWidth = 120;
        le.minHeight = 44;
        le.preferredWidth = 140;
        le.preferredHeight = 44;

        // Hover/pressed colors via ColorBlock
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.12f, 0.35f, 0.75f, 0.95f);
        cb.highlightedColor = new Color(0.18f, 0.5f, 0.9f, 0.95f);
        cb.pressedColor = new Color(0.08f, 0.25f, 0.55f, 0.95f);
        cb.selectedColor = cb.highlightedColor;
        cb.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        cb.colorMultiplier = 1f;
        btn.colors = cb;

        // Add collider for mouse raycasting (for AnimationPanelMouseController)
        BoxCollider box = btnObj.AddComponent<BoxCollider>();
        box.size = new Vector3(168f, 52.8f, 0.1f); // Slightly larger than visual (140 * 1.2, 44 * 1.2)

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
        if (pauseButton != null) pauseButton.onClick.AddListener(Pause);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (nextStepButton != null) nextStepButton.onClick.AddListener(NextStep);
        if (prevStepButton != null) prevStepButton.onClick.AddListener(PrevStep);
        if (progressSlider != null) progressSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void UnbindControls()
    {
        if (playButton != null) playButton.onClick.RemoveListener(Play);
        if (pauseButton != null) pauseButton.onClick.RemoveListener(Pause);
        if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
        if (nextStepButton != null) nextStepButton.onClick.RemoveListener(NextStep);
        if (prevStepButton != null) prevStepButton.onClick.RemoveListener(PrevStep);
        if (progressSlider != null) progressSlider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void OnSliderChanged(float value)
    {
        int step = Mathf.RoundToInt(value * (int)AnimationStep.Count);
        GoToStep((AnimationStep)Mathf.Clamp(step, 0, (int)AnimationStep.Count - 1));
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

        // Calculate positions for 5 nodes evenly spaced
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
        UpdateProgressSlider();
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
            // Create a cylinder node programmatically to match table nodes exactly
            node = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            node.transform.SetParent(animationRoot, false);

            // Scale to match table nodes EXACTLY (0.42, 0.16, 0.42)
            node.transform.localScale = new Vector3(0.42f, 0.16f, 0.42f);

            // Add text label using 3D TextMesh (not Canvas)
            GameObject labelObj = new GameObject("ValueLabel");
            labelObj.transform.SetParent(node.transform, false);
            labelObj.transform.localPosition = new Vector3(0, 0.22f, 0);

            TextMesh textMesh = labelObj.AddComponent<TextMesh>();
            textMesh.text = value.ToString();
            textMesh.fontSize = 48;
            textMesh.characterSize = 0.16f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;

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
            line.startWidth = 0.025f;
            line.endWidth = 0.025f;
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
        float inset = Mathf.Min(0.23f, Vector3.Distance(start, end) * 0.2f);
        Vector3 arrowStart = start + direction * inset;
        Vector3 arrowEnd = end - direction * inset;
        Vector3 side = Vector3.Cross(direction, Vector3.up).normalized;
        if (side.sqrMagnitude < 0.01f) side = Vector3.Cross(direction, Vector3.forward).normalized;
        float arrowHeadSize = Mathf.Min(0.1f, Vector3.Distance(arrowStart, arrowEnd) * 0.25f);

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
        if (isPlaying) return;
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
        if (currentStep < AnimationStep.Count - 1)
        {
            GoToStep(currentStep + 1);
        }
        UpdateButtonStates();
    }

    public void PrevStep()
    {
        if (currentStep > AnimationStep.InitialState)
        {
            GoToStep(currentStep - 1);
        }
        UpdateButtonStates();
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
        UpdateProgressSlider();
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
                        detachedNode3.transform.localScale = Vector3.Lerp(Vector3.zero, new Vector3(0.42f, 0.16f, 0.42f), t);
                        yield return null;
                    }
                    detachedNode3.transform.localScale = new Vector3(0.42f, 0.16f, 0.42f);
                }
                break;

            case AnimationStep.CreateArrow3to4:
                {
                    // Show arrow 3→4 with grow animation
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
                        Vector3 currentEnd = Vector3.Lerp(arrowStart, arrowEnd, t);
                        UpdateArrowGeometry(arrow3to4, arrowStart, currentEnd);
                        yield return null;
                    }
                    UpdateArrowGeometry(arrow3to4, arrowStart, arrowEnd);
                }
                break;

            case AnimationStep.CreateArrow2to3:
                // Phase 1: Ensure arrow 3→4 is correct
                GameObject node3Final = detachedNode3;
                GameObject node4Final2 = animationNodes[2];
                UpdateArrowGeometry(arrow3to4, node3Final.transform.localPosition, node4Final2.transform.localPosition);

                yield return new WaitForSeconds(0.3f);

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
                    Vector3 currentEnd = Vector3.Lerp(animationNodes[1].transform.localPosition, node3Final.transform.localPosition, t);
                    UpdateArrowGeometry(arrow2to3, animationNodes[1].transform.localPosition, currentEnd);
                    yield return null;
                }
                UpdateArrowGeometry(arrow2to3, animationNodes[1].transform.localPosition, node3Final.transform.localPosition);

                // Phase 3: Remove old arrow 2→4 (fade out)
                if (oldArrow2to4 != null)
                {
                    float fadeElapsed = 0f;
                    float fadeDuration = 0.5f;
                    LineRenderer oldLine = oldArrow2to4.GetComponent<LineRenderer>();
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
        UpdateProgressSlider();
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

        if (playButton != null) playButton.interactable = canPlay;
        if (pauseButton != null) pauseButton.interactable = canPause;
        if (nextStepButton != null) nextStepButton.interactable = canNext;
        if (prevStepButton != null) prevStepButton.interactable = canPrev;
        if (restartButton != null) restartButton.interactable = canRestart;
    }

    private void UpdateProgressSlider()
    {
        if (progressSlider != null)
        {
            progressSlider.value = (float)currentStep / ((int)AnimationStep.Count - 1);
        }
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