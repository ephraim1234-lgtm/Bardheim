namespace Bardheim.Songs;

public static class DrumMidiMapper
{
    private const int PercussionChannel = 9;

    public static bool TryMap(MidiNoteEvent note, out string? hitId)
    {
        hitId = null;

        if (note.Channel != PercussionChannel)
        {
            return false;
        }

        hitId = note.MidiNote switch
        {
            27 or 28 or 29 or 30 or 31 or 32 or 33 or 34 => "rim",
            35 => "low_kick",
            36 => "kick",
            38 => "snare",
            40 => "electric_snare",
            37 => "rim",
            39 => "clap",
            42 => "muted",
            44 => "pedal_hat",
            46 => "pedal_hat",
            49 or 52 or 55 or 57 => "crash",
            51 or 53 or 59 or 73 => "ride",
            41 or 43 => "low_tom",
            45 or 47 => "mid_tom",
            48 or 50 => "high_tom",
            54 or 70 or 72 or 74 or 83 => "tambourine",
            56 or 58 or 84 => "cowbell",
            60 or 65 or 67 or 81 => "high_tom",
            61 or 64 or 68 or 77 or 80 => "low_tom",
            62 or 63 or 78 or 79 or 82 => "muted",
            66 => "mid_tom",
            69 => "clap",
            71 => "pedal_hat",
            75 or 76 => "rim",
            85 or 86 or 87 => "open",
            _ => null
        };

        return hitId != null;
    }

    public static bool HasMappedPercussion(System.Collections.Generic.IEnumerable<MidiNoteEvent> events)
    {
        foreach (var note in events)
        {
            if (TryMap(note, out _))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryMapMelodicFallback(MidiNoteEvent note, out string hitId)
    {
        hitId = note.MidiNote switch
        {
            < 30 => "low_kick",
            < 40 => "kick",
            < 47 => "low_tom",
            < 54 => "mid_tom",
            < 61 => "snare",
            < 69 => "high_tom",
            < 78 => "rim",
            _ => "tambourine"
        };

        return true;
    }
}
