using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace Bardheim.Tests;

public sealed class DrumItemRegistrationSourceTests
{
    [Fact]
    public void DrumItemRegistrationClonesHammerAsOneHandedCarrier()
    {
        var source = ReadItemSource("DrumItemRegistration.cs");

        Assert.Contains("PrefabName = \"Bardheim_Drum\"", source);
        Assert.Contains("BasePrefabName = \"Hammer\"", source);
        Assert.Contains("new CustomItem(PrefabName, BasePrefabName, config)", source);
        Assert.Contains("ConfigureHeldOnlyDrum(drum.ItemDrop)", source);
        Assert.Contains("DrumHandDrumVisualProbe.Apply(drum.ItemPrefab", source);
        Assert.Contains("m_itemType = ItemDrop.ItemData.ItemType.Tool", source);
        Assert.Contains("m_animationState = ItemDrop.ItemData.AnimationState.OneHanded", source);
        Assert.Contains("m_attack = null", source);
        Assert.Contains("m_secondaryAttack = null", source);
        Assert.Contains("ItemManager.Instance.AddItem(drum)", source);
    }

    [Fact]
    public void DrumItemRegistrationUsesSeparateRecipeAndNoAssetBundle()
    {
        var source = ReadItemSource("DrumItemRegistration.cs");

        Assert.Contains("Name = \"Drum\"", source);
        Assert.Contains("CraftingStation = CraftingStations.Workbench", source);
        Assert.Contains("AddRequirement(\"Wood\"", source);
        Assert.Contains("AddRequirement(\"LeatherScraps\"", source);
        Assert.Contains("AddRequirement(\"DeerHide\"", source);
        Assert.DoesNotContain("AssetBundle", source);
        Assert.DoesNotContain("FindObjects", source);
    }

    [Fact]
    public void DrumItemRegistrationUsesGeneratedVisualAndIconWithoutAssetBundle()
    {
        var registration = ReadItemSource("DrumItemRegistration.cs");
        var itemsDir = Path.Combine(GetModRoot(), "src", "Bardheim", "Items");

        Assert.Contains("Icon = DrumInventoryIconFactory.Create()", registration);
        Assert.Contains("DrumHandDrumVisualProbe.Apply", registration);
        Assert.True(File.Exists(Path.Combine(itemsDir, "DrumInventoryIconFactory.cs")));
        Assert.True(File.Exists(Path.Combine(itemsDir, "DrumStagbreakerVisualProbe.cs")));
        var visualSource = File.ReadAllText(Path.Combine(itemsDir, "DrumStagbreakerVisualProbe.cs"));
        Assert.Contains("OneHandedHangingDrum", visualSource);
        Assert.Contains("AddTopGrip", visualSource);
        Assert.Contains("AddCylinderZ", visualSource);
        Assert.Contains("radiusX: 0.56f", visualSource);
        Assert.Contains("depth: 0.42f", visualSource);
        Assert.Contains("materialSide: 0", visualSource);
        Assert.Contains("AddOutsideShellBand", visualSource);
        Assert.Contains("AddCylinderSideZ", visualSource);
        Assert.Contains("AddQuad(back[index], back[next], front[next], front[index]", visualSource);
        Assert.Contains("AddSolidSideShellPanels", visualSource);
        Assert.Contains("AddRimBinding", visualSource);
        Assert.Contains("AddLacing", visualSource);
        Assert.Contains("AddSideCrossBraces", visualSource);
        Assert.Contains("AddLaceKnots", visualSource);
        Assert.Contains("AddExtraEdgeTabs", visualSource);
        Assert.Contains("AddBroadHidePanelVariation", visualSource);
        Assert.Contains("AddIrregularHidePatches", visualSource);
        Assert.Contains("AddSideHandles", visualSource);
        Assert.Contains("AddRaisedKnotworkPaint", visualSource);
        Assert.DoesNotContain("AssetBundle", visualSource);
        Assert.DoesNotContain("Mallet", visualSource);
        Assert.DoesNotContain("Beater", visualSource);

        var iconSource = File.ReadAllText(Path.Combine(itemsDir, "DrumInventoryIconFactory.cs"));
        Assert.Contains("DrawSideHandles", iconSource);
        Assert.Contains("DrawEdgeTabs", iconSource);
        Assert.Contains("DrawShellBraces", iconSource);
        Assert.Contains("DrawWeatheredHide", iconSource);
    }

    [Fact]
    public void DrumVisualPosePersistsCalibrationToConfigFile()
    {
        var visualSource = File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", "Visuals", "DrumPlayModeVisualPose.cs"));
        var plugin = File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", "Plugin.cs"));

        Assert.Contains("drum-hand-visual-pose.txt", visualSource);
        Assert.Contains("new Vector3(0.05f, 0.10f, 0.30f)", visualSource);
        Assert.Contains("new Vector3(-15.0f, -55.0f, 85.0f)", visualSource);
        Assert.Contains("File.WriteAllLines", visualSource);
        Assert.Contains("File.ReadAllLines", visualSource);
        Assert.Contains("PoseDirectoryName = \"Bardheim\"", visualSource);
        Assert.DoesNotContain("LegacyPoseDirectoryName", visualSource);
        Assert.DoesNotContain("PoseReadPath", visualSource);
        Assert.Contains("Input.GetKey(KeyCode.LeftShift)", visualSource);
        Assert.Contains("DrumPlayModeVisualPose.UpdateCalibration", plugin);
        Assert.Contains("activeInstrumentId != InstrumentCatalog.DrumsId", visualSource);
        Assert.Contains("KeyCode.LeftBracket", visualSource);
        Assert.Contains("KeyCode.RightBracket", visualSource);
        Assert.Contains("KeyCode.Semicolon", visualSource);
        Assert.Contains("KeyCode.Quote", visualSource);
        Assert.Contains("KeyCode.Comma", visualSource);
        Assert.Contains("KeyCode.Period", visualSource);
        Assert.Contains("DrumPlayModeVisualPose.UpdateCalibration(_config.EnablePlayPoseCalibration.Value, activeInstrumentId, Logger)", plugin);
    }

    [Fact]
    public void PluginRegistersLyreAndDrumItems()
    {
        var plugin = File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", "Plugin.cs"));

        Assert.Contains("LyreItemRegistration", plugin);
        Assert.Contains("DrumItemRegistration", plugin);
        Assert.Contains("_lyreItemRegistration.Register()", plugin);
        Assert.Contains("_drumItemRegistration.Register()", plugin);
    }

    private static string ReadItemSource(string filename)
    {
        return File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", "Items", filename));
    }

    private static string GetModRoot([CallerFilePath] string sourceFile = "")
    {
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, "..", ".."));
    }
}
