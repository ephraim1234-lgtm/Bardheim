using System.IO;
using BepInEx;
using BepInEx.Logging;
using Bardheim.Audio;
using Bardheim.Instruments;
using Bardheim.Network;
using Bardheim.Notes;
using Bardheim.UI;
using UnityEngine;

namespace Bardheim.Songs;

public sealed class MidiSongController
{
    private readonly MidiSongLibrary _library;
    private readonly MidiPlaybackSession _session = new();
    private readonly LocalNotePlayback _playback;
    private readonly LocalDrumPlayback _drumPlayback;
    private readonly LocalFlutePlayback _flutePlayback;
    private readonly FluteMidiRateLimiter _fluteMidiRateLimiter = new();
    private readonly ILyreNetworkNoteEmitter _noteEmitter;
    private readonly PlayerFeedback _feedback;
    private readonly ManualLogSource _logger;
    private readonly GUIStyle _mutedStyle = new();
    private readonly GUIStyle _selectedStyle = new();
    private readonly GUIStyle _playingStyle = new();
    private MidiSongProfile _profile = MidiSongProfile.Default;
    private int _lastLoadedCount = -1;
    private string _activeInstrumentId = InstrumentCatalog.LyreId;
    private MidiSong? _drumAnalysisSong;
    private bool _drumAnalysisHasMappedPercussion;
    private bool _stylesInitialized;

    public MidiSongController(LocalNotePlayback playback, PlayerFeedback feedback, ManualLogSource logger)
        : this(Path.Combine(Paths.PluginPath, "Bardheim", "songs"), playback, new LocalDrumPlayback(logger), new LocalFlutePlayback(logger), DisabledLyreNetworkNoteEmitter.Instance, feedback, logger)
    {
    }

    public MidiSongController(
        string songsDirectory,
        LocalNotePlayback playback,
        LocalDrumPlayback drumPlayback,
        LocalFlutePlayback flutePlayback,
        ILyreNetworkNoteEmitter noteEmitter,
        PlayerFeedback feedback,
        ManualLogSource logger)
    {
        _library = new MidiSongLibrary(songsDirectory);
        _playback = playback;
        _drumPlayback = drumPlayback;
        _flutePlayback = flutePlayback;
        _noteEmitter = noteEmitter;
        _feedback = feedback;
        _logger = logger;
        RefreshSongs();
    }

    public bool IsPlaying => _session.IsPlaying;

    public MidiSong? CurrentSong => _session.CurrentSong;

    public MidiSongLibrary Library => _library;

