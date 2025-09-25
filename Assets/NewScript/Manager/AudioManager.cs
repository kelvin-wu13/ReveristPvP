using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-500)] // inisialisasi lebih awal dari UI/Input
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("BGM Clips")]
    [SerializeField] private AudioClip mainMenuBGM;   // Main Menu + Character Select
    [SerializeField] private AudioClip pvpBGM;        // PvP/Battle

    [Header("Scene Keys (substring, ignore case)")]
    [SerializeField] private string[] menuAndSelectScenes = new[] { "MainMenu", "CharacterSelect" };
    [SerializeField] private string[] pvpScenes = new[] { "PvP", "Battle", "Arena" };

    [Header("Options")]
    [SerializeField] private bool loopBGM = true;

    [Header("Volumes")]
    [Range(0f, 1f)][SerializeField] private float masterBgmVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float masterSfxVolume = 1f;
    [SerializeField] private bool saveVolumes = true;
    const string PP_BGM = "VOL_BGM";
    const string PP_SFX = "VOL_SFX";

    [Header("Fade")]
    [SerializeField] private bool useFade = true;
    [SerializeField] private float fadeOutTime = 0.35f;
    [SerializeField] private float fadeInTime = 0.35f;

    [Header("UI SFX (default/opsional)")]
    [SerializeField] private AudioClip buttonHoverSFX;
    [SerializeField] private AudioClip buttonClickSFX;
    [SerializeField] private AudioClip buttonSelectSFX;

    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private Coroutine fadeRoutine;

    // ---------- BOOTSTRAP DARI RESOURCES ----------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoBootstrap()
    {
        if (Instance != null) return;

        // Cari prefab di Resources/Audio/AudioManager (WAJIB kamu buat di langkah setup)
        var prefab = Resources.Load<GameObject>("Audio/AudioManager");
        if (prefab != null)
        {
            var go = UnityEngine.Object.Instantiate(prefab);
            go.name = "AudioManager (bootstrapped)";
            Debug.Log("[Audio] Bootstrapped AudioManager dari Resources/Audio/AudioManager.");
        }
        else
        {
            Debug.LogWarning("[Audio] Tidak menemukan prefab 'Resources/Audio/AudioManager'. " +
                             "Buat dulu prefabnya agar BGM/SFX terkonfigurasi.");
        }
    }

    void Awake()
    {
        // SINGLETON
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // AudioSource
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = loopBGM;
        bgmSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        // Load saved volumes (jika ada)
        if (saveVolumes)
        {
            if (PlayerPrefs.HasKey(PP_BGM)) masterBgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PP_BGM, masterBgmVolume));
            if (PlayerPrefs.HasKey(PP_SFX)) masterSfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PP_SFX, masterSfxVolume));
        }
        ApplyVolumes();

        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log($"[Audio] Awake on '{SceneManager.GetActiveScene().name}' | main={NameOf(mainMenuBGM)}, pvp={NameOf(pvpBGM)}");
    }

    void Start()
    {
        DecideBgmForScene(SceneManager.GetActiveScene().name, firstInit: true);
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // --------------------- PUBLIC API ---------------------
    public void PlayMenuBGM() => SwitchBgm(mainMenuBGM, restartIfSame: false);
    public void PlayPvPBGM() => SwitchBgm(pvpBGM, restartIfSame: true);
    public void StopBGM(bool fade = true) => DoStopBgm(fade);

    // Volumes (untuk slider UI)
    public void SetBgmVolume(float v)
    {
        masterBgmVolume = Mathf.Clamp01(v);
        bgmSource.volume = masterBgmVolume;
        if (saveVolumes) PlayerPrefs.SetFloat(PP_BGM, masterBgmVolume);
    }
    public void SetSfxVolume(float v)
    {
        masterSfxVolume = Mathf.Clamp01(v);
        sfxSource.volume = masterSfxVolume;
        if (saveVolumes) PlayerPrefs.SetFloat(PP_SFX, masterSfxVolume);
    }
    public float GetBgmVolume() => masterBgmVolume;
    public float GetSfxVolume() => masterSfxVolume;

    // Default SFX (dipakai UISfxBinder bila override kosong)
    public void PlayButtonHover() { if (buttonHoverSFX) PlaySFX(buttonHoverSFX); }
    public void PlayButtonClick() { if (buttonClickSFX) PlaySFX(buttonClickSFX); }
    public void PlayButtonSelect() { if (buttonSelectSFX) PlaySFX(buttonSelectSFX); }

    public void PlaySFX(AudioClip clip)
    {
        if (!clip) return;
        sfxSource.PlayOneShot(clip);
    }

    // --------------------- SCENE SWITCH ---------------------
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[Audio] SceneLoaded -> '{scene.name}'");
        DecideBgmForScene(scene.name, firstInit: false);
    }

    void DecideBgmForScene(string sceneName, bool firstInit)
    {
        bool Match(string[] keys) =>
            keys != null && keys.Any(k =>
                !string.IsNullOrWhiteSpace(k) &&
                sceneName.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);

        if (Match(menuAndSelectScenes))
        {
            Debug.Log($"[Audio] Scene '{sceneName}' → MENU/SELECT BGM");
            if (bgmSource.clip != mainMenuBGM || !bgmSource.isPlaying)
                SwitchBgm(mainMenuBGM, restartIfSame: false); // lanjut mulus
        }
        else if (Match(pvpScenes))
        {
            Debug.Log($"[Audio] Scene '{sceneName}' → PVP BGM");
            SwitchBgm(pvpBGM, restartIfSame: true);          // ganti pasti
        }
        else
        {
            Debug.Log($"[Audio] Scene '{sceneName}' → DEFAULT (pakai PvP BGM)");
            SwitchBgm(pvpBGM, restartIfSame: false);
        }
    }

    void SwitchBgm(AudioClip target, bool restartIfSame)
    {
        if (!target) { Debug.LogWarning("[Audio] SwitchBgm target NULL."); return; }

        if (bgmSource.clip == target && !restartIfSame)
        {
            if (!bgmSource.isPlaying) bgmSource.Play();
            return;
        }

        if (!useFade)
        {
            bgmSource.Stop();
            bgmSource.clip = target;
            bgmSource.volume = masterBgmVolume;
            bgmSource.loop = true;
            bgmSource.Play();
            return;
        }

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(CoFadeTo(target));
    }

    IEnumerator CoFadeTo(AudioClip next)
    {
        float t = 0f, startVol = bgmSource.volume;
        while (t < fadeOutTime)
        {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, 0f, fadeOutTime <= 0f ? 1f : t / fadeOutTime);
            yield return null;
        }
        bgmSource.volume = 0f;

        bgmSource.Stop();
        bgmSource.clip = next;
        bgmSource.loop = true;
        bgmSource.Play();

        t = 0f;
        while (t < fadeInTime)
        {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(0f, masterBgmVolume, fadeInTime <= 0f ? 1f : t / fadeInTime);
            yield return null;
        }
        bgmSource.volume = masterBgmVolume;
        fadeRoutine = null;
    }

    void DoStopBgm(bool fade)
    {
        if (!bgmSource) return;
        if (!fade) { bgmSource.Stop(); return; }
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        StartCoroutine(CoFadeOutStop());
    }

    IEnumerator CoFadeOutStop()
    {
        float t = 0f, startVol = bgmSource.volume;
        while (t < fadeOutTime)
        {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, 0f, fadeOutTime <= 0f ? 1f : t / fadeOutTime);
            yield return null;
        }
        bgmSource.Stop();
        bgmSource.volume = masterBgmVolume;
    }

    // --------------------- helpers ---------------------
    void ApplyVolumes()
    {
        if (bgmSource) bgmSource.volume = masterBgmVolume;
        if (sfxSource) sfxSource.volume = masterSfxVolume;
    }
    static string NameOf(AudioClip c) => c ? c.name : "NULL";
}
