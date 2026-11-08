using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class LinkedListValidationResult
{
    public bool IsValid;
    public bool StructureValid;
    public bool ValuesValid;
    public bool OrderValid;
    public string Message;
    public override string ToString() => Message;
}

/// Node positions and pointer topology are independent. Only Reset arranges the whole list.
public sealed class LinkedListManager : MonoBehaviour
{
    [SerializeField] private Transform nodeRoot;
    [SerializeField] private Transform linkRoot;
    [SerializeField] private LinkedListNode[] authoredNodes = Array.Empty<LinkedListNode>();
    [SerializeField] private LinkInteraction[] authoredLinks = Array.Empty<LinkInteraction>();
    [SerializeField, Min(0.1f)] private float nodeSpacing = 1.05f;
    [SerializeField] private Vector3 listOrigin = new Vector3(-1.7f, 1.15f, 0.15f);
    private readonly List<LinkedListNode> nodes = new List<LinkedListNode>();
    private readonly Dictionary<LinkedListNode, Pose> initialPoses = new Dictionary<LinkedListNode, Pose>();
    private LinkedListNode selectedNode;
    private LinkedListNode connectionSource;
    private bool initialized;
    private bool isConnectionMode;
    public IReadOnlyList<LinkedListNode> Nodes => nodes;
    public LinkedListNode SelectedNode => selectedNode;
    public LinkedListNode ConnectionSource => connectionSource;
    public bool IsConnectionMode => isConnectionMode;
    public string LastMessage { get; private set; }
    public event Action<LinkedListNode> SelectionChanged;
    public event Action StructureChanged;
    public event Action<string> Feedback;
    public event Action<bool> ConnectionModeChanged;

    public void ConfigureScene(Transform nodeContainer, Transform linkContainer, LinkedListNode[] sceneNodes, LinkInteraction[] sceneLinks)
    {
        nodeRoot = nodeContainer;
        linkRoot = linkContainer;
        authoredNodes = sceneNodes ?? Array.Empty<LinkedListNode>();
        authoredLinks = sceneLinks ?? Array.Empty<LinkInteraction>();
        initialized = false;
        initialPoses.Clear();
    }

    private void OnValidate()
    {
        if (nodeSpacing < 0.1f) nodeSpacing = 0.1f;
    }

    public void Initialize(Transform nodeContainer, Transform linkContainer)
    {
        nodeRoot = nodeContainer;
        linkRoot = linkContainer;
        InitializeFromScene();
    }

    public void InitializeFromScene()
    {
        if (initialized) return;
        initialized = true;
        nodes.Clear();
        foreach (LinkedListNode node in authoredNodes.Where(n => n != null).Distinct())
        {
            node.SetManager(this);
            node.SetSelected(false);
            initialPoses[node] = new Pose(node.transform.position, node.transform.rotation);
            if (node.gameObject.activeSelf) nodes.Add(node);
        }
        RefreshLinks();
    }

    public void ResetList(IReadOnlyList<int> values)
    {
        InitializeFromScene();
        LinkedListNode[] pool = authoredNodes.Where(n => n != null).Distinct().ToArray();
        int count = values == null ? 0 : values.Count;
        if (count > pool.Length || count - 1 > authoredLinks.Count(l => l != null))
        { Report("Not enough authored nodes/links for this reset; list unchanged."); return; }
        if (nodes.Any(n => n.IsHeld)) { Report("Release held nodes before resetting."); return; }
        connectionSource = null;
        SelectNode(null);
        nodes.Clear();
        for (int i = 0; i < pool.Length; i++)
        {
            LinkedListNode node = pool[i];
            node.SetNext(null);
            node.SetSelected(false);
            node.gameObject.SetActive(i < count);
            if (i >= count) continue;
            node.SetValue(values[i]);
            node.SetIndex(i);
            if (initialPoses.TryGetValue(node, out Pose pose)) node.transform.SetPositionAndRotation(pose.position, pose.rotation);
            nodes.Add(node);
        }
        for (int i = 0; i + 1 < nodes.Count; i++) nodes[i].SetNext(nodes[i + 1]);
        Changed("List reset. Grab to move; Connect Next changes pointers, not positions.");
    }

