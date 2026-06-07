using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace Bardheim.Tests;

public sealed class LyreNetworkSenderSourceTests
{
    [Fact]
    public void ManualInputEmitsNetworkEventOnlyAfterLocalPlaybackSucceeds()
    {
        var source = ReadSource("PlayMode", "LyreInputRouter.cs");

        Assert.Contains("ILyreNetworkNoteEmitter", source);
        Assert.Contains("LyreNetworkNoteSource.Manual", source);
        Assert.Contains("if (_playback.Play(request, maxSimultaneousNotes, volume))", source);
        Assert.True(
            source.IndexOf("_playback.Play(request, maxSimultaneousNotes, volume)", StringComparison.Ordinal) <
            source.IndexOf("_noteEmitter.Emit(request, LyreNetworkNoteSource.Manual, volume)", StringComparison.Ordinal));
    }

    [Fact]
    public void MidiPlaybackEmitsNetworkEventOnlyAfterLocalPlaybackSucceeds()
    {
        var source = ReadSource("Songs", "MidiSongController.cs");

        Assert.Contains("ILyreNetworkNoteEmitter", source);
        Assert.Contains("LyreNetworkNoteSource.Midi", source);
        Assert.Contains("if (_playback.Play(request, maxSimultaneousNotes, volume))", source);
        Assert.True(
            source.IndexOf("_playback.Play(request, maxSimultaneousNotes, volume)", StringComparison.Ordinal) <
            source.IndexOf("_noteEmitter.Emit(request, LyreNetworkNoteSource.Midi, volume)", StringComparison.Ordinal));
    }

    [Fact]
    public void DrumManualInputEmitsInstrumentAwareNetworkEventOnlyAfterLocalPlaybackSucceeds()
    {
        var source = ReadSource("PlayMode", "LyreInputRouter.cs");

        Assert.Contains("InstrumentCatalog.DrumsId", source);
        Assert.Contains("_drumPlayback.Play", source);
        Assert.Contains("_noteEmitter.Emit(InstrumentCatalog.DrumsId", source);
        Assert.True(
            source.IndexOf("_drumPlayback.Play", StringComparison.Ordinal) <
            source.IndexOf("_noteEmitter.Emit(InstrumentCatalog.DrumsId", StringComparison.Ordinal));
    }

    [Fact]
    public void DrumMidiPlaybackEmitsInstrumentAwareNetworkEventOnlyAfterLocalPlaybackSucceeds()
    {
        var source = ReadSource("Songs", "MidiSongController.cs");

        Assert.Contains("DrumMidiMapper.TryMap", source);
        Assert.Contains("_drumPlayback.Play", source);
        Assert.Contains("_noteEmitter.Emit(InstrumentCatalog.DrumsId", source);
        Assert.True(
            source.IndexOf("_drumPlayback.Play", StringComparison.Ordinal) <
            source.IndexOf("_noteEmitter.Emit(InstrumentCatalog.DrumsId", StringComparison.Ordinal));
    }

    [Fact]
    public void PluginWiresNetworkEmitterThroughRoutedNetwork()
    {
        var source = ReadSource("Plugin.cs");

        Assert.Contains("ValheimRoutedLyreNetwork", source);
        Assert.Contains("_noteEmitter", source);
        Assert.Contains("_noteEmitter = _network", source);
    }

    [Fact]
    public void NetworkEmitterBuildsVersionedPayloadForTransport()
    {
        var source = ReadSource("Network", "LyreNetworkNoteEmitter.cs");

        Assert.Contains("LyreNetworkNoteSerializer.Serialize", source);
        Assert.Contains("LyreNetworkNoteEvent", source);
        Assert.Contains("string instrumentId", source);
        Assert.Contains("Interlocked.Increment", source);
        Assert.Contains("ILyreNetworkNoteTransport", source);
    }

    private static string ReadSource(params string[] parts)
    {
        return File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", Path.Combine(parts)));
    }

    private static string GetModRoot([CallerFilePath] string sourceFile = "")
    {
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, "..", ".."));
    }
}
