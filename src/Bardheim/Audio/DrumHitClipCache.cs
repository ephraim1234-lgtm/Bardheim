using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace Bardheim.Audio;

public sealed class DrumHitClipCache
{
    private readonly Dictionary<string, AudioClip> _clipsByHitId = new();
    private readonly ManualLogSource _logger;

    public DrumHitClipCache(ManualLogSource logger)
    {
        _logger = logger;
    }

    public AudioClip? GetOrCreate(string hitId)
    {
        if (_clipsByHitId.TryGetValue(hitId, out var clip))
        {
            return clip;
        }

        clip = EmbeddedDrumSampleFactory.TryCreate(hitId, _logger);
        if (clip is not null)
        {
            _clipsByHitId.Add(hitId, clip);
        }

        return clip;
    }
}
