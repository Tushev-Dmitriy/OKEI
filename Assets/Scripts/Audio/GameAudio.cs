using System;
using System.Collections.Generic;
using DevionGames;
using DevionGames.InventorySystem;
using DevionGames.UIWidgets;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public enum GameAudioLoopChannel
{
    Ambient,
    System,
    Alarm,
    Music
}

[DefaultExecutionOrder(-1000)]
public sealed class GameAudio : MonoBehaviour
{
    private const string AudioResourcesPath = "Audio";
    private const string RuntimeObjectName = "GameAudio";
    private const float SceneAmbientVolume = 0.09f;
    private const float Level1AmbientVolume = 0.3f;
    private const string MenuAmbientSceneCue = "AMB_Menu_Soft 1";
    private const string Level1AmbientSceneCue = "AMB_Level1_Lab 1";
    private const string Level2AmbientSceneCue = "AMB_Level2_Facility 1";
    private const string Level3AmbientSceneCue = "AMB_Level3_TerminalSpace 1";
    private const string Level4AmbientSceneCue = "AMB_Level4_FactoryClean 1";

    private static GameAudio _instance;

    private readonly Dictionary<string, AudioClip> _clips = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _spatialCueCooldowns = new(StringComparer.Ordinal);
    private AudioSource _uiSource;
    private AudioSource _oneShotSource;
    private readonly Dictionary<GameAudioLoopChannel, AudioSource> _loopSources = new();
    private readonly Dictionary<string, AudioSource> _namedLoopSources = new(StringComparer.Ordinal);
    private readonly HashSet<int> _boundInventoryWidgets = new();
    private AudioMixerGroup _sfxMixerGroup;
    private AudioMixerGroup _ambienceMixerGroup;
    private AudioMixerGroup _musicMixerGroup;
    private bool _mixerGroupsResolved;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static bool HasClip(string cueId)
    {
        return EnsureInstance() != null && _instance.TryGetClip(cueId, out _);
    }

    public static void PlayUi(string cueId, float volume = 1f)
    {
        if (EnsureInstance() == null)
            return;

        _instance.PlayOneShot(_instance._uiSource, cueId, volume);
    }

    public static void PlayGlobal(string cueId, float volume = 1f)
    {
        if (EnsureInstance() == null)
            return;

        _instance.PlayOneShot(_instance._oneShotSource, cueId, volume);
    }

    public static void PlayOn(Component owner, string cueId, float volume = 1f, float minDistance = 1f, float maxDistance = 18f)
    {
        if (EnsureInstance() == null || owner == null)
            return;

        if (!_instance.TryGetClip(cueId, out AudioClip clip))
            return;

        GameAudioEmitter emitter = owner.GetComponent<GameAudioEmitter>();
        if (emitter == null)
            emitter = owner.gameObject.AddComponent<GameAudioEmitter>();

        emitter.PlayOneShot(clip, volume, minDistance, maxDistance);
    }

    public static void PlayAtPoint(string cueId, Vector3 worldPosition, float volume = 1f, float minDistance = 1f, float maxDistance = 18f)
    {
        if (EnsureInstance() == null)
            return;

        if (_instance.ShouldSuppressSpatialCue(cueId))
            return;

        if (!_instance.TryGetClip(cueId, out AudioClip clip))
            return;

        GameObject temp = new GameObject($"Audio_{cueId}");
        temp.transform.position = worldPosition;
        AudioSource source = temp.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.dopplerLevel = 0f;
        source.minDistance = Mathf.Max(0.1f, minDistance);
        source.maxDistance = Mathf.Max(source.minDistance + 0.1f, maxDistance);
        _instance.AssignMixerGroup(source, _instance._sfxMixerGroup);
        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        source.Play();
        Destroy(temp, clip.length + 0.15f);
    }

    public static void PlayRandomAtPoint(Vector3 worldPosition, float volume, params string[] cueIds)
    {
        if (cueIds == null || cueIds.Length == 0)
            return;

        string cueId = cueIds[UnityEngine.Random.Range(0, cueIds.Length)];
        PlayAtPoint(cueId, worldPosition, volume);
    }

