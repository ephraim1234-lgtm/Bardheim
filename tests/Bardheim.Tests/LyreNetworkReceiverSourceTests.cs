using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace Bardheim.Tests;

public sealed class LyreNetworkReceiverSourceTests
{
    [Fact]
    public void RoutedRpcTransportBroadcastsSerializedPayloadsThroughZPackage()
    {
        var source = ReadSource("Network", "ValheimRoutedLyreNetwork.cs");

        Assert.Contains("ZRoutedRpc.instance", source);
        Assert.Contains("ZRoutedRpc.Everybody", source);
        Assert.Contains("InvokeRoutedRPC", source);
        Assert.Contains("new ZPackage()", source);
        Assert.Contains("package.Write(payload)", source);
        Assert.Contains("RpcName", source);
        Assert.Contains("RpcName = \"Bardheim_NoteEvent\"", source);
        Assert.DoesNotContain("LyreInstruments_NoteEvent", source);
    }

    [Fact]
    public void RoutedRpcReceiverRegistersAndValidatesBeforeRemotePlayback()
    {
        var source = ReadSource("Network", "ValheimRoutedLyreNetwork.cs");

        Assert.Contains("Register<ZPackage>", source);
        Assert.Contains("LyreNetworkNoteSerializer.TryDeserialize", source);
        Assert.Contains("LyreNetworkNoteValidator.TryValidate", source);
        Assert.Contains("_intakeGate.TryAccept", source);
        Assert.Contains("_remotePlayback.Play", source);
        Assert.Contains("_remoteDrumPlayback.Play", source);
        Assert.Contains("_remoteFlutePlayback.Play", source);
        Assert.True(
            source.IndexOf("LyreNetworkNoteValidator.TryValidate", StringComparison.Ordinal) <
            source.IndexOf("_remotePlayback.Play", StringComparison.Ordinal));
    }

    [Fact]
    public void PluginWiresRoutedNetworkAsEmitterAndReceiver()
    {
        var source = ReadSource("Plugin.cs");

        Assert.Contains("ValheimRoutedLyreNetwork", source);
        Assert.Contains("_network = new ValheimRoutedLyreNetwork", source);
        Assert.Contains("RemoteDrumPlayback", source);
        Assert.Contains("RemoteFlutePlayback", source);
        Assert.Contains("_config.EnableMultiplayerAudio.Value", source);
        Assert.Contains("_config.RemoteNoteVolume.Value", source);
        Assert.Contains("_config.RemoteMaxSimultaneousNotes.Value", source);
        Assert.Contains("_config.RemoteMaxNoteEventsPerSecond.Value", source);
        Assert.Contains("_config.RemoteAudibleDistance.Value", source);
        Assert.Contains("_noteEmitter = _network", source);
        Assert.Contains("_network.RegisterReceiver()", source);
        Assert.Contains("RemoteNotePlayback", source);
    }

    [Fact]
    public void RoutedRpcReceiverRoutesByInstrumentId()
    {
        var source = ReadSource("Network", "ValheimRoutedLyreNetwork.cs");

        Assert.Contains("InstrumentCatalog.LyreId", source);
        Assert.Contains("InstrumentCatalog.DrumsId", source);
        Assert.Contains("InstrumentCatalog.FluteId", source);
        Assert.Contains("BuildValidEventNamesByInstrument", source);
        Assert.Contains("TryPlayRemoteLyreEvent", source);
        Assert.Contains("TryPlayRemoteDrumEvent", source);
        Assert.Contains("TryPlayRemoteFluteEvent", source);
        Assert.Contains("_remoteDrumPlayback.Play", source);
        Assert.Contains("_remoteFlutePlayback.Play", source);
        Assert.Contains("[InstrumentCatalog.FluteId]", source);
    }

    [Fact]
    public void RoutedNetworkUsesConfiguredRemoteLimits()
    {
        var source = ReadSource("Network", "ValheimRoutedLyreNetwork.cs");

        Assert.DoesNotContain("DefaultRemoteMaxSimultaneousNotes", source);
        Assert.DoesNotContain("DefaultRemoteMaxEventsPerSecond", source);
        Assert.DoesNotContain("DefaultRemoteVolume", source);
        Assert.DoesNotContain("DefaultRemoteMaxDistance", source);
        Assert.Contains("_multiplayerEnabled", source);
        Assert.Contains("_remoteVolume", source);
        Assert.Contains("_remoteMaxSimultaneousNotes", source);
        Assert.Contains("_remoteAudibleDistance", source);
        Assert.Contains("new LyreRemoteNoteIntakeGate(remoteMaxNoteEventsPerSecond)", source);
        Assert.Contains("if (!_multiplayerEnabled)", source);
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
