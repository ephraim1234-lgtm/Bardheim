using System;
using System.Collections.Generic;
using System.Linq;

namespace Bardheim.Songs;

public static class FluteMidiArranger
{
    private const double MelodyBucketSeconds = 0.05;

    public static IEnumerable<MidiNoteEvent> SelectPlayableEvents(IEnumerable<MidiNoteEvent> dueNotes, MidiSongProfile profile)
    {
        var allowed = dueNotes.Where(profile.Allows).ToArray();
        if (profile.ArrangementMode == MidiArrangementMode.Full)
        {
            return allowed;
        }

        return allowed
            .GroupBy(note => Math.Floor(note.StartSeconds / MelodyBucketSeconds))
            .OrderBy(group => group.Min(note => note.StartSeconds))
            .SelectMany(group => group
                .OrderByDescending(note => profile.ApplyOctaveOffset(note))
                .ThenByDescending(note => note.Velocity)
                .Take(profile.MaxSimultaneousMelodyNotes));
    }
}
