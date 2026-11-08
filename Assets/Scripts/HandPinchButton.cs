using UnityEngine;
using UnityEngine.UI;

/// A thin, authored trigger on a world-space button, pressed once per nearby pinch.
[RequireComponent(typeof(Button), typeof(BoxCollider))]
public sealed class HandPinchButton : MonoBehaviour
{
    private Button button;
    private BoxCollider hitBox;
    public bool CanPress => isActiveAndEnabled && ResolveButton().IsInteractable();
    private Button ResolveButton() { if (button == null) button = GetComponent<Button>(); return button; }
    public float DistanceTo(Vector3 point)
    {
        if (hitBox == null) hitBox = GetComponent<BoxCollider>();
        return hitBox == null ? float.PositiveInfinity : Vector3.Distance(point, hitBox.ClosestPoint(point));
    }
    public void Press() { if (CanPress) ResolveButton().onClick.Invoke(); }
}
