using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to add AnimationPanelController to "animation panel 2" GameObject.
/// Run from Unity Editor: Tools > Animation Panel > Setup Animation Panel 2
/// </summary>
public class SetupAnimationPanel2
{
    [MenuItem("Tools/Animation Panel/Setup Animation Panel 2")]
    public static void SetupPanel2()
    {
        // Find the "animation panel 2" GameObject
        GameObject panel2 = GameObject.Find("animation panel 2");
        if (panel2 == null)
        {
            Debug.LogError("Could not find 'animation panel 2' GameObject in scene. Create it first in the Hierarchy.");
            return;
        }

        // Add AnimationPanelController component
        var controller = panel2.GetComponent<AnimationPanelController>();
        if (controller == null)
        {
            controller = panel2.AddComponent<AnimationPanelController>();
        }

        // The component will use its default values from the script (autoCreateUI=true, panelWidth=3, panelHeight=2, etc.)
        // But we need to ensure the animationRoot exists - it will be auto-created at runtime

        // Also add BoxCollider for the panel background (if not present)
        if (panel2.GetComponent<BoxCollider>() == null)
        {
            panel2.AddComponent<BoxCollider>();
        }

        // Ensure it has a Transform with reasonable position
        // Position it offset from the original "animation panel"
        GameObject original = GameObject.Find("animation panel");
        if (original != null)
        {
            Vector3 origPos = original.transform.position;
            panel2.transform.position = origPos + new Vector3(4f, 0, 0); // 4 units to the right
        }

        // Set scale to match the compact 3x2 panel (script will override at runtime via autoCreateUI)
        panel2.transform.localScale = new Vector3(3f, 2f, 0.1f);

        Debug.Log("Added AnimationPanelController to 'animation panel 2'");
        Debug.Log("Both panels will create their own UI at runtime (autoCreateUI=true).");
        Debug.Log("At runtime, press Play on both panels in the Inspector to run them simultaneously.");

        Selection.activeGameObject = panel2;
    }
}