using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bardheim.Songs;

public sealed class MidiSongLibrary
{
    private readonly string _songsDirectory;
    private readonly List<MidiSong> _songs = new();
    private readonly List<string> _skippedFiles = new();

    public MidiSongLibrary(string songsDirectory)
    {
        _songsDirectory = songsDirectory;
    }

    public IReadOnlyList<MidiSong> Songs => _songs;

    public IReadOnlyList<string> SkippedFiles => _skippedFiles;

    public int SelectedIndex { get; private set; }

    public MidiSong? SelectedSong => _songs.Count == 0 ? null : _songs[SelectedIndex];

    public void Refresh()
    {
        Directory.CreateDirectory(_songsDirectory);
        var selectedName = SelectedSong?.DisplayName;
        _songs.Clear();
        _skippedFiles.Clear();

        foreach (var path in Directory.GetFiles(_songsDirectory, "*.mid").OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                _songs.Add(MidiFileParser.Parse(Path.GetFileName(path), File.ReadAllBytes(path), path));
            }
            catch
            {
                _skippedFiles.Add(path);
                // Runtime integration logs skipped files; this pure class keeps refresh resilient.
            }
        }

        if (_songs.Count == 0)
        {
            SelectedIndex = 0;
            return;
        }

        var restoredIndex = selectedName is null
            ? -1
            : _songs.FindIndex(song => string.Equals(song.DisplayName, selectedName, StringComparison.OrdinalIgnoreCase));
        SelectedIndex = restoredIndex >= 0 ? restoredIndex : Math.Min(SelectedIndex, _songs.Count - 1);
    }

    public MidiSong? SelectNext()
    {
        if (_songs.Count == 0)
        {
            return null;
        }

        SelectedIndex = (SelectedIndex + 1) % _songs.Count;
        return SelectedSong;
    }

    public MidiSong? SelectPrevious()
    {
        if (_songs.Count == 0)
        {
            return null;
        }

        SelectedIndex = (SelectedIndex + _songs.Count - 1) % _songs.Count;
        return SelectedSong;
    }
}
