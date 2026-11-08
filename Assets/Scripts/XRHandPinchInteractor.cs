using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// Uses real XR Hands joints to drive an authored XRI interactor. No simulated hand poses.
/// Pinch near a node or a button. Release/tracking loss always ends selection.
public sealed class XRHandPinchInteractor : MonoBehaviour
{
    [SerializeField] private bool leftHand;
    [SerializeField] private Transform trackingSpace;
    [SerializeField] private XRDirectInteractor interactor;
    [SerializeField] private LinkedListManager listManager;
    [SerializeField] private HandPinchButton[] buttons;
    [SerializeField] private Transform thumbMarker;
    [SerializeField] private Transform indexMarker;
    [SerializeField, Min(0.005f)] private float pinchDistance = 0.025f;
    [SerializeField, Min(0.01f)] private float releaseDistance = 0.045f;
    [SerializeField, Min(0.01f)] private float grabRadius = 0.09f;
    private readonly List<XRHandSubsystem> subsystems = new List<XRHandSubsystem>();
    private XRHandSubsystem subsystem;
    private bool pinching;
    private LinkedListNode heldNode;
    private Vector3 heldOffset;
    private float lastSample;
    private bool reportedMissingSubsystem;
    public bool IsTracking { get; private set; }

    public void Configure(bool isLeft, Transform space, XRDirectInteractor direct, LinkedListManager manager,
        HandPinchButton[] controls, Transform thumb, Transform index)
    {
        leftHand = isLeft; trackingSpace = space; interactor = direct; listManager = manager;
        buttons = controls; thumbMarker = thumb; indexMarker = index;
    }

    private void Update()
    {
        if (subsystem == null || !subsystem.running)
        {
            if (subsystem != null) subsystem.updatedHands -= OnHands;
            subsystem = null;
            SubsystemManager.GetSubsystems(subsystems);
            foreach (XRHandSubsystem candidate in subsystems)
            {
                if (!candidate.running)
                {
                    try { candidate.Start(); }
                    catch (System.Exception exception)
                    {
                        if (!reportedMissingSubsystem)
                        {
                            Debug.LogWarning("XR Hands subsystem could not start: " + exception.Message, this);
                            reportedMissingSubsystem = true;
                        }
                    }
                }
                if (candidate.running)
                {
                    subsystem = candidate;
                    subsystem.updatedHands += OnHands;
                    reportedMissingSubsystem = false;
                    break;
                }
            }
            if (subsystem == null)
            {
                LoseTracking();
                if (!reportedMissingSubsystem)
                {
                    Debug.LogWarning("No running XR Hands subsystem. In XR Device Simulator press H to switch to hand mode.", this);
                    reportedMissingSubsystem = true;
                }
            }
        }
        if (IsTracking && Time.unscaledTime - lastSample > 0.25f) LoseTracking();
    }

    private void OnDisable()
    {
        if (subsystem != null) subsystem.updatedHands -= OnHands;
        subsystem = null;
        LoseTracking();
    }

    private void OnHands(XRHandSubsystem source, XRHandSubsystem.UpdateSuccessFlags flags, XRHandSubsystem.UpdateType updateType)
    {
        if (updateType != XRHandSubsystem.UpdateType.Dynamic && updateType != XRHandSubsystem.UpdateType.BeforeRender) return;
        XRHandSubsystem.UpdateSuccessFlags joints = leftHand ? XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints : XRHandSubsystem.UpdateSuccessFlags.RightHandJoints;
        XRHand hand = leftHand ? source.leftHand : source.rightHand;
        if ((flags & joints) == 0 || !hand.isTracked || trackingSpace == null || interactor == null ||
            !hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumb) ||
            !hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose index) ||
            !hand.GetJoint(XRHandJointID.Palm).TryGetPose(out Pose palm))
        { LoseTracking(); return; }
        IsTracking = true;
        lastSample = Time.unscaledTime;
        Vector3 point = trackingSpace.TransformPoint((thumb.position + index.position) * 0.5f);
        interactor.transform.SetPositionAndRotation(point, trackingSpace.rotation * palm.rotation);
        if (heldNode != null && heldNode.gameObject.activeInHierarchy) heldNode.transform.position = point + heldOffset;
        ShowMarker(thumbMarker, trackingSpace.TransformPoint(thumb.position));
        ShowMarker(indexMarker, trackingSpace.TransformPoint(index.position));
        float gap = Vector3.Distance(thumb.position, index.position);
        if (gap >= Mathf.Max(releaseDistance, pinchDistance + 0.005f))
        {
            Release();
        }
        else if (!pinching && gap <= pinchDistance)
        {
            pinching = true;
            BeginPinch(point);
        }
    }

    private void BeginPinch(Vector3 point)
    {
        HandPinchButton nearestButton = null;
        float closest = grabRadius;
        if (buttons != null)
            foreach (HandPinchButton button in buttons)
            {
                if (button == null || !button.CanPress) continue;
                float distance = button.DistanceTo(point);
                if (distance < closest) { closest = distance; nearestButton = button; }
            }
        if (nearestButton != null) { nearestButton.Press(); return; }
        if (listManager == null || heldNode != null) return;
        XRGrabInteractable nearest = null;
        closest = grabRadius;
        foreach (LinkedListNode node in listManager.Nodes)
        {
            if (node == null || !node.gameObject.activeInHierarchy || node.IsHeld) continue;
            XRGrabInteractable grab = node.GetComponent<XRGrabInteractable>();
            Collider collider = node.GetComponent<Collider>();
            if (grab == null || !grab.isActiveAndEnabled || grab.isSelected || collider == null) continue;
            float distance = Vector3.Distance(point, collider.ClosestPoint(point));
            if (distance < closest) { closest = distance; nearest = grab; }
        }
        if (nearest == null) return;
        heldNode = nearest.GetComponent<LinkedListNode>();
        if (heldNode == null) return;
        heldOffset = heldNode.transform.position - point;
        heldNode.IsHeld = true;
        listManager.SelectNode(heldNode);
    }

    private void Release()
    {
        if (heldNode != null) heldNode.IsHeld = false;
        heldNode = null;
        pinching = false;
    }
    private void LoseTracking()
    {
        Release(); IsTracking = false;
        if (thumbMarker != null) thumbMarker.gameObject.SetActive(false);
        if (indexMarker != null) indexMarker.gameObject.SetActive(false);
    }
    private static void ShowMarker(Transform marker, Vector3 position)
    { if (marker == null) return; marker.gameObject.SetActive(true); marker.position = position; }
}



