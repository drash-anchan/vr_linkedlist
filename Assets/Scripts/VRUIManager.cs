using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// Binds authored controls; all actions also work through near-hand pinch buttons.
public sealed class VRUIManager : MonoBehaviour
{
    [Header("Challenge Panel")]
    [SerializeField] private Canvas challengeCanvas;
    [SerializeField] private Font legacyRuntimeFont;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TMP_InputField valueInput;
    [SerializeField] private Button insertButton;
    [SerializeField] private Button editButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button validateButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Button cancelConnectButton;
    [SerializeField] private Button addButton;
    [SerializeField] private Button increaseButton;
    [SerializeField] private Button decreaseButton;

    [Header("Animation Panel")]
    [SerializeField] private AnimationPanelController animationPanel;

    private ChallengeManager challengeManager;
    private LinkedListManager listManager;

    public void Initialize(ChallengeManager challenges, LinkedListManager list)
    {
        Unbind();
        challengeManager = challenges;
        listManager = list;
        if (challenges == null || list == null) { SetStatus("Missing lab manager references."); return; }
        BindButtons(true);
        challenges.ChallengeChanged += RenderChallenge;
        challenges.ValidationCompleted += RenderValidation;
        list.Feedback += SetStatus;
        list.SelectionChanged += RenderSelection;
        list.ConnectionModeChanged += OnConnectionModeChanged;
        RenderChallenge(challenges.Current);
    }

    private void OnDestroy() => Unbind();
    private void Unbind()
    {
        BindButtons(false);
        if (challengeManager != null) { challengeManager.ChallengeChanged -= RenderChallenge; challengeManager.ValidationCompleted -= RenderValidation; }
        if (listManager != null) { listManager.Feedback -= SetStatus; listManager.SelectionChanged -= RenderSelection; listManager.ConnectionModeChanged -= OnConnectionModeChanged; }
    }
    private void BindButtons(bool add)
    {
        Bind(insertButton, Insert, add); Bind(editButton, Edit, add); Bind(deleteButton, Delete, add);
        Bind(validateButton, Validate, add); Bind(resetButton, ResetCurrent, add); Bind(addButton, AddNode, add);
        Bind(connectButton, BeginConnect, add); Bind(disconnectButton, Disconnect, add); Bind(cancelConnectButton, CancelConnect, add);
        Bind(increaseButton, IncreaseValue, add); Bind(decreaseButton, DecreaseValue, add);
    }
    private static void Bind(Button button, UnityAction action, bool add)
    { if (button == null) return; button.onClick.RemoveListener(action); if (add) button.onClick.AddListener(action); }
    private bool TryValue(out int value)
    {
        if (int.TryParse(valueInput == null ? "" : valueInput.text, out value)) return true;
        SetStatus("Enter a valid 32-bit integer, or use +/-.");
        return false;
    }
    public void Insert() { if (TryValue(out int value)) listManager?.InsertAfterSelected(value); }
    public void AddNode() { if (TryValue(out int value)) listManager?.AddNode(value); }
    public void Edit() { if (TryValue(out int value)) listManager?.EditSelected(value); }
    public void Delete() => listManager?.DeleteSelected();
    public void Validate() => challengeManager?.ValidateCurrent();
    public void ResetCurrent() => challengeManager?.ResetCurrent();
    public void BeginConnect() => listManager?.BeginConnection();
    public void Disconnect() => listManager?.DisconnectSelected();
    public void CancelConnect() => listManager?.CancelConnection();
    public void IncreaseValue() => AdjustValue(1);
    public void DecreaseValue() => AdjustValue(-1);
    private void AdjustValue(int delta)
    {
        if (valueInput == null) return;
        int.TryParse(valueInput.text, out int value);
        long next = (long)value + delta;
        if (next < int.MinValue || next > int.MaxValue) { SetStatus("Integer limit reached."); return; }
        valueInput.text = next.ToString();
    }
    private void RenderSelection(LinkedListNode node) => SetStatus(node == null ? "No node selected." : "Selected " + node.Value + ". Move it freely, or choose a pointer action.");

    private void OnConnectionModeChanged(bool isConnectionMode)
    {
        // Update button states based on connection mode
        if (connectButton != null) connectButton.interactable = !isConnectionMode;
        if (disconnectButton != null) disconnectButton.interactable = !isConnectionMode;
        if (cancelConnectButton != null) cancelConnectButton.interactable = isConnectionMode;
        if (insertButton != null) insertButton.interactable = !isConnectionMode;
        if (editButton != null) editButton.interactable = !isConnectionMode;
        if (deleteButton != null) deleteButton.interactable = !isConnectionMode;
        if (addButton != null) addButton.interactable = !isConnectionMode;

        if (isConnectionMode)
        {
            SetStatus("Connection mode: Select target node for " + (listManager.ConnectionSource != null ? listManager.ConnectionSource.Value + " -> Next" : "Next"));
        }
    }

    private void RenderChallenge(LinkedListChallenge challenge)
    {
        if (titleText != null) titleText.text = "Linked List Lab";
        if (promptText != null) promptText.text = "Pinch a node to move it. Connect Next, then pinch the target.\nDisconnect removes Next. Release, then Delete removes a node.";
        SetStatus("Build 1 -> 2 -> 3 -> 4 -> 5. Positions do not determine list order.");
    }
    private void RenderValidation(LinkedListValidationResult result) => SetStatus(result == null ? "" : result.Message);
    private void SetStatus(string message) { if (statusText != null) statusText.text = message; }
}
