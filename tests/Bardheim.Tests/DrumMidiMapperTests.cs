using Bardheim.Songs;
using Xunit;

namespace Bardheim.Tests;

public sealed class DrumMidiMapperTests
{
    [Theory]
    [InlineData(35, "low_kick")]
    [InlineData(36, "kick")]
    [InlineData(38, "snare")]
    [InlineData(40, "electric_snare")]
    [InlineData(37, "rim")]
    [InlineData(39, "clap")]
    [InlineData(42, "muted")]
    [InlineData(44, "pedal_hat")]
    [InlineData(46, "pedal_hat")]
    [InlineData(49, "crash")]
    [InlineData(57, "crash")]
    [InlineData(51, "ride")]
    [InlineData(59, "ride")]
    [InlineData(41, "low_tom")]
    [InlineData(43, "low_tom")]
    [InlineData(45, "mid_tom")]
    [InlineData(47, "mid_tom")]
    [InlineData(48, "high_tom")]
    [InlineData(50, "high_tom")]
    [InlineData(56, "cowbell")]
    [InlineData(54, "tambourine")]
    public void TryMapReturnsHitIdForKnownGeneralMidiPercussionNotes(int midiNote, string expectedHitId)
    {
        var note = new MidiNoteEvent(midiNote, 9, 0, 0.1, 100);

        var mapped = DrumMidiMapper.TryMap(note, out var hitId);

        Assert.True(mapped);
        Assert.Equal(expectedHitId, hitId);
    }

    [Theory]
    [InlineData(27, "rim")]
    [InlineData(28, "rim")]
    [InlineData(29, "rim")]
    [InlineData(30, "rim")]
    [InlineData(31, "rim")]
    [InlineData(32, "rim")]
    [InlineData(33, "rim")]
    [InlineData(34, "rim")]
    [InlineData(52, "crash")]
    [InlineData(53, "ride")]
    [InlineData(55, "crash")]
    [InlineData(58, "cowbell")]
    [InlineData(60, "high_tom")]
    [InlineData(61, "low_tom")]
    [InlineData(62, "muted")]
    [InlineData(63, "muted")]
    [InlineData(64, "low_tom")]
    [InlineData(65, "high_tom")]
    [InlineData(66, "mid_tom")]
    [InlineData(67, "high_tom")]
    [InlineData(68, "low_tom")]
    [InlineData(69, "clap")]
    [InlineData(70, "tambourine")]
    [InlineData(71, "pedal_hat")]
    [InlineData(72, "tambourine")]
    [InlineData(73, "ride")]
    [InlineData(74, "tambourine")]
    [InlineData(75, "rim")]
    [InlineData(76, "rim")]
    [InlineData(77, "low_tom")]
    [InlineData(78, "muted")]
    [InlineData(79, "muted")]
    [InlineData(80, "low_tom")]
    [InlineData(81, "high_tom")]
    [InlineData(82, "muted")]
    [InlineData(83, "tambourine")]
    [InlineData(84, "cowbell")]
    [InlineData(85, "open")]
    [InlineData(86, "open")]
    [InlineData(87, "open")]
    public void TryMapCoversCommonGeneralMidiPercussionFallbackNotes(int midiNote, string expectedHitId)
    {
        var note = new MidiNoteEvent(midiNote, 9, 0, 0.1, 100);

        var mapped = DrumMidiMapper.TryMap(note, out var hitId);

        Assert.True(mapped);
        Assert.Equal(expectedHitId, hitId);
    }

    [Fact]
    public void TryMapReturnsFalseForUnknownPercussionNote()
    {
        var note = new MidiNoteEvent(88, 9, 0, 0.1, 100);

        var mapped = DrumMidiMapper.TryMap(note, out var hitId);

        Assert.False(mapped);
        Assert.Null(hitId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(10)]
    public void TryMapReturnsFalseForNonPercussionChannels(int channel)
    {
        var note = new MidiNoteEvent(35, channel, 0, 0.1, 100);

        var mapped = DrumMidiMapper.TryMap(note, out var hitId);

        Assert.False(mapped);
        Assert.Null(hitId);
    }

    [Theory]
    [InlineData(24, "low_kick")]
    [InlineData(35, "kick")]
    [InlineData(43, "low_tom")]
    [InlineData(50, "mid_tom")]
    [InlineData(57, "snare")]
    [InlineData(64, "high_tom")]
    [InlineData(72, "rim")]
    [InlineData(84, "tambourine")]
    public void TryMapMelodicFallbackMapsPitchBandsToDrumHits(int midiNote, string expectedHitId)
    {
        var note = new MidiNoteEvent(midiNote, 0, 0, 0.1, 100);

        var mapped = DrumMidiMapper.TryMapMelodicFallback(note, out var hitId);

        Assert.True(mapped);
        Assert.Equal(expectedHitId, hitId);
    }

    [Fact]
    public void HasMappedPercussionReturnsTrueWhenSongContainsMappedChannelTenHits()
    {
        var events = new[]
        {
            new MidiNoteEvent(60, 0, 0, 0.1, 100),
            new MidiNoteEvent(36, 9, 0, 0.1, 100)
        };

        Assert.True(DrumMidiMapper.HasMappedPercussion(events));
    }

    [Fact]
    public void HasMappedPercussionReturnsFalseWhenSongHasOnlyMelodicChannels()
    {
        var events = new[]
        {
            new MidiNoteEvent(60, 0, 0, 0.1, 100),
            new MidiNoteEvent(72, 1, 0, 0.1, 100)
        };

        Assert.False(DrumMidiMapper.HasMappedPercussion(events));
    }
}
