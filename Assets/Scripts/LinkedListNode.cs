using UnityEngine;

/// A pre-authored, grabbable node. Next is the authoritative structural link.
[ExecuteAlways]
public sealed class LinkedListNode : MonoBehaviour
{
    [SerializeField] private int value;
    [SerializeField] private LinkedListNode next;
    [SerializeField] private int index;
    [SerializeField] private LinkedListManager manager;
    private Renderer nodeRenderer;
    private Color baseColor = new Color(0.12f, 0.55f, 0.95f);
    private Color selectedColor = new Color(1f, 0.62f, 0.12f);

    // Back-reference to NodeInteraction for visual updates
    private NodeInteraction nodeInteraction;

    public int Value => value;
    public LinkedListNode Next => next;
    public int Index => index;
    public LinkedListManager Manager => manager;
    public bool IsHeld { get; internal set; }
    public bool Selected { get; private set; }

    public void SetManager(LinkedListManager owner) => manager = owner;
    public void SetIndex(int listIndex) => index = listIndex;
    public void SetNext(LinkedListNode successor) => next = successor;

    public void SetValue(int newValue)
    {
        value = newValue;
        if (nodeInteraction == null) nodeInteraction = GetComponent<NodeInteraction>();
        if (nodeInteraction != null) nodeInteraction.RefreshLabel();
    }

    public void SetSelected(bool selected)
    {
        Selected = selected;
        if (nodeInteraction != null)
        {
            nodeInteraction.SetVisualState(selected, false);
        }
        else
        {
            ApplyColor(selected ? selectedColor : baseColor);
        }
    }

    public void ApplyColor(Color color)
    {
        if (nodeRenderer == null) nodeRenderer = GetComponent<Renderer>();
        if (nodeRenderer == null) return;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        nodeRenderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        nodeRenderer.SetPropertyBlock(block);
    }

    public void NotifyClicked()
    {
        if (manager != null) manager.SelectNode(this);
    }

    // Cache the interaction reference
    private void Awake()
    {
        nodeInteraction = GetComponent<NodeInteraction>();
    }
}
