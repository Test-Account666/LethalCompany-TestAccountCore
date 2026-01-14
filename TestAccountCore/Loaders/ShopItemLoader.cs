using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Dawn;

namespace TestAccountCore.Loaders;

public static class ShopItemLoader {
    internal static void RegisterAllShopItems(List<ShopItemWithDefaultPrice> itemsWithDefaultPrice, ConfigFile? configFile) {
        if (configFile is null) return;

        itemsWithDefaultPrice.ForEach(item => RegisterShopItem(item, configFile));
    }

    private static void RegisterShopItem(ShopItemWithDefaultPrice item, ConfigFile? configFile) {
        if (configFile is null) return;
        if (item.item is null) throw new NullReferenceException("ItemProperties cannot be null!");

        var section = $"{item.item.itemName} - Shop";

        var canItemSpawn = configFile.Bind(section, "1. Enabled", true,
            $"If false, {item.item.itemName} will not be registered. This is different from a spawn weight of 0!");

        if (!canItemSpawn.Value) return;

        TestAccountCore.Logger.LogInfo($"Registering shop item {item.item.itemName}...");

        var price = configFile.Bind(section, "2. Price", item.defaultPrice, $"How much {item.item.itemName} costs to buy!");

        var itemConductivity = configFile.Bind($"{item.item.itemName} - General", "1. Is Conductive", item.item.isConductiveMetal,
            "If set to true, will make the item conductive. Conductive defines, if the item attracts lightning");

        item.item.isConductiveMetal = itemConductivity.Value;

        var namespacedKey = NamespacedKey<DawnItemInfo>.From("testaccountcore", "shop" + item.item.itemName.ToLower());

        DawnLib.DefineItem(namespacedKey, item.item,
            itemBuilder => { itemBuilder.DefineShop(builder => { builder.OverrideCost(price.Value); }); });

        DawnLib.RegisterNetworkPrefab(item.item.spawnPrefab);
        item.connectedNetworkPrefabs.ForEach(DawnLib.RegisterNetworkPrefab);

        item.isRegistered = true;

        TestAccountCore.Logger.LogInfo($"Fully registered shop item {item.item.itemName}!");
    }
}