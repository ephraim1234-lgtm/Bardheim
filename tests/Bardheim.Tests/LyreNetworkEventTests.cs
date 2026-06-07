using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bardheim.Instruments;
using Bardheim.Network;
using Xunit;

namespace Bardheim.Tests;

public sealed class LyreNetworkEventTests
{
    private static readonly HashSet<string> ValidNotes = new(StringComparer.Ordinal)
    {
        "D3",
        "C4",
        "D5"
    };

    private static readonly Dictionary<string, ISet<string>> ValidEventsByInstrument = new(StringComparer.Ordinal)
    {
        [InstrumentCatalog.LyreId] = new HashSet<string>(StringComparer.Ordinal)
        {
            "D3",
            "C4",
            "D5"
        },
        [InstrumentCatalog.DrumsId] = new HashSet<string>(StringComparer.Ordinal)
        {
            "kick",
            "snare"
        },
        [InstrumentCatalog.FluteId] = new HashSet<string>(StringComparer.Ordinal)
        {
            "D4",
            "D5"
        }
    };

    [Fact]
    public void SerializerRoundTripsVersionedNoteEvent()
    {
        var noteEvent = new LyreNetworkNoteEvent(
            eventId: 42,
            instrumentId: InstrumentCatalog.DrumsId,
            noteName: "kick",
            position: new LyreNetworkPosition(1.25f, 2.5f, -3.75f),
            volume: 0.8f,
            source: LyreNetworkNoteSource.Midi);

        var bytes = LyreNetworkNoteSerializer.Serialize(noteEvent);
        var parsed = LyreNetworkNoteSerializer.TryDeserialize(bytes, out var roundTrip);

        Assert.True(parsed);
        Assert.Equal(noteEvent.EventId, roundTrip.EventId);
        Assert.Equal(noteEvent.InstrumentId, roundTrip.InstrumentId);
        Assert.Equal(noteEvent.NoteName, roundTrip.NoteName);
        Assert.Equal(noteEvent.Position.X, roundTrip.Position.X);
        Assert.Equal(noteEvent.Position.Y, roundTrip.Position.Y);
        Assert.Equal(noteEvent.Position.Z, roundTrip.Position.Z);
        Assert.Equal(noteEvent.Volume, roundTrip.Volume);
        Assert.Equal(noteEvent.Source, roundTrip.Source);
    }

    [Fact]
    public void SerializerReadsVersionOnePayloadsAsLyreEvents()
    {
        var bytes = CreateVersionOnePayload(
            eventId: 43,
            noteName: "C4",
            position: new LyreNetworkPosition(1.25f, 2.5f, -3.75f),
            volume: 0.8f,
            source: LyreNetworkNoteSource.Midi);

        var parsed = LyreNetworkNoteSerializer.TryDeserialize(bytes, out var roundTrip);

        Assert.True(parsed);
        Assert.Equal(43, roundTrip.EventId);
        Assert.Equal(InstrumentCatalog.LyreId, roundTrip.InstrumentId);
        Assert.Equal("C4", roundTrip.NoteName);
        Assert.Equal(0.8f, roundTrip.Volume);
        Assert.Equal(LyreNetworkNoteSource.Midi, roundTrip.Source);
    }

    [Fact]
    public void SerializerRejectsUnknownPayloadVersion()
    {
        var bytes = LyreNetworkNoteSerializer.Serialize(new LyreNetworkNoteEvent(
            eventId: 7,
            noteName: "D3",
            position: LyreNetworkPosition.Zero,
            volume: 1.0f,
            source: LyreNetworkNoteSource.Manual));
        bytes[0] = 99;

        var parsed = LyreNetworkNoteSerializer.TryDeserialize(bytes, out _);

        Assert.False(parsed);
    }

    [Fact]
    public void ValidatorRejectsSelfOriginatedEvents()
    {
        var noteEvent = new LyreNetworkNoteEvent(1, "D3", LyreNetworkPosition.Zero, 1.0f, LyreNetworkNoteSource.Manual);

        var accepted = LyreNetworkNoteValidator.TryValidate(
            noteEvent,
            senderPeerId: 123,
            localPeerId: 123,
            validNoteNames: ValidNotes,
            out _);

        Assert.False(accepted);
    }

    [Fact]
    public void ValidatorRejectsUnavailableNoteNames()
    {
        var noteEvent = new LyreNetworkNoteEvent(1, "F9", LyreNetworkPosition.Zero, 1.0f, LyreNetworkNoteSource.Midi);

        var accepted = LyreNetworkNoteValidator.TryValidate(
            noteEvent,
            senderPeerId: 123,
            localPeerId: 456,
            validNoteNames: ValidNotes,
            out var reason);

        Assert.False(accepted);
        Assert.Equal(LyreNetworkNoteRejectReason.UnknownNote, reason);
    }

