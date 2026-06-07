using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bardheim.Songs;
using Xunit;

namespace Bardheim.Tests;

public sealed class MidiSongEngineTests
{
    [Fact]
    public void ParserReadsFormatOneTempoAndNoteEvents()
    {
        var midi = MidiFileParser.Parse("test.mid", CreateSingleNoteMidi(midiNote: 62, channel: 0));

        Assert.Equal("test.mid", midi.DisplayName);
        var note = Assert.Single(midi.Events);
        Assert.Equal(62, note.MidiNote);
        Assert.Equal(0, note.Channel);
        Assert.Equal(0.0, note.StartSeconds, precision: 3);
        Assert.Equal(0.5, note.DurationSeconds, precision: 3);
    }

    [Fact]
    public void ParserKeepsSimultaneousNotesFromMultipleChannels()
    {
        var midi = MidiFileParser.Parse("channels.mid", CreateTwoChannelChordMidi());

        Assert.Equal(new[] { 60, 67 }, midi.Events.Select(note => note.MidiNote).OrderBy(note => note));
        Assert.Equal(new[] { 0, 1 }, midi.Events.Select(note => note.Channel).OrderBy(channel => channel));
        Assert.All(midi.Events, note => Assert.Equal(0.0, note.StartSeconds, precision: 3));
    }

    [Fact]
    public void SongLibraryRefreshesUserAddedMidiFiles()
    {
        var songsDir = CreateTempDirectory();
        try
        {
            File.WriteAllBytes(Path.Combine(songsDir, "first.mid"), CreateSingleNoteMidi(62, 0));
            var library = new MidiSongLibrary(songsDir);

            library.Refresh();
            Assert.Equal(new[] { "first.mid" }, library.Songs.Select(song => song.DisplayName));

            File.WriteAllBytes(Path.Combine(songsDir, "second.mid"), CreateSingleNoteMidi(64, 1));
            File.WriteAllText(Path.Combine(songsDir, "broken.mid"), "not a midi file");

            library.Refresh();

            Assert.Equal(new[] { "first.mid", "second.mid" }, library.Songs.Select(song => song.DisplayName));
            Assert.Contains("broken.mid", library.SkippedFiles.Select(Path.GetFileName));
            Assert.Equal(0, library.SelectedIndex);
        }
        finally
        {
            Directory.Delete(songsDir, recursive: true);
        }
    }

    [Fact]
    public void PlaybackSessionEmitsNotesAtScheduledTimes()
    {
        var song = new MidiSong(
            "scheduled.mid",
            new[]
            {
                new MidiNoteEvent(62, 0, 0.0, 0.25, 100),
                new MidiNoteEvent(64, 0, 0.5, 0.25, 100)
            });
        var session = new MidiPlaybackSession();
        session.Start(song, startTimeSeconds: 10.0);

        Assert.Equal(new[] { 62 }, session.CollectDueNotes(nowSeconds: 10.0).Select(note => note.MidiNote));
        Assert.Empty(session.CollectDueNotes(nowSeconds: 10.25));
        Assert.Equal(new[] { 64 }, session.CollectDueNotes(nowSeconds: 10.5).Select(note => note.MidiNote));

        session.Stop();
        Assert.Empty(session.CollectDueNotes(nowSeconds: 11.0));
    }

    [Fact]
    public void PlaybackSessionAppliesSpeedMultiplierToSchedule()
    {
        var song = new MidiSong(
            "speed.mid",
            new[]
            {
                new MidiNoteEvent(62, 0, 0.0, 0.25, 100),
                new MidiNoteEvent(64, 0, 1.0, 0.25, 100)
            });
        var session = new MidiPlaybackSession();
        session.SetSpeed(2.0);
        session.Start(song, startTimeSeconds: 10.0);

        Assert.Equal(new[] { 62 }, session.CollectDueNotes(nowSeconds: 10.0).Select(note => note.MidiNote));
        Assert.Empty(session.CollectDueNotes(nowSeconds: 10.49));
        Assert.Equal(new[] { 64 }, session.CollectDueNotes(nowSeconds: 10.5).Select(note => note.MidiNote));
    }

    [Theory]
    [InlineData(0.1, 0.25)]
    [InlineData(1.0, 1.0)]
    [InlineData(5.0, 3.0)]
    public void PlaybackSessionClampsSpeedMultiplier(double requestedSpeed, double expectedSpeed)
    {
        var session = new MidiPlaybackSession();

        session.SetSpeed(requestedSpeed);

        Assert.Equal(expectedSpeed, session.SpeedMultiplier);
    }

    [Fact]
    public void PlaybackSessionLoopsBackToStartWhenLoopEnabled()
    {
        var song = new MidiSong(
            "loop.mid",
            new[]
            {
                new MidiNoteEvent(62, 0, 0.0, 0.25, 100),
                new MidiNoteEvent(64, 0, 0.5, 0.25, 100)
            });
        var session = new MidiPlaybackSession();
        session.SetLoopEnabled(true);
        session.Start(song, startTimeSeconds: 10.0);

        Assert.Equal(new[] { 62 }, session.CollectDueNotes(nowSeconds: 10.0).Select(note => note.MidiNote));
        Assert.Equal(new[] { 64 }, session.CollectDueNotes(nowSeconds: 10.5).Select(note => note.MidiNote));
        Assert.Equal(new[] { 62 }, session.CollectDueNotes(nowSeconds: 10.76).Select(note => note.MidiNote));
        Assert.True(session.IsPlaying);
        Assert.True(session.LoopEnabled);
    }

