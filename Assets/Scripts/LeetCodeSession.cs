using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class LeetCodeSession : MonoBehaviour
{
    [Header("Challenge")]
    [SerializeField] private ChallengeDefinition challenge = new ChallengeDefinition();
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button runButton;

    public ChallengeDefinition CurrentChallenge => challenge;

    private void Awake()
    {
        if (runButton != null) runButton.onClick.AddListener(RunSolution);
        LoadChallenge(challenge);
    }

    public void LoadChallenge(ChallengeDefinition definition)
    {
        challenge = definition ?? new ChallengeDefinition();
        if (titleText != null) titleText.text = challenge.title;
        if (promptText != null) promptText.text = challenge.prompt;
        if (codeInput != null) codeInput.text = challenge.starterCode;
        if (resultText != null) resultText.text = "Ready. Press Run Solution.";
    }

    public void RunSolution()
    {
        // Educational shell: a production build can route this text to a sandboxed judge service.
        // Keeping the local fallback deterministic makes desktop and headset demos usable offline.
        bool hasCode = codeInput != null && !string.IsNullOrWhiteSpace(codeInput.text);
        if (resultText != null) resultText.text = hasCode ? "Submitted locally ✓\nExpected: " + challenge.expectedOutput : "Enter code before running.";
    }
}
