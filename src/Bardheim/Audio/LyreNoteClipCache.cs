using System.Collections.Generic;
using BepInEx.Logging;
using Bardheim.Notes;
using UnityEngine;

namespace Bardheim.Audio;

public sealed class LyreNoteClipCache
{
    private readonly Dictionary<string, AudioClip> _clipsByNoteName = new();
    private readonly ManualLogSource _logger;

    public LyreNoteClipCache(ManualLogSource logger)
    {
        _logger = logger;
    }

    public AudioClip GetOrCreate(NoteDefinition note)
    {
        if (_clipsByNoteName.TryGetValue(note.Name, out var clip))
        {
            return clip;
        }

        clip = EmbeddedNoteSampleFactory.TryCreate(note, _logger) ??
            PlaceholderToneFactory.Create(note);
        _clipsByNoteName.Add(note.Name, clip);
        return clip;
    }
}
