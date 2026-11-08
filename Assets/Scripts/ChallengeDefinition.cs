using System;
using UnityEngine;

[Serializable]
public sealed class ChallengeDefinition
{
    public string id = "two-sum";
    public string title = "Two Sum";
    [TextArea(3, 8)] public string prompt = "Given an array of integers, return indices of two numbers that add up to target.";
    [TextArea(3, 12)] public string starterCode = "public int[] TwoSum(int[] nums, int target)\n{\n    // Write your solution here\n    return new int[0];\n}";
    [TextArea(2, 8)] public string expectedOutput = "[0, 1]";
}
