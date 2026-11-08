using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handles mouse interaction with the animation panel controls.
/// Works with World Space Canvas buttons via EventSystem.
/// Also provides hover highlighting for 3D objects if needed.
/// </summary>
public sealed class AnimationPanelMouseController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AnimationPanelController animationPanel;
    [SerializeField] private Camera mainCamera;

    [Header("Control Names (must match AnimationPanelController button GameObjects)")]
    [SerializeField] private string playButtonName = "Play";
    [SerializeField] private string pauseButtonName = "Pause";
    [SerializeField] private string restartButtonName = "Restart";
    [SerializeField] private string nextStepButtonName = "NextStep";
    [SerializeField] private string prevStepButtonName = "PrevStep";

    [Header("Visual Feedback (for 3D objects if any)")]
    [SerializeField] private Color hoverColor = new Color(1f, 0.62f, 0.12f, 1f);
    [SerializeField] private float hoverScale = 1.05f;

    // Track hovered state for 3D objects
    private GameObject currentlyHovered3D;
    private readonly Dictionary<GameObject, Color> originalColors3D = new Dictionary<GameObject, Color>();
    private readonly Dictionary<GameObject, Vector3> originalScales3D = new Dictionary<GameObject, Vector3>();

    private GraphicRaycaster graphicRaycaster;
    private Canvas targetCanvas;
    private EventSystem eventSystem;

    private void Awake()
    {
        if (animationPanel == null)
        {
            animationPanel = GetComponent<AnimationPanelController>();
        }
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        eventSystem = EventSystem.current;
    }

    private void Start()
    {
        // Wait one frame for AnimationPanelController to create UI
        StartCoroutine(InitializeControls());
    }

    private System.Collections.IEnumerator InitializeControls()
    {
        yield return null; // Wait one frame for autoCreateUI to complete

        // Find the World Space Canvas and its GraphicRaycaster
        targetCanvas = animationPanel != null ? animationPanel.GetComponentInChildren<Canvas>() : null;
        if (targetCanvas != null)
        {
            graphicRaycaster = targetCanvas.GetComponent<GraphicRaycaster>();
        }

        // The buttons on the Canvas should work automatically with EventSystem
        // once mouse input is enabled. We just need to ensure they have the right names
        // for any custom logic.

        // Verify buttons exist and are properly configured
        VerifyButtonSetup();
    }

    private void VerifyButtonSetup()
    {
        if (targetCanvas == null) return;

        // Find buttons by name and ensure they have proper navigation/transition
        var buttons = targetCanvas.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            // Ensure button has proper ColorBlock for hover feedback
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.12f, 0.35f, 0.75f, 0.95f);
            cb.highlightedColor = new Color(0.18f, 0.5f, 0.9f, 0.95f);
            cb.pressedColor = new Color(0.08f, 0.25f, 0.55f, 0.95f);
            cb.selectedColor = cb.highlightedColor;
            cb.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            cb.colorMultiplier = 1f;
            btn.colors = cb;
        }
    }

    private void Update()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (eventSystem == null) eventSystem = EventSystem.current;

        // Handle 3D object hover (for animation nodes/arrows if they have colliders)
        Handle3DHover();

        // UI buttons on Canvas are handled automatically by EventSystem + GraphicRaycaster
        // when mouse input is enabled. No additional code needed.
    }

    private void Handle3DHover()
    {
        if (mainCamera == null) return;

        // Don't process 3D hover if pointer is over UI
        if (eventSystem != null && eventSystem.IsPointerOverGameObject())
        {
            Handle3DHoverExit();
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObj = hit.collider.gameObject;
            // Only handle hover for animation nodes/arrows (under animationRoot)
            if (animationPanel != null && animationPanel.transform.IsChildOf(hitObj.transform) || hitObj.transform.IsChildOf(animationPanel.transform))
            {
                Handle3DHoverEnter(hitObj);
            }
            else
            {
                Handle3DHoverExit();
            }
        }
        else
        {
            Handle3DHoverExit();
        }
    }

    private void Handle3DHoverEnter(GameObject obj)
    {
        if (currentlyHovered3D == obj) return;

        Handle3DHoverExit();

        currentlyHovered3D = obj;

        // Apply hover visual for 3D objects (nodes/arrows)
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            originalColors3D[obj] = block.GetColor("_BaseColor");
            block.SetColor("_BaseColor", hoverColor);
            block.SetColor("_Color", hoverColor);
            renderer.SetPropertyBlock(block);
        }

        // Slight scale up
        originalScales3D[obj] = obj.transform.localScale;
        obj.transform.localScale = originalScales3D[obj] * hoverScale;
    }

    private void Handle3DHoverExit()
    {
        if (currentlyHovered3D == null) return;

        // Restore original visual
        Renderer renderer = currentlyHovered3D.GetComponent<Renderer>();
        if (renderer != null && originalColors3D.TryGetValue(currentlyHovered3D, out Color original))
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", original);
            block.SetColor("_Color", original);
            renderer.SetPropertyBlock(block);
        }

        // Restore original scale
        if (originalScales3D.TryGetValue(currentlyHovered3D, out Vector3 originalScale))
        {
            currentlyHovered3D.transform.localScale = originalScale;
        }

        currentlyHovered3D = null;
    }

    private void OnDisable()
    {
        Handle3DHoverExit();
    }
}