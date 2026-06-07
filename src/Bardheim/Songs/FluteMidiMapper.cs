using System;
using Bardheim.Notes;

namespace Bardheim.Songs;

public static class FluteMidiMapper
{
    private static readonly (int MidiNote, string Name)[] FluteBank =
    {
        (62, "D4"),
        (63, "D#4"),
        (64, "E4"),
        (65, "F4"),
        (66, "F#4"),
        (67, "G4"),
        (68, "G#4"),
        (69, "A4"),
        (70, "Bb4"),
        (71, "B4"),
        (72, "C5"),
        (73, "C#5"),
        (74, "D5"),
        (75, "D#5"),
        (76, "E5"),
        (77, "F5"),
        (78, "F#5"),
        (79, "G5"),
        (80, "G#5"),
        (81, "A5"),
        (82, "Bb5"),
        (83, "B5")
    };

    public static NoteDefinition MapToFluteNote(int midiNote)
    {
        var mappedMidiNote = midiNote;
        while (mappedMidiNote < FluteBank[0].MidiNote)
        {
            mappedMidiNote += 12;
        }

        while (mappedMidiNote > FluteBank[FluteBank.Length - 1].MidiNote)
        {
            mappedMidiNote -= 12;
        }

        var nearest = FluteBank[0];
        var nearestDistance = Math.Abs(mappedMidiNote - nearest.MidiNote);
        foreach (var candidate in FluteBank)
        {
            var distance = Math.Abs(mappedMidiNote - candidate.MidiNote);
            if (distance < nearestDistance)
            {
                nearest = candidate;
                nearestDistance = distance;
            }
        }

        return new NoteDefinition(0, nearest.Name, GetFrequency(nearest.MidiNote));
    }

    private static float GetFrequency(int midiNote)
    {
        return (float)(440.0 * Math.Pow(2.0, (midiNote - 69) / 12.0));
    }
}
