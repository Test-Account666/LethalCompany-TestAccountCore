using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using LethalLib.Modules;

namespace TestAccountCore.Loaders;

public static class ShopItemLoader {
    internal static void RegisterAllShopItems(List<ShopItemWithDefaultPrice> itemsWithDefaultPrice, ConfigFile? configFile) {
        if (configFile is null) return;

        itemsWithDefaultPrice.ForEach(item => RegisterShopItem(item, configFile));
    }

    private static void RegisterShopItem(ShopItemWithDefaultPrice item, ConfigFile? configFile) {
        if (configFile is null) return;

        if (item.item is null) throw new NullReferenceException("ItemProperties cannot be null!");

        var canItemSpawn = configFile.Bind($"{item.item.itemName}", "1. Enabled", true,
                                           $"If false, {item.item.itemName} will not be registered. This is different from a spawn weight of 0!");

        if (!canItemSpawn.Value) return;

        TestAccountCore.Logger.LogInfo($"Registering shop item {item.item.itemName}...");

        var price = configFile.Bind($"{item.item.itemName}", "2. Price", item.defaultPrice, $"How much {item.item.itemName} costs to buy!");

        var itemConductivity = configFile.Bind($"{item.item.itemName}", "3. Is Conductive", item.item.isConductiveMetal,
                                               "If set to true, will make the item conductive. Conductive defines, if the item attracts lightning");

        item.item.isConductiveMetal = itemConductivity.Value;

        foreach (var networkPrefab in item.connectedNetworkPrefabs) NetworkPrefabs.RegisterNetworkPrefab(networkPrefab);

        NetworkPrefabs.RegisterNetworkPrefab(item.item.spawnPrefab);

        Items.RegisterShopItem(item.item, price.Value);

        item.isRegistered = true;

        TestAccountCore.Logger.LogInfo($"Fully registered shop item {item.item.itemName}!");
    }
}