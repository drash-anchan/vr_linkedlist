using UnityEngine;

public sealed class WorldSpaceUIAnchor : MonoBehaviour
{
    [SerializeField] private Transform head;
    [SerializeField] private float distance = 1.6f;
    [SerializeField] private float height = 1.45f;

    private void LateUpdate()
    {
        if (head == null && Camera.main != null) head = Camera.main.transform;
        if (head == null) return;
        transform.position = head.position + head.forward * distance + Vector3.up * (height - head.position.y);
        transform.rotation = Quaternion.LookRotation(transform.position - head.position, Vector3.up);
    }
}
