using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace Bardheim.Tests;

public sealed class VanillaItemRegistrationSourceTests
{
    [Fact]
    public void ItemRegistrationClonesHammerAsVanillaVisualFallback()
    {
        var source = ReadSource("LyreItemRegistration.cs");

        Assert.Contains("BasePrefabName = \"Hammer\"", source);
        Assert.Contains("new CustomItem(PrefabName, BasePrefabName, config)", source);
        Assert.Contains("cloned from {BasePrefabName}", source);
    }

    [Fact]
    public void ItemRegistrationDoesNotUseCustomVisualAssetPipeline()
    {
        var source = ReadSource("LyreItemRegistration.cs");
        var plugin = File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", "Plugin.cs"));
        var itemsDir = Path.Combine(GetModRoot(), "src", "Bardheim", "Items");

        Assert.DoesNotContain("new CustomItem(PrefabName, true", source);
        Assert.DoesNotContain("LyreVisualCustomizer", source);
        Assert.DoesNotContain("ConfigureStandaloneItemDrop", source);
        Assert.DoesNotContain("EnsureDropPhysics", source);
        Assert.DoesNotContain("LyreVisualPoseController", plugin);

        foreach (var itemSourcePath in Directory.GetFiles(itemsDir, "*.cs"))
        {
            var itemSource = File.ReadAllText(itemSourcePath);
            Assert.DoesNotContain("FindObjects", itemSource);
            Assert.DoesNotContain("AssetBundle", itemSource);
        }
    }

    [Fact]
    public void ProjectDoesNotReferenceVisualAssetBundleOrPhysicsModules()
    {
        var project = File.ReadAllText(Path.Combine(
            GetModRoot(),
            "src",
            "Bardheim",
            "Bardheim.csproj"));

        Assert.DoesNotContain("UnityEngine.AssetBundleModule", project);
        Assert.DoesNotContain("UnityEngine.PhysicsModule", project);
        Assert.DoesNotContain("lyre-icon.png", project);
    }

    [Fact]
    public void VisualProbeBuildUsesVersionZeroSeven()
    {
        var plugin = File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", "Plugin.cs"));
        var project = File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", "Bardheim.csproj"));

        Assert.Contains("PluginVersion = \"0.7.0\"", plugin);
        Assert.Contains("<Version>0.7.0</Version>", project);
    }

    [Fact]
    public void BardheimRenameUsesBardheimRuntimeIds()
    {
        var plugin = File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", "Plugin.cs"));
        var lyreRegistration = ReadSource("LyreItemRegistration.cs");

        Assert.Contains("PluginName = \"Bardheim\"", plugin);
        Assert.Contains("PluginGuid = \"com.valheimmodlab.bardheim\"", plugin);
        Assert.Contains("PrefabName = \"Bardheim_Lyre\"", lyreRegistration);
        Assert.DoesNotContain("com.valheimmodlab.lyreinstruments", plugin);
        Assert.DoesNotContain("LyreInstruments_Lyre", lyreRegistration);
    }

    [Fact]
    public void ItemRegistrationAppliesHammerCarrierVisualProbeBeforeAddItem()
    {
        var source = ReadSource("LyreItemRegistration.cs");

        Assert.Contains("config.Icon = LyreInventoryIconFactory.Create()", source);
        Assert.Contains("ConfigureHeldOnlyLyre(lyre.ItemDrop)", source);
        Assert.Contains("LyreHammerVisualProbe.Apply(lyre.ItemPrefab, _logger)", source);
        Assert.True(
            source.IndexOf("ConfigureHeldOnlyLyre", StringComparison.Ordinal) <
            source.IndexOf("LyreHammerVisualProbe.Apply", StringComparison.Ordinal));
        Assert.True(
            source.IndexOf("LyreHammerVisualProbe.Apply", StringComparison.Ordinal) <
            source.IndexOf("ItemManager.Instance.AddItem(lyre)", StringComparison.Ordinal));
    }

    [Fact]
    public void ItemRegistrationNeutralizesHammerToolActions()
    {
        var source = ReadSource("LyreItemRegistration.cs");

        Assert.Contains("m_buildPieces = null", source);
        Assert.Contains("m_attack = null", source);
        Assert.Contains("m_secondaryAttack = null", source);
        Assert.Contains("m_toolTier = 0", source);
        Assert.Contains("m_useDurability = false", source);
        Assert.Contains("m_canBeReparied = false", source);
        Assert.Contains("m_itemType = ItemDrop.ItemData.ItemType.Tool", source);
        Assert.Contains("m_animationState = ItemDrop.ItemData.AnimationState.OneHanded", source);
    }

