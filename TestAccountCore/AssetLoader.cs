using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx.Configuration;
using LethalLib.Modules;
using UnityEngine;

namespace TestAccountCore;

public static class AssetLoader {
    private static AssetBundle? _assets;

    public static void LoadBundle(Assembly assembly, string assetBundleName) {
        var assemblyLocation = Path.GetDirectoryName(assembly.Location);
        if (assemblyLocation == null) {
            TestAccountCore.Logger.LogError($"Failed to determine assembly '{assembly.FullName}' location.");
            return;
        }

        var assetBundlePath = Path.Combine(assemblyLocation, assetBundleName);
        if (!File.Exists(assetBundlePath)) {
            TestAccountCore.Logger.LogFatal(new StringBuilder($"Asset bundle '{assetBundleName}' not found at {assetBundlePath}.")
                                            .Append(" ")
                                            .Append("Check if the asset bundle is in the same directory as the plugin.")
                                            .ToString());
            return;
        }

        try {
            _assets = AssetBundle.LoadFromFile(assetBundlePath);
        } catch (Exception ex) {
            TestAccountCore.Logger.LogError($"Failed to load asset bundle '{assetBundleName}' for assembly {assembly.FullName}: {ex.Message
            }");
        }
    }


    public static void LoadItems(ConfigFile? configFile) {
        if (_assets is null || configFile is null) return;

        var allAssets = _assets.LoadAllAssets<ItemWithDefaultWeight>();

        var allItemsWithDefaultWeight = allAssets.OfType<ItemWithDefaultWeight>();

        var itemsWithDefaultWeight = allItemsWithDefaultWeight.ToList();

        RegisterAllScrap(itemsWithDefaultWeight, configFile);
    }

    private static void RegisterAllScrap(List<ItemWithDefaultWeight> itemsWithDefaultWeight, ConfigFile? configFile) {
        if (configFile is null) return;

        itemsWithDefaultWeight.ForEach(item => RegisterScrap(item, configFile));
    }

    private static void RegisterScrap(ItemWithDefaultWeight item, ConfigFile? configFile) {
        if (configFile is null) return;

        if (item.item is null)
            throw new NullReferenceException("ItemProperties cannot be null!");

        var canItemSpawn = configFile.Bind($"{item.item.itemName}", "1. Enabled", true,
                                           $"If false, {item.item.itemName
                                           } will not be registered. This is different from a spawn weight of 0!");

        if (!canItemSpawn.Value) return;

        var maxValue = configFile.Bind($"{item.item.itemName}", "2. Maximum Value", item.item.maxValue,
                                       $"Defines the maximum scrap value for {item.item.itemName}.");

        var minValue = configFile.Bind($"{item.item.itemName}", "3. Minimum Value", item.item.minValue,
                                       $"Defines the minimum scrap value for {item.item.itemName}.");

        var configMoonRarity =
            configFile.Bind($"{item.item.itemName}", "4. Moon Spawn Weight", $"Vanilla:{item.defaultWeight}, Modded:{item.defaultWeight}",
                            $"Defines the spawn weight per moon. e.g. Assurance:{item.defaultWeight}");

        item.item.maxValue = maxValue.Value;
        item.item.minValue = minValue.Value;

        var itemConductivity = configFile.Bind($"{item.item.itemName}", "5. Is Conductive", item.item.isConductiveMetal,
                                               "If set to true, will make the item conductive. Conductive defines, if the item attracts lightning");

        item.item.isConductiveMetal = itemConductivity.Value;

        var parsedConfig = configMoonRarity.Value.ParseConfig(item.item.itemName);

        Items.RegisterScrap(item.item, parsedConfig.spawnRateByLevelType, parsedConfig.spawnRateByCustomLevelType);

        NetworkPrefabs.RegisterNetworkPrefab(item.item.spawnPrefab);

        TestAccountCore.Logger.LogInfo($"Fully registered item {item.item.itemName}!");
    }

    public static void LoadHazards(ConfigFile? configFile) {
        if (_assets is null || configFile is null) return;

        var allAssets = _assets.LoadAllAssets<MapHazardWithDefaultWeight>();

        var allHazardsWithDefaultWeight = allAssets.OfType<MapHazardWithDefaultWeight>();

        var hazardsWithDefaultWeight = allHazardsWithDefaultWeight.ToList();

        RegisterAllHazards(hazardsWithDefaultWeight, configFile);
    }

    private static void RegisterAllHazards(List<MapHazardWithDefaultWeight> hazardsWithDefaultWeight, ConfigFile? configFile) {
        if (configFile is null) return;

        hazardsWithDefaultWeight.ForEach(hazard => RegisterHazard(hazard, configFile));
    }

    private static void RegisterHazard(MapHazardWithDefaultWeight hazard, ConfigFile? configFile) {
        if (configFile is null) return;

        if (hazard.spawnableMapObject is null) throw new NullReferenceException("Map Hazard cannot be null!");

        if (hazard.hazardName is null) throw new NullReferenceException("Map Hazard name cannot be null!");

        var canHazardSpawn = configFile.Bind($"{hazard.hazardName}", "1. Enabled", true,
                                             $"If false, {hazard.hazardName} will not be registered.");

        if (!canHazardSpawn.Value) return;

        var spawnWeight = configFile.Bind($"{hazard.hazardName}", "2. Spawn Weight", hazard.amount,
                                          $"The Spawn weight for {hazard.hazardName}.").Value;

        MapObjects.RegisterMapObject(new SpawnableMapObject {
            prefabToSpawn = hazard.spawnableMapObject,
        }, Levels.LevelTypes.All, _ => new(new Keyframe(0, 0), new Keyframe(1, spawnWeight)));

        NetworkPrefabs.RegisterNetworkPrefab(hazard.spawnableMapObject);

        TestAccountCore.Logger.LogInfo($"Fully registered hazard {hazard.hazardName}!");
    }

    private static (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType)
        ParseConfig(this string configMoonRarity, string itemName) {
        Dictionary<Levels.LevelTypes, int> spawnRateByLevelType = [
        ];
        Dictionary<string, int> spawnRateByCustomLevelType = [
        ];

        foreach (var entry in configMoonRarity.Split(',').Select(configEntry => configEntry.Trim())) {
            if (string.IsNullOrWhiteSpace(entry)) continue;

            string[] entryParts = entry.Split(':');

            if (entryParts.Length != 2) continue;

            var name = entryParts[0];

            if (!int.TryParse(entryParts[1], out var spawnWeight)) continue;

            if (Enum.TryParse<Levels.LevelTypes>(name, true, out var levelType)) {
                spawnRateByLevelType[levelType] = spawnWeight;
                TestAccountCore.Logger.LogInfo($"Registered {itemName}'s weight for level type {levelType} to {spawnWeight}");
                continue;
            }

            spawnRateByCustomLevelType[name] = spawnWeight;
            TestAccountCore.Logger.LogInfo($"Registered {itemName}'s weight for custom level type {name} to {spawnWeight}");
        }

        return (spawnRateByLevelType, spawnRateByCustomLevelType);
    }
}