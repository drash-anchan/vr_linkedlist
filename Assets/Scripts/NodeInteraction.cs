using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Handles XR and desktop mouse interaction for linked list nodes.
/// Manages visual states: Default (blue), Hover (green), Selected (orange).
/// Selection persists until another node is selected or operation clears it.
/// </summary>
[ExecuteAlways]
public sealed class NodeInteraction : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool desktopFallbackEnabled = true;
    [SerializeField] private Color selectedColor = new Color(1f, 0.62f, 0.12f);
    [SerializeField] private Color defaultColor = new Color(0.12f, 0.55f, 0.95f);
    [SerializeField] private Color hoverColor = new Color(0.12f, 0.95f, 0.6f);

    private LinkedListNode node;
    private TextMesh label;
    private XRGrabInteractable grab;
    private Plane dragPlane;
    private Vector3 dragOffset;
    private bool dragging;
    private Renderer nodeRenderer;
    private MaterialPropertyBlock propertyBlock;

    // Visual state tracking
    private bool isHovered = false;
    private bool isSelected = false;

    public void Initialize(LinkedListNode owner) { node = owner; RefreshLabel(); }

    /// <summary>
    /// Called by LinkedListNode.SetSelected to update visual state
    /// </summary>
    public void SetVisualState(bool selected, bool hovered)
    {
        isSelected = selected;
        isHovered = hovered && !selected; // Don't show hover if selected

        if (nodeRenderer == null || propertyBlock == null) return;

        nodeRenderer.GetPropertyBlock(propertyBlock);
        Color color = selected ? selectedColor : (hovered ? hoverColor : defaultColor);
        propertyBlock.SetColor("_BaseColor", color);
        propertyBlock.SetColor("_Color", color);
        nodeRenderer.SetPropertyBlock(propertyBlock);
    }

    public void RefreshLabel()
    {
        if (node == null) node = GetComponent<LinkedListNode>();
        if (label == null) label = GetComponentInChildren<TextMesh>(true);
        if (label != null && node != null && label.text != node.Value.ToString()) label.text = node.Value.ToString();
    }

    private void Awake()
    {
        node = GetComponent<LinkedListNode>();
        label = GetComponentInChildren<TextMesh>(true);
        grab = GetComponent<XRGrabInteractable>();
        nodeRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        RefreshLabel();
        if (!Application.isPlaying) return;
        if (grab != null)
        {
            grab.selectEntered.AddListener(OnGrab);
            grab.selectExited.AddListener(OnRelease);
            grab.hoverEntered.AddListener(OnHoverEnter);
            grab.hoverExited.AddListener(OnHoverExit);
        }
    }

    private void OnDisable()
    {
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnGrab);
            grab.selectExited.RemoveListener(OnRelease);
            grab.hoverEntered.RemoveListener(OnHoverEnter);
            grab.hoverExited.RemoveListener(OnHoverExit);
        }
        dragging = false;
        if (node != null) node.IsHeld = false;
        SetVisualState(false, false);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        node.IsHeld = true;
        SelectFromXR();
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        node.IsHeld = false;
        PlaceFromXR();
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (!node.IsHeld && !node.Selected)
        {
            isHovered = true;
            SetVisualState(false, true);
        }
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        if (!node.IsHeld && !node.Selected)
        {
            isHovered = false;
            SetVisualState(false, false);
        }
    }

    /// <summary>
    /// Called when node is selected via XR (pinch/grab)
    /// </summary>
    public void SelectFromXR()
    {
        RefreshLabel();
        if (node != null) node.NotifyClicked();
    }

    /// <summary>
    /// Called when node is selected via desktop mouse click
    /// </summary>
    public void SelectFromDesktop()
    {
        RefreshLabel();
        if (node != null) node.NotifyClicked();
    }

    public void PlaceFromXR() { if (node != null && node.Manager != null) node.Manager.PlaceNodeAtNearestSlot(node); }

    private void Update()
    {
        RefreshLabel();
        if (!Application.isPlaying || !desktopFallbackEnabled || Mouse.current == null || Camera.main == null) return;
        if (grab != null && grab.isSelected) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Check if mouse is over a UI element (Canvas)
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (Mouse.current.leftButton.wasPressedThisFrame && !overUI &&
            Physics.Raycast(ray, out RaycastHit hit) && hit.collider.GetComponentInParent<LinkedListNode>() == node)
        {
            SelectFromDesktop();
            dragPlane = new Plane(-Camera.main.transform.forward, transform.position);
            if (dragPlane.Raycast(ray, out float distance)) dragOffset = transform.position - ray.GetPoint(distance);
            dragging = true;
            node.IsHeld = true;
        }
        if (dragging && Mouse.current.leftButton.isPressed && dragPlane.Raycast(ray, out float d)) transform.position = ray.GetPoint(d) + dragOffset;
        if (dragging && Mouse.current.leftButton.wasReleasedThisFrame) { dragging = false; node.IsHeld = false; PlaceFromXR(); }

        // Handle hover for desktop (only when not dragging)
        if (!dragging && !overUI)
        {
            bool hitNode = Physics.Raycast(ray, out RaycastHit hit2) && hit2.collider.GetComponentInParent<LinkedListNode>() == node;

            if (hitNode && !isHovered && !node.Selected && !node.IsHeld)
            {
                isHovered = true;
                SetVisualState(false, true);
            }
            else if (!hitNode && isHovered && !node.Selected && !node.IsHeld)
            {
                isHovered = false;
                SetVisualState(false, false);
            }
        }
    }
}