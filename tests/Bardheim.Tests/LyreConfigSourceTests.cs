using System.IO;
using Xunit;

namespace Bardheim.Tests;

public sealed class LyreConfigSourceTests
{
    [Fact]
    public void ConfigDefinesSeparateMidiOverlapLimit()
    {
        var source = File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", "Config", "LyreConfig.cs"));

        Assert.Contains("MaxSimultaneousMidiNotes", source);
        Assert.Contains("Maximum overlapping local MIDI notes", source);
    }

    [Fact]
    public void ConfigDefinesSeparateFluteMidiRateLimit()
    {
        var source = File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", "Config", "LyreConfig.cs"));

        Assert.Contains("MaxFluteMidiNotesPerSecond", source);
        Assert.Contains("Maximum flute MIDI note starts per second", source);
    }

    [Fact]
    public void ConfigDefinesMultiplayerAudioControls()
    {
        var source = File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", "Config", "LyreConfig.cs"));

        Assert.Contains("EnableMultiplayerAudio", source);
        Assert.Contains("RemoteNoteVolume", source);
        Assert.Contains("RemoteMaxSimultaneousNotes", source);
        Assert.Contains("RemoteMaxNoteEventsPerSecond", source);
        Assert.Contains("RemoteAudibleDistance", source);
        Assert.Contains("\"Multiplayer\"", source);
        Assert.True(
            source.Contains("\"EnableMultiplayerAudio\",\r\n                false") ||
            source.Contains("\"EnableMultiplayerAudio\",\n                false"));
    }

    private static string GetModRoot([System.Runtime.CompilerServices.CallerFilePath] string sourceFile = "")
    {
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, "..", ".."));
    }
}