    public LinkedListNode AddNode(int value)
    {
        InitializeFromScene();
        LinkedListNode node = authoredNodes.FirstOrDefault(n => n != null && !nodes.Contains(n));
        if (node == null) { Report("All authored nodes are in use. Delete a node to reuse it."); return null; }
        node.SetManager(this);
        node.SetNext(null);
        node.SetValue(value);
        node.SetSelected(false);
        if (initialPoses.TryGetValue(node, out Pose pose)) node.transform.SetPositionAndRotation(pose.position, pose.rotation);
        node.gameObject.SetActive(true);
        nodes.Add(node);
        connectionSource = null;
        SelectNode(node);
        Changed("Node added, disconnected. Connect it to another node.");
        return node;
    }

    public LinkedListNode InsertAfterSelected(int value)
    {
        LinkedListNode source = selectedNode;
        LinkedListNode successor = source != null ? source.Next : null;
        if (source != null && EdgeCount() + 1 > authoredLinks.Count(l => l != null))
        { Report("No available link visuals. Disconnect a pointer first."); return null; }
        LinkedListNode added = AddNode(value);
        if (added == null) return null;
        if (source != null)
        {
            source.SetNext(added);
            added.SetNext(successor);
            Changed("Node inserted after the selected node; existing positions preserved.");
        }
        return added;
    }

    // Compatibility: index refers to traversal order, never a spatial slot.
    public LinkedListNode InsertAt(int index, int value)
    {
        if (!TryGetOrderedNodes(out List<LinkedListNode> ordered)) { Report("Connect the fragments before using indexed insertion."); return null; }
        int position = Mathf.Clamp(index, 0, ordered.Count);
        if (position > 0) { SelectNode(ordered[position - 1]); return InsertAfterSelected(value); }
        if (ordered.Count > 0 && EdgeCount() + 1 > authoredLinks.Count(l => l != null)) return null;
        LinkedListNode added = AddNode(value);
        if (added != null && ordered.Count > 0) { added.SetNext(ordered[0]); Changed("Node prepended."); }
        return added;
    }

    public bool BeginConnection()
    {
        if (selectedNode == null) { Report("Select a source node first."); return false; }
        connectionSource = selectedNode;
        SetConnectionMode(true);
        Report("Choose the target node for " + connectionSource.Value + " -> Next (or Cancel Connect).");
        return true;
    }

    public void CancelConnection()
    {
        connectionSource = null;
        SetConnectionMode(false);
        Report("Connection cancelled.");
    }

    public bool Connect(LinkedListNode source, LinkedListNode target)
    {
        if (!Owns(source) || !Owns(target)) { Report("Both nodes must belong to this workspace."); return false; }
        if (source == target) { Report("A node cannot connect to itself."); return false; }
        // Reject a cycle before mutating either the old output or target's old input.
        var visited = new HashSet<LinkedListNode>();
        for (LinkedListNode cursor = target; cursor != null; cursor = cursor.Next)
        {
            if (cursor == source || !visited.Add(cursor)) { Report("That connection would create a cycle. Disconnect first."); return false; }
        }
        LinkedListNode oldParent = nodes.FirstOrDefault(n => n != source && n.Next == target);
        int resultingEdges = EdgeCount() - (source.Next != null ? 1 : 0) - (oldParent != null ? 1 : 0) + 1;
        if (resultingEdges > authoredLinks.Count(l => l != null)) { Report("No free link visual; disconnect a pointer first."); return false; }
        if (oldParent != null) oldParent.SetNext(null);
        source.SetNext(target);
        connectionSource = null;
        SetConnectionMode(false);
        Changed(source.Value + " -> " + target.Value + ". Previous input/output connections were detached.");
        return true;
    }

    public bool DisconnectSelected()
    {
        if (selectedNode == null) { Report("Select a node first."); return false; }
        return Disconnect(selectedNode);
    }

    public bool Disconnect(LinkedListNode source)
    {
        if (!Owns(source)) return false;
        source.SetNext(null);
        connectionSource = null;
        Changed("Next pointer disconnected.");
        return true;
    }

    public bool DeleteSelected() => DeleteNode(selectedNode);

    public bool DeleteNode(LinkedListNode node)
    {
        if (!Owns(node)) { Report("Select a node first."); return false; }
        if (node.IsHeld) { Report("Release the node before deleting it."); return false; }
        // Deletion intentionally disconnects incident edges rather than inventing a new connection.
        foreach (LinkedListNode other in nodes) if (other.Next == node) other.SetNext(null);
        node.SetNext(null);
        node.SetSelected(false);
        nodes.Remove(node);
        if (connectionSource == node) connectionSource = null;
        if (selectedNode == node) SelectNode(null);
        node.gameObject.SetActive(false);
        Changed("Node deleted; incoming and outgoing pointers removed.");
        return true;
    }

