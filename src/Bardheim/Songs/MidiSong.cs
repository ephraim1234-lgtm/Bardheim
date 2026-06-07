using System;
using System.Collections.Generic;
using System.Linq;

namespace Bardheim.Songs;

public sealed class MidiSong
{
    public MidiSong(string displayName, IEnumerable<MidiNoteEvent> events, string? sourcePath = null)
    {
        DisplayName = displayName;
        SourcePath = sourcePath;
        Events = events.OrderBy(item => item.StartSeconds).ToArray();
        LengthSeconds = Events.Count == 0
            ? 0.0
            : Events.Max(item => item.StartSeconds + item.DurationSeconds);
    }

    public string DisplayName { get; }

    public string? SourcePath { get; }

    public IReadOnlyList<MidiNoteEvent> Events { get; }

    public double LengthSeconds { get; }
}
