using Bardheim.Notes;
using System.Collections.Generic;
using Xunit;

namespace Bardheim.Tests;

public sealed class NoteMapTests
{
    [Fact]
    public void DefaultNoteSetsContainThreeOrderedTunings()
    {
        var sets = NoteSetCatalog.CreateDefaultSets();

        Assert.Collection(
            sets,
            set =>
            {
                Assert.Equal("D minor pentatonic", set.Name);
                Assert.Equal(new[] { "D3", "F3", "G3", "A3", "C4", "D4", "F4", "G4" }, GetNoteNames(set));
            },
            set =>
            {
                Assert.Equal("D natural minor", set.Name);
                Assert.Equal(new[] { "D3", "E3", "F3", "G3", "A3", "Bb3", "C4", "D4" }, GetNoteNames(set));
            },
            set =>
            {
                Assert.Equal("D folk/dorian", set.Name);
                Assert.Equal(new[] { "D3", "E3", "F3", "G3", "A3", "C4", "D4", "E4" }, GetNoteNames(set));
            });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    public void TryGetReturnsFalseForSlotsOutsideMvpRange(int slot)
    {
        var map = NoteSetCatalog.CreateDefaultSets()[0].Map;

        Assert.False(map.TryGet(slot, out _));
    }

    [Fact]
    public void GetRequiredThrowsForMissingSlot()
    {
        var map = NoteSetCatalog.CreateDefaultSets()[0].Map;

        Assert.Throws<KeyNotFoundException>(() => map.GetRequired(9));
    }

    [Fact]
    public void NoteSetSelectorCyclesThroughDefaultTunings()
    {
        var selector = new NoteSetSelector(NoteSetCatalog.CreateDefaultSets());

        Assert.Equal("D minor pentatonic", selector.Current.Name);
        Assert.Equal("D natural minor", selector.CycleNext().Name);
        Assert.Equal("D folk/dorian", selector.CycleNext().Name);
        Assert.Equal("D minor pentatonic", selector.CycleNext().Name);
    }

    private static string[] GetNoteNames(NoteSet set)
    {
        var names = new List<string>();
        foreach (var note in set.Map.Notes)
        {
            names.Add(note.Name);
        }

        return names.ToArray();
    }
}
