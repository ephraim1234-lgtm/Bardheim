using System.Collections.Generic;
using BepInEx.Logging;
using Bardheim.Notes;
using UnityEngine;

namespace Bardheim.Audio;

public sealed class LocalFlutePlayback
{
    private readonly List<float> _activeUntil = new();
    private readonly FluteNoteClipCache _clipCache;
    private readonly ManualLogSource _logger;
    private AudioSource? _source;

    public LocalFlutePlayback(ManualLogSource logger)
        : this(logger, new FluteNoteClipCache(logger))
    {
    }

    public LocalFlutePlayback(ManualLogSource logger, FluteNoteClipCache clipCache)
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
            _logger.LogDebug("Skipped flute note because the local overlap limit is active.");
            return false;
        }

        var clip = _clipCache.GetOrCreate(request.Note);
        if (clip is null)
        {
            return false;
        }

        var source = EnsureSource();
        source.transform.position = request.Position;
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

        var host = new GameObject("Bardheim_LocalFluteAudio");
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
