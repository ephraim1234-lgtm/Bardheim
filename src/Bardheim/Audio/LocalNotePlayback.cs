using System.Collections.Generic;
using BepInEx.Logging;
using Bardheim.Notes;
using UnityEngine;

namespace Bardheim.Audio;

public sealed class LocalNotePlayback
{
    private readonly List<float> _activeUntil = new();
    private readonly LyreNoteClipCache _clipCache;
    private readonly ManualLogSource _logger;
    private AudioSource? _source;

    public LocalNotePlayback(ManualLogSource logger)
        : this(logger, new LyreNoteClipCache(logger))
    {
    }

    public LocalNotePlayback(ManualLogSource logger, LyreNoteClipCache clipCache)
    {
        _logger = logger;
        _clipCache = clipCache;
    }

    public bool Play(NoteRequest request, int maxSimultaneousNotes, float volume)
    {
        var now = Time.time;
        _activeUntil.RemoveAll(until => until <= now);

        if (_activeUntil.Count >= maxSimultaneousNotes)
        {
            _logger.LogDebug("Skipped lyre note because the local overlap limit is active.");
            return false;
        }

        var source = EnsureSource();
        source.transform.position = request.Position;

        var clip = _clipCache.GetOrCreate(request.Note);
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

        var host = new GameObject("Bardheim_LocalAudio");
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
