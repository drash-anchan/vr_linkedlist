using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// Authored representation of one Next pointer. Runtime only changes endpoints and visibility.
/// Now interactive: can be selected to trigger disconnection.
[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(XRSimpleInteractable))]
[RequireComponent(typeof(BoxCollider))]
public sealed class LinkInteraction : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private LinkedListNode from;
    [SerializeField] private LinkedListNode to;

    [Header("Visual")]
    [SerializeField] private Material lineMaterial;
    [SerializeField, Min(0.005f)] private float width = 0.025f;
    [SerializeField] private Color defaultColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.62f, 0.12f, 1f);
    [SerializeField] private Color hoverColor = new Color(0.12f, 0.95f, 0.6f, 1f);

    [Header("Interaction")]
    [SerializeField] private LinkedListManager manager;

    private LineRenderer line;
    private XRSimpleInteractable interactable;
    private new BoxCollider collider;
    private Material lineMaterialInstance;
    private bool isSelected;
    private bool isHovered;

    public LinkedListNode Source => from;
    public LinkedListNode Target => to;
    public bool IsSelected => isSelected;
    public bool IsHovered => isHovered;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        interactable = GetComponent<XRSimpleInteractable>();
        collider = GetComponent<BoxCollider>();
        ConfigureRenderer();
        ConfigureInteraction();
        Refresh();
    }

    private void OnEnable()
    {
        line = GetComponent<LineRenderer>();
        interactable = GetComponent<XRSimpleInteractable>();
        collider = GetComponent<BoxCollider>();
        ConfigureRenderer();
        ConfigureInteraction();
        Refresh();
    }

    private void ConfigureRenderer()
    {
        if (line == null) line = GetComponent<LineRenderer>();
        line.positionCount = 5;
        line.startWidth = width;
        line.endWidth = width;
        line.useWorldSpace = true;

        // Create a material instance for color changes
        if (lineMaterial != null)
        {
            lineMaterialInstance = new Material(lineMaterial);
            line.sharedMaterial = lineMaterialInstance;
            SetLineColor(defaultColor);
        }
    }

    private void ConfigureInteraction()
    {
        if (interactable == null) interactable = GetComponent<XRSimpleInteractable>();
        if (collider == null) collider = GetComponent<BoxCollider>();

        // Configure collider to encompass the line
        collider.isTrigger = true;
        collider.size = new Vector3(1f, 0.1f, 0.1f);

        // Interaction events
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
        interactable.selectEntered.AddListener(OnSelectEnter);
        interactable.selectExited.AddListener(OnSelectExit);

        // Disable selection by default - only enable when in connection mode
        interactable.enabled = false;
    }

    public void SetEndpoints(LinkedListNode source, LinkedListNode destination)
    {
        from = source;
        to = destination;
        Refresh();
    }

    public void SetMaterial(Material material)
    {
        lineMaterial = material;
        ConfigureRenderer();
    }

    public void SetManager(LinkedListManager mgr)
    {
        manager = mgr;
    }

    /// Enable/disable interaction (used when entering/exiting connection mode)
    public void SetInteractable(bool enabled)
    {
        if (interactable != null)
        {
            interactable.enabled = enabled;
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (lineMaterialInstance != null)
        {
            SetLineColor(selected ? selectedColor : (isHovered ? hoverColor : defaultColor));
        }
    }

    public void SetHovered(bool hovered)
    {
        isHovered = hovered;
        if (lineMaterialInstance != null && !isSelected)
        {
            SetLineColor(hovered ? hoverColor : defaultColor);
        }
    }

    private void SetLineColor(Color color)
    {
        if (lineMaterialInstance != null)
        {
            lineMaterialInstance.SetColor("_BaseColor", color);
            lineMaterialInstance.SetColor("_Color", color);
            lineMaterialInstance.SetColor("_EmissionColor", color * 0.5f);
        }
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        SetHovered(true);
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        SetHovered(false);
    }

    private void OnSelectEnter(SelectEnterEventArgs args)
    {
        isSelected = true;
        SetSelected(true);

        // Trigger disconnection through manager
        if (manager != null && from != null)
        {
            manager.Disconnect(from);
        }
    }

    private void OnSelectExit(SelectExitEventArgs args)
    {
        isSelected = false;
        SetSelected(false);
    }

    private void OnDestroy()
    {
        if (lineMaterialInstance != null)
        {
            Destroy(lineMaterialInstance);
        }
    }

    public void Refresh()
    {
        if (line == null) line = GetComponent<LineRenderer>();
        if (from == null || to == null)
        {
            line.enabled = false;
            if (collider != null) collider.enabled = false;
            return;
        }
        line.enabled = true;
        if (collider != null) collider.enabled = true;

        line.positionCount = 5;
        Vector3 start = from.transform.position;
        Vector3 end = to.transform.position;
        Vector3 direction = (end - start).normalized;
        float inset = Mathf.Min(0.23f, Vector3.Distance(start, end) * 0.2f);
        start += direction * inset;
        end -= direction * inset;
        Vector3 side = Vector3.Cross(direction, Vector3.up).normalized;
        if (side.sqrMagnitude < 0.01f) side = Vector3.Cross(direction, Vector3.forward).normalized;
        float arrow = Mathf.Min(0.1f, Vector3.Distance(start, end) * 0.25f);
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.SetPosition(2, end - direction * arrow + side * arrow * 0.5f);
        line.SetPosition(3, end);
        line.SetPosition(4, end - direction * arrow - side * arrow * 0.5f);

        // Update collider to match line position and orientation
        UpdateCollider(start, end, direction);
    }

    private void UpdateCollider(Vector3 start, Vector3 end, Vector3 direction)
    {
        if (collider == null) return;

        Vector3 midPoint = (start + end) * 0.5f;
        float length = Vector3.Distance(start, end);

        collider.transform.position = midPoint;
        collider.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        collider.size = new Vector3(width * 4f, width * 4f, length);
    }

    private void LateUpdate() => Refresh();
}
