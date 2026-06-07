using System;
using System.Collections.Generic;

namespace Bardheim.Notes;

public sealed class NoteSetSelector
{
    private readonly IReadOnlyList<NoteSet> _sets;
    private int _index;

    public NoteSetSelector(IReadOnlyList<NoteSet> sets)
    {
        if (sets.Count == 0)
        {
            throw new ArgumentException("At least one note set is required.", nameof(sets));
        }

        _sets = sets;
        Current = _sets[0];
    }

    public NoteSet Current { get; private set; }

    public NoteSet CycleNext()
    {
        _index = (_index + 1) % _sets.Count;
        Current = _sets[_index];
        return Current;
    }
}
