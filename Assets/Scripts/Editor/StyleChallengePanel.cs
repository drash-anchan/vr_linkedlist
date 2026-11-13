using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to apply Animation Panel styling to Challenge Panel.
/// Run from Unity Editor: Tools > Animation Panel > Style Challenge Panel
/// </summary>
public class StyleChallengePanel
{
    [MenuItem("Tools/Animation Panel/Style Challenge Panel")]
    public static void StylePanel()
    {
        // Find the Challenge panel (3D background) - include inactive
        GameObject challengePanel = FindInactiveObjectByName("Challenge panel");
        if (challengePanel == null)
        {
            Debug.LogError("Could not find 'Challenge panel' GameObject in scene.");
            return;
        }

        // Find the ChallengePanelCanvas - include inactive
        GameObject canvasObj = FindInactiveObjectByName("ChallengePanelCanvas");
        if (canvasObj == null)
        {
            Debug.LogError("Could not find 'ChallengePanelCanvas' GameObject in scene.");
            return;
        }

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("ChallengePanelCanvas missing Canvas component.");
            return;
        }

        Undo.RecordObjects(new Object[] { challengePanel, canvasObj }, "Style Challenge Panel");

        // ===== 1. Style the 3D background (Challenge panel) =====
        // Set compact 3x2 size
        challengePanel.transform.localScale = new Vector3(3f, 2f, 0.1f);

        // Position it appropriately (keep current position or adjust)
        // The animation panel is at (13.71, 5.32, 1.66) - challenge panel at (-2.6, 2.09, 2.65)
        // Keep challenge panel position but ensure z-layering works

        // Update MeshRenderer material to dark charcoal semi-transparent
        var renderer = challengePanel.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.09f, 0.095f, 0.11f, 0.8f)); // #17181D ~80%
            mat.SetColor("_Color", new Color(0.09f, 0.095f, 0.11f, 0.8f));
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 1);
            mat.SetFloat("_SrcBlend", 5); // SrcAlpha
            mat.SetFloat("_DstBlend", 10); // OneMinusSrcAlpha
            mat.SetFloat("_ZWrite", 0);
            mat.renderQueue = 2500; // After opaque, before transparent UI
            renderer.sharedMaterial = mat;
        }

        // Position background BEHIND canvas (like animation panel: background at z=-0.1)
        // Canvas is child of some parent, need to check hierarchy
        var canvasTransform = canvasObj.transform;
        var panelTransform = challengePanel.transform;

        // Ensure background is at z=-0.1 relative to its parent
        Vector3 panelLocalPos = panelTransform.localPosition;
        panelLocalPos.z = -0.1f;
        panelTransform.localPosition = panelLocalPos;

        // ===== 2. Style the World Space Canvas for crisp 200px/unit text =====
        // Animation panel uses scale 0.005 (200px/unit)
        canvasObj.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);

        // Canvas size in pixels: 3 world units * 200 = 600 width, 2 * 200 = 400 height
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(600f, 400f);

        // Position canvas IN FRONT of background (z = +0.06 like animation panel)
        Vector3 canvasLocalPos = canvasObj.transform.localPosition;
        canvasLocalPos.z = 0.06f;
        canvasObj.transform.localPosition = canvasLocalPos;

        // Ensure canvas render mode is World Space
        canvas.renderMode = RenderMode.WorldSpace;

        // ===== 3. Update CanvasScaler for proper scaling =====
        var canvasScaler = canvasObj.GetComponent<UnityEngine.UI.CanvasScaler>();
        if (canvasScaler != null)
        {
            canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasScaler.scaleFactor = 1f;
            canvasScaler.referencePixelsPerUnit = 200f; // Match 200px/unit
        }

        // ===== 4. Ensure GraphicRaycaster exists for XR interaction =====
        if (canvasObj.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
        {
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        // TrackedDeviceGraphicRaycaster is optional - only add if XR Interaction Toolkit is available
        // (handled via reflection to avoid compile error if package not installed)
        var xrRaycaster = canvasObj.GetComponent("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster");
        if (xrRaycaster == null)
        {
            System.Type xrRaycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
            if (xrRaycasterType != null)
            {
                canvasObj.AddComponent(xrRaycasterType);
            }
        }

        Debug.Log("Challenge Panel styled to match Animation Panel:");
        Debug.Log("- 3x2 world units, dark charcoal background (#17181D ~80%)");
        Debug.Log("- Canvas at 200px/unit (scale 0.005) for crisp text");
        Debug.Log("- Background at z=-0.1 (renderQueue 2500), Canvas at z=+0.06");
        Debug.Log("- VRUIManager logic preserved");

        Selection.activeGameObject = challengePanel;
    }

    private static GameObject FindInactiveObjectByName(string name)
    {
        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.name == name && obj.hideFlags == HideFlags.None)
            {
                return obj;
            }
        }
        return null;
    }
}