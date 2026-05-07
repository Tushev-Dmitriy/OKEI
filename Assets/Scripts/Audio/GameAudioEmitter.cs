using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public sealed class GameAudioEmitter : MonoBehaviour
{
    private readonly Dictionary<string, AudioSource> _loopSources = new();
    private AudioSource _oneShotSource;

    public void PlayLoop(string loopKey, AudioClip clip, float volume, float minDistance, float maxDistance)
    {
        if (clip == null)
            return;

        AudioSource loopSource = GetOrCreateLoopSource(loopKey);
        bool clipChanged = loopSource.clip != clip;
        if (clipChanged)
        {
            loopSource.Stop();
            loopSource.clip = clip;
        }

        loopSource.volume = Mathf.Clamp01(volume);
        loopSource.minDistance = Mathf.Max(0.1f, minDistance);
        loopSource.maxDistance = Mathf.Max(loopSource.minDistance + 0.1f, maxDistance);

        if (clipChanged || !loopSource.isPlaying)
            loopSource.Play();
    }

    public void StopLoop(string loopKey = "default")
    {
        if (!_loopSources.TryGetValue(loopKey, out AudioSource loopSource) || loopSource == null)
            return;

        loopSource.Stop();
        loopSource.clip = null;
    }

    public void PlayOneShot(AudioClip clip, float volume, float minDistance, float maxDistance)
    {
        if (clip == null)
            return;

        EnsureOneShotSource();
        _oneShotSource.minDistance = Mathf.Max(0.1f, minDistance);
        _oneShotSource.maxDistance = Mathf.Max(_oneShotSource.minDistance + 0.1f, maxDistance);
        _oneShotSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private AudioSource GetOrCreateLoopSource(string loopKey)
    {
        string key = string.IsNullOrWhiteSpace(loopKey) ? "default" : loopKey;
        if (_loopSources.TryGetValue(key, out AudioSource existing) && existing != null)
            return existing;

        AudioSource source = gameObject.AddComponent<AudioSource>();
        Configure3DSource(source);
        source.loop = true;
        _loopSources[key] = source;
        return source;
    }

    private void EnsureOneShotSource()
    {
        if (_oneShotSource != null)
            return;

        _oneShotSource = gameObject.AddComponent<AudioSource>();
        Configure3DSource(_oneShotSource);
        _oneShotSource.loop = false;
    }

    private static void Configure3DSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.dopplerLevel = 0f;
        GameAudio.ConfigureRuntimeSpatialSource(source);
    }
}
