using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;

namespace Bardheim.Items;

public sealed class LyreItemRegistration
{
    public const string PrefabName = "Bardheim_Lyre";
    public const string BasePrefabName = "Hammer";

    private readonly ManualLogSource _logger;
    private bool _registered;

    public LyreItemRegistration(ManualLogSource logger)
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
        var lyre = new CustomItem(PrefabName, BasePrefabName, config);
        ConfigureHeldOnlyLyre(lyre.ItemDrop);
        LyreHammerVisualProbe.Apply(lyre.ItemPrefab, _logger);
        ItemManager.Instance.AddItem(lyre);

        _registered = true;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterWhenPrefabsAvailable;
        _logger.LogInfo($"Registered lyre item {PrefabName} cloned from {BasePrefabName}; custom visual asset pipeline disabled.");
    }

    private ItemConfig CreateItemConfig()
    {
        var config = new ItemConfig
        {
            Name = "Lyre",
            Description = "A simple lyre for playing local placeholder notes.",
            CraftingStation = CraftingStations.Workbench,
            Amount = 1
        };
        config.Icon = LyreInventoryIconFactory.Create();

        config.AddRequirement("Wood", 10, 1);
        config.AddRequirement("LeatherScraps", 2, 1);
        return config;
    }

    private void ConfigureHeldOnlyLyre(ItemDrop itemDrop)
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

        _logger.LogInfo("Configured lyre as held-only Hammer carrier: build pieces, attacks, tool tier, and repair/durability actions disabled.");
    }
}
