using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using LethalLib.Modules;

namespace TestAccountCore.Loaders;

public static class ScrapLoader {
    internal static void RegisterAllScrap(List<ItemWithDefaultWeight> itemsWithDefaultWeight, ConfigFile? configFile) {
        if (configFile is null) return;

        itemsWithDefaultWeight.ForEach(item => RegisterScrap(item, configFile));
    }

    private static void RegisterScrap(ItemWithDefaultWeight item, ConfigFile? configFile) {
        if (configFile is null) return;

        if (item.item is null) throw new NullReferenceException("ItemProperties cannot be null!");

        var canItemSpawn = configFile.Bind($"{item.item.itemName}", "1. Enabled", true,
                                           $"If false, {item.item.itemName} will not be registered. This is different from a spawn weight of 0!");

        if (!canItemSpawn.Value) return;

        TestAccountCore.Logger.LogInfo($"Registering item {item.item.itemName}...");

        var maxValue = configFile.Bind($"{item.item.itemName}", "2. Maximum Value", item.item.maxValue,
                                       $"Defines the maximum scrap value for {item.item.itemName}.");

        var minValue = configFile.Bind($"{item.item.itemName}", "3. Minimum Value", item.item.minValue,
                                       $"Defines the minimum scrap value for {item.item.itemName}.");

        var configMoonRarity = configFile.Bind($"{item.item.itemName}", "4. Moon Spawn Weight", $"Vanilla:{item.defaultWeight}, Modded:{item.defaultWeight}",
                                               $"Defines the spawn weight per moon. e.g. Assurance:{item.defaultWeight}");

        item.item.maxValue = maxValue.Value;
        item.item.minValue = minValue.Value;

        var itemConductivity = configFile.Bind($"{item.item.itemName}", "5. Is Conductive", item.item.isConductiveMetal,
                                               "If set to true, will make the item conductive. Conductive defines, if the item attracts lightning");

        item.item.isConductiveMetal = itemConductivity.Value;

        var parsedConfig = configMoonRarity.Value.ParseConfig(item.item.itemName);

        foreach (var networkPrefab in item.connectedNetworkPrefabs) NetworkPrefabs.RegisterNetworkPrefab(networkPrefab);

        NetworkPrefabs.RegisterNetworkPrefab(item.item.spawnPrefab);

        Items.RegisterScrap(item.item, parsedConfig.spawnRateByLevelType, parsedConfig.spawnRateByCustomLevelType);

        item.isRegistered = true;

        TestAccountCore.Logger.LogInfo($"Fully registered item {item.item.itemName}!");
    }
}