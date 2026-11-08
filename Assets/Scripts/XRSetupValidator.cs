using System;
using System.Text;
using UnityEngine;

/// Reports package/runtime availability without creating XR objects at runtime.
public sealed class XRSetupValidator : MonoBehaviour
{
    [SerializeField] private bool logOnStart = true;
    public string LastReport { get; private set; }

    private void Start()
    {
        if (logOnStart) Debug.Log(ValidateSetup(), this);
    }

    public string ValidateSetup()
    {
        StringBuilder report = new StringBuilder("Authored XR setup: ");
        bool openXR = Type.GetType("UnityEngine.XR.OpenXR.OpenXRSettings, Unity.XR.OpenXR") != null;
        bool toolkit = Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRInteractionManager, Unity.XR.Interaction.Toolkit") != null;
        bool origin = Type.GetType("Unity.XR.CoreUtils.XROrigin, Unity.XR.CoreUtils") != null;
        bool hands = Type.GetType("UnityEngine.XR.Hands.XRHandSubsystem, Unity.XR.Hands") != null;
        report.Append(openXR ? "OpenXR package found; " : "OpenXR package unavailable; ");
        report.Append(toolkit ? "XR Interaction Toolkit found; " : "XR Interaction Toolkit unavailable; ");
        report.Append(origin ? "XR Origin type found; " : "XR Origin type unavailable; ");
        report.Append(hands ? "XR Hands package found." : "XR Hands package unavailable.");
        LastReport = report.ToString();
        return LastReport;
    }
}
