using Bardheim.Notes;
using UnityEngine;

namespace Bardheim.Network;

public interface ILyreNetworkNoteEmitter
{
    void Emit(NoteRequest request, LyreNetworkNoteSource source, float volume);

    void Emit(string instrumentId, string eventName, Vector3 position, LyreNetworkNoteSource source, float volume);
}
