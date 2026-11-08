using UnityEngine;
using UnityEngine.EventSystems;
using System.Reflection;
using UnityEngine.UI;

/// <summary>
/// Enables mouse input for UI at runtime.
/// This allows the Challenge Panel (World Space Canvas) to work with mouse in Play Mode.
/// Does not modify project settings or XR Device Simulator bindings.
/// </summary>
[DefaultExecutionOrder(-50)]
public sealed class EnableUIMouseInput : MonoBehaviour
{
    [SerializeField] private bool enableOnStart = true;

    private void Start()
    {
        if (!enableOnStart) return;

        // Find the XRUIInputModule (XR Interaction Toolkit's input module)
        // It's in UnityEngine.XR.Interaction.Toolkit.UI namespace
        var eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem != null)
        {
            // Get all BaseInputModule components
            var inputModules = eventSystem.GetComponents<BaseInputModule>();
            foreach (var module in inputModules)
            {
                // Check if it's the XRUIInputModule by type name
                if (module.GetType().Name == "XRUIInputModule")
                {
                    // Enable mouse input via reflection
                    var field = module.GetType().GetField("m_EnableMouseInput",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(module, true);
                        Debug.Log("EnableUIMouseInput: Enabled mouse input on XRUIInputModule");
                    }
                }
            }

            // Also ensure StandaloneInputModule is present and enabled
            var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (standaloneModule != null)
            {
                standaloneModule.enabled = true;
                Debug.Log("EnableUIMouseInput: Ensured StandaloneInputModule is enabled");
            }
            else
            {
                // Add StandaloneInputModule if missing
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
                Debug.Log("EnableUIMouseInput: Added StandaloneInputModule to EventSystem");
            }
        }
    }
}