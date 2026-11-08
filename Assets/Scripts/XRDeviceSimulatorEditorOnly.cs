using UnityEngine;

/// Keeps the desktop XR Device Simulator from disabling the real Quest HMD in a player build.
[DefaultExecutionOrder(-1000)]
public sealed class XRDeviceSimulatorEditorOnly : MonoBehaviour
{
    private void Awake()
    {
        if (Application.isEditor) return;

        Behaviour simulator = GetComponent("XRDeviceSimulator") as Behaviour;
        if (simulator != null) simulator.enabled = false;
    }
}
