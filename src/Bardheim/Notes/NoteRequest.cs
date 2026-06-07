using UnityEngine;

namespace Bardheim.Notes;

public sealed class NoteRequest
{
    public NoteRequest(NoteDefinition note, Vector3 position)
    {
        Note = note;
        Position = position;
    }

    public NoteDefinition Note { get; }

    public Vector3 Position { get; }
}
