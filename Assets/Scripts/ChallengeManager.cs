using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class LinkedListChallenge
{
    public string id;
    public string title;
    [TextArea(2, 5)] public string prompt;
    public int[] targetValues;

    public LinkedListChallenge(string challengeId, string challengeTitle, string challengePrompt, int[] values)
    {
        id = challengeId; title = challengeTitle; prompt = challengePrompt; targetValues = values;
    }
}

/// Coordinates the current exercise and validation feedback.
public sealed class ChallengeManager : MonoBehaviour
{
    private readonly List<LinkedListChallenge> challenges = new List<LinkedListChallenge>();
    private LinkedListManager listManager;
    private int challengeIndex;

    public LinkedListChallenge Current => challenges.Count == 0 ? null : challenges[challengeIndex];
    public event Action<LinkedListChallenge> ChallengeChanged;
    public event Action<LinkedListValidationResult> ValidationCompleted;

    public void Initialize(LinkedListManager manager)
    {
        listManager = manager;
        challenges.Clear();
        challenges.Add(new LinkedListChallenge(
            "insert-middle",
            "Insert Node in the Middle of a Linked List",
            "Input: 1 → 2 → 4 → 5\nTask: Insert a node with value 3 between nodes 2 and 4.\nExpected output: 1 → 2 → 3 → 4 → 5.",
            new[] { 1, 2, 3, 4, 5 }));
        challengeIndex = 0;
        // Preserve the user's authored graph and poses on entering Play. Reset is explicit.
        ChallengeChanged?.Invoke(Current);
    }

    public void ResetCurrent()
    {
        if (Current == null || listManager == null) return;
        listManager.ResetList(new[] { 1, 2, 4, 5 });
        ValidationCompleted?.Invoke(new LinkedListValidationResult { Message = "List reset. Select a node to edit or delete it." });
    }

    public LinkedListValidationResult ValidateCurrent()
    {
        LinkedListValidationResult result = Current == null || listManager == null
            ? new LinkedListValidationResult { Message = "No challenge loaded." }
            : listManager.ValidateAgainst(Current.targetValues);
        ValidationCompleted?.Invoke(result);
        return result;
    }

    public void NextChallenge()
    {
        if (challenges.Count == 0 || listManager == null) return;
        challengeIndex = (challengeIndex + 1) % challenges.Count;
        ResetCurrent();
        ChallengeChanged?.Invoke(Current);
    }
}