    public bool EditSelected(int value)
    {
        if (selectedNode == null) { Report("Select a node first."); return false; }
        selectedNode.SetValue(value);
        Changed("Node value updated.");
        return true;
    }

    public void SelectNode(LinkedListNode node)
    {
        if (node != null && !Owns(node)) return;
        if (selectedNode != null) selectedNode.SetSelected(false);
        selectedNode = node;
        if (node != null) node.SetSelected(true);
        SelectionChanged?.Invoke(node);
        if (connectionSource != null && node != null) Connect(connectionSource, node);
    }

    // Retained for existing serialized events. Free placement never changes topology or position.
    public void PlaceNodeAtNearestSlot(LinkedListNode node) { if (Owns(node)) RefreshLinks(); }

    private bool Owns(LinkedListNode node) => node != null && nodes.Contains(node) && node.gameObject.activeSelf;
    private int EdgeCount() => nodes.Count(n => n != null && n.Next != null);
    private void Changed(string message) { RefreshLinks(); StructureChanged?.Invoke(); Report(message); }
    private void Report(string message) { LastMessage = message; Feedback?.Invoke(message); }

    /// <summary>
/// Enable/disable connection mode - when true, arrows become selectable for disconnection
/// </summary>
public void SetConnectionMode(bool enabled)
{
    isConnectionMode = enabled;
    ConnectionModeChanged?.Invoke(enabled);

    // Update all link interactables
    foreach (LinkInteraction link in authoredLinks.Where(l => l != null))
    {
        link.SetInteractable(enabled);
        link.SetManager(this);
    }
}

public void RefreshLinks()
{
    int edge = 0;
    LinkInteraction[] visuals = authoredLinks.Where(l => l != null).ToArray();
    foreach (LinkedListNode source in nodes)
    {
        if (!Owns(source) || !Owns(source.Next) || edge >= visuals.Length) continue;
        LinkInteraction link = visuals[edge++];
        link.SetEndpoints(source, source.Next);
        link.SetManager(this);
        link.SetInteractable(isConnectionMode);
        link.gameObject.SetActive(true);
    }
    for (; edge < visuals.Length; edge++)
    {
        visuals[edge].SetEndpoints(null, null);
        visuals[edge].SetInteractable(false);
        visuals[edge].gameObject.SetActive(false);
    }
}

    public bool TryGetOrderedNodes(out List<LinkedListNode> ordered)
    {
        ordered = new List<LinkedListNode>();
        if (nodes.Count == 0) return true;
        if (nodes.Any(n => !Owns(n)) || nodes.Distinct().Count() != nodes.Count) return false;
        var incoming = nodes.ToDictionary(n => n, n => 0);
        foreach (LinkedListNode node in nodes)
        {
            if (node.Next == null) continue;
            if (!incoming.ContainsKey(node.Next) || ++incoming[node.Next] > 1) return false;
        }
        var heads = nodes.Where(n => incoming[n] == 0).ToList();
        if (heads.Count != 1) return false;
        var seen = new HashSet<LinkedListNode>();
        for (LinkedListNode node = heads[0]; node != null; node = node.Next)
        {
            if (!seen.Add(node)) return false;
            ordered.Add(node);
        }
        return ordered.Count == nodes.Count;
    }

    public bool ValidateStructure() => TryGetOrderedNodes(out _);
    public int[] GetValues() => TryGetOrderedNodes(out List<LinkedListNode> ordered) ? ordered.Select(n => n.Value).ToArray() : Array.Empty<int>();
    public LinkedListValidationResult ValidateAgainst(IReadOnlyList<int> expected)
    {
        bool structure = TryGetOrderedNodes(out List<LinkedListNode> ordered);
        bool values = expected != null && nodes.All(n => n != null) && nodes.Select(n => n.Value).OrderBy(v => v).SequenceEqual(expected.OrderBy(v => v));
        bool order = structure && expected != null && ordered.Select(n => n.Value).SequenceEqual(expected);
        bool valid = structure && values && order;
        return new LinkedListValidationResult { IsValid = valid, StructureValid = structure, ValuesValid = values, OrderValid = order,
            Message = valid ? "Correct! One connected list with the expected values and order." :
            "Structure: " + (structure ? "OK" : "disconnected, cyclic or invalid") + "; values: " + (values ? "OK" : "mismatch") + "; order: " + (order ? "OK" : "mismatch") + "." };
    }
}
