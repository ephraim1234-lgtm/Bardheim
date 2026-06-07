using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Bardheim.Audio;
using Bardheim.Instruments;
using Bardheim.Notes;
using UnityEngine;

namespace Bardheim.Network;

public sealed class ValheimRoutedLyreNetwork : ILyreNetworkNoteEmitter, ILyreNetworkNoteTransport
{
    private const string RpcName = "Bardheim_NoteEvent";

    private readonly LyreNetworkNoteEmitter _emitter;
    private readonly Dictionary<string, ISet<string>> _validEventNamesByInstrument;
    private readonly LyreRemoteNoteIntakeGate _intakeGate;
    private readonly RemoteNotePlayback _remotePlayback;
    private readonly RemoteDrumPlayback _remoteDrumPlayback;
    private readonly RemoteFlutePlayback _remoteFlutePlayback;
    private readonly ManualLogSource _logger;
    private readonly bool _multiplayerEnabled;
    private readonly float _remoteVolume;
    private readonly int _remoteMaxSimultaneousNotes;
    private readonly float _remoteAudibleDistance;
    private bool _registered;

    public ValheimRoutedLyreNetwork(
        RemoteNotePlayback remotePlayback,
        RemoteDrumPlayback remoteDrumPlayback,
        RemoteFlutePlayback remoteFlutePlayback,
        ManualLogSource logger,
        bool multiplayerEnabled,
        float remoteVolume,
        int remoteMaxSimultaneousNotes,
        int remoteMaxNoteEventsPerSecond,
        float remoteAudibleDistance)
    {
        _emitter = new LyreNetworkNoteEmitter(this);
        _validEventNamesByInstrument = BuildValidEventNamesByInstrument();
        _intakeGate = new LyreRemoteNoteIntakeGate(remoteMaxNoteEventsPerSecond);
        _remotePlayback = remotePlayback;
        _remoteDrumPlayback = remoteDrumPlayback;
        _remoteFlutePlayback = remoteFlutePlayback;
        _logger = logger;
        _multiplayerEnabled = multiplayerEnabled;
        _remoteVolume = remoteVolume;
        _remoteMaxSimultaneousNotes = remoteMaxSimultaneousNotes;
        _remoteAudibleDistance = remoteAudibleDistance;
    }

    public void RegisterReceiver()
    {
        if (!_multiplayerEnabled)
        {
            _logger.LogInfo("Lyre multiplayer audio is disabled by config.");
            return;
        }

        if (_registered)
        {
            return;
        }

        var rpc = ZRoutedRpc.instance;
        if (rpc is null)
        {
            _logger.LogWarning("Lyre multiplayer audio RPC registration skipped because ZRoutedRpc is not ready.");
            return;
        }

        rpc.Register<ZPackage>(RpcName, OnRoutedNote);
        _registered = true;
        _logger.LogInfo("Lyre multiplayer audio RPC receiver registered.");
    }

    public void Emit(NoteRequest request, LyreNetworkNoteSource source, float volume)
    {
        _emitter.Emit(request, source, volume);
    }

    public void Emit(string instrumentId, string eventName, Vector3 position, LyreNetworkNoteSource source, float volume)
    {
        _emitter.Emit(instrumentId, eventName, position, source, volume);
    }

    public void Send(byte[] payload)
    {
        if (!_multiplayerEnabled)
        {
            return;
        }

        var rpc = ZRoutedRpc.instance;
        if (rpc is null)
        {
            return;
        }

        var package = new ZPackage();
        package.Write(payload);
        rpc.InvokeRoutedRPC(ZRoutedRpc.Everybody, RpcName, package);
    }

    private void OnRoutedNote(long senderPeerId, ZPackage package)
    {
        if (!_multiplayerEnabled)
        {
            return;
        }

        if (package is null)
        {
            return;
        }

        var payload = package.ReadByteArray();
        if (!LyreNetworkNoteSerializer.TryDeserialize(payload, out var noteEvent))
        {
            _logger.LogDebug("Skipped lyre multiplayer note because the payload was invalid.");
            return;
        }

        var localPeerId = ZNet.GetUID();
        if (!LyreNetworkNoteValidator.TryValidate(noteEvent, senderPeerId, localPeerId, _validEventNamesByInstrument, out var reason))
        {
            LogRejected(reason);
            return;
        }

        if (!_intakeGate.TryAccept(senderPeerId, noteEvent, Time.timeAsDouble, out reason))
        {
            LogRejected(reason);
            return;
        }

        if (noteEvent.InstrumentId == InstrumentCatalog.LyreId)
        {
            TryPlayRemoteLyreEvent(noteEvent);
        }
        else if (noteEvent.InstrumentId == InstrumentCatalog.DrumsId)
        {
            TryPlayRemoteDrumEvent(noteEvent);
        }
        else if (noteEvent.InstrumentId == InstrumentCatalog.FluteId)
        {
            TryPlayRemoteFluteEvent(noteEvent);
        }
    }