    public static void SetLoop(Component owner, string cueId, bool enabled, float volume = 1f, float minDistance = 1f, float maxDistance = 18f)
    {
        SetLoop(owner, "default", cueId, enabled, volume, minDistance, maxDistance);
    }

    public static void SetLoop(Component owner, string loopKey, string cueId, bool enabled, float volume = 1f, float minDistance = 1f, float maxDistance = 18f)
    {
        if (EnsureInstance() == null || owner == null)
            return;

        GameAudioEmitter emitter = owner.GetComponent<GameAudioEmitter>();
        if (emitter == null)
            emitter = owner.gameObject.AddComponent<GameAudioEmitter>();

        if (!enabled)
        {
            emitter.StopLoop(loopKey);
            return;
        }

        if (!_instance.TryGetClip(cueId, out AudioClip clip))
            return;

        emitter.PlayLoop(loopKey, clip, volume, minDistance, maxDistance);
    }

    public static void PlayLoop(GameAudioLoopChannel channel, string cueId, float volume = 1f)
    {
        if (EnsureInstance() == null)
            return;

        if (!_instance.TryGetClip(cueId, out AudioClip clip))
            return;

        _instance.PlayLoopInternal(channel, clip, volume);
    }

    private void PlayLoopInternal(GameAudioLoopChannel channel, AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            clip.LoadAudioData();
            StartCoroutine(PlayLoopWhenClipReady(channel, clip, volume));
            return;
        }

