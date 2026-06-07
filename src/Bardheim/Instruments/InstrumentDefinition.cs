using Bardheim.Notes;
using System.Collections.Generic;

namespace Bardheim.Instruments;

public sealed class InstrumentDefinition
{
    public InstrumentDefinition(string id, IReadOnlyList<NoteSet> noteSets, IReadOnlyList<DrumHitDefinition> drumHits)
    {
        Id = id;
        NoteSets = noteSets;
        DrumHits = drumHits;
    }

    public string Id { get; }

    public IReadOnlyList<NoteSet> NoteSets { get; }

    public IReadOnlyList<DrumHitDefinition> DrumHits { get; }
}
