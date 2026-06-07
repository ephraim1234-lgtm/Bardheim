using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace Bardheim.Audio;

public sealed class LocalDrumPlayback
{
    private readonly List<float> _activeUntil = new();
    private readonly DrumHitClipCache _clipCache;
    private readonly ManualLogSource _logger;
    private AudioSource? _source;

    public LocalDrumPlayback(ManualLogSource logger)
        : this(logger, new DrumHitClipCache(logger))
    {
    }

    public LocalDrumPlayback(ManualLogSource logger, DrumHitClipCache clipCache)
    {
        _logger = logger;
        _clipCache = clipCache;
    }

    public bool Play(string hitId, Vector3 position, int maxSimultaneousHits, float volume)
    {
        var now = Time.time;
        _activeUntil.RemoveAll(until => until <= now);

        if (_activeUntil.Count >= maxSimultaneousHits)
        {
            _logger.LogDebug("Skipped drum hit because the local overlap limit is active.");
            return false;
        }

        var clip = _clipCache.GetOrCreate(hitId);
        if (clip is null)
        {
            return false;
        }

        var source = EnsureSource();
        source.transform.position = position;
        source.PlayOneShot(clip, volume);
        _activeUntil.Add(now + clip.length);
        return true;
    }

    private AudioSource EnsureSource()
    {
        if (_source is not null)
        {
            return _source;
        }

        var host = new GameObject("Bardheim_LocalDrumAudio");
        Object.DontDestroyOnLoad(host);

        _source = host.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.spatialBlend = 0.0f;
        _source.rolloffMode = AudioRolloffMode.Linear;
        _source.minDistance = 1.0f;
        _source.maxDistance = 24.0f;
        return _source;
    }
}
