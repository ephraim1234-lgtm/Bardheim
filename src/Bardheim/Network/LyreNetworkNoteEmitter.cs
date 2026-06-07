using System.Threading;
using Bardheim.Instruments;
using Bardheim.Notes;
using UnityEngine;

namespace Bardheim.Network;

public sealed class LyreNetworkNoteEmitter : ILyreNetworkNoteEmitter
{
    private readonly ILyreNetworkNoteTransport _transport;
    private long _nextEventId;

    public LyreNetworkNoteEmitter(ILyreNetworkNoteTransport transport)
    {
        _transport = transport;
    }

    public void Emit(NoteRequest request, LyreNetworkNoteSource source, float volume)
    {
        Emit(InstrumentCatalog.LyreId, request.Note.Name, request.Position, source, volume);
    }

    public void Emit(string instrumentId, string eventName, Vector3 position, LyreNetworkNoteSource source, float volume)
    {
        var noteEvent = new LyreNetworkNoteEvent(
            Interlocked.Increment(ref _nextEventId),
            instrumentId,
            eventName,
            new LyreNetworkPosition(position.x, position.y, position.z),
            volume,
            source);
        var payload = LyreNetworkNoteSerializer.Serialize(noteEvent);

        _transport.Send(payload);
    }
}
