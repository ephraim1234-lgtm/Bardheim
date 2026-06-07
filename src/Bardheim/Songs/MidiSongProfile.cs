using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bardheim.Instruments;

namespace Bardheim.Songs;

public sealed class MidiSongProfile
{
    private readonly HashSet<int> _activeChannels;
    private readonly HashSet<int> _mutedChannels;
    private readonly Dictionary<int, int> _channelOctaveOffsets;

    private MidiSongProfile(
        HashSet<int> activeChannels,
        HashSet<int> mutedChannels,
        Dictionary<int, int> channelOctaveOffsets,
        MidiArrangementMode arrangementMode,
        int maxSimultaneousMelodyNotes)
    {
        _activeChannels = activeChannels;
        _mutedChannels = mutedChannels;
        _channelOctaveOffsets = channelOctaveOffsets;
        ArrangementMode = arrangementMode;
        MaxSimultaneousMelodyNotes = maxSimultaneousMelodyNotes;
    }

    public static MidiSongProfile Default { get; } = new(new HashSet<int>(), new HashSet<int> { 10 }, new Dictionary<int, int>(), MidiArrangementMode.Full, int.MaxValue);

    public MidiArrangementMode ArrangementMode { get; }

    public int MaxSimultaneousMelodyNotes { get; }

    public string DescribeChannelSelection()
    {
        var active = _activeChannels.Count == 0
            ? "all"
            : string.Join(",", _activeChannels.OrderBy(channel => channel));
        var muted = _mutedChannels.Count == 0
            ? "none"
            : string.Join(",", _mutedChannels.OrderBy(channel => channel));
        return $"activeChannels={active}; mutedChannels={muted}; arrangementMode={ArrangementMode}; maxSimultaneousMelodyNotes={MaxSimultaneousMelodyNotes}";
    }

    public static MidiSongProfile ForInstrument(string instrumentId)
    {
        return Default;
    }

    public static MidiSongProfile LoadFor(MidiSong song)
    {
        return LoadFor(song, InstrumentCatalog.LyreId);
    }

    public static MidiSongProfile LoadFor(MidiSong song, string instrumentId)
    {
        if (song.SourcePath is null)
        {
            return ForSong(instrumentId, song);
        }

        var profilePath = song.SourcePath + $".{instrumentId}-profile.json";
        if (!File.Exists(profilePath))
        {
            return ForSong(instrumentId, song);
        }

        var json = File.ReadAllText(profilePath);
        if (!ProfileMatchesInstrument(json, instrumentId))
        {
            return ForSong(instrumentId, song);
        }

        var fallback = ForInstrument(instrumentId);
        return new MidiSongProfile(
            ParseChannelSet(json, "activeChannels", fallback._activeChannels),
            ParseChannelSet(json, "mutedChannels", fallback._mutedChannels),
            ParseOctaveOffsets(json),
            ParseArrangementMode(json, fallback.ArrangementMode),
            ParseInt(json, "maxSimultaneousMelodyNotes", fallback.MaxSimultaneousMelodyNotes));
    }

    private static MidiSongProfile ForSong(string instrumentId, MidiSong song)
    {
        return ForInstrument(instrumentId);
    }

    public bool Allows(MidiNoteEvent note)
    {
        var oneBasedChannel = note.Channel + 1;
        if (_mutedChannels.Contains(oneBasedChannel))
        {
            return false;
        }

        return _activeChannels.Count == 0 || _activeChannels.Contains(oneBasedChannel);
    }

    public int ApplyOctaveOffset(MidiNoteEvent note)
    {
        var oneBasedChannel = note.Channel + 1;
        return note.MidiNote + (_channelOctaveOffsets.TryGetValue(oneBasedChannel, out var offset) ? offset * 12 : 0);
    }

    private static HashSet<int> ParseChannelSet(string json, string propertyName, HashSet<int> fallback)
    {
        var result = new HashSet<int>();
        var match = Regex.Match(json, $"\"{propertyName}\"\\s*:\\s*\\[(?<values>[^\\]]*)\\]");
        if (!match.Success)
        {
            return new HashSet<int>(fallback);
        }

        foreach (Match value in Regex.Matches(match.Groups["values"].Value, "\\d+"))
        {
            result.Add(int.Parse(value.Value));
        }

        return result;
    }

    private static Dictionary<int, int> ParseOctaveOffsets(string json)
    {
        var result = new Dictionary<int, int>();
        var match = Regex.Match(json, "\"channelOctaveOffsets\"\\s*:\\s*\\{(?<values>[^}]*)\\}");
        if (!match.Success)
        {
            return result;
        }

        foreach (Match pair in Regex.Matches(match.Groups["values"].Value, "\"(?<channel>\\d+)\"\\s*:\\s*(?<offset>-?\\d+)"))
        {
            result[int.Parse(pair.Groups["channel"].Value)] = int.Parse(pair.Groups["offset"].Value);
        }

        return result;
    }

    private static MidiArrangementMode ParseArrangementMode(string json, MidiArrangementMode fallback)
    {
        var match = Regex.Match(json, "\"arrangementMode\"\\s*:\\s*\"(?<value>[^\"]+)\"");
        if (!match.Success)
        {
            return fallback;
        }

        return match.Groups["value"].Value == "Full" ? MidiArrangementMode.Full : MidiArrangementMode.Melody;
    }

    private static int ParseInt(string json, string propertyName, int fallback)
    {
        var match = Regex.Match(json, $"\"{propertyName}\"\\s*:\\s*(?<value>\\d+)");
        return match.Success ? int.Parse(match.Groups["value"].Value) : fallback;
    }

    private static bool ProfileMatchesInstrument(string json, string instrumentId)
    {
        var match = Regex.Match(json, "\"instrumentId\"\\s*:\\s*\"(?<value>[^\"]+)\"");
        return !match.Success || match.Groups["value"].Value == instrumentId;
    }

}
