using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace Bardheim.Tests;

public sealed class AudioPlaybackSourceTests
{
    [Fact]
    public void PlaybackRolesUseSharedClipCacheWithSeparateOverlapState()
    {
        var audioDir = Path.Combine(GetModRoot(), "src", "Bardheim", "Audio");
        var localSource = File.ReadAllText(Path.Combine(audioDir, "LocalNotePlayback.cs"));
        var remoteSource = File.ReadAllText(Path.Combine(audioDir, "RemoteNotePlayback.cs"));
        var cacheSource = File.ReadAllText(Path.Combine(audioDir, "LyreNoteClipCache.cs"));

        Assert.Contains("LyreNoteClipCache", localSource);
        Assert.Contains("LyreNoteClipCache", remoteSource);
        Assert.Contains("EmbeddedNoteSampleFactory.TryCreate", cacheSource);
        Assert.Contains("PlaceholderToneFactory.Create", cacheSource);
        Assert.Contains("Bardheim_LocalAudio", localSource);
        Assert.Contains("Bardheim_RemoteAudio", remoteSource);
        Assert.Contains("private readonly List<float> _activeUntil", localSource);
        Assert.Contains("private readonly List<float> _activeUntil", remoteSource);
    }

    [Fact]
    public void RemotePlaybackIsSpatialAndIndependentFromLocalPlayback()
    {
        var audioDir = Path.Combine(GetModRoot(), "src", "Bardheim", "Audio");
        var localSource = File.ReadAllText(Path.Combine(audioDir, "LocalNotePlayback.cs"));
        var remoteSource = File.ReadAllText(Path.Combine(audioDir, "RemoteNotePlayback.cs"));

        Assert.Contains("spatialBlend = 0.0f", localSource);
        Assert.Contains("spatialBlend = 1.0f", remoteSource);
        Assert.Contains("source.transform.position = request.Position", remoteSource);
        Assert.Contains("maxDistance", remoteSource);
        Assert.Contains("Skipped lyre note because the remote overlap limit is active.", remoteSource);
        Assert.DoesNotContain("LocalNotePlayback", remoteSource);
    }

    [Fact]
    public void DrumPlaybackUsesSeparateEmbeddedDrumClipCache()
    {
        var audioDir = Path.Combine(GetModRoot(), "src", "Bardheim", "Audio");
        var drumPlayback = File.ReadAllText(Path.Combine(audioDir, "LocalDrumPlayback.cs"));
        var drumCache = File.ReadAllText(Path.Combine(audioDir, "DrumHitClipCache.cs"));
        var drumFactory = File.ReadAllText(Path.Combine(audioDir, "EmbeddedDrumSampleFactory.cs"));

        Assert.Contains("DrumHitClipCache", drumPlayback);
        Assert.Contains("EmbeddedDrumSampleFactory.TryCreate", drumCache);
        Assert.Contains("Bardheim.Assets.Audio.Drums.", drumFactory);
        Assert.Contains("Bardheim_LocalDrumAudio", drumPlayback);
        Assert.DoesNotContain("PlaceholderToneFactory", drumCache);
    }

    [Fact]
    public void RemoteDrumPlaybackIsSpatialAndIndependentFromLocalDrumPlayback()
    {
        var audioDir = Path.Combine(GetModRoot(), "src", "Bardheim", "Audio");
        var localSource = File.ReadAllText(Path.Combine(audioDir, "LocalDrumPlayback.cs"));
        var remoteSource = File.ReadAllText(Path.Combine(audioDir, "RemoteDrumPlayback.cs"));

        Assert.Contains("DrumHitClipCache", remoteSource);
        Assert.Contains("spatialBlend = 1.0f", remoteSource);
        Assert.Contains("source.transform.position = position", remoteSource);
        Assert.Contains("maxDistance", remoteSource);
        Assert.Contains("Skipped drum hit because the remote overlap limit is active.", remoteSource);
        Assert.DoesNotContain("LocalDrumPlayback", remoteSource);
        Assert.Contains("spatialBlend = 0.0f", localSource);
    }

    [Fact]
    public void FlutePlaybackUsesSeparateEmbeddedFluteClipCache()
    {
        var audioDir = Path.Combine(GetModRoot(), "src", "Bardheim", "Audio");
        var flutePlayback = File.ReadAllText(Path.Combine(audioDir, "LocalFlutePlayback.cs"));
        var fluteCache = File.ReadAllText(Path.Combine(audioDir, "FluteNoteClipCache.cs"));
        var fluteFactory = File.ReadAllText(Path.Combine(audioDir, "EmbeddedFluteSampleFactory.cs"));
        var router = File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", "PlayMode", "LyreInputRouter.cs"));

        Assert.Contains("FluteNoteClipCache", flutePlayback);
        Assert.Contains("EmbeddedFluteSampleFactory.TryCreate", fluteCache);
        Assert.Contains("Bardheim.Assets.Audio.Flute.", fluteFactory);
        Assert.Contains("Bardheim_LocalFluteAudio", flutePlayback);
        Assert.Contains("LocalFlutePlayback", router);
        Assert.DoesNotContain("_playback.Play(request, maxSimultaneousNotes, volume)", GetMethodBody(router, "private void PlayFluteSlot"));
    }

    [Fact]
    public void FluteSampleFactoryLogsFluteSpecificResourceNames()
    {
        var factory = File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", "Audio", "EmbeddedFluteSampleFactory.cs"));

        Assert.Contains("Flute note sample resource not found", factory);
        Assert.Contains("Could not load flute note sample", factory);
        Assert.Contains("Loaded flute note sample", factory);
        Assert.Contains("Flute_", factory);
    }

    [Fact]
    public void RemoteFlutePlaybackIsSpatialAndIndependentFromLocalFlutePlayback()
    {
        var audioDir = Path.Combine(GetModRoot(), "src", "Bardheim", "Audio");
        var localSource = File.ReadAllText(Path.Combine(audioDir, "LocalFlutePlayback.cs"));
        var remoteSource = File.ReadAllText(Path.Combine(audioDir, "RemoteFlutePlayback.cs"));

        Assert.Contains("FluteNoteClipCache", remoteSource);
        Assert.Contains("spatialBlend = 1.0f", remoteSource);
        Assert.Contains("source.transform.position = request.Position", remoteSource);
        Assert.Contains("maxDistance", remoteSource);
        Assert.Contains("Skipped flute note because the remote overlap limit is active.", remoteSource);
        Assert.DoesNotContain("LocalFlutePlayback", remoteSource);
        Assert.Contains("spatialBlend = 0.0f", localSource);
    }

    private static string GetMethodBody(string source, string methodName)
    {
        var index = source.IndexOf(methodName, StringComparison.Ordinal);
        return index < 0 ? string.Empty : source.Substring(index, Math.Min(900, source.Length - index));
    }

    private static string GetModRoot([CallerFilePath] string sourceFile = "")
    {
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, "..", ".."));
    }
}
