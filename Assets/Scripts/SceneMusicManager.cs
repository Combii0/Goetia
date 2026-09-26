using System.Collections;
using UnityEngine;

/// <summary>
/// Put one instance in each scene and assign its clips in the Inspector.
/// The audio sources persist between scenes so Shuffle can keep the menu track playing.
/// </summary>
public sealed class SceneMusicManager : MonoBehaviour
{
    public enum SceneMusicMode
    {
        MainMenu,
        Shuffle,
        Room
    }

    [Header("Scene")]
    public SceneMusicMode sceneMusicMode;
    [Tooltip("Only used when Scene Music is empty. Keeps the previous scene's track playing.")]
    public bool keepCurrentMusic;

    [Header("Music Assets")]
    public AudioClip sceneMusic;
    public AudioClip pauseMusic;

    [Header("Volumes")]
    [Range(0f, 1f)] public float sceneMusicVolume = 1f;
    [Range(0f, 1f)] public float pausedGameplayVolume = 0.25f;
    [Range(0f, 1f)] public float pauseMusicVolume = 1f;
    [Min(0f)] public float fadeDuration = 0.6f;

    [Header("Win / Death Fade")]
    [Range(0f, 1f)] public float endStateMusicVolume = 0.15f;
    [Min(0f)] public float endStateFadeDuration = 1.5f;

    [Header("SFX Assets")]
    public AudioClip uiSelectionSfx;
    public AudioClip cardSelectionSfx;
    public AudioClip maskTankImpactSfx;
    public AudioClip demonLightningSfx;
    public AudioClip playerHitSfx;

    [Header("SFX Volumes")]
    [Range(0f, 1f)] public float uiSelectionSfxVolume = 0.8f;
    [Range(0f, 1f)] public float cardSelectionSfxVolume = 1f;
    [Range(0f, 1f)] public float maskTankImpactSfxVolume = 1f;
    [Range(0f, 1f)] public float demonLightningSfxVolume = 1f;
    [Range(0f, 1f)] public float playerHitSfxVolume = 1f;

    private static SceneMusicManager activeManager;
    private static AudioSource gameplaySource;
    private static AudioSource secondaryGameplaySource;
    private static AudioSource pauseSource;
    private static AudioSource sfxSource;
    private static Coroutine fadeRoutine;
    private static MonoBehaviour coroutineHost;

    private void Awake()
    {
        activeManager = this;
        EnsureAudioSources();

        // Pause ambience only belongs to Room; never let it leak into another scene.
        pauseSource.Stop();
        pauseSource.volume = 0f;

        // A selected clip always wins. Keep Current Music is only used when this field is empty.
        if (sceneMusic != null)
        {
            CrossfadeToGameplayMusic(sceneMusic, sceneMusicVolume);
        }
        else if (keepCurrentMusic)
        {
            FadeTo(sceneMusicVolume, 0f, false);
        }
        else
        {
            FadeTo(0f, 0f, false);
        }
    }

    private void OnDestroy()
    {
        if (activeManager == this)
        {
            activeManager = null;
        }
    }

    /// <summary>Called by the pause menu. It uses unscaled time, so it works while timeScale is zero.</summary>
    public static void SetPaused(bool isPaused)
    {
        if (activeManager == null)
        {
            return;
        }

        activeManager.ApplyPauseState(isPaused);
    }

    public static void FadeForEndState()
    {
        if (activeManager == null)
        {
            return;
        }

        FadeTo(
            activeManager.endStateMusicVolume,
            0f,
            true,
            activeManager.endStateFadeDuration);
    }

    public static void PlayUiSelectionSfx()
    {
        PlaySfx(activeManager != null ? activeManager.uiSelectionSfx : null,
            activeManager != null ? activeManager.uiSelectionSfxVolume : 0f);
    }

    public static void PlayOneShotSfx(AudioClip clip, float volume = 1f)
    {
        PlaySfx(clip, volume);
    }

    public static void PlayCardSelectionSfx()
    {
        PlaySfx(activeManager != null ? activeManager.cardSelectionSfx : null,
            activeManager != null ? activeManager.cardSelectionSfxVolume : 0f);
    }

    public static void PlayMaskTankImpactSfx()
    {
        PlaySfx(activeManager != null ? activeManager.maskTankImpactSfx : null,
            activeManager != null ? activeManager.maskTankImpactSfxVolume : 0f);
    }

    public static void PlayDemonLightningSfx()
    {
        PlaySfx(activeManager != null ? activeManager.demonLightningSfx : null,
            activeManager != null ? activeManager.demonLightningSfxVolume : 0f);
    }

    public static void PlayPlayerHitSfx()
    {
        PlaySfx(activeManager != null ? activeManager.playerHitSfx : null,
            activeManager != null ? activeManager.playerHitSfxVolume : 0f);
    }

