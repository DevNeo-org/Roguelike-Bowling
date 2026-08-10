using System.Collections;
using UnityEngine;

public enum BGM
{
    MainMenu,
    Gameplay,
}

public enum SFX
{
    BallRoll,
    PinHit,
    Strike,
    Spare,
    Gutter,
    BallRespawn,
    ButtonClick,
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("BGM")]
    [SerializeField] private AudioClip[] bgmClips;
    [SerializeField] private float defaultFadeDuration = 0.5f;

    [Header("SFX")]
    [SerializeField] private AudioClip[] sfxClips;

    [Header("Volume (Inspector 초기값)")]
    [SerializeField, Range(0f, 1f)] private float initialBgmVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float initialSfxVolume = 1f;

    private AudioSource _bgmSource;
    private AudioSource _sfxSource;

    private const string BgmVolumeKey = "Sound_BgmVolume";
    private const string SfxVolumeKey = "Sound_SfxVolume";

    public float BgmVolume { get; private set; }
    public float SfxVolume { get; private set; }

    private Coroutine _bgmFadeCoroutine;

    // ─────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateAudioSources();
        LoadVolumes();
    }

    private void CreateAudioSources()
    {
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.loop = false;
        _sfxSource.playOnAwake = false;
        _sfxSource.volume = 1f; // 볼륨은 PlayOneShot의 volumeScale로만 관리
    }

    private void LoadVolumes()
    {
        BgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, initialBgmVolume);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, initialSfxVolume);

        _bgmSource.volume = BgmVolume;
    }

    // ─────────────────────────────────────────────
    // BGM
    // ─────────────────────────────────────────────

    /// <summary>BGM enum으로 BGM을 재생합니다. 배열 순서는 BGM enum과 일치해야 합니다.</summary>
    public void PlayBGM(BGM bgm, bool fade = false)
    {
        int index = (int)bgm;
        if (bgmClips == null || index < 0 || index >= bgmClips.Length)
        {
            Debug.LogWarning($"[SoundManager] BGM.{bgm}에 해당하는 클립이 없습니다. (index: {index})");
            return;
        }
        PlayBGM(bgmClips[index], fade);
    }

    /// <summary>BGM을 재생합니다. 이미 같은 클립이 재생 중이면 무시합니다.</summary>
    public void PlayBGM(AudioClip clip, bool fade = false)
    {
        if (clip == null) return;

        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

        if (_bgmFadeCoroutine != null)
            StopCoroutine(_bgmFadeCoroutine);

        if (fade)
            _bgmFadeCoroutine = StartCoroutine(CrossfadeBGM(clip));
        else
        {
            _bgmSource.clip = clip;
            _bgmSource.volume = BgmVolume;
            _bgmSource.Play();
        }
    }

    /// <summary>BGM을 정지합니다.</summary>
    public void StopBGM(bool fade = false)
    {
        if (!_bgmSource.isPlaying && _bgmFadeCoroutine == null) return;

        if (_bgmFadeCoroutine != null)
            StopCoroutine(_bgmFadeCoroutine);

        if (fade)
            _bgmFadeCoroutine = StartCoroutine(FadeOutBGM());
        else
            _bgmSource.Stop();
    }

    /// <summary>BGM을 일시 정지합니다.</summary>
    public void PauseBGM() => _bgmSource.Pause();

    /// <summary>일시 정지된 BGM을 재개합니다.</summary>
    public void ResumeBGM() => _bgmSource.UnPause();

    // ─────────────────────────────────────────────
    // SFX
    // ─────────────────────────────────────────────

    /// <summary>SFX enum으로 효과음을 재생합니다. 배열 순서는 SFX enum과 일치해야 합니다.</summary>
    public void PlaySFX(SFX sfx, float volumeScale = 1f)
    {
        int index = (int)sfx;
        if (sfxClips == null || index < 0 || index >= sfxClips.Length)
        {
            Debug.LogWarning($"[SoundManager] SFX.{sfx}에 해당하는 클립이 없습니다. (index: {index})");
            return;
        }
        PlaySFX(sfxClips[index], volumeScale);
    }

    /// <summary>효과음을 재생합니다. volumeScale로 개별 클립 볼륨을 조절합니다.</summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip, SfxVolume * volumeScale);
    }

    // ─────────────────────────────────────────────
    // Volume Control
    // ─────────────────────────────────────────────

    /// <summary>BGM 볼륨을 설정하고 PlayerPrefs에 저장합니다.</summary>
    public void SetBgmVolume(float volume)
    {
        BgmVolume = Mathf.Clamp01(volume);
        _bgmSource.volume = BgmVolume;
        PlayerPrefs.SetFloat(BgmVolumeKey, BgmVolume);
        PlayerPrefs.Save();
    }

    /// <summary>SFX 볼륨을 설정하고 PlayerPrefs에 저장합니다.</summary>
    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        PlayerPrefs.Save();
    }

    // ─────────────────────────────────────────────
    // Fade Coroutines
    // ─────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnValidate()
    {
        int bgmCount = System.Enum.GetValues(typeof(BGM)).Length;
        int sfxCount = System.Enum.GetValues(typeof(SFX)).Length;

        if (bgmClips != null && bgmClips.Length != bgmCount)
            Debug.LogWarning($"[SoundManager] bgmClips 크기가 {bgmCount}이어야 합니다. (현재: {bgmClips.Length})");
        if (sfxClips != null && sfxClips.Length != sfxCount)
            Debug.LogWarning($"[SoundManager] sfxClips 크기가 {sfxCount}이어야 합니다. (현재: {sfxClips.Length})");
    }
#endif

    private IEnumerator CrossfadeBGM(AudioClip nextClip)
    {
        // 페이드 아웃
        float startVolume = _bgmSource.volume;
        float elapsed = 0f;

        while (elapsed < defaultFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / defaultFadeDuration);
            yield return null;
        }

        _bgmSource.Stop();
        _bgmSource.clip = nextClip;
        _bgmSource.Play();

        // 페이드 인
        elapsed = 0f;
        while (elapsed < defaultFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _bgmSource.volume = Mathf.Lerp(0f, BgmVolume, elapsed / defaultFadeDuration);
            yield return null;
        }

        _bgmSource.volume = BgmVolume;
        _bgmFadeCoroutine = null;
    }

    private IEnumerator FadeOutBGM()
    {
        float startVolume = _bgmSource.volume;
        float elapsed = 0f;

        while (elapsed < defaultFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / defaultFadeDuration);
            yield return null;
        }

        _bgmSource.Stop();
        _bgmSource.volume = BgmVolume;
        _bgmFadeCoroutine = null;
    }
}
