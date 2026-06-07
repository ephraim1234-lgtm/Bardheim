using System.Collections.Generic;

namespace Bardheim.Notes;

public static class NoteSetCatalog
{
    public static IReadOnlyList<NoteSet> CreateDefaultSets()
    {
        return new[]
        {
            Create(
                "D minor pentatonic",
                ("D3", 146.83f),
                ("F3", 174.61f),
                ("G3", 196.00f),
                ("A3", 220.00f),
                ("C4", 261.63f),
                ("D4", 293.66f),
                ("F4", 349.23f),
                ("G4", 392.00f)),
            Create(
                "D natural minor",
                ("D3", 146.83f),
                ("E3", 164.81f),
                ("F3", 174.61f),
                ("G3", 196.00f),
                ("A3", 220.00f),
                ("Bb3", 233.08f),
                ("C4", 261.63f),
                ("D4", 293.66f)),
            Create(
                "D folk/dorian",
                ("D3", 146.83f),
                ("E3", 164.81f),
                ("F3", 174.61f),
                ("G3", 196.00f),
                ("A3", 220.00f),
                ("C4", 261.63f),
                ("D4", 293.66f),
                ("E4", 329.63f)),
        };
    }

    public static NoteSet Create(string name, params (string Name, float FrequencyHz)[] notes)
    {
        var definitions = new NoteDefinition[notes.Length];
        for (var index = 0; index < notes.Length; index++)
        {
            definitions[index] = new NoteDefinition(index + 1, notes[index].Name, notes[index].FrequencyHz);
        }

        return new NoteSet(name, new NoteMap(definitions));
    }
}
