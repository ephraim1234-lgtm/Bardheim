using System.Linq;
using Bardheim.Songs;
using Xunit;

namespace Bardheim.Tests;

public sealed class FluteMidiRateLimiterTests
{
    [Fact]
    public void LimitsFluteMidiNoteStartsPerSongSecondWithoutChangingChannels()
    {
        var limiter = new FluteMidiRateLimiter();
        var notes = new[]
        {
            new MidiNoteEvent(60, 0, 12.00, 0.25, 100),
            new MidiNoteEvent(64, 1, 12.10, 0.25, 100),
            new MidiNoteEvent(67, 2, 12.20, 0.25, 100),
            new MidiNoteEvent(71, 3, 12.30, 0.25, 100),
            new MidiNoteEvent(72, 4, 13.00, 0.25, 100)
        };

        limiter.Prepare(notes, MidiSongProfile.Default, maxNoteStartsPerSecond: 3);
        var selected = limiter
            .SelectPlayableEvents(notes, MidiSongProfile.Default, maxNoteStartsPerSecond: 3)
            .ToArray();

        Assert.Equal(new[] { 64, 67, 71, 72 }, selected.Select(note => note.MidiNote));
        Assert.Equal(new[] { 1, 2, 3, 4 }, selected.Select(note => note.Channel));
    }

    [Fact]
    public void KeepsLaterHigherMelodyNotesInsteadOfEarlierLowAccompaniment()
    {
        var limiter = new FluteMidiRateLimiter();
        var notes = new[]
        {
            new MidiNoteEvent(48, 0, 24.00, 0.25, 100),
            new MidiNoteEvent(52, 1, 24.10, 0.25, 100),
            new MidiNoteEvent(76, 2, 24.65, 0.25, 100),
            new MidiNoteEvent(79, 3, 24.80, 0.25, 100)
        };

        limiter.Prepare(notes, MidiSongProfile.Default, maxNoteStartsPerSecond: 2);

        var firstUpdate = limiter
            .SelectPlayableEvents(notes.Take(2), MidiSongProfile.Default, maxNoteStartsPerSecond: 2)
            .ToArray();
        var secondUpdate = limiter
            .SelectPlayableEvents(notes.Skip(2), MidiSongProfile.Default, maxNoteStartsPerSecond: 2)
            .ToArray();

        Assert.Empty(firstUpdate);
        Assert.Equal(new[] { 76, 79 }, secondUpdate.Select(note => note.MidiNote));
    }

    [Fact]
    public void DoesNotCountMutedPercussionAgainstFluteMidiRateLimit()
    {
        var limiter = new FluteMidiRateLimiter();
        var notes = new[]
        {
            new MidiNoteEvent(38, 9, 20.00, 0.10, 127),
            new MidiNoteEvent(60, 0, 20.01, 0.25, 100),
            new MidiNoteEvent(64, 1, 20.02, 0.25, 100)
        };

        limiter.Prepare(notes, MidiSongProfile.Default, maxNoteStartsPerSecond: 2);
        var selected = limiter
            .SelectPlayableEvents(notes, MidiSongProfile.Default, maxNoteStartsPerSecond: 2)
            .ToArray();

        Assert.Equal(new[] { 60, 64 }, selected.Select(note => note.MidiNote));
    }

    [Fact]
    public void ResetsWhenPlaybackStartsOver()
    {
        var limiter = new FluteMidiRateLimiter();
        var firstPass = new[]
        {
            new MidiNoteEvent(60, 0, 0.00, 0.25, 100),
            new MidiNoteEvent(64, 1, 0.10, 0.25, 100)
        };

        limiter.Prepare(firstPass, MidiSongProfile.Default, maxNoteStartsPerSecond: 1);
        Assert.Single(limiter.SelectPlayableEvents(firstPass, MidiSongProfile.Default, maxNoteStartsPerSecond: 1));

        limiter.Reset();
        limiter.Prepare(firstPass, MidiSongProfile.Default, maxNoteStartsPerSecond: 1);

        var secondPass = limiter
            .SelectPlayableEvents(firstPass, MidiSongProfile.Default, maxNoteStartsPerSecond: 1)
            .ToArray();

        Assert.Single(secondPass);
        Assert.Equal(64, secondPass[0].MidiNote);
    }
}
