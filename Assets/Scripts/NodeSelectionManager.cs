using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized selection manager for linked list nodes.
/// Coordinates between XR hand/controller and desktop mouse input.
/// Ensures consistent selection state (orange) and hover state (green).
/// </summary>
public sealed class NodeSelectionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LinkedListManager listManager;

    private static NodeSelectionManager instance;
    public static NodeSelectionManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<NodeSelectionManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (listManager == null)
        {
            listManager = FindFirstObjectByType<LinkedListManager>();
        }
    }

    /// <summary>
    /// Select a node (called by both XR and mouse input)
    /// </summary>
    public void SelectNode(LinkedListNode node)
    {
        if (listManager != null)
        {
            listManager.SelectNode(node);
        }
    }

    /// <summary>
    /// Get the currently selected node
    /// </summary>
    public LinkedListNode GetSelectedNode()
    {
        return listManager != null ? listManager.SelectedNode : null;
    }

    /// <summary>
    /// Clear current selection
    /// </summary>
    public void ClearSelection()
    {
        if (listManager != null)
        {
            listManager.SelectNode(null);
        }
    }

    /// <summary>
    /// Check if a specific node is currently selected
    /// </summary>
    public bool IsNodeSelected(LinkedListNode node)
    {
        return listManager != null && listManager.SelectedNode == node;
    }
}