    [Fact]
    public void ValidatorAcceptsKnownEventForMatchingInstrument()
    {
        var noteEvent = new LyreNetworkNoteEvent(
            1,
            InstrumentCatalog.DrumsId,
            "kick",
            LyreNetworkPosition.Zero,
            1.0f,
            LyreNetworkNoteSource.Midi);

        var accepted = LyreNetworkNoteValidator.TryValidate(
            noteEvent,
            senderPeerId: 123,
            localPeerId: 456,
            validEventNamesByInstrument: ValidEventsByInstrument,
            out var reason);

        Assert.True(accepted);
        Assert.Equal(LyreNetworkNoteRejectReason.None, reason);
    }

    [Fact]
    public void ValidatorRejectsUnknownInstrumentIds()
    {
        var noteEvent = new LyreNetworkNoteEvent(
            1,
            "bagpipes",
            "kick",
            LyreNetworkPosition.Zero,
            1.0f,
            LyreNetworkNoteSource.Midi);

        var accepted = LyreNetworkNoteValidator.TryValidate(
            noteEvent,
            senderPeerId: 123,
            localPeerId: 456,
            validEventNamesByInstrument: ValidEventsByInstrument,
            out var reason);

        Assert.False(accepted);
        Assert.Equal(LyreNetworkNoteRejectReason.UnknownNote, reason);
    }

    [Fact]
    public void ValidatorAcceptsKnownFluteEventForFluteInstrument()
    {
        var noteEvent = new LyreNetworkNoteEvent(
            1,
            InstrumentCatalog.FluteId,
            "D5",
            LyreNetworkPosition.Zero,
            1.0f,
            LyreNetworkNoteSource.Midi);

        var accepted = LyreNetworkNoteValidator.TryValidate(
            noteEvent,
            senderPeerId: 123,
            localPeerId: 456,
            validEventNamesByInstrument: ValidEventsByInstrument,
            out var reason);

        Assert.True(accepted);
        Assert.Equal(LyreNetworkNoteRejectReason.None, reason);
    }

    [Fact]
    public void IntakeGateRejectsDuplicateEventsFromSameSender()
    {
        var gate = new LyreRemoteNoteIntakeGate(maxEventsPerSecond: 8);
        var noteEvent = new LyreNetworkNoteEvent(88, "C4", LyreNetworkPosition.Zero, 1.0f, LyreNetworkNoteSource.Midi);

        Assert.True(gate.TryAccept(100, noteEvent, nowSeconds: 10.0, out _));
        Assert.False(gate.TryAccept(100, noteEvent, nowSeconds: 10.1, out var reason));
        Assert.Equal(LyreNetworkNoteRejectReason.Duplicate, reason);
    }

    [Fact]
    public void IntakeGateRateLimitsEventsPerSender()
    {
        var gate = new LyreRemoteNoteIntakeGate(maxEventsPerSecond: 2);

        Assert.True(gate.TryAccept(100, new LyreNetworkNoteEvent(1, "D3", LyreNetworkPosition.Zero, 1.0f, LyreNetworkNoteSource.Manual), 10.0, out _));
        Assert.True(gate.TryAccept(100, new LyreNetworkNoteEvent(2, "C4", LyreNetworkPosition.Zero, 1.0f, LyreNetworkNoteSource.Manual), 10.2, out _));
        Assert.False(gate.TryAccept(100, new LyreNetworkNoteEvent(3, "D5", LyreNetworkPosition.Zero, 1.0f, LyreNetworkNoteSource.Manual), 10.4, out var reason));
        Assert.Equal(LyreNetworkNoteRejectReason.RateLimited, reason);
        Assert.True(gate.TryAccept(100, new LyreNetworkNoteEvent(4, "D3", LyreNetworkPosition.Zero, 1.0f, LyreNetworkNoteSource.Manual), 11.1, out _));
    }

    private static byte[] CreateVersionOnePayload(
        long eventId,
        string noteName,
        LyreNetworkPosition position,
        float volume,
        LyreNetworkNoteSource source)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8);

        writer.Write(1);
        writer.Write(eventId);
        writer.Write(noteName);
        writer.Write(position.X);
        writer.Write(position.Y);
        writer.Write(position.Z);
        writer.Write(volume);
        writer.Write((byte)source);
        writer.Flush();

        return stream.ToArray();
    }
}
