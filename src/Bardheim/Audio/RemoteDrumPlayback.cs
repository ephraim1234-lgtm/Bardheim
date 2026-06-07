using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace Bardheim.Audio;

public sealed class RemoteDrumPlayback
{
    private readonly List<float> _activeUntil = new();
    private readonly DrumHitClipCache _clipCache;
    private readonly ManualLogSource _logger;
    private AudioSource? _source;

    public RemoteDrumPlayback(ManualLogSource logger)
        : this(logger, new DrumHitClipCache(logger))
    {
    }

    public RemoteDrumPlayback(ManualLogSource logger, DrumHitClipCache clipCache)
    {
        _logger = logger;
        _clipCache = clipCache;
    }

    public bool Play(string hitId, Vector3 position, int maxSimultaneousHits, float volume, float maxDistance)
    {
        var now = Time.time;
        _activeUntil.RemoveAll(until => until <= now);

        if (_activeUntil.Count >= maxSimultaneousHits)
        {
            _logger.LogDebug("Skipped drum hit because the remote overlap limit is active.");
            return false;
        }

        var clip = _clipCache.GetOrCreate(hitId);
        if (clip is null)
        {
            return false;
        }

        var source = EnsureSource(maxDistance);
        source.transform.position = position;
        source.maxDistance = maxDistance;
        source.PlayOneShot(clip, volume);
        _activeUntil.Add(now + clip.length);
        return true;
    }

    private AudioSource EnsureSource(float maxDistance)
    {
        if (_source is not null)
        {
            return _source;
        }

        var host = new GameObject("Bardheim_RemoteDrumAudio");
        Object.DontDestroyOnLoad(host);

        _source = host.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.spatialBlend = 1.0f;
        _source.rolloffMode = AudioRolloffMode.Linear;
        _source.minDistance = 1.0f;
        _source.maxDistance = maxDistance;
        return _source;
    }
}
