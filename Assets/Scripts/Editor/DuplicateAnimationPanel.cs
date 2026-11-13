using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to duplicate the Animation Panel with all components.
/// Run from Unity Editor: Tools > Animation Panel > Duplicate Animation Panel
/// </summary>
public class DuplicateAnimationPanel
{
    [MenuItem("Tools/Animation Panel/Duplicate Animation Panel")]
    public static void DuplicatePanel()
    {
        // Find the original animation panel
        GameObject original = GameObject.Find("animation panel");
        if (original == null)
        {
            Debug.LogError("Could not find 'animation panel' GameObject in scene.");
            return;
        }

        // Duplicate the GameObject
        GameObject duplicate = UnityEngine.Object.Instantiate(original);
        duplicate.name = "animation panel 2";

        // Position it offset from the original
        Vector3 originalPos = original.transform.position;
        duplicate.transform.position = originalPos + new Vector3(4f, 0, 0); // 4 units to the right

        // Register undo
        Undo.RegisterCreatedObjectUndo(duplicate, "Duplicate Animation Panel");

        // Ensure the AnimationPanelController is enabled
        var controller = duplicate.GetComponent<AnimationPanelController>();
        if (controller != null)
        {
            controller.enabled = true;
        }

        // Also duplicate any child objects (the runtime-created UI will be created separately per instance)
        // The autoCreateUI=true will create separate UI for each panel at runtime

        Debug.Log($"Created 'animation panel 2' at position {duplicate.transform.position}");
        Debug.Log("Both panels will create their own UI at runtime via autoCreateUI.");

        Selection.activeGameObject = duplicate;
    }
}