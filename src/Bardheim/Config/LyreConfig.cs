using BepInEx.Configuration;
using UnityEngine;

namespace Bardheim.Config;

public sealed class LyreConfig
{
    private LyreConfig(
        ConfigEntry<KeyboardShortcut> playModeToggle,
        ConfigEntry<float> noteVolume,
        ConfigEntry<int> maxSimultaneousNotes,
        ConfigEntry<int> maxSimultaneousMidiNotes,
        ConfigEntry<int> maxFluteMidiNotesPerSecond,
        ConfigEntry<bool> enableMultiplayerAudio,
        ConfigEntry<float> remoteNoteVolume,
        ConfigEntry<int> remoteMaxSimultaneousNotes,
        ConfigEntry<int> remoteMaxNoteEventsPerSecond,
        ConfigEntry<float> remoteAudibleDistance,
        ConfigEntry<bool> enablePlayPoseCalibration)
    {
        PlayModeToggle = playModeToggle;
        NoteVolume = noteVolume;
        MaxSimultaneousNotes = maxSimultaneousNotes;
        MaxSimultaneousMidiNotes = maxSimultaneousMidiNotes;
        MaxFluteMidiNotesPerSecond = maxFluteMidiNotesPerSecond;
        EnableMultiplayerAudio = enableMultiplayerAudio;
        RemoteNoteVolume = remoteNoteVolume;
        RemoteMaxSimultaneousNotes = remoteMaxSimultaneousNotes;
        RemoteMaxNoteEventsPerSecond = remoteMaxNoteEventsPerSecond;
        RemoteAudibleDistance = remoteAudibleDistance;
        EnablePlayPoseCalibration = enablePlayPoseCalibration;
    }

    public ConfigEntry<KeyboardShortcut> PlayModeToggle { get; }

    public ConfigEntry<float> NoteVolume { get; }

    public ConfigEntry<int> MaxSimultaneousNotes { get; }

    public ConfigEntry<int> MaxSimultaneousMidiNotes { get; }

    public ConfigEntry<int> MaxFluteMidiNotesPerSecond { get; }

    public ConfigEntry<bool> EnableMultiplayerAudio { get; }

    public ConfigEntry<float> RemoteNoteVolume { get; }

    public ConfigEntry<int> RemoteMaxSimultaneousNotes { get; }

    public ConfigEntry<int> RemoteMaxNoteEventsPerSecond { get; }

    public ConfigEntry<float> RemoteAudibleDistance { get; }

    public ConfigEntry<bool> EnablePlayPoseCalibration { get; }

    public static LyreConfig Bind(ConfigFile config)
    {
        return new LyreConfig(
            config.Bind(
                "Input",
                "PlayModeToggle",
                new KeyboardShortcut(KeyCode.P, KeyCode.LeftAlt),
                "Toggles lyre play mode while the lyre is equipped."),
            config.Bind(
                "Audio",
                "NoteVolume",
                0.45f,
                new ConfigDescription(
                    "Local placeholder note volume.",
                    new AcceptableValueRange<float>(0.0f, 1.0f))),
            config.Bind(
                "Audio",
                "MaxSimultaneousNotes",
                6,
                new ConfigDescription(
                    "Maximum overlapping local placeholder notes.",
                    new AcceptableValueRange<int>(1, 12))),
            config.Bind(
                "Audio",
                "MaxSimultaneousMidiNotes",
                24,
                new ConfigDescription(
                    "Maximum overlapping local MIDI notes. Raise this for dense multi-channel songs; lower it if playback is too busy.",
                    new AcceptableValueRange<int>(1, 48))),
            config.Bind(
                "Audio",
                "MaxFluteMidiNotesPerSecond",
                14,
                new ConfigDescription(
                    "Maximum flute MIDI note starts per second. Lower this if dense songs drown out the flute melody; raise it for busier flute playback.",
                    new AcceptableValueRange<int>(1, 48))),
            config.Bind(
                "Multiplayer",
                "EnableMultiplayerAudio",
                false,
                "Enables experimental sending and receiving of nearby-player instrument audio through Valheim routed RPC."),
            config.Bind(
                "Multiplayer",
                "RemoteNoteVolume",
                1.0f,
                new ConfigDescription(
                    "Volume multiplier applied to lyre notes heard from other players.",
                    new AcceptableValueRange<float>(0.0f, 2.0f))),
            config.Bind(
                "Multiplayer",
                "RemoteMaxSimultaneousNotes",
                24,
                new ConfigDescription(
                    "Maximum overlapping lyre notes heard from other players.",
                    new AcceptableValueRange<int>(1, 48))),
            config.Bind(
                "Multiplayer",
                "RemoteMaxNoteEventsPerSecond",
                48,
                new ConfigDescription(
                    "Maximum remote lyre note events accepted per sender per second.",
                    new AcceptableValueRange<int>(1, 120))),
            config.Bind(
                "Multiplayer",
                "RemoteAudibleDistance",
                32.0f,
                new ConfigDescription(
                    "Maximum distance for spatial lyre notes heard from other players.",
                    new AcceptableValueRange<float>(4.0f, 96.0f))),
            config.Bind(
                "VisualDebug",
                "EnablePlayPoseCalibration",
                false,
                "Enables in-game play-mode visual pose nudge keys for calibration. Keep disabled during normal play."));
    }
}