    [Theory]
    [InlineData(50, "D3")]
    [InlineData(51, "D#3")]
    [InlineData(61, "C#4")]
    [InlineData(70, "A#4")]
    [InlineData(74, "D5")]
    [InlineData(86, "D5")]
    [InlineData(38, "D3")]
    public void MidiNoteMapperKeepsPlaybackInsideAvailableLyreBank(int midiNote, string expectedNoteName)
    {
        var note = MidiNoteMapper.MapToLyreNote(midiNote);

        Assert.Equal(expectedNoteName, note.Name);
    }

    [Theory]
    [InlineData(50, "D4")]
    [InlineData(62, "D4")]
    [InlineData(63, "D#4")]
    [InlineData(64, "E4")]
    [InlineData(66, "F#4")]
    [InlineData(70, "Bb4")]
    [InlineData(73, "C#5")]
    [InlineData(72, "C5")]
    [InlineData(74, "D5")]
    [InlineData(75, "D#5")]
    [InlineData(81, "A5")]
    [InlineData(82, "Bb5")]
    [InlineData(83, "B5")]
    [InlineData(86, "D5")]
    [InlineData(98, "D5")]
    public void FluteMidiMapperKeepsPlaybackInsideAvailableFluteBank(int midiNote, string expectedNoteName)
    {
        var note = FluteMidiMapper.MapToFluteNote(midiNote);

        Assert.Equal(expectedNoteName, note.Name);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"BardheimTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static byte[] CreateSingleNoteMidi(int midiNote, int channel)
    {
        var track = new List<byte>();
        WriteVariableLength(track, 0);
        track.AddRange(new byte[] { 0xFF, 0x51, 0x03, 0x07, 0xA1, 0x20 });
        WriteVariableLength(track, 0);
        track.Add((byte)(0x90 | channel));
        track.Add((byte)midiNote);
        track.Add(100);
        WriteVariableLength(track, 480);
        track.Add((byte)(0x80 | channel));
        track.Add((byte)midiNote);
        track.Add(0);
        WriteVariableLength(track, 0);
        track.AddRange(new byte[] { 0xFF, 0x2F, 0x00 });

        using var stream = new MemoryStream();
        WriteAscii(stream, "MThd");
        WriteInt32(stream, 6);
        WriteInt16(stream, 1);
        WriteInt16(stream, 1);
        WriteInt16(stream, 480);
        WriteAscii(stream, "MTrk");
        WriteInt32(stream, track.Count);
        stream.Write(track.ToArray(), 0, track.Count);
        return stream.ToArray();
    }

    private static byte[] CreateTwoChannelChordMidi()
    {
        var track = new List<byte>();
        WriteVariableLength(track, 0);
        track.AddRange(new byte[] { 0xFF, 0x51, 0x03, 0x07, 0xA1, 0x20 });
        WriteVariableLength(track, 0);
        track.AddRange(new byte[] { 0x90, 60, 100 });
        WriteVariableLength(track, 0);
        track.AddRange(new byte[] { 0x91, 67, 100 });
        WriteVariableLength(track, 480);
        track.AddRange(new byte[] { 0x80, 60, 0 });
        WriteVariableLength(track, 0);
        track.AddRange(new byte[] { 0x81, 67, 0 });
        WriteVariableLength(track, 0);
        track.AddRange(new byte[] { 0xFF, 0x2F, 0x00 });

        using var stream = new MemoryStream();
        WriteAscii(stream, "MThd");
        WriteInt32(stream, 6);
        WriteInt16(stream, 1);
        WriteInt16(stream, 1);
        WriteInt16(stream, 480);
        WriteAscii(stream, "MTrk");
        WriteInt32(stream, track.Count);
        stream.Write(track.ToArray(), 0, track.Count);
        return stream.ToArray();
    }

    private static void WriteVariableLength(List<byte> bytes, int value)
    {
        var buffer = value & 0x7F;
        while ((value >>= 7) > 0)
        {
            buffer <<= 8;
            buffer |= ((value & 0x7F) | 0x80);
        }

        while (true)
        {
            bytes.Add((byte)buffer);
            if ((buffer & 0x80) == 0)
            {
                break;
            }

            buffer >>= 8;
        }
    }

    private static void WriteAscii(Stream stream, string text)
    {
        foreach (var character in text)
        {
            stream.WriteByte((byte)character);
        }
    }

    private static void WriteInt16(Stream stream, int value)
    {
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)(value & 0xFF));
    }

    private static void WriteInt32(Stream stream, int value)
    {
        stream.WriteByte((byte)((value >> 24) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)(value & 0xFF));
    }
}