    private void ApplyPauseState(bool isPaused)
    {
        EnsureAudioSources();

        if (isPaused && pauseMusic != null)
        {
            if (pauseSource.clip != pauseMusic)
            {
                pauseSource.Stop();
                pauseSource.clip = pauseMusic;
            }

            if (!pauseSource.isPlaying)
            {
                pauseSource.volume = 0f;
                pauseSource.Play();
            }
        }

        float gameplayTarget = isPaused ? pausedGameplayVolume : sceneMusicVolume;
        float pauseTarget = isPaused && pauseMusic != null ? pauseMusicVolume : 0f;
        FadeTo(gameplayTarget, pauseTarget, !isPaused);
    }

    private static void EnsureAudioSources()
    {
        if (coroutineHost != null)
        {
            return;
        }

        GameObject root = new GameObject("PersistentMusicPlayer");
        DontDestroyOnLoad(root);
        coroutineHost = root.AddComponent<MusicCoroutineHost>();

        gameplaySource = root.AddComponent<AudioSource>();
        gameplaySource.loop = true;
        gameplaySource.playOnAwake = false;

        secondaryGameplaySource = root.AddComponent<AudioSource>();
        secondaryGameplaySource.loop = true;
        secondaryGameplaySource.playOnAwake = false;

        pauseSource = root.AddComponent<AudioSource>();
        pauseSource.loop = true;
        pauseSource.playOnAwake = false;

        sfxSource = root.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    private static void CrossfadeToGameplayMusic(AudioClip clip, float targetVolume)
    {
        if (gameplaySource.clip == clip && gameplaySource.isPlaying)
        {
            FadeTo(targetVolume, 0f, false);
            return;
        }

        if (!gameplaySource.isPlaying)
        {
            gameplaySource.Stop();
            gameplaySource.clip = clip;
            gameplaySource.volume = 0f;
            gameplaySource.Play();
            FadeTo(targetVolume, 0f, false);
            return;
        }

        if (fadeRoutine != null)
        {
            coroutineHost.StopCoroutine(fadeRoutine);
        }

        AudioSource outgoingSource = gameplaySource;
        AudioSource incomingSource = secondaryGameplaySource;
        incomingSource.Stop();
        incomingSource.clip = clip;
        incomingSource.volume = 0f;
        incomingSource.Play();

        // From now on, pause fades control the incoming (new) track.
        gameplaySource = incomingSource;
        secondaryGameplaySource = outgoingSource;
        float duration = activeManager != null ? activeManager.fadeDuration : 0f;
        fadeRoutine = coroutineHost.StartCoroutine(CrossfadeRoutine(
            outgoingSource,
            incomingSource,
            Mathf.Clamp01(targetVolume),
            duration));
    }

    private static void FadeTo(float gameplayTarget, float pauseTarget, bool stopPauseWhenDone, float durationOverride = -1f)
    {
        EnsureAudioSources();
        if (fadeRoutine != null)
        {
            coroutineHost.StopCoroutine(fadeRoutine);
        }

        // If a pause/end fade interrupts a scene crossfade, do not leave the
        // outgoing track playing forever at its last intermediate volume.
        if (secondaryGameplaySource != null && secondaryGameplaySource.isPlaying)
        {
            secondaryGameplaySource.Stop();
            secondaryGameplaySource.volume = 0f;
        }

        float duration = durationOverride >= 0f
            ? durationOverride
            : activeManager != null ? activeManager.fadeDuration : 0f;
        fadeRoutine = coroutineHost.StartCoroutine(FadeRoutine(
            Mathf.Clamp01(gameplayTarget),
            Mathf.Clamp01(pauseTarget),
            duration,
            stopPauseWhenDone));
    }

    private static IEnumerator FadeRoutine(float gameplayTarget, float pauseTarget, float duration, bool stopPauseWhenDone)
    {
        float gameplayStart = gameplaySource.volume;
        float pauseStart = pauseSource.volume;

        if (duration <= 0f)
        {
            gameplaySource.volume = gameplayTarget;
            pauseSource.volume = pauseTarget;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                gameplaySource.volume = Mathf.Lerp(gameplayStart, gameplayTarget, t);
                pauseSource.volume = Mathf.Lerp(pauseStart, pauseTarget, t);
                yield return null;
            }
        }

        if (stopPauseWhenDone && pauseTarget <= 0f)
        {
            pauseSource.Stop();
        }

        fadeRoutine = null;
    }

    private static void PlaySfx(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            return;
        }

        EnsureAudioSources();
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private static IEnumerator CrossfadeRoutine(AudioSource outgoingSource, AudioSource incomingSource, float incomingTarget, float duration)
    {
        float outgoingStart = outgoingSource.volume;

        if (duration <= 0f)
        {
            outgoingSource.volume = 0f;
            incomingSource.volume = incomingTarget;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                outgoingSource.volume = Mathf.Lerp(outgoingStart, 0f, t);
                incomingSource.volume = Mathf.Lerp(0f, incomingTarget, t);
                yield return null;
            }
        }

        outgoingSource.Stop();
        fadeRoutine = null;
    }

    private sealed class MusicCoroutineHost : MonoBehaviour { }
}
