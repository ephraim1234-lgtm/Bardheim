using System;
using System.IO;
using System.Linq;
using Bardheim.Instruments;
using Bardheim.Songs;
using Xunit;

namespace Bardheim.Tests;

public sealed class MidiSongProfileTests
{
    [Fact]
    public void FluteDoesNotLoadLyreSpecificSidecarProfile()
    {
        var songPath = CreateTempMidiPath();
        try
        {
            File.WriteAllText(
                songPath + ".lyre-profile.json",
                "{ \"schemaVersion\": 1, \"instrumentId\": \"lyre\", \"activeChannels\": [1], \"channelOctaveOffsets\": { \"1\": -1 } }");
            var song = new MidiSong("test.mid", Array.Empty<MidiNoteEvent>(), songPath);
            var channelTwo = new MidiNoteEvent(64, 1, 0.0, 0.25, 100);

            var profile = MidiSongProfile.LoadFor(song, InstrumentCatalog.FluteId);

            Assert.True(profile.Allows(channelTwo));
            Assert.Equal(64, profile.ApplyOctaveOffset(channelTwo));
            Assert.Equal(MidiArrangementMode.Full, profile.ArrangementMode);
        }
        finally
        {
            DeleteSidecarSet(songPath);
        }
    }

    [Fact]
    public void FluteLoadsFluteSpecificSidecarProfile()
    {
        var songPath = CreateTempMidiPath();
        try
        {
            File.WriteAllText(
                songPath + ".flute-profile.json",
                "{ \"schemaVersion\": 1, \"instrumentId\": \"flute\", \"activeChannels\": [2], \"channelOctaveOffsets\": { \"2\": 1 }, \"maxSimultaneousMelodyNotes\": 1 }");
            var song = new MidiSong("test.mid", Array.Empty<MidiNoteEvent>(), songPath);
            var channelOne = new MidiNoteEvent(64, 0, 0.0, 0.25, 100);
            var channelTwo = new MidiNoteEvent(64, 1, 0.0, 0.25, 100);

            var profile = MidiSongProfile.LoadFor(song, InstrumentCatalog.FluteId);

            Assert.False(profile.Allows(channelOne));
            Assert.True(profile.Allows(channelTwo));
            Assert.False(profile.Allows(new MidiNoteEvent(72, 9, 0.0, 0.25, 127)));
            Assert.Equal(76, profile.ApplyOctaveOffset(channelTwo));
            Assert.Equal(1, profile.MaxSimultaneousMelodyNotes);
        }
        finally
        {
            DeleteSidecarSet(songPath);
        }
    }

    [Fact]
    public void FluteSidecarKeepsDefaultChannelTenMuteWhenMutedChannelsAreOmitted()
    {
        var songPath = CreateTempMidiPath();
        try
        {
            File.WriteAllText(
                songPath + ".flute-profile.json",
                "{ \"schemaVersion\": 1, \"instrumentId\": \"flute\", \"maxSimultaneousMelodyNotes\": 1 }");
            var song = new MidiSong("test.mid", Array.Empty<MidiNoteEvent>(), songPath);

            var profile = MidiSongProfile.LoadFor(song, InstrumentCatalog.FluteId);

            Assert.False(profile.Allows(new MidiNoteEvent(72, 9, 0.0, 0.25, 127)));
            Assert.True(profile.Allows(new MidiNoteEvent(72, 0, 0.0, 0.25, 100)));
        }
        finally
        {
            DeleteSidecarSet(songPath);
        }
    }

    [Fact]
    public void FluteDefaultProfileMirrorsLyreStyleMidiPlayback()
    {
        var song = new MidiSong(
            "auto.mid",
            Enumerable.Range(0, 48)
                .Select(index => new MidiNoteEvent(40 + (index % 3), 0, index * 0.20, 0.10, 100))
                .Concat(Enumerable.Range(0, 48).Select(index => new MidiNoteEvent(57 + (index % 4), 1, index * 0.20, 0.10, 90)))
                .Concat(Enumerable.Range(0, 24).Select(index => new MidiNoteEvent(72 + (index % 5), 2, index * 0.40, 0.18, 105)))
                .Concat(new[] { new MidiNoteEvent(76, 3, 0.0, 0.25, 127), new MidiNoteEvent(38, 9, 0.0, 0.1, 127) }));

        var profile = MidiSongProfile.LoadFor(song, InstrumentCatalog.FluteId);

        Assert.True(profile.Allows(new MidiNoteEvent(42, 0, 0.0, 0.10, 100)));
        Assert.True(profile.Allows(new MidiNoteEvent(59, 1, 0.0, 0.10, 90)));
        Assert.True(profile.Allows(new MidiNoteEvent(74, 2, 0.0, 0.18, 105)));
        Assert.True(profile.Allows(new MidiNoteEvent(76, 3, 0.0, 0.25, 127)));
        Assert.False(profile.Allows(new MidiNoteEvent(38, 9, 0.0, 0.10, 127)));
        Assert.Equal(MidiArrangementMode.Full, profile.ArrangementMode);
        Assert.Equal(int.MaxValue, profile.MaxSimultaneousMelodyNotes);
    }

    [Fact]
    public void FluteDefaultArrangerKeepsFullLyreStyleChords()
    {
        var profile = MidiSongProfile.ForInstrument(InstrumentCatalog.FluteId);
        var dueNotes = new[]
        {
            new MidiNoteEvent(40, 1, 10.000, 0.25, 110),
            new MidiNoteEvent(64, 0, 10.000, 0.25, 90),
            new MidiNoteEvent(76, 2, 10.010, 0.25, 80),
            new MidiNoteEvent(79, 3, 10.020, 0.25, 70),
            new MidiNoteEvent(72, 9, 10.000, 0.25, 127),
            new MidiNoteEvent(67, 0, 10.090, 0.25, 100)
        };

        var arranged = FluteMidiArranger.SelectPlayableEvents(dueNotes, profile).ToArray();

        Assert.Equal(new[] { 40, 64, 76, 79, 67 }, arranged.Select(note => note.MidiNote));
        Assert.DoesNotContain(arranged, note => note.Channel == 9);
    }

    private static string CreateTempMidiPath()
    {
        return Path.Combine(Path.GetTempPath(), $"LyreProfile-{Guid.NewGuid():N}.mid");
    }

    private static void DeleteSidecarSet(string songPath)
    {
        foreach (var path in Directory.GetFiles(Path.GetDirectoryName(songPath)!, Path.GetFileName(songPath) + ".*profile.json"))
        {
            File.Delete(path);
        }
    }
}
