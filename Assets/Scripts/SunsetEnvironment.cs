using UnityEngine;
using UnityEngine.Rendering;

/// Creates a warm procedural sunset sky and soft, long-distance environment lighting.
[ExecuteAlways]
public sealed class SunsetEnvironment : MonoBehaviour
{
    [SerializeField] private Light sun;
    [SerializeField] private Light fill;
    [SerializeField, Min(0f)] private float fogDensity = 0.0035f;
    private Material skyboxMaterial;

    private void OnEnable() => Apply();

    private void Start() => Apply();

    private void Apply()
    {
        Shader shader = Shader.Find("Skybox/Procedural");
        if (shader != null)
        {
            if (skyboxMaterial == null || skyboxMaterial.shader != shader)
            {
                skyboxMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
            }

            skyboxMaterial.SetColor("_SkyTint", new Color(0.62f, 0.25f, 0.12f));
            skyboxMaterial.SetColor("_GroundColor", new Color(0.36f, 0.16f, 0.08f));
            skyboxMaterial.SetColor("_SunTint", new Color(1f, 0.48f, 0.22f));
            skyboxMaterial.SetFloat("_AtmosphereThickness", 1.12f);
            skyboxMaterial.SetFloat("_Exposure", 0.82f);
            skyboxMaterial.SetFloat("_SunSize", 0.045f);
            skyboxMaterial.SetFloat("_SunSizeConvergence", 5f);
            skyboxMaterial.SetFloat("_SunDisk", 2f);
            RenderSettings.skybox = skyboxMaterial;
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.48f, 0.22f, 0.11f);
        RenderSettings.ambientEquatorColor = new Color(0.28f, 0.13f, 0.075f);
        RenderSettings.ambientGroundColor = new Color(0.10f, 0.055f, 0.03f);
        RenderSettings.ambientIntensity = 0.9f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.reflectionIntensity = 0.75f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.42f, 0.20f, 0.11f);
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.sun = sun;

        ConfigureLight(sun, new Color(1f, 0.55f, 0.30f), 1.25f,
            Quaternion.Euler(18f, -35f, 0f));
        ConfigureLight(fill, new Color(0.32f, 0.16f, 0.10f), 0.35f,
            Quaternion.Euler(38f, 145f, 0f));
        QualitySettings.shadowDistance = 80f;
    }

    private static void ConfigureLight(Light light, Color color, float intensity, Quaternion rotation)
    {
        if (light == null) return;
        light.type = LightType.Directional;
        light.color = color;
        light.intensity = intensity;
        light.transform.rotation = rotation;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.75f;
    }
}
