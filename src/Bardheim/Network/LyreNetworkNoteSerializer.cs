using System;
using System.IO;
using System.Text;
using Bardheim.Instruments;

namespace Bardheim.Network;

public static class LyreNetworkNoteSerializer
{
    public const int CurrentVersion = 2;

    public static byte[] Serialize(LyreNetworkNoteEvent noteEvent)
    {
        if (noteEvent is null)
        {
            throw new ArgumentNullException(nameof(noteEvent));
        }

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8);

        writer.Write(CurrentVersion);
        writer.Write(noteEvent.EventId);
        writer.Write(noteEvent.InstrumentId);
        writer.Write(noteEvent.NoteName);
        writer.Write(noteEvent.Position.X);
        writer.Write(noteEvent.Position.Y);
        writer.Write(noteEvent.Position.Z);
        writer.Write(noteEvent.Volume);
        writer.Write((byte)noteEvent.Source);
        writer.Flush();

        return stream.ToArray();
    }

    public static bool TryDeserialize(byte[]? bytes, out LyreNetworkNoteEvent noteEvent)
    {
        noteEvent = new LyreNetworkNoteEvent(0, InstrumentCatalog.LyreId, string.Empty, LyreNetworkPosition.Zero, 0.0f, LyreNetworkNoteSource.Manual);

        if (bytes is null || bytes.Length == 0)
        {
            return false;
        }

        try
        {
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream, Encoding.UTF8);

            var version = reader.ReadInt32();
            if (version == 1)
            {
                return TryReadVersionOne(reader, out noteEvent);
            }

            if (version != CurrentVersion)
            {
                return false;
            }

            var eventId = reader.ReadInt64();
            var instrumentId = reader.ReadString();
            var noteName = reader.ReadString();
            var position = new LyreNetworkPosition(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            var volume = reader.ReadSingle();
            var source = (LyreNetworkNoteSource)reader.ReadByte();

            if (string.IsNullOrWhiteSpace(instrumentId) || string.IsNullOrWhiteSpace(noteName))
            {
                return false;
            }

            noteEvent = new LyreNetworkNoteEvent(eventId, instrumentId, noteName, position, volume, source);
            return true;
        }
        catch (EndOfStreamException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static bool TryReadVersionOne(BinaryReader reader, out LyreNetworkNoteEvent noteEvent)
    {
        var eventId = reader.ReadInt64();
        var noteName = reader.ReadString();
        var position = new LyreNetworkPosition(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        var volume = reader.ReadSingle();
        var source = (LyreNetworkNoteSource)reader.ReadByte();

        if (string.IsNullOrWhiteSpace(noteName))
        {
            noteEvent = new LyreNetworkNoteEvent(0, InstrumentCatalog.LyreId, string.Empty, LyreNetworkPosition.Zero, 0.0f, LyreNetworkNoteSource.Manual);
            return false;
        }

        noteEvent = new LyreNetworkNoteEvent(eventId, InstrumentCatalog.LyreId, noteName, position, volume, source);
        return true;
    }
}
