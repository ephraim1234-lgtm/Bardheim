using System.Collections.Generic;

namespace Bardheim.Songs;

public sealed class MidiPlaybackSession
{
    private const double MinSpeedMultiplier = 0.25;
    private const double MaxSpeedMultiplier = 3.0;

    private MidiSong? _song;
    private double _startTimeSeconds;
    private double _baseSongSeconds;
    private int _nextEventIndex;

    public bool IsPlaying => _song is not null;

    public MidiSong? CurrentSong => _song;

    public double SpeedMultiplier { get; private set; } = 1.0;

    public double ElapsedSongSeconds { get; private set; }

    public bool LoopEnabled { get; private set; }

    public void Start(MidiSong song, double startTimeSeconds)
    {
        _song = song;
        _startTimeSeconds = startTimeSeconds;
        _baseSongSeconds = 0.0;
        ElapsedSongSeconds = 0.0;
        _nextEventIndex = 0;
    }

    public void Stop()
    {
        _song = null;
        _nextEventIndex = 0;
        _startTimeSeconds = 0.0;
        _baseSongSeconds = 0.0;
        ElapsedSongSeconds = 0.0;
    }

    public IReadOnlyList<MidiNoteEvent> CollectDueNotes(double nowSeconds)
    {
        if (_song is null)
        {
            return System.Array.Empty<MidiNoteEvent>();
        }

        var elapsed = GetElapsedSongSeconds(nowSeconds);
        if (_song.LengthSeconds > 0.0 && LoopEnabled)
        {
            while (elapsed > _song.LengthSeconds)
            {
                elapsed -= _song.LengthSeconds;
                _baseSongSeconds -= _song.LengthSeconds;
                _nextEventIndex = 0;
            }
        }

        ElapsedSongSeconds = elapsed;
        var due = new List<MidiNoteEvent>();
        while (_nextEventIndex < _song.Events.Count &&
            _song.Events[_nextEventIndex].StartSeconds <= elapsed)
        {
            due.Add(_song.Events[_nextEventIndex]);
            _nextEventIndex++;
        }

        if (_nextEventIndex >= _song.Events.Count && elapsed > _song.LengthSeconds)
        {
            Stop();
        }

        return due;
    }

    public void SetSpeed(double speedMultiplier, double? nowSeconds = null)
    {
        var clamped = System.Math.Max(MinSpeedMultiplier, System.Math.Min(MaxSpeedMultiplier, speedMultiplier));
        if (_song is not null && nowSeconds.HasValue)
        {
            _baseSongSeconds = GetElapsedSongSeconds(nowSeconds.Value);
            _startTimeSeconds = nowSeconds.Value;
            ElapsedSongSeconds = _baseSongSeconds;
        }

        SpeedMultiplier = clamped;
    }

    public void SetLoopEnabled(bool loopEnabled)
    {
        LoopEnabled = loopEnabled;
    }

    public bool ToggleLoopEnabled()
    {
        LoopEnabled = !LoopEnabled;
        return LoopEnabled;
    }

    private double GetElapsedSongSeconds(double nowSeconds)
    {
        return _baseSongSeconds + ((nowSeconds - _startTimeSeconds) * SpeedMultiplier);
    }
}
