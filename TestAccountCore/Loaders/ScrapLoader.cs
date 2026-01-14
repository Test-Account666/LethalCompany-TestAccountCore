using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Dawn;

namespace TestAccountCore.Loaders;

public static class ScrapLoader {
    internal static void RegisterAllScrap(List<ItemWithDefaultWeight> itemsWithDefaultWeight, ConfigFile? configFile) {
        if (configFile is null) return;

        itemsWithDefaultWeight.ForEach(item => RegisterScrap(item, configFile));
    }

    private static void RegisterScrap(ItemWithDefaultWeight item, ConfigFile? configFile) {
        if (configFile is null) return;

        if (item.item is null) throw new NullReferenceException("ItemProperties cannot be null!");

        var itemName = item.item.itemName;
        var section = $"{itemName} - Scrap";

        var canItemSpawn = configFile.Bind(section, "1. Enabled", true,
            $"If false, {itemName} will not be registered. This is different from a spawn weight of 0!");

        if (!canItemSpawn.Value) return;

        TestAccountCore.Logger.LogInfo($"Registering item {itemName}...");

        var maxValue = configFile.Bind(section, "2. Maximum Value", item.item.maxValue,
            $"Defines the maximum scrap value for {itemName}.");

        var minValue = configFile.Bind(section, "3. Minimum Value", item.item.minValue,
            $"Defines the minimum scrap value for {itemName}.");

        var configMoonRarity = configFile.Bind(section, "4. Moon Spawn Weight",
            $"Vanilla:{item.defaultWeight}, Modded:{item.defaultWeight}",
            $"Defines the spawn weight per moon. e.g. Assurance:{item.defaultWeight}");

        item.item.maxValue = maxValue.Value;
        item.item.minValue = minValue.Value;

        var itemConductivity = configFile.Bind($"{itemName} - General", "1. Is Conductive", item.item.isConductiveMetal,
            "If set to true, will make the item conductive. Conductive defines, if the item attracts lightning");

        item.item.isConductiveMetal = itemConductivity.Value;

        var spawnRateByCustomLevelType = configMoonRarity.Value.ParseConfig(itemName);

        var namespacedKey = NamespacedKey<DawnItemInfo>.From("testaccountcore", "scrap" + itemName.ToLower());

        DawnLib.DefineItem(namespacedKey, item.item,
            builder => { builder.DefineScrap(scrapBuilder => { scrapBuilder.SetWeights(SetWeights); }); });

        for (var index = 0; index < item.alternativeItems.Count; index++) {
            var alternativeItem = item.alternativeItems[index];
            var alternativeKey = NamespacedKey<DawnItemInfo>.From("testaccountcore", "scrap" + alternativeItem.itemName.ToLower() + index);
            DawnLib.DefineItem(alternativeKey, alternativeItem, builder => builder.DefineScrap(_ => {}));
        }

        DawnLib.RegisterNetworkPrefab(item.item.spawnPrefab);
        item.connectedNetworkPrefabs.ForEach(DawnLib.RegisterNetworkPrefab);

        item.isRegistered = true;

        TestAccountCore.Logger.LogInfo($"Fully registered item {itemName}!");
        return;

        void SetWeights(WeightTableBuilder<DawnMoonInfo> weightBuilder) {
            foreach (var (moon, weight) in spawnRateByCustomLevelType) {
                if (moon is null) continue;

                if (moon.Equals("all")) {
                    weightBuilder.SetGlobalWeight(weight);
                    continue;
                }

                var foundKey = NamespacedKey.ForceParse(moon, true);
                if (foundKey == null!) {
                    TestAccountCore.Logger.LogError($"Could not parse key {moon} for scrap {itemName}");
                    continue;
                }

                if (foundKey is not NamespacedKey<DawnMoonInfo> moonKey) {
                    weightBuilder.AddTagWeight(foundKey, weight);
                    continue;
                }

                weightBuilder.AddWeight(moonKey, weight);
            }

            weightBuilder.Build();
        }
    }
}