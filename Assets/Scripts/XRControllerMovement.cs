using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// Moves the XR Origin from real/simulated XR controller thumbsticks.
/// Headset tracking continues to drive the camera pose; hand pinching remains node interaction.
public sealed class XRControllerMovement : MonoBehaviour
{
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Transform forwardSource;
    [SerializeField, Min(0.1f)] private float moveSpeed = 1.5f;

    private readonly List<InputDevice> devices = new List<InputDevice>();

    private void Update()
    {
        if (xrOrigin == null) return;

        Vector2 input = ReadStick(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller);
        input += ReadStick(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller);
        input = Vector2.ClampMagnitude(input, 1f);
        if (input == Vector2.zero) return;

        Transform source = forwardSource != null ? forwardSource : xrOrigin;
        Vector3 forward = source.forward;
        Vector3 right = source.right;
        forward.y = 0f;
        right.y = 0f;
        if (forward.sqrMagnitude < 0.001f) return;
        forward.Normalize();
        right.Normalize();
        xrOrigin.position += (right * input.x + forward * input.y) * moveSpeed * Time.deltaTime;
    }

    private Vector2 ReadStick(InputDeviceCharacteristics characteristics)
    {
        devices.Clear();
        InputDevices.GetDevicesWithCharacteristics(characteristics, devices);
        foreach (InputDevice device in devices)
            if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis)) return axis;
        return Vector2.zero;
    }
}
