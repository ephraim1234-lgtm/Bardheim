using System;
using Bardheim.Notes;

namespace Bardheim.Songs;

public static class MidiNoteMapper
{
    private static readonly string[] NoteNames =
    {
        "C",
        "C#",
        "D",
        "D#",
        "E",
        "F",
        "F#",
        "G",
        "G#",
        "A",
        "A#",
        "B"
    };

    public static NoteDefinition MapToLyreNote(int midiNote)
    {
        var mappedMidiNote = midiNote;
        while (mappedMidiNote < 50)
        {
            mappedMidiNote += 12;
        }

        while (mappedMidiNote > 74)
        {
            mappedMidiNote -= 12;
        }

        mappedMidiNote = Math.Max(50, Math.Min(74, mappedMidiNote));
        var octave = (mappedMidiNote / 12) - 1;
        var noteName = $"{NoteNames[mappedMidiNote % 12]}{octave}";
        return new NoteDefinition(0, noteName, GetFrequency(mappedMidiNote));
    }

    private static float GetFrequency(int midiNote)
    {
        return (float)(440.0 * Math.Pow(2.0, (midiNote - 69) / 12.0));
    }
}
