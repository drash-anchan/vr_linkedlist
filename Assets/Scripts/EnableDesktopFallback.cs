using UnityEngine;

/// <summary>
/// Enables desktop fallback (mouse interaction) for all NodeInteraction components at runtime.
/// This allows mouse selection in Play Mode without modifying the scene.
/// </summary>
[DefaultExecutionOrder(-100)] // Run early
public sealed class EnableDesktopFallback : MonoBehaviour
{
    [SerializeField] private bool enableOnStart = true;

    private void Start()
    {
        if (!enableOnStart) return;

        NodeInteraction[] nodes = FindObjectsByType<NodeInteraction>(FindObjectsSortMode.None);
        foreach (var node in nodes)
        {
            // Use reflection to set the private field since it's serialized but private
            var field = typeof(NodeInteraction).GetField("desktopFallbackEnabled",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(node, true);
            }
        }

        Debug.Log($"EnableDesktopFallback: Enabled desktop fallback for {nodes.Length} nodes");
    }
}