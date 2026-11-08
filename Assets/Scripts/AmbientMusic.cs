using UnityEngine;

/// Low-volume, looping ambient music for the sunset lab.
public sealed class AmbientMusic : MonoBehaviour
{
    [SerializeField] private AudioClip clip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.12f;
    private AudioSource source;

    private void Start()
    {
        if (clip == null) return;
        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = volume;
        source.spatialBlend = 0f;
        source.priority = 128;
        source.Play();
    }
}
