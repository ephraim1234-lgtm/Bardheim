using System;
using System.IO;
using System.Runtime.CompilerServices;
using Bardheim.Instruments;
using Xunit;

namespace Bardheim.Tests;

public sealed class MidiSongControllerSourceTests
{
    [Fact]
    public void MidiSongControllerRoutesPlaybackByActiveInstrument()
    {
        var source = ReadSource(Path.Combine("Songs", "MidiSongController.cs"));

        Assert.Contains("LocalDrumPlayback", source);
        Assert.Contains("string? activeInstrumentId", source);
        Assert.Contains("InstrumentCatalog.LyreId", source);
        Assert.Contains("InstrumentCatalog.DrumsId", source);
        Assert.Contains("InstrumentCatalog.FluteId", source);
        Assert.Contains("PlayLyreMidiEvent", source);
        Assert.Contains("PlayDrumMidiEvent", source);
        Assert.Contains("PlayFluteMidiEvent", source);
        Assert.Contains("FluteMidiRateLimiter", source);
        Assert.Contains("_fluteMidiRateLimiter.SelectPlayableEvents", source);
        Assert.Contains("LocalFlutePlayback", source);
        Assert.Contains("DrumMidiMapper.TryMap", source);
        Assert.Contains("_drumPlayback.Play", source);
        Assert.Contains("_flutePlayback.Play", source);
    }

    [Fact]
    public void MidiSongControllerKeepsLyreChannelTenMutedButLetsDrumsUsePercussion()
    {
        var source = ReadSource(Path.Combine("Songs", "MidiSongController.cs"));

        Assert.True(
            source.IndexOf("InstrumentCatalog.LyreId", StringComparison.Ordinal) <
            source.IndexOf("_profile.Allows(noteEvent)", StringComparison.Ordinal));
        Assert.True(
            source.IndexOf("InstrumentCatalog.DrumsId", StringComparison.Ordinal) <
            source.IndexOf("DrumMidiMapper.TryMap", StringComparison.Ordinal));
        Assert.True(
            source.IndexOf("InstrumentCatalog.FluteId", StringComparison.Ordinal) <
            source.IndexOf("_profile.Allows(noteEvent)", source.IndexOf("InstrumentCatalog.FluteId", StringComparison.Ordinal), StringComparison.Ordinal));
    }

    [Fact]
    public void MidiSongControllerUsesDrumFallbackOnlyWhenSongHasNoMappedPercussion()
    {
        var source = ReadSource(Path.Combine("Songs", "MidiSongController.cs"));

        Assert.Contains("HasMappedPercussion", source);
        Assert.Contains("TryMapMelodicFallback", source);
        Assert.Contains("CurrentDrumSongHasMappedPercussion", source);
        Assert.True(
            source.IndexOf("CurrentDrumSongHasMappedPercussion", StringComparison.Ordinal) <
            source.IndexOf("TryMapMelodicFallback", StringComparison.Ordinal));
    }

    [Fact]
    public void MidiSongHudHasInstrumentAwareLabels()
    {
        var source = ReadSource(Path.Combine("Songs", "MidiSongController.cs"));

        Assert.Contains("GetSongLabel", source);
        Assert.Contains("LYRE SONGS", source);
        Assert.Contains("DRUM SONGS", source);
        Assert.Contains("FLUTE SONGS", source);
        Assert.Contains("Drum song", source);
        Assert.Contains("Flute song", source);
    }

    [Fact]
    public void MidiSongControllerLoadsProfilesForActiveInstrument()
    {
        var source = ReadSource(Path.Combine("Songs", "MidiSongController.cs"));

        Assert.Contains("MidiSongProfile.LoadFor(song, _activeInstrumentId)", source);
        Assert.DoesNotContain("MidiSongProfile.LoadFor(song);", source);
    }

    [Fact]
    public void FluteMidiUsesLyreStyleConfiguredPlaybackVolume()
    {
        var source = ReadSource(Path.Combine("Songs", "MidiSongController.cs"));
        var fluteMethodStart = source.IndexOf("private void PlayFluteMidiEvent", StringComparison.Ordinal);
        var nextMethodStart = source.IndexOf("private bool CurrentDrumSongHasMappedPercussion", fluteMethodStart, StringComparison.Ordinal);
        var fluteMethod = source.Substring(fluteMethodStart, nextMethodStart - fluteMethodStart);

        Assert.Contains("_flutePlayback.Play(request, maxSimultaneousNotes, volume)", fluteMethod);
        Assert.Contains("LyreNetworkNoteSource.Midi, volume", fluteMethod);
        Assert.DoesNotContain("Velocity", fluteMethod);
    }

    [Fact]
    public void PluginPassesActiveInstrumentToMidiSongController()
    {
        var plugin = ReadSource("Plugin.cs");

        Assert.Contains("_songController.Update(", plugin);
        Assert.Contains("activeInstrumentId", plugin);
    }

    [Fact]
    public void PluginPassesFluteMidiRateLimitToMidiSongController()
    {
        var plugin = ReadSource("Plugin.cs");

        Assert.Contains("_config.MaxFluteMidiNotesPerSecond.Value", plugin);
    }

    [Fact]
    public void PluginHidesMidiSongHudWhenGlobalHudIsHidden()
    {
        var plugin = ReadSource("Plugin.cs");

        Assert.Contains("HudVisibilityState", plugin);
        Assert.Contains("Input.GetKeyDown(KeyCode.F3)", plugin);
        Assert.Contains("Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)", plugin);
        Assert.Contains("_songController?.DrawHud(_playMode?.IsActive == true && !_hudVisibility.IsHidden)", plugin);
    }

    [Fact]
    public void BardheimSongPathUsesBardheimFolderOnly()
    {
        var plugin = ReadSource("Plugin.cs");

        Assert.Contains("return Path.Combine(Paths.PluginPath, \"Bardheim\", \"songs\")", plugin);
        Assert.DoesNotContain("LyreInstruments", plugin);
        Assert.DoesNotContain("legacyPath", plugin);
    }

    private static string ReadSource(string relativePath)
    {
        return File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", relativePath));
    }

    private static string GetModRoot([CallerFilePath] string sourceFile = "")
    {
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, "..", ".."));
    }
}
