using System;
using Bardheim.Instruments;

namespace Bardheim.Network;

public sealed class LyreNetworkNoteEvent
{
    public LyreNetworkNoteEvent(
        long eventId,
        string noteName,
        LyreNetworkPosition position,
        float volume,
        LyreNetworkNoteSource source)
        : this(eventId, InstrumentCatalog.LyreId, noteName, position, volume, source)
    {
    }

    public LyreNetworkNoteEvent(
        long eventId,
        string instrumentId,
        string noteName,
        LyreNetworkPosition position,
        float volume,
        LyreNetworkNoteSource source)
    {
        EventId = eventId;
        InstrumentId = instrumentId ?? throw new ArgumentNullException(nameof(instrumentId));
        NoteName = noteName ?? throw new ArgumentNullException(nameof(noteName));
        Position = position;
        Volume = volume;
        Source = source;
    }

    public long EventId { get; }

    public string InstrumentId { get; }

    public string NoteName { get; }

    public LyreNetworkPosition Position { get; }

    public float Volume { get; }

    public LyreNetworkNoteSource Source { get; }
}
