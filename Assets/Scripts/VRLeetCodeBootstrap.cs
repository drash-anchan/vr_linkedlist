using UnityEngine;

/// Scene composition root. All XR, geometry, links, and UI are authored in the scene.
/// Runtime code only wires references and coordinates state changes.
public sealed class VRLeetCodeBootstrap : MonoBehaviour
{
    [Header("Authored scene references")]
    [SerializeField] private LinkedListManager listManager;
    [SerializeField] private ChallengeManager challengeManager;
    [SerializeField] private VRUIManager uiManager;
    [SerializeField] private XRSetupValidator xrSetupValidator;
    [SerializeField] private AnimationPanelController animationPanel;

    public LinkedListManager ListManager => listManager;
    public ChallengeManager Challenges => challengeManager;
    public VRUIManager UI => uiManager;

    private void Awake()
    {
        if (listManager == null || challengeManager == null || uiManager == null)
        {
            Debug.LogError("VR LeetCode Lab requires authored manager and UI references.", this);
            return;
        }

        listManager.InitializeFromScene();
        challengeManager.Initialize(listManager);
        uiManager.Initialize(challengeManager, listManager);

        // Auto-find AnimationPanelController if not assigned
        if (animationPanel == null)
        {
            animationPanel = FindFirstObjectByType<AnimationPanelController>();
        }

        // Initialize animation panel mouse controller
        if (animationPanel != null)
        {
            var mouseController = animationPanel.GetComponent<AnimationPanelMouseController>();
            if (mouseController == null)
            {
                mouseController = animationPanel.gameObject.AddComponent<AnimationPanelMouseController>();
            }
            mouseController.GetType().GetField("animationPanel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(mouseController, animationPanel);
        }

        // Enable desktop fallback for all nodes
        var enableFallback = new GameObject("EnableDesktopFallback").AddComponent<EnableDesktopFallback>();
        enableFallback.transform.SetParent(transform, false);

        // Enable mouse input for UI (Challenge Panel)
        var enableUIMouse = new GameObject("EnableUIMouseInput").AddComponent<EnableUIMouseInput>();
        enableUIMouse.transform.SetParent(transform, false);

        if (xrSetupValidator != null) xrSetupValidator.ValidateSetup();
    }
}
