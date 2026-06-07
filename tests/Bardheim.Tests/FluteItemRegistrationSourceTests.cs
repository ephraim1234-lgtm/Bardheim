using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace Bardheim.Tests;

public sealed class FluteItemRegistrationSourceTests
{
    [Fact]
    public void FluteItemRegistrationClonesHammerAsOneHandedCarrier()
    {
        var source = ReadItemSource("FluteItemRegistration.cs");

        Assert.Contains("PrefabName = \"Bardheim_Flute\"", source);
        Assert.Contains("BasePrefabName = \"Hammer\"", source);
        Assert.Contains("new CustomItem(PrefabName, BasePrefabName, config)", source);
        Assert.Contains("ConfigureHeldOnlyFlute(flute.ItemDrop)", source);
        Assert.Contains("FluteHandVisualProbe.Apply(flute.ItemPrefab", source);
        Assert.Contains("m_itemType = ItemDrop.ItemData.ItemType.Tool", source);
        Assert.Contains("m_animationState = ItemDrop.ItemData.AnimationState.OneHanded", source);
        Assert.Contains("m_attack = null", source);
        Assert.Contains("m_secondaryAttack = null", source);
        Assert.Contains("ItemManager.Instance.AddItem(flute)", source);
    }

    [Fact]
    public void FluteItemRegistrationUsesSeparateRecipeAndNoAssetBundle()
    {
        var source = ReadItemSource("FluteItemRegistration.cs");

        Assert.Contains("Name = \"Flute\"", source);
        Assert.Contains("CraftingStation = CraftingStations.Workbench", source);
        Assert.Contains("AddRequirement(\"Wood\"", source);
        Assert.Contains("AddRequirement(\"DeerHide\"", source);
        Assert.DoesNotContain("AssetBundle", source);
        Assert.DoesNotContain("FindObjects", source);
    }

    [Fact]
    public void FluteItemRegistrationUsesGeneratedVisualAndIconWithoutAssetBundle()
    {
        var registration = ReadItemSource("FluteItemRegistration.cs");
        var itemsDir = Path.Combine(GetModRoot(), "src", "Bardheim", "Items");

        Assert.Contains("Icon = FluteInventoryIconFactory.Create()", registration);
        Assert.Contains("FluteHandVisualProbe.Apply", registration);
        Assert.True(File.Exists(Path.Combine(itemsDir, "FluteInventoryIconFactory.cs")));
        Assert.True(File.Exists(Path.Combine(itemsDir, "FluteHandVisualProbe.cs")));

        var visualSource = File.ReadAllText(Path.Combine(itemsDir, "FluteHandVisualProbe.cs"));
        Assert.Contains("Bardheim_OneHandedFluteVisual", visualSource);
        Assert.Contains("GeneratedOneHandedFlute", visualSource);
        Assert.Contains("AddFingerHoles", visualSource);
        Assert.Contains("AddMouthNotch", visualSource);
        Assert.Contains("AddCarvedBands", visualSource);
        Assert.Contains("AddTaperedBody", visualSource);
        Assert.Contains("AddRaisedMouthpiece", visualSource);
        Assert.Contains("AddInsetToneHoleRims", visualSource);
        Assert.Contains("AddMetalInlayBands", visualSource);
        Assert.Contains("AddEndCaps", visualSource);
        Assert.Contains("FlutePlayModeVisualPose", visualSource);
        Assert.DoesNotContain("AssetBundle", visualSource);
        Assert.DoesNotContain("FindObjects", visualSource);

        var iconSource = File.ReadAllText(Path.Combine(itemsDir, "FluteInventoryIconFactory.cs"));
        Assert.Contains("Texture2D", iconSource);
        Assert.Contains("Sprite.Create", iconSource);
        Assert.Contains("DrawFingerHoles", iconSource);
        Assert.Contains("DrawCarvedBands", iconSource);
        Assert.Contains("DrawTaperedBody", iconSource);
        Assert.Contains("DrawRaisedMouthpiece", iconSource);
        Assert.Contains("DrawCleanSilhouette", iconSource);
        Assert.Contains("DrawReadableToneHoles", iconSource);
        Assert.Contains("DrawSimpleEndCaps", iconSource);
        Assert.Contains("DrawSubtleInlays", iconSource);
        Assert.DoesNotContain("DrawWoodGrain", iconSource);
        Assert.DoesNotContain("File.", iconSource);
        Assert.DoesNotContain("AssetBundle", iconSource);
    }

    [Fact]
    public void FluteVisualPosePersistsCalibrationToConfigFile()
    {
        var visualSource = File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", "Visuals", "FlutePlayModeVisualPose.cs"));
        var plugin = File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", "Plugin.cs"));

        Assert.Contains("flute-hand-visual-pose.txt", visualSource);
        Assert.Contains("new Vector3(0.03f, 0.03f, 0.08f)", visualSource);
        Assert.Contains("new Vector3(0.0f, -70.0f, 90.0f)", visualSource);
        Assert.Contains("InstrumentCatalog.FluteId", visualSource);
        Assert.Contains("activeInstrumentId != InstrumentCatalog.FluteId", visualSource);
        Assert.Contains("File.WriteAllLines", visualSource);
        Assert.Contains("File.ReadAllLines", visualSource);
        Assert.Contains("PoseDirectoryName = \"Bardheim\"", visualSource);
        Assert.DoesNotContain("LegacyPoseDirectoryName", visualSource);
        Assert.DoesNotContain("PoseReadPath", visualSource);
        Assert.Contains("Input.GetKey(KeyCode.LeftShift)", visualSource);
        Assert.Contains("Flute visual pose saved", visualSource);
        Assert.Contains("FlutePlayModeVisualPose.UpdateCalibration(_config.EnablePlayPoseCalibration.Value, activeInstrumentId, Logger)", plugin);
    }

    [Fact]
    public void PluginRegistersLyreDrumAndFluteItems()
    {
        var plugin = File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", "Plugin.cs"));

        Assert.Contains("LyreItemRegistration", plugin);
        Assert.Contains("DrumItemRegistration", plugin);
        Assert.Contains("FluteItemRegistration", plugin);
        Assert.Contains("_lyreItemRegistration.Register()", plugin);
        Assert.Contains("_drumItemRegistration.Register()", plugin);
        Assert.Contains("_fluteItemRegistration.Register()", plugin);
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
