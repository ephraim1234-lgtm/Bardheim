using System.Collections.Generic;
using System.Linq;

namespace Bardheim.Songs;

public sealed class FluteMidiRateLimiter
{
    private HashSet<MidiNoteEvent>? _plannedEvents;

    public void Prepare(
        IEnumerable<MidiNoteEvent> songEvents,
        MidiSongProfile profile,
        int maxNoteStartsPerSecond)
    {
        var maxStarts = System.Math.Max(1, maxNoteStartsPerSecond);
        _plannedEvents = songEvents
            .Where(profile.Allows)
            .GroupBy(note => (int)System.Math.Floor(note.StartSeconds))
            .SelectMany(group => group
                .OrderByDescending(profile.ApplyOctaveOffset)
                .ThenByDescending(note => note.Velocity)
                .ThenBy(note => note.StartSeconds)
                .Take(maxStarts)
                .OrderBy(note => note.StartSeconds))
            .ToHashSet();
    }

    public IEnumerable<MidiNoteEvent> SelectPlayableEvents(
        IEnumerable<MidiNoteEvent> dueNotes,
        MidiSongProfile profile,
        int maxNoteStartsPerSecond)
    {
        foreach (var note in dueNotes)
        {
            if (!profile.Allows(note))
            {
                continue;
            }

            if (_plannedEvents is not null && !_plannedEvents.Contains(note))
            {
                continue;
            }

            yield return note;
        }
    }

    public void Reset()
    {
        _plannedEvents = null;
    }
}
