using Bardheim.Notes;
using UnityEngine;

namespace Bardheim.Network;

public sealed class DisabledLyreNetworkNoteEmitter : ILyreNetworkNoteEmitter
{
    public static readonly DisabledLyreNetworkNoteEmitter Instance = new();

    private DisabledLyreNetworkNoteEmitter()
    {
    }

    public void Emit(NoteRequest request, LyreNetworkNoteSource source, float volume)
    {
    }

    public void Emit(string instrumentId, string eventName, Vector3 position, LyreNetworkNoteSource source, float volume)
    {
    }
}
