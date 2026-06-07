using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Linq;
using Bardheim.Instruments;
using Xunit;

namespace Bardheim.Tests;

public sealed class ManualRoutingSourceTests
{
    [Fact]
    public void PluginDetectsActiveInstrumentByCustomPrefabName()
    {
        var plugin = ReadSource("Plugin.cs");

        Assert.Contains("GetEquippedInstrumentId", plugin);
        Assert.Contains("LyreItemRegistration.PrefabName", plugin);
        Assert.Contains("DrumItemRegistration.PrefabName", plugin);
        Assert.Contains("activeInstrumentId is not null", plugin);
    }

    [Fact]
    public void InputRouterRoutesManualSlotsByActiveInstrument()
    {
        var router = ReadSource(Path.Combine("PlayMode", "LyreInputRouter.cs"));

        Assert.Contains("string? activeInstrumentId", router);
        Assert.Contains("InstrumentCatalog.LyreId", router);
        Assert.Contains("InstrumentCatalog.DrumsId", router);
        Assert.Contains("InstrumentCatalog.FluteId", router);
        Assert.Contains("_drumPlayback.Play", router);
        Assert.Contains("PlayFluteSlot", router);
        Assert.Contains("TryCycleNoteSet()", router);
        Assert.Contains("CycleButtonName = \"Bardheim_CycleNoteSet\"", router);
        Assert.Contains("return $\"Bardheim_Note{slot}\"", router);
        Assert.DoesNotContain("LyreInstruments_CycleNoteSet", router);
        Assert.DoesNotContain("LyreInstruments_Note", router);
        Assert.True(
            router.IndexOf("InstrumentCatalog.LyreId", StringComparison.Ordinal) <
            router.IndexOf("TryCycleNoteSet()", StringComparison.Ordinal));
    }

    [Fact]
    public void DrumManualSlotsUseInstrumentCatalogHitIds()
    {
        var router = ReadSource(Path.Combine("PlayMode", "LyreInputRouter.cs"));

        foreach (var hit in InstrumentCatalog.CreateDefaultInstruments()[1].DrumHits.Where(hit => hit.Slot <= 8))
        {
            Assert.Contains(hit.Id, router);
        }
    }

    [Fact]
    public void PluginDetectsFluteItemAsActiveInstrument()
    {
        var plugin = ReadSource("Plugin.cs");

        Assert.Contains("FluteItemRegistration.PrefabName", plugin);
        Assert.Contains("InstrumentCatalog.FluteId", plugin);
    }

    private static string ReadSource(string relativePath)
    {
        return File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", relativePath));
    }

    private static string GetModRoot([CallerFilePath] string sourceFile = "")
    {
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, "..", ".."));
    }
}
