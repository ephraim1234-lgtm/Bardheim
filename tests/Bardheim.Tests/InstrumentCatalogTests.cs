using Bardheim.Instruments;
using Bardheim.Notes;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Bardheim.Tests;

public sealed class InstrumentCatalogTests
{
    [Fact]
    public void DefaultInstrumentIdsAreStableAndOrdered()
    {
        var instruments = InstrumentCatalog.CreateDefaultInstruments();

        Assert.Equal(new[] { "lyre", "drums", "flute" }, instruments.Select(instrument => instrument.Id));
    }

    [Fact]
    public void LyreInstrumentWrapsDefaultTuningsUnchanged()
    {
        var lyre = InstrumentCatalog.CreateDefaultInstruments().Single(instrument => instrument.Id == "lyre");
        var defaultSets = NoteSetCatalog.CreateDefaultSets();

        Assert.Empty(lyre.DrumHits);
        Assert.Equal(defaultSets.Count, lyre.NoteSets.Count);

        for (var setIndex = 0; setIndex < defaultSets.Count; setIndex++)
        {
            Assert.Equal(defaultSets[setIndex].Name, lyre.NoteSets[setIndex].Name);
            Assert.Equal(GetNotes(defaultSets[setIndex]), GetNotes(lyre.NoteSets[setIndex]));
        }
    }

    [Fact]
    public void DrumsInstrumentExposesNonMelodicHitIdsInManualSlotOrder()
    {
        var drums = InstrumentCatalog.CreateDefaultInstruments().Single(instrument => instrument.Id == "drums");

        Assert.Empty(drums.NoteSets);
        Assert.Collection(
            drums.DrumHits,
            hit => AssertDrumHit(hit, 1, "kick"),
            hit => AssertDrumHit(hit, 2, "snare"),
            hit => AssertDrumHit(hit, 3, "rim"),
            hit => AssertDrumHit(hit, 4, "clap"),
            hit => AssertDrumHit(hit, 5, "muted"),
            hit => AssertDrumHit(hit, 6, "open"),
            hit => AssertDrumHit(hit, 7, "low_tom"),
            hit => AssertDrumHit(hit, 8, "high_tom"),
            hit => AssertDrumHit(hit, 9, "low_kick"),
            hit => AssertDrumHit(hit, 10, "electric_snare"),
            hit => AssertDrumHit(hit, 11, "pedal_hat"),
            hit => AssertDrumHit(hit, 12, "crash"),
            hit => AssertDrumHit(hit, 13, "ride"),
            hit => AssertDrumHit(hit, 14, "mid_tom"),
            hit => AssertDrumHit(hit, 15, "cowbell"),
            hit => AssertDrumHit(hit, 16, "tambourine"));
    }

    [Fact]
    public void FluteInstrumentExposesMelodicManualNotesAndNoDrumHits()
    {
        var flute = InstrumentCatalog.CreateDefaultInstruments().Single(instrument => instrument.Id == "flute");

        Assert.Empty(flute.DrumHits);
        var noteSet = Assert.Single(flute.NoteSets);
        Assert.Equal("D natural minor flute", noteSet.Name);
        Assert.Equal(
            new[]
            {
                ("D4", 293.66f),
                ("E4", 329.63f),
                ("F4", 349.23f),
                ("G4", 392.00f),
                ("A4", 440.00f),
                ("Bb4", 466.16f),
                ("C5", 523.25f),
                ("D5", 587.33f)
            },
            GetNotes(noteSet));
    }

    private static void AssertDrumHit(DrumHitDefinition hit, int slot, string id)
    {
        Assert.Equal(slot, hit.Slot);
        Assert.Equal(id, hit.Id);
    }

    private static IReadOnlyList<(string Name, float FrequencyHz)> GetNotes(NoteSet set)
    {
        return set.Map.Notes
            .Select(note => (note.Name, note.FrequencyHz))
            .ToArray();
    }
}
