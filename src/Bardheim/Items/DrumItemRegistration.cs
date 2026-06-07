using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;

namespace Bardheim.Items;

public sealed class DrumItemRegistration
{
    public const string PrefabName = "Bardheim_Drum";
    public const string BasePrefabName = "Hammer";

    private readonly ManualLogSource _logger;
    private bool _registered;

    public DrumItemRegistration(ManualLogSource logger)
    {
        _logger = logger;
    }

    public void Register()
    {
        PrefabManager.OnVanillaPrefabsAvailable += RegisterWhenPrefabsAvailable;
    }

    private void RegisterWhenPrefabsAvailable()
    {
        if (_registered)
        {
            return;
        }

        var config = CreateItemConfig();
        var drum = new CustomItem(PrefabName, BasePrefabName, config);
        ConfigureHeldOnlyDrum(drum.ItemDrop);
        DrumHandDrumVisualProbe.Apply(drum.ItemPrefab, _logger);
        ItemManager.Instance.AddItem(drum);

        _registered = true;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterWhenPrefabsAvailable;
        _logger.LogInfo($"Registered drum item {PrefabName} cloned from {BasePrefabName}; generated one-handed frame-drum visual/icon probe applied and custom asset bundle pipeline disabled.");
    }

    private static ItemConfig CreateItemConfig()
    {
        var config = new ItemConfig
        {
            Name = "Drum",
            Description = "A simple hand drum for playing rhythm hits.",
            CraftingStation = CraftingStations.Workbench,
            Amount = 1,
            Icon = DrumInventoryIconFactory.Create()
        };

        config.AddRequirement("Wood", 8, 1);
        config.AddRequirement("LeatherScraps", 4, 1);
        config.AddRequirement("DeerHide", 2, 1);
        return config;
    }

    private void ConfigureHeldOnlyDrum(ItemDrop itemDrop)
    {
        var shared = itemDrop.m_itemData.m_shared;

        shared.m_itemType = ItemDrop.ItemData.ItemType.Tool;
        shared.m_attachOverride = ItemDrop.ItemData.ItemType.None;
        shared.m_animationState = ItemDrop.ItemData.AnimationState.OneHanded;
        shared.m_buildPieces = null;
        shared.m_attack = null;
        shared.m_secondaryAttack = null;
        shared.m_toolTier = 0;
        shared.m_skillType = Skills.SkillType.None;
        shared.m_useDurability = false;
        shared.m_canBeReparied = false;
        shared.m_maxDurability = 0.0f;
        shared.m_durabilityDrain = 0.0f;
        shared.m_useDurabilityDrain = 0.0f;

        _logger.LogInfo("Configured drum as held-only one-handed Hammer carrier: build pieces, attacks, tool tier, and repair/durability actions disabled.");
    }
}