    private void TryPlayRemoteLyreEvent(LyreNetworkNoteEvent noteEvent)
    {
        if (!TryFindNote(noteEvent.NoteName, out var note))
        {
            LogRejected(LyreNetworkNoteRejectReason.UnknownNote);
            return;
        }

        var request = new NoteRequest(
            note,
            new Vector3(noteEvent.Position.X, noteEvent.Position.Y, noteEvent.Position.Z));
        _remotePlayback.Play(
            request,
            _remoteMaxSimultaneousNotes,
            noteEvent.Volume * _remoteVolume,
            _remoteAudibleDistance);
    }

    private void TryPlayRemoteDrumEvent(LyreNetworkNoteEvent noteEvent)
    {
        _remoteDrumPlayback.Play(
            noteEvent.NoteName,
            new Vector3(noteEvent.Position.X, noteEvent.Position.Y, noteEvent.Position.Z),
            _remoteMaxSimultaneousNotes,
            noteEvent.Volume * _remoteVolume,
            _remoteAudibleDistance);
    }

    private void TryPlayRemoteFluteEvent(LyreNetworkNoteEvent noteEvent)
    {
        if (!TryFindInstrumentNote(InstrumentCatalog.FluteId, noteEvent.NoteName, out var note))
        {
            LogRejected(LyreNetworkNoteRejectReason.UnknownNote);
            return;
        }

        var request = new NoteRequest(
            note,
            new Vector3(noteEvent.Position.X, noteEvent.Position.Y, noteEvent.Position.Z));
        _remoteFlutePlayback.Play(
            request,
            _remoteMaxSimultaneousNotes,
            noteEvent.Volume * _remoteVolume,
            _remoteAudibleDistance);
    }

    private void LogRejected(LyreNetworkNoteRejectReason reason)
    {
        if (reason != LyreNetworkNoteRejectReason.SelfOriginated)
        {
            _logger.LogDebug($"Skipped lyre multiplayer note: {reason}.");
        }
    }

    private bool TryFindNote(string noteName, out NoteDefinition note)
    {
        return TryFindInstrumentNote(InstrumentCatalog.LyreId, noteName, out note);
    }

    private bool TryFindInstrumentNote(string instrumentId, string noteName, out NoteDefinition note)
    {
        foreach (var instrument in InstrumentCatalog.CreateDefaultInstruments())
        {
            if (instrument.Id != instrumentId)
            {
                continue;
            }

            foreach (var noteSet in instrument.NoteSets)
            {
                foreach (var candidate in noteSet.Map.Notes)
                {
                    if (candidate.Name == noteName)
                    {
                        note = candidate;
                        return true;
                    }
                }
            }
        }

        note = new NoteDefinition(0, noteName, 0.0f);
        return false;
    }

    private static Dictionary<string, ISet<string>> BuildValidEventNamesByInstrument()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var noteSet in NoteSetCatalog.CreateDefaultSets())
        {
            foreach (var note in noteSet.Map.Notes)
            {
                names.Add(note.Name);
            }
        }

        var drumHits = new HashSet<string>(StringComparer.Ordinal);
        var fluteNotes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var instrument in InstrumentCatalog.CreateDefaultInstruments())
        {
            if (instrument.Id == InstrumentCatalog.DrumsId)
            {
                foreach (var hit in instrument.DrumHits)
                {
                    drumHits.Add(hit.Id);
                }
            }

            if (instrument.Id == InstrumentCatalog.FluteId)
            {
                foreach (var noteSet in instrument.NoteSets)
                {
                    foreach (var note in noteSet.Map.Notes)
                    {
                        fluteNotes.Add(note.Name);
                    }
                }
            }
        }

        return new Dictionary<string, ISet<string>>(StringComparer.Ordinal)
        {
            [InstrumentCatalog.LyreId] = names,
            [InstrumentCatalog.DrumsId] = drumHits,
            [InstrumentCatalog.FluteId] = fluteNotes
        };
    }
}
