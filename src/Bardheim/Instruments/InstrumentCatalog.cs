using Bardheim.Notes;
using System.Collections.Generic;

namespace Bardheim.Instruments;

public static class InstrumentCatalog
{
    public const string LyreId = "lyre";
    public const string DrumsId = "drums";
    public const string FluteId = "flute";

    public static IReadOnlyList<InstrumentDefinition> CreateDefaultInstruments()
    {
        return new[]
        {
            new InstrumentDefinition(LyreId, NoteSetCatalog.CreateDefaultSets(), new DrumHitDefinition[0]),
            new InstrumentDefinition(
                DrumsId,
                new NoteSet[0],
                new[]
                {
                    new DrumHitDefinition(1, "kick"),
                    new DrumHitDefinition(2, "snare"),
                    new DrumHitDefinition(3, "rim"),
                    new DrumHitDefinition(4, "clap"),
                    new DrumHitDefinition(5, "muted"),
                    new DrumHitDefinition(6, "open"),
                    new DrumHitDefinition(7, "low_tom"),
                    new DrumHitDefinition(8, "high_tom"),
                    new DrumHitDefinition(9, "low_kick"),
                    new DrumHitDefinition(10, "electric_snare"),
                    new DrumHitDefinition(11, "pedal_hat"),
                    new DrumHitDefinition(12, "crash"),
                    new DrumHitDefinition(13, "ride"),
                    new DrumHitDefinition(14, "mid_tom"),
                    new DrumHitDefinition(15, "cowbell"),
                    new DrumHitDefinition(16, "tambourine"),
                }),
            new InstrumentDefinition(
                FluteId,
                new[]
                {
                    NoteSetCatalog.Create(
                        "D natural minor flute",
                        ("D4", 293.66f),
                        ("E4", 329.63f),
                        ("F4", 349.23f),
                        ("G4", 392.00f),
                        ("A4", 440.00f),
                        ("Bb4", 466.16f),
                        ("C5", 523.25f),
                        ("D5", 587.33f))
                },
                new DrumHitDefinition[0]),
        };
    }
}
