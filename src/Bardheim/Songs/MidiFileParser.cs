using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Bardheim.Songs;

public static class MidiFileParser
{
    private const int DefaultMicrosecondsPerQuarterNote = 500000;

    public static MidiSong Parse(string displayName, byte[] data, string? sourcePath = null)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream, Encoding.ASCII);

        Require(ReadAscii(reader, 4) == "MThd", "Missing MIDI header.");
        Require(ReadInt32(reader) == 6, "Unsupported MIDI header length.");
        var format = ReadInt16(reader);
        var trackCount = ReadInt16(reader);
        var ticksPerQuarterNote = ReadInt16(reader);

        Require(format == 0 || format == 1, $"Unsupported MIDI format {format}.");
        Require(trackCount > 0, "MIDI file has no tracks.");
        Require(ticksPerQuarterNote > 0 && (ticksPerQuarterNote & 0x8000) == 0, "SMPTE time division is not supported.");

        var events = new List<TimedMidiEvent>();
        for (var trackIndex = 0; trackIndex < trackCount; trackIndex++)
        {
            Require(ReadAscii(reader, 4) == "MTrk", "Missing MIDI track header.");
            var trackLength = ReadInt32(reader);
            var trackEnd = stream.Position + trackLength;
            ParseTrack(reader, trackEnd, events);
            stream.Position = trackEnd;
        }

        return new MidiSong(displayName, BuildNoteEvents(events, ticksPerQuarterNote), sourcePath);
    }

    private static void ParseTrack(BinaryReader reader, long trackEnd, List<TimedMidiEvent> events)
    {
        var tick = 0;
        byte runningStatus = 0;

        while (reader.BaseStream.Position < trackEnd)
        {
            tick += ReadVariableLength(reader);
            var statusOrData = reader.ReadByte();
            byte status;
            byte firstData;

            if ((statusOrData & 0x80) == 0)
            {
                Require(runningStatus != 0, "Running status appeared before any status byte.");
                status = runningStatus;
                firstData = statusOrData;
            }
            else
            {
                status = statusOrData;
                firstData = 0;
                if (status < 0xF0)
                {
                    runningStatus = status;
                }
            }

            if (status == 0xFF)
            {
                var metaType = reader.ReadByte();
                var length = ReadVariableLength(reader);
                var payload = reader.ReadBytes(length);
                if (metaType == 0x51 && payload.Length == 3)
                {
                    var tempo = (payload[0] << 16) | (payload[1] << 8) | payload[2];
                    events.Add(TimedMidiEvent.Tempo(tick, tempo));
                }

                if (metaType == 0x2F)
                {
                    break;
                }

                continue;
            }

            if (status == 0xF0 || status == 0xF7)
            {
                var length = ReadVariableLength(reader);
                reader.BaseStream.Position += length;
                continue;
            }

            var eventType = status & 0xF0;
            var channel = status & 0x0F;
            var data1 = (statusOrData & 0x80) == 0 ? firstData : reader.ReadByte();

            if (eventType == 0xC0 || eventType == 0xD0)
            {
                continue;
            }

            var data2 = reader.ReadByte();
            if (eventType == 0x90 || eventType == 0x80)
            {
                events.Add(TimedMidiEvent.NoteEvent(tick, eventType == 0x90 && data2 > 0, channel, data1, data2));
            }
        }
    }

    private static IEnumerable<MidiNoteEvent> BuildNoteEvents(List<TimedMidiEvent> events, int ticksPerQuarterNote)
    {
        events.Sort((left, right) => left.Tick == right.Tick ? left.SortOrder.CompareTo(right.SortOrder) : left.Tick.CompareTo(right.Tick));

        var tempo = DefaultMicrosecondsPerQuarterNote;
        var previousTick = 0;
        var currentSeconds = 0.0;
        var activeNotes = new Dictionary<(int Channel, int Note), Queue<(double StartSeconds, int Velocity)>>();
        var notes = new List<MidiNoteEvent>();

        foreach (var item in events)
        {
            var deltaTicks = item.Tick - previousTick;
            currentSeconds += deltaTicks * (tempo / 1000000.0) / ticksPerQuarterNote;
            previousTick = item.Tick;

            if (item.Kind == TimedMidiEventKind.Tempo)
            {
                tempo = item.TempoMicrosecondsPerQuarterNote;
                continue;
            }

            var key = (item.Channel, item.Note);
            if (item.IsNoteOn)
            {
                if (!activeNotes.TryGetValue(key, out var queue))
                {
                    queue = new Queue<(double StartSeconds, int Velocity)>();
                    activeNotes.Add(key, queue);
                }

                queue.Enqueue((currentSeconds, item.Velocity));
            }
            else if (activeNotes.TryGetValue(key, out var queue) && queue.Count > 0)
            {
                var started = queue.Dequeue();
                notes.Add(new MidiNoteEvent(item.Note, item.Channel, started.StartSeconds, Math.Max(0.03, currentSeconds - started.StartSeconds), started.Velocity));
            }
        }

        return notes.OrderBy(item => item.StartSeconds).ThenByDescending(item => item.Velocity);
    }

    private static int ReadVariableLength(BinaryReader reader)
    {
        var value = 0;
        for (var count = 0; count < 4; count++)
        {
            var next = reader.ReadByte();
            value = (value << 7) | (next & 0x7F);
            if ((next & 0x80) == 0)
            {
                return value;
            }
        }

        throw new InvalidDataException("Invalid MIDI variable-length value.");
    }

    private static string ReadAscii(BinaryReader reader, int length)
    {
        return Encoding.ASCII.GetString(reader.ReadBytes(length));
    }

    private static int ReadInt16(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(2);
        Require(bytes.Length == 2, "Unexpected end of MIDI file.");
        return (bytes[0] << 8) | bytes[1];
    }

    private static int ReadInt32(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(4);
        Require(bytes.Length == 4, "Unexpected end of MIDI file.");
        return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidDataException(message);
        }
    }

    private sealed class TimedMidiEvent
    {
        private TimedMidiEvent(int tick, TimedMidiEventKind kind, int channel, int note, int velocity, bool isNoteOn, int tempo)
        {
            Tick = tick;
            Kind = kind;
            Channel = channel;
            Note = note;
            Velocity = velocity;
            IsNoteOn = isNoteOn;
            TempoMicrosecondsPerQuarterNote = tempo;
        }

        public int Tick { get; }

        public TimedMidiEventKind Kind { get; }

        public int Channel { get; }

        public int Note { get; }

        public int Velocity { get; }

        public bool IsNoteOn { get; }

        public int TempoMicrosecondsPerQuarterNote { get; }

        public int SortOrder => Kind == TimedMidiEventKind.Tempo ? 0 : IsNoteOn ? 2 : 1;

        public static TimedMidiEvent Tempo(int tick, int tempo)
        {
            return new TimedMidiEvent(tick, TimedMidiEventKind.Tempo, 0, 0, 0, false, tempo);
        }

        public static TimedMidiEvent NoteEvent(int tick, bool isNoteOn, int channel, int note, int velocity)
        {
            return new TimedMidiEvent(tick, TimedMidiEventKind.Note, channel, note, velocity, isNoteOn, 0);
        }
    }

    private enum TimedMidiEventKind
    {
        Tempo,
        Note
    }
}
