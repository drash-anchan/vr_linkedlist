using UnityEngine;

/// Retained only as a migration guard. Lab geometry must be authored in the scene.
[System.Obsolete("Author the low-poly lab in VRLeetCodeLab.unity; runtime generation is disabled.")]
public sealed class ProceduralLowPolyScene : MonoBehaviour
{
    [SerializeField] private Transform authoredRoot;

    public Transform NodeRoot { get; private set; }
    public Transform LinkRoot { get; private set; }

    public void Build()
    {
        if (authoredRoot == null)
        {
            Debug.LogError("ProceduralLowPolyScene.Build is disabled. Assign authored scene content instead.", this);
            return;
        }
        NodeRoot = authoredRoot.Find("LinkedListNodes");
        LinkRoot = authoredRoot.Find("LinkedListLinks");
    }
}
