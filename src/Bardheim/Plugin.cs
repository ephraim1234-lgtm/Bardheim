using System.IO;
using BepInEx;
using Bardheim.Audio;
using Bardheim.Config;
using Bardheim.Instruments;
using Bardheim.Items;
using Bardheim.Network;
using Bardheim.Notes;
using Bardheim.PlayMode;
using Bardheim.Songs;
using Bardheim.UI;
using Bardheim.Visuals;
using UnityEngine;

namespace Bardheim;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(Jotunn.Main.ModGuid)]
[DefaultExecutionOrder(-1000)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.valheimmodlab.bardheim";
    public const string PluginName = "Bardheim";
    public const string PluginVersion = "0.7.0";

    private LyreConfig? _config;
    private LyreItemRegistration? _lyreItemRegistration;
    private DrumItemRegistration? _drumItemRegistration;
    private FluteItemRegistration? _fluteItemRegistration;
    private LyrePlayMode? _playMode;
    private LyreInputRouter? _inputRouter;
    private LocalNotePlayback? _playback;
    private LocalDrumPlayback? _drumPlayback;
    private LocalFlutePlayback? _flutePlayback;
    private RemoteNotePlayback? _remotePlayback;
    private RemoteDrumPlayback? _remoteDrumPlayback;
    private RemoteFlutePlayback? _remoteFlutePlayback;
    private ILyreNetworkNoteEmitter? _noteEmitter;
    private ValheimRoutedLyreNetwork? _network;
    private MidiSongController? _songController;
    private PlayerFeedback? _feedback;
    private readonly HudVisibilityState _hudVisibility = new();

    private void Awake()
    {
        _config = LyreConfig.Bind(Config);
        _feedback = new PlayerFeedback(Logger);
        _playMode = new LyrePlayMode();
        var clipCache = new LyreNoteClipCache(Logger);
        _playback = new LocalNotePlayback(Logger, clipCache);
        _drumPlayback = new LocalDrumPlayback(Logger);
        _flutePlayback = new LocalFlutePlayback(Logger);
        _remotePlayback = new RemoteNotePlayback(Logger, clipCache);
        _remoteDrumPlayback = new RemoteDrumPlayback(Logger);
        _remoteFlutePlayback = new RemoteFlutePlayback(Logger);
        _network = new ValheimRoutedLyreNetwork(
            _remotePlayback,
            _remoteDrumPlayback,
            _remoteFlutePlayback,
            Logger,
            _config.EnableMultiplayerAudio.Value,
            _config.RemoteNoteVolume.Value,
            _config.RemoteMaxSimultaneousNotes.Value,
            _config.RemoteMaxNoteEventsPerSecond.Value,
            _config.RemoteAudibleDistance.Value);
        _noteEmitter = _network;
        _songController = new MidiSongController(
            PathForSongs(),
            _playback,
            _drumPlayback,
            _flutePlayback,
            _noteEmitter,
            _feedback,
            Logger);
        _inputRouter = new LyreInputRouter(
            new NoteSetSelector(NoteSetCatalog.CreateDefaultSets()),
            _playMode,
            _playback,
            _drumPlayback,
            _flutePlayback,
            _noteEmitter,
            _feedback,
            Logger);
        _lyreItemRegistration = new LyreItemRegistration(Logger);
        _drumItemRegistration = new DrumItemRegistration(Logger);
        _fluteItemRegistration = new FluteItemRegistration(Logger);

        _inputRouter.RegisterButtons();
        _network.RegisterReceiver();
        _lyreItemRegistration.Register();
        _drumItemRegistration.Register();
        _fluteItemRegistration.Register();

        Logger.LogInfo($"{PluginName} {PluginVersion} loaded. Equip the lyre, drum, or flute and press {_config.PlayModeToggle.Value} to toggle play mode.");
    }

    private void Update()
    {
        _hudVisibility.Update(IsControlHeld(), Input.GetKeyDown(KeyCode.F3));

        if (_config is null || _playMode is null || _inputRouter is null || _feedback is null || _songController is null)
        {
            return;
        }

        var player = Player.m_localPlayer;
        var activeInstrumentId = GetEquippedInstrumentId(player);
        var canPlay = activeInstrumentId is not null;

        if (_playMode.UpdateEligibility(canPlay))
        {
            _songController.StopIfPlaying();
            _feedback.Center("Instrument play mode off.");
        }

        if (_config.PlayModeToggle.Value.IsDown())
        {
            var changed = _playMode.Toggle(canPlay);
            if (changed)
            {
                if (!_playMode.IsActive)
                {
                    _songController.StopIfPlaying();
                }

                _feedback.Center(_playMode.IsActive ? $"{GetInstrumentDisplayName(activeInstrumentId)} play mode on." : "Instrument play mode off.");
            }
            else
            {
                _feedback.Center("Equip the lyre, drum, or flute to play.");
            }
        }

        _inputRouter.Update(player, activeInstrumentId, _config.MaxSimultaneousNotes.Value, _config.NoteVolume.Value);
        if (_playMode.IsActive && player is not null)
        {
            _songController.Update(
                player,
                activeInstrumentId,
                _config.MaxSimultaneousMidiNotes.Value,
                _config.MaxFluteMidiNotesPerSecond.Value,
                _config.NoteVolume.Value);
        }

        LyrePlayModeVisualPose.UpdateCalibration(_config.EnablePlayPoseCalibration.Value, Logger);
        DrumPlayModeVisualPose.UpdateCalibration(_config.EnablePlayPoseCalibration.Value, activeInstrumentId, Logger);
        FlutePlayModeVisualPose.UpdateCalibration(_config.EnablePlayPoseCalibration.Value, activeInstrumentId, Logger);
    }

    private void OnGUI()
    {
        _songController?.DrawHud(_playMode?.IsActive == true && !_hudVisibility.IsHidden);
    }

    private static bool IsControlHeld()
    {
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }

    private static string? GetEquippedInstrumentId(Player? player)
    {
        var equippedItems = player?.GetInventory()?.GetEquippedItems();
        if (equippedItems is null)
        {
            return null;
        }

        foreach (var item in equippedItems)
        {
            if (item?.m_dropPrefab is null)
            {
                continue;
            }

            if (item.m_dropPrefab.name == LyreItemRegistration.PrefabName)
            {
                return InstrumentCatalog.LyreId;
            }

            if (item.m_dropPrefab.name == DrumItemRegistration.PrefabName)
            {
                return InstrumentCatalog.DrumsId;
            }

            if (item.m_dropPrefab.name == FluteItemRegistration.PrefabName)
            {
                return InstrumentCatalog.FluteId;
            }
        }

        return null;
    }

    private static string GetInstrumentDisplayName(string? instrumentId)
    {
        return instrumentId switch
        {
            InstrumentCatalog.LyreId => "Lyre",
            InstrumentCatalog.DrumsId => "Drum",
            InstrumentCatalog.FluteId => "Flute",
            _ => "Instrument"
        };
    }

    private static string PathForSongs()
    {
        return Path.Combine(Paths.PluginPath, "Bardheim", "songs");
    }
}