    public void Update(Player player, string? activeInstrumentId, int maxSimultaneousNotes, int maxFluteMidiNotesPerSecond, float volume)
    {
        _activeInstrumentId = activeInstrumentId ?? InstrumentCatalog.LyreId;

        if (Input.GetKeyDown(KeyCode.O))
        {
            TogglePlayback(maxFluteMidiNotesPerSecond);
        }

        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            SelectPrevious();
        }

        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            SelectNext();
        }

        if (Input.GetKeyDown(KeyCode.Minus))
        {
            ChangeSpeed(-0.25);
        }

        if (Input.GetKeyDown(KeyCode.Equals))
        {
            ChangeSpeed(0.25);
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ResetSpeed();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            ToggleLoop();
        }

        var dueNotes = _session.CollectDueNotes(Time.timeAsDouble);
        if (activeInstrumentId == InstrumentCatalog.FluteId)
        {
            var arrangedDueNotes = FluteMidiArranger.SelectPlayableEvents(dueNotes, _profile);
            foreach (var noteEvent in _fluteMidiRateLimiter.SelectPlayableEvents(arrangedDueNotes, _profile, maxFluteMidiNotesPerSecond))
            {
                PlayFluteMidiEvent(player, noteEvent, maxSimultaneousNotes, volume);
            }

            return;
        }

        foreach (var noteEvent in dueNotes)
        {
            if (activeInstrumentId == InstrumentCatalog.LyreId)
            {
                if (!_profile.Allows(noteEvent))
                {
                    continue;
                }

                PlayLyreMidiEvent(player, noteEvent, maxSimultaneousNotes, volume);
            }
            else if (activeInstrumentId == InstrumentCatalog.DrumsId)
            {
                PlayDrumMidiEvent(player, noteEvent, maxSimultaneousNotes, volume);
            }
        }
    }

    public void StopIfPlaying()
    {
        if (_session.IsPlaying)
        {
            _session.Stop();
            _fluteMidiRateLimiter.Reset();
            _feedback.Center($"{GetSongLabel()} stopped.");
        }
    }

    public void DrawHud(bool visible)
    {
        if (!visible)
        {
            return;
        }

        EnsureStyles();

        var visibleRows = Mathf.Min(10, Mathf.Max(1, _library.Songs.Count));
        var box = new Rect(24.0f, 100.0f, 520.0f, 142.0f + (visibleRows * 24.0f));
        GUI.Box(box, string.Empty);
        GUILayout.BeginArea(new Rect(box.x + 14.0f, box.y + 10.0f, box.width - 28.0f, box.height - 20.0f));
        GUILayout.Label(GetHudTitle(), _playingStyle);
        GUILayout.Label(GetStatusLine());
        GUILayout.Label("O play/stop    [ previous    ] next    - slower    = faster    0 reset    L loop", _mutedStyle);
        GUILayout.Space(4.0f);

        if (_library.Songs.Count == 0)
        {
            GUILayout.Label($"No .mid files in Bardheim/songs for {GetInstrumentName().ToLowerInvariant()}", _mutedStyle);
        }
        else
        {
            var start = Mathf.Max(0, Mathf.Min(_library.SelectedIndex - 4, Mathf.Max(0, _library.Songs.Count - visibleRows)));
            var end = Mathf.Min(_library.Songs.Count, start + visibleRows);
            for (var index = start; index < end; index++)
            {
                var song = _library.Songs[index];
                var isSelected = index == _library.SelectedIndex;
                var isPlaying = _session.CurrentSong == song && _session.IsPlaying;
                var marker = isPlaying ? ">" : isSelected ? "*" : " ";
                var style = isPlaying ? _playingStyle : isSelected ? _selectedStyle : GUI.skin.label;
                GUILayout.Label($"{marker} {index + 1:00}. {song.DisplayName}", style);
            }
        }

        GUILayout.EndArea();
    }

    private void TogglePlayback(int maxFluteMidiNotesPerSecond)
    {
        RefreshSongs();
        if (_session.IsPlaying)
        {
            _session.Stop();
            _fluteMidiRateLimiter.Reset();
            _feedback.Center($"{GetSongLabel()} stopped.");
            return;
        }

        var song = _library.SelectedSong;
        if (song is null)
        {
            _feedback.Center($"No {GetInstrumentName().ToLowerInvariant()} songs found.");
            return;
        }

        _profile = MidiSongProfile.LoadFor(song, _activeInstrumentId);
        if (_activeInstrumentId == InstrumentCatalog.FluteId)
        {
            _logger.LogInfo($"Flute MIDI profile for {song.DisplayName}: {_profile.DescribeChannelSelection()}");
        }

        _session.Start(song, Time.timeAsDouble);
        _fluteMidiRateLimiter.Prepare(
            FluteMidiArranger.SelectPlayableEvents(song.Events, _profile),
            _profile,
            maxFluteMidiNotesPerSecond);
        _feedback.Center($"{GetSongLabel()}: {song.DisplayName}");
    }

    private void SelectNext()
    {
        RefreshSongs();
        var song = _library.SelectNext();
        AnnounceSelection(song);
    }

    private void SelectPrevious()
    {
        RefreshSongs();
        var song = _library.SelectPrevious();
        AnnounceSelection(song);
    }

    private void AnnounceSelection(MidiSong? song)
    {
        if (song is null)
        {
            _feedback.Center($"No {GetInstrumentName().ToLowerInvariant()} songs found.");
            return;
        }

        if (_session.IsPlaying)
        {
            _session.Stop();
            _fluteMidiRateLimiter.Reset();
        }

        _feedback.Center($"Selected {GetSongLabel().ToLowerInvariant()}: {song.DisplayName}");
    }

    private void RefreshSongs()
    {
        _library.Refresh();
        if (_library.Songs.Count != _lastLoadedCount)
        {
            _lastLoadedCount = _library.Songs.Count;
            _logger.LogInfo($"Loaded {_library.Songs.Count} MIDI song(s).");
        }

        foreach (var skippedFile in _library.SkippedFiles)
        {
            _logger.LogWarning($"Skipped unsupported MIDI file: {Path.GetFileName(skippedFile)}");
        }
    }

    private void ChangeSpeed(double delta)
    {
        _session.SetSpeed(_session.SpeedMultiplier + delta, Time.timeAsDouble);
        _feedback.Center($"{GetSongLabel()} speed: {_session.SpeedMultiplier:0.00}x");
    }

    private void ResetSpeed()
    {
        _session.SetSpeed(1.0, Time.timeAsDouble);
        _feedback.Center($"{GetSongLabel()} speed: 1.00x");
    }

    private void ToggleLoop()
    {
        var enabled = _session.ToggleLoopEnabled();
        _feedback.Center(enabled ? $"{GetSongLabel()} loop on." : $"{GetSongLabel()} loop off.");
    }

    private void PlayLyreMidiEvent(Player player, MidiNoteEvent noteEvent, int maxSimultaneousNotes, float volume)
    {
        var mapped = MidiNoteMapper.MapToLyreNote(_profile.ApplyOctaveOffset(noteEvent));
        var request = new NoteRequest(mapped, player.transform.position);
        if (_playback.Play(request, maxSimultaneousNotes, volume))
        {
            _noteEmitter.Emit(request, LyreNetworkNoteSource.Midi, volume);
        }
    }

    private void PlayDrumMidiEvent(Player player, MidiNoteEvent noteEvent, int maxSimultaneousHits, float volume)
    {
        if (!DrumMidiMapper.TryMap(noteEvent, out var hitId) || hitId is null)
        {
            if (CurrentDrumSongHasMappedPercussion() || !DrumMidiMapper.TryMapMelodicFallback(noteEvent, out hitId))
            {
                return;
            }
        }

        if (hitId is null)
        {
            return;
        }

        var velocityVolume = volume * Mathf.Clamp01(noteEvent.Velocity / 127.0f);
        if (_drumPlayback.Play(hitId, player.transform.position, maxSimultaneousHits, velocityVolume))
        {
            _noteEmitter.Emit(InstrumentCatalog.DrumsId, hitId, player.transform.position, LyreNetworkNoteSource.Midi, velocityVolume);
        }
    }

    private void PlayFluteMidiEvent(Player player, MidiNoteEvent noteEvent, int maxSimultaneousNotes, float volume)
    {
        var mapped = FluteMidiMapper.MapToFluteNote(_profile.ApplyOctaveOffset(noteEvent));
        var request = new NoteRequest(mapped, player.transform.position);
        if (_flutePlayback.Play(request, maxSimultaneousNotes, volume))
        {
            _noteEmitter.Emit(InstrumentCatalog.FluteId, mapped.Name, player.transform.position, LyreNetworkNoteSource.Midi, volume);
        }
    }

    private bool CurrentDrumSongHasMappedPercussion()
    {
        var song = _session.CurrentSong;
        if (song is null)
        {
            return false;
        }

        if (!ReferenceEquals(song, _drumAnalysisSong))
        {
            _drumAnalysisSong = song;
            _drumAnalysisHasMappedPercussion = DrumMidiMapper.HasMappedPercussion(song.Events);
        }

        return _drumAnalysisHasMappedPercussion;
    }

    private string GetStatusLine()
    {
        var selected = _library.SelectedSong?.DisplayName ?? "No song";
        var state = _session.IsPlaying ? "Playing" : "Selected";
        var progress = string.Empty;
        if (_session.CurrentSong is not null && _session.CurrentSong.LengthSeconds > 0.0)
        {
            progress = $"  {FormatSeconds(_session.ElapsedSongSeconds)} / {FormatSeconds(_session.CurrentSong.LengthSeconds)}";
        }

        var loop = _session.LoopEnabled ? "On" : "Off";
        return $"{state}: {selected}  Songs: {_library.Songs.Count}  Speed: {_session.SpeedMultiplier:0.00}x  Loop: {loop}{progress}";
    }

    private string GetHudTitle()
    {
        return _activeInstrumentId switch
        {
            InstrumentCatalog.DrumsId => "DRUM SONGS",
            InstrumentCatalog.FluteId => "FLUTE SONGS",
            _ => "LYRE SONGS"
        };
    }

    private string GetSongLabel()
    {
        return _activeInstrumentId switch
        {
            InstrumentCatalog.DrumsId => "Drum song",
            InstrumentCatalog.FluteId => "Flute song",
            _ => "Lyre song"
        };
    }

    private string GetInstrumentName()
    {
        return _activeInstrumentId switch
        {
            InstrumentCatalog.DrumsId => "Drum",
            InstrumentCatalog.FluteId => "Flute",
            _ => "Lyre"
        };
    }

    private void EnsureStyles()
    {
        if (_stylesInitialized)
        {
            return;
        }

        _mutedStyle.normal.textColor = new Color(0.72f, 0.72f, 0.72f);
        _selectedStyle.normal.textColor = new Color(1.0f, 0.88f, 0.48f);
        _playingStyle.normal.textColor = new Color(0.48f, 1.0f, 0.62f);
        _stylesInitialized = true;
    }

    private static string FormatSeconds(double seconds)
    {
        var wholeSeconds = Mathf.Max(0, Mathf.FloorToInt((float)seconds));
        return $"{wholeSeconds / 60:00}:{wholeSeconds % 60:00}";
    }
}
