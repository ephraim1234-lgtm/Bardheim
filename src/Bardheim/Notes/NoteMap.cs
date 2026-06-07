using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Bardheim.Notes;

public sealed class NoteMap
{
    private readonly Dictionary<int, NoteDefinition> _notesBySlot;

    public NoteMap(IEnumerable<NoteDefinition> notes)
    {
        _notesBySlot = new Dictionary<int, NoteDefinition>();
        var orderedNotes = new List<NoteDefinition>();

        foreach (var note in notes)
        {
            if (note.Slot < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(notes), "Note slots must be positive.");
            }

            _notesBySlot.Add(note.Slot, note);
            orderedNotes.Add(note);
        }

        Notes = new ReadOnlyCollection<NoteDefinition>(orderedNotes);
    }

    public IReadOnlyList<NoteDefinition> Notes { get; }

    public bool TryGet(int slot, out NoteDefinition note)
    {
        return _notesBySlot.TryGetValue(slot, out note);
    }

    public NoteDefinition GetRequired(int slot)
    {
        if (!TryGet(slot, out var note))
        {
            throw new KeyNotFoundException($"No lyre note is mapped to slot {slot}.");
        }

        return note;
    }
}
