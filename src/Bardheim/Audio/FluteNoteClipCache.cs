using System.Collections.Generic;
using BepInEx.Logging;
using Bardheim.Notes;
using UnityEngine;

namespace Bardheim.Audio;

public sealed class FluteNoteClipCache
{
    private readonly Dictionary<string, AudioClip> _clipsByNoteName = new();
    private readonly ManualLogSource _logger;

    public FluteNoteClipCache(ManualLogSource logger)
    {
        _logger = logger;
    }

    public AudioClip? GetOrCreate(NoteDefinition note)
    {
        if (_clipsByNoteName.TryGetValue(note.Name, out var clip))
        {
            return clip;
        }

        clip = EmbeddedFluteSampleFactory.TryCreate(note, _logger);
        if (clip is not null)
        {
            _clipsByNoteName.Add(note.Name, clip);
        }

        return clip;
    }
}