        PlayLoopOnSource(channel, clip, volume);
    }

    private System.Collections.IEnumerator PlayLoopWhenClipReady(GameAudioLoopChannel channel, AudioClip clip, float volume)
    {
        const float timeoutSeconds = 3f;
        float startedAt = Time.unscaledTime;

        while (clip != null &&
               clip.loadState == AudioDataLoadState.Loading &&
               Time.unscaledTime - startedAt < timeoutSeconds)
        {
            yield return null;
        }

        if (clip == null)
            yield break;

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogWarning($"[GameAudio] Failed to load loop clip '{clip.name}'. loadState={clip.loadState}");
            yield break;
        }

        PlayLoopOnSource(channel, clip, volume);
    }

    private void PlayLoopOnSource(GameAudioLoopChannel channel, AudioClip clip, float volume)
    {
        AudioSource source = _instance.GetLoopSource(channel);
        bool clipChanged = source.clip != clip;
        if (clipChanged)
        {
            source.Stop();
            source.clip = clip;
        }

        source.volume = Mathf.Clamp01(volume);
        source.loop = true;

        if (clipChanged || !source.isPlaying)
            source.Play();
    }

    public static void StopLoop(GameAudioLoopChannel channel)
    {
        if (EnsureInstance() == null)
            return;

        AudioSource source = _instance.GetLoopSource(channel);
        source.Stop();
        source.clip = null;
    }

    public static void SetGlobalLoop(string loopKey, string cueId, bool enabled, float volume = 1f)
    {
        if (EnsureInstance() == null)
            return;

        string key = string.IsNullOrWhiteSpace(loopKey) ? "default" : loopKey;
        AudioSource source = _instance.GetNamedLoopSource(key);
        if (!enabled)
        {
            source.Stop();
            source.clip = null;
            return;
        }

        if (!_instance.TryGetClip(cueId, out AudioClip clip))
            return;

        bool clipChanged = source.clip != clip;
        if (clipChanged)
        {
            source.Stop();
            source.clip = clip;
        }

        source.volume = Mathf.Clamp01(volume);
        source.loop = true;

        if (clipChanged || !source.isPlaying)
            source.Play();
    }

    internal static void ConfigureRuntimeSpatialSource(AudioSource source)
    {
        if (EnsureInstance() == null || source == null)
            return;

        _instance.AssignMixerGroup(source, _instance._sfxMixerGroup);
    }

    private static GameAudio EnsureInstance()
    {
        if (_instance != null)
            return _instance;

        GameObject runtimeObject = new GameObject(RuntimeObjectName);
        DontDestroyOnLoad(runtimeObject);
        _instance = runtimeObject.AddComponent<GameAudio>();
        return _instance;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        LoadClips();
        ResolveMixerGroups();
        CreateSources();
        ApplyMixerGroupsToExistingSources();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ApplyRuntimeOverrides();
    }

    private void OnDestroy()
    {
        if (_instance == this)
            SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void LoadClips()
    {
        _clips.Clear();
        AudioClip[] clips = Resources.LoadAll<AudioClip>(AudioResourcesPath);
        for (int i = 0; i < clips.Length; i++)
        {
            AudioClip clip = clips[i];
            if (clip == null || string.IsNullOrWhiteSpace(clip.name))
                continue;

            _clips[clip.name] = clip;
        }
    }

    private void CreateSources()
    {
        _uiSource = Create2DSource("UI", ignorePause: true, _sfxMixerGroup);
        _oneShotSource = Create2DSource("OneShot", ignorePause: false, _sfxMixerGroup);
        _loopSources[GameAudioLoopChannel.Ambient] = Create2DSource("AmbientLoop", ignorePause: false, _sfxMixerGroup);
        _loopSources[GameAudioLoopChannel.System] = Create2DSource("SystemLoop", ignorePause: false, _sfxMixerGroup);
        _loopSources[GameAudioLoopChannel.Alarm] = Create2DSource("AlarmLoop", ignorePause: false, _sfxMixerGroup);
        _loopSources[GameAudioLoopChannel.Music] = Create2DSource("MusicLoop", ignorePause: false, _musicMixerGroup);
    }

    private AudioSource Create2DSource(string name, bool ignorePause, AudioMixerGroup mixerGroup)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(transform, false);

        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = false;
        source.ignoreListenerPause = ignorePause;
        AssignMixerGroup(source, mixerGroup);
        return source;
    }

    private AudioSource GetLoopSource(GameAudioLoopChannel channel)
    {
        if (_loopSources.TryGetValue(channel, out AudioSource source) && source != null)
            return source;

        AudioMixerGroup mixerGroup = channel switch
        {
            GameAudioLoopChannel.Ambient => _sfxMixerGroup,
            GameAudioLoopChannel.Music => _musicMixerGroup,
            _ => _sfxMixerGroup
        };
        source = Create2DSource(channel.ToString(), ignorePause: false, mixerGroup);
        source.loop = true;
        _loopSources[channel] = source;
        return source;
    }

    private AudioSource GetNamedLoopSource(string loopKey)
    {
        if (_namedLoopSources.TryGetValue(loopKey, out AudioSource source) && source != null)
            return source;

        source = Create2DSource($"Loop_{loopKey}", ignorePause: false, _sfxMixerGroup);
        source.loop = true;
        _namedLoopSources[loopKey] = source;
        return source;
    }

    private bool TryGetClip(string cueId, out AudioClip clip)
    {
        if (string.IsNullOrWhiteSpace(cueId))
        {
            clip = null;
            return false;
        }

        if (_clips.Count == 0)
            LoadClips();

        return _clips.TryGetValue(cueId, out clip);
    }

    private void PlayOneShot(AudioSource source, string cueId, float volume)
    {
        if (source == null || !TryGetClip(cueId, out AudioClip clip))
            return;

        source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveMixerGroups();
        ApplyMixerGroupsToExistingSources();
        ApplyRuntimeOverrides();
        ApplySceneAudio(GetSceneAudioTargetName(scene, mode));
        BindInventoryAudio();
    }

    private static string GetSceneAudioTargetName(Scene loadedScene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
            return loadedScene.name;

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.isLoaded && !string.IsNullOrWhiteSpace(activeScene.name))
            return activeScene.name;

        return loadedScene.name;
    }

    private void ResolveMixerGroups()
    {
        if (_mixerGroupsResolved && _sfxMixerGroup != null && _ambienceMixerGroup != null)
            return;

        AudioSource[] sceneSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sceneSources.Length; i++)
        {
            AudioSource sceneSource = sceneSources[i];
            if (sceneSource == null || sceneSource.outputAudioMixerGroup == null)
                continue;

            AudioMixerGroup directGroup = sceneSource.outputAudioMixerGroup;
            AudioMixer mixer = directGroup.audioMixer;
            if (mixer == null)
                continue;

            CacheMixerGroupByName(directGroup);
            TryResolveMixerGroupFromMixer(mixer, "SFX", ref _sfxMixerGroup);
            TryResolveMixerGroupFromMixer(mixer, "Ambience", ref _ambienceMixerGroup);
            TryResolveMixerGroupFromMixer(mixer, "Music", ref _musicMixerGroup);
        }

        AudioMixerGroup[] groups = Resources.FindObjectsOfTypeAll<AudioMixerGroup>();
        for (int i = 0; i < groups.Length; i++)
        {
            AudioMixerGroup group = groups[i];
            if (group == null)
                continue;

            CacheMixerGroupByName(group);
        }

        _mixerGroupsResolved = _sfxMixerGroup != null || _ambienceMixerGroup != null || _musicMixerGroup != null;
    }

    private void CacheMixerGroupByName(AudioMixerGroup group)
    {
        if (group == null)
            return;

        if (_sfxMixerGroup == null && string.Equals(group.name, "SFX", StringComparison.OrdinalIgnoreCase))
            _sfxMixerGroup = group;
        else if (_ambienceMixerGroup == null && string.Equals(group.name, "Ambience", StringComparison.OrdinalIgnoreCase))
            _ambienceMixerGroup = group;
        else if (_musicMixerGroup == null && string.Equals(group.name, "Music", StringComparison.OrdinalIgnoreCase))
            _musicMixerGroup = group;
    }

    private void TryResolveMixerGroupFromMixer(AudioMixer mixer, string groupName, ref AudioMixerGroup targetGroup)
    {
        if (mixer == null || targetGroup != null || string.IsNullOrWhiteSpace(groupName))
            return;

        AudioMixerGroup[] matches = mixer.FindMatchingGroups(groupName);
        for (int i = 0; i < matches.Length; i++)
        {
            AudioMixerGroup match = matches[i];
            if (match == null)
                continue;

            if (string.Equals(match.name, groupName, StringComparison.OrdinalIgnoreCase))
            {
                targetGroup = match;
                return;
            }
        }
    }

    private void ApplyMixerGroupsToExistingSources()
    {
        AssignMixerGroup(_uiSource, _sfxMixerGroup);
        AssignMixerGroup(_oneShotSource, _sfxMixerGroup);

        foreach ((GameAudioLoopChannel channel, AudioSource source) in _loopSources)
        {
            AudioMixerGroup mixerGroup = channel switch
            {
                GameAudioLoopChannel.Ambient => _sfxMixerGroup,
                GameAudioLoopChannel.Music => _musicMixerGroup,
                _ => _sfxMixerGroup
            };

            AssignMixerGroup(source, mixerGroup);
        }

        foreach (AudioSource source in _namedLoopSources.Values)
        {
            AssignMixerGroup(source, _sfxMixerGroup);
        }
    }

    private void AssignMixerGroup(AudioSource source, AudioMixerGroup mixerGroup)
    {
        if (source == null || mixerGroup == null)
            return;

        source.outputAudioMixerGroup = mixerGroup;
    }

    private void ApplyRuntimeOverrides()
    {
        StarterAssets.ThirdPersonController[] controllers = FindObjectsByType<StarterAssets.ThirdPersonController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < controllers.Length; i++)
        {
            StarterAssets.ThirdPersonController controller = controllers[i];
            if (controller == null)
                continue;

            AudioClip landingClip = ResolveLandingClip(controller);
            if (landingClip != null)
                controller.LandingAudioClip = landingClip;
        }
    }

    private AudioClip ResolveLandingClip(StarterAssets.ThirdPersonController controller)
    {
        if (controller != null && controller.FootstepAudioClips != null && controller.FootstepAudioClips.Length > 0)
        {
            AudioClip footstepClip = controller.FootstepAudioClips[0];
            if (footstepClip != null)
                return footstepClip;
        }

        if (TryGetClip(AudioCueIds.PlayerLand, out AudioClip landingClip))
            return landingClip;

        return null;
    }

    private void ApplySceneAudio(string sceneName)
    {
        bool hasAmbient = TryGetSceneAmbientCue(sceneName, out string ambientCueId, out float ambientVolume);
        AudioSource ambientSource = GetLoopSource(GameAudioLoopChannel.Ambient);
        ambientSource.ignoreListenerPause = string.Equals(sceneName, "Level1", StringComparison.Ordinal);

        if (hasAmbient)
            PlayLoop(GameAudioLoopChannel.Ambient, ambientCueId, ambientVolume);
        else
            StopLoop(GameAudioLoopChannel.Ambient);

        // Keep one primary background bed per scene for now.
        // Dedicated music can be re-enabled later once the scene ambience is finalized.
        StopLoop(GameAudioLoopChannel.Music);
    }

    private bool TryGetSceneAmbientCue(string sceneName, out string ambientCueId, out float ambientVolume)
    {
        ambientCueId = null;
        ambientVolume = SceneAmbientVolume;

        switch (sceneName)
        {
            case "Bootstrap":
                ambientCueId = MenuAmbientSceneCue;
                return true;
            case "Level1":
                ambientCueId = Level1AmbientSceneCue;
                ambientVolume = Level1AmbientVolume;
                return true;
            case "Level2":
                ambientCueId = Level2AmbientSceneCue;
                return true;
            case "Level3":
                ambientCueId = Level3AmbientSceneCue;
                return true;
            case "Level4":
                ambientCueId = Level4AmbientSceneCue;
                return true;
            default:
                return false;
        }
    }

    private bool ShouldSuppressSpatialCue(string cueId)
    {
        if (string.IsNullOrWhiteSpace(cueId))
            return false;

        if (!IsDoorDebounceCue(cueId))
            return false;

        float now = Time.unscaledTime;
        if (_spatialCueCooldowns.TryGetValue(cueId, out float lastTime) && now - lastTime < 0.2f)
            return true;

        _spatialCueCooldowns[cueId] = now;
        return false;
    }

    private static bool IsDoorDebounceCue(string cueId)
    {
        return string.Equals(cueId, AudioCueIds.Level3DoorOpenLight, StringComparison.Ordinal) ||
               string.Equals(cueId, AudioCueIds.Level3DoorCloseLight, StringComparison.Ordinal);
    }

    private void BindInventoryAudio()
    {
        ItemContainer[] containers = WidgetUtility.FindAll<ItemContainer>("Inventory");
        for (int i = 0; i < containers.Length; i++)
        {
            ItemContainer container = containers[i];
            if (container == null)
                continue;

            int id = container.GetInstanceID();
            if (!_boundInventoryWidgets.Add(id))
                continue;

            container.RegisterListener("OnShow", HandleInventoryShown);
            container.RegisterListener("OnClose", HandleInventoryClosed);
            container.RegisterListener("OnAddItem", HandleInventoryItemAdded);
        }
    }

    private void HandleInventoryShown(CallbackEventData eventData)
    {
        PlayUi(AudioCueIds.UiInventoryOpen, 0.9f);
    }

    private void HandleInventoryClosed(CallbackEventData eventData)
    {
        PlayUi(AudioCueIds.UiInventoryClose, 0.9f);
    }

    private void HandleInventoryItemAdded(CallbackEventData eventData)
    {
        PlayUi(AudioCueIds.Level3ArtifactPickupVariants[0], 0.8f);
    }
}

[DisallowMultipleComponent]
public sealed class GameAudioPointerBridge : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private string hoverCueId;
    [SerializeField] private float hoverVolume = 0.85f;
    [SerializeField] private string clickCueId;
    [SerializeField] private float clickVolume = 0.9f;

    public void Configure(string hoverCue, float hoverCueVolume, string clickCue, float clickCueVolume)
    {
        hoverCueId = hoverCue;
        hoverVolume = hoverCueVolume;
        clickCueId = clickCue;
        clickVolume = clickCueVolume;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!string.IsNullOrWhiteSpace(hoverCueId))
            GameAudio.PlayUi(hoverCueId, hoverVolume);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!string.IsNullOrWhiteSpace(clickCueId))
            GameAudio.PlayUi(clickCueId, clickVolume);
    }
}