    [Fact]
    public void HammerVisualProbeLogsHierarchyAndUsesOneGeneratedLyreMesh()
    {
        var source = ReadSource("LyreHammerVisualProbe.cs");

        Assert.Contains("GetComponentsInChildren<Renderer>(true)", source);
        Assert.Contains("GetComponentsInChildren<Component>(true)", source);
        Assert.Contains("component is null", source);
        Assert.Contains("\"Collider\"", source);
        Assert.Contains("\"Rigidbody\"", source);
        Assert.Contains("BuildLyreMesh()", source);
        Assert.Contains("VisualScale = 2.4f", source);
        Assert.Contains("GripOffset = new Vector3(0.50f, 0.0f, 0.0f)", source);
        Assert.Contains("ApplyGripOffset", source);
        Assert.Contains("FrameSegmentCount = 14", source);
        Assert.Contains("StringCount = 6", source);
        Assert.Contains("StringThickness = new Vector3(0.012f, 0.012f, 0.009f)", source);
        Assert.Contains("StringBottomY = -0.29f", source);
        Assert.Contains("StringTopY = 0.34f", source);
        Assert.Contains("AddRoundedFrame", source);
        Assert.Contains("AddRoundedCornerCaps", source);
        Assert.Contains("AddSoundboardPanel", source);
        Assert.Contains("AddBridge", source);
        Assert.Contains("AddTuningPeg", source);
        Assert.Contains("AddDecorativeWoodwork", source);
        Assert.Contains("AddCarvedBands", source);
        Assert.Contains("AddVikingKnotwork", source);
        Assert.Contains("AddOrientedBox", source);
        Assert.Contains("new Material[2]", source);
        Assert.Contains("sharedMesh = lyreMesh", source);
        Assert.Contains("EnsurePlayModePose", source);
        Assert.Contains("Bardheim_HammerCarrierLyreVisual", source);
        Assert.DoesNotContain("Update()", source);
        Assert.DoesNotContain("FindObjects", source);
        Assert.DoesNotContain("AssetBundle", source);
    }

    [Fact]
    public void VisualPoseCalibrationTunesCarryPoseWithoutPlayModeDeltaOrSceneSearch()
    {
        var plugin = File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", "Plugin.cs"));
        var config = File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", "Config", "LyreConfig.cs"));
        var source = File.ReadAllText(Path.Combine(
            GetModRoot(),
            "src",
            "Bardheim",
            "Visuals",
            "LyrePlayModeVisualPose.cs"));

        Assert.Contains("LyrePlayModeVisualPose.UpdateCalibration(_config.EnablePlayPoseCalibration.Value, Logger)", plugin);
        Assert.DoesNotContain("SetPlayModePose", plugin);
        Assert.Contains("EnablePlayPoseCalibration", config);
        Assert.Contains("CarryLocalPosition = new Vector3(0.05f, 0.00f, -0.05f)", source);
        Assert.Contains("CarryLocalEuler = new Vector3(10.0f, -15.0f, 10.0f)", source);
        Assert.Contains("ApplyCarryPose", source);
        Assert.Contains("UpdateCalibration", source);
        Assert.Contains("LogCalibrationConstants", source);
        Assert.Contains("Lyre carry pose calibration constants", source);
        Assert.Contains("KeyCode.Keypad", source);
        Assert.Contains("KeyCode.LeftBracket", source);
        Assert.Contains("KeyCode.RightBracket", source);
        Assert.Contains("HashSet<LyrePlayModeVisualPose>", source);
        Assert.DoesNotContain("PlayModeLocalPosition", source);
        Assert.DoesNotContain("PlayModeLocalEuler", source);
        Assert.DoesNotContain("Update()", source);
        Assert.DoesNotContain("FindObjects", source);
        Assert.DoesNotContain("FindGameObjects", source);
    }

    [Fact]
    public void InventoryIconIsGeneratedInCodeWithoutRuntimeAssetPath()
    {
        var source = ReadSource("LyreInventoryIconFactory.cs");
        var project = File.ReadAllText(Path.Combine(GetModRoot(), "src", "Bardheim", "Bardheim.csproj"));

        Assert.Contains("Texture2D", source);
        Assert.Contains("Sprite.Create", source);
        Assert.Contains("DrawLyre", source);
        Assert.Contains("DrawLine", source);
        Assert.Contains("IconStringCount = 6", source);
        Assert.Contains("DrawDecorativeWoodwork", source);
        Assert.Contains("DrawVikingKnotwork", source);
        Assert.Contains("Bardheim_GeneratedInventoryIcon", source);
        Assert.DoesNotContain("File.", source);
        Assert.DoesNotContain("LoadImage", source);
        Assert.DoesNotContain("AssetBundle", source);
        Assert.DoesNotContain("lyre-icon.png", project);
    }

    private static string ReadSource(string filename)
    {
        return File.ReadAllText(Path.Combine(
            GetModRoot(),
            "src",
            "Bardheim",
            "Items",
            filename));
    }

    private static string GetModRoot([CallerFilePath] string sourceFile = "")
    {
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, "..", ".."));
    }
}
