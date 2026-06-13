using UnityEngine;

public sealed class GameSfxManager : MonoBehaviour
{
    private static GameSfxManager instance;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip recordingStartClip;
    [SerializeField] private AudioClip recordingStopClip;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private Vector2 deathPitchRange = new Vector2(0.9f, 1.12f);
    [SerializeField] private Vector2 shootPitchRange = new Vector2(0.98f, 1.05f);

    public static GameSfxManager Instance => instance != null ? instance : FindAnyObjectByType<GameSfxManager>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = volume;
    }

    public void PlayShoot()
    {
        PlayClip(shootClip, Random.Range(shootPitchRange.x, shootPitchRange.y), volume);
    }

    public void PlayDeath()
    {
        PlayClip(deathClip, Random.Range(deathPitchRange.x, deathPitchRange.y), Random.Range(0.9f, 1.05f));
    }

    public void PlayDeath(float volumeScale)
    {
        PlayClip(deathClip, Random.Range(deathPitchRange.x, deathPitchRange.y), Mathf.Clamp01(volume * volumeScale));
    }

    public void PlayRecordingStart()
    {
        PlayClip(recordingStartClip, 1f, volume);
    }

    public void PlayRecordingStop()
    {
        PlayClip(recordingStopClip, 1f, volume);
    }

    private void PlayClip(AudioClip clip, float pitch, float clipVolume)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, clipVolume);
        audioSource.pitch = 1f;
    }
}
