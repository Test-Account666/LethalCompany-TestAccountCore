using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx.Configuration;
using LethalLib.Extras;
using LethalLib.Modules;
using MonoMod.Utils;
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
            TestAccountCore.Logger.LogError($"Failed to load asset bundle '{assetBundleName}' for assembly {assembly.FullName}: {ex.Message}");
        }
    }

    public static void LoadCustomScripts(ConfigFile? configFile) {
        if (_assets is null || configFile is null) return;

        var allAssets = _assets.LoadAllAssets<CustomScript>();

        var allCustomScripts = allAssets.OfType<CustomScript>();

        foreach (var customScript in allCustomScripts) customScript.Initialize(configFile);
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

        if (item.item is null) throw new NullReferenceException("ItemProperties cannot be null!");

        var canItemSpawn = configFile.Bind($"{item.item.itemName}", "1. Enabled", true,
                                           $"If false, {item.item.itemName} will not be registered. This is different from a spawn weight of 0!");

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

        foreach (var networkPrefab in item.connectedNetworkPrefabs) NetworkPrefabs.RegisterNetworkPrefab(networkPrefab);

        NetworkPrefabs.RegisterNetworkPrefab(item.item.spawnPrefab);

        Items.RegisterScrap(item.item, parsedConfig.spawnRateByLevelType, parsedConfig.spawnRateByCustomLevelType);

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

        if (hazard.hazardName is null) throw new NullReferenceException("Map Hazard name cannot be null!");

        TestAccountCore.Logger.LogInfo($"Registering hazard {hazard.hazardName}...");

        if (hazard.spawnableMapObject is null) throw new NullReferenceException($"({hazard.hazardName}) Map Hazard cannot be null!");


        var canHazardSpawn = configFile.Bind($"{hazard.hazardName}", "1. Enabled", true,
                                             $"If false, {hazard.hazardName} will not be registered.");

        if (!canHazardSpawn.Value) return;

        AnimationCurve animationCurve;

        if (string.IsNullOrWhiteSpace(hazard.spawnCurve)) {
            var spawnMaxAmount = configFile.Bind($"{hazard.hazardName}", "2. Max Spawn Amount", hazard.amount,
                                                 $"The max spawn amount for {hazard.hazardName}.").Value;

            animationCurve = new(new Keyframe(0, 0), new Keyframe(1, spawnMaxAmount));
        } else {
            var defaultCurve = hazard.spawnCurve;

            var spawnCurveString = configFile.Bind($"{hazard.hazardName}", "2. Spawn Curve", defaultCurve,
                                                   $"The spawn curve for {hazard.hazardName}. "
                                                 + $"First number is between 0 and 1. The second one is the max amount.").Value;

            spawnCurveString = spawnCurveString.Replace(" ", "");

            if (spawnCurveString.StartsWith(",")) spawnCurveString = spawnCurveString[1..];

            if (spawnCurveString.EndsWith(",")) spawnCurveString = spawnCurveString[..^1];

            var keyFrames = new List<Keyframe>();

            foreach (var keyframeString in spawnCurveString.Split(",")) {
                var keyframe = keyframeString.Split(':');

                if (keyframe.Length <= 1)
                    try {
                        throw new InvalidDataException($"'{spawnCurveString}' is invalid! Point: {keyframeString}");
                    } catch (Exception exception) {
                        TestAccountCore.Logger.LogError($"Map Hazard spawn curve is not correctly configured: {exception.Message}");

                        exception.LogDetailed();
                        return;
                    }

                var parsedTime = float.TryParse(keyframe[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var time);

                if (!parsedTime) {
                    try {
                        throw new InvalidDataException($"'{spawnCurveString}' is invalid!"
                                                     + $" Point: {keyframeString} (Could not parse time '{keyframe[0]}' as float)");
                    } catch (Exception exception) {
                        TestAccountCore.Logger.LogError($"Map Hazard spawn curve is not correctly configured: {exception.Message}");

                        exception.LogDetailed();
                        return;
                    }
                }

                var parsedAmount = int.TryParse(keyframe[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount);

                if (!parsedAmount) {
                    try {
                        throw new InvalidDataException($"'{spawnCurveString}' is invalid!"
                                                     + $" Point: {keyframeString} (Could not parse amount '{keyframe[1]}' as int)");
                    } catch (Exception exception) {
                        TestAccountCore.Logger.LogError($"Map Hazard spawn curve is not correctly configured: {exception.Message}");

                        exception.LogDetailed();
                        return;
                    }
                }


                keyFrames.Add(new(time, amount));
            }

            animationCurve = new(keyFrames.ToArray());
        }

        MapObjects.RegisterMapObject(new SpawnableMapObject {
            prefabToSpawn = hazard.spawnableMapObject,
            spawnFacingWall = hazard.spawnFacingWall,
            spawnWithBackToWall = hazard.spawnWithBackToWall,
            spawnFacingAwayFromWall = hazard.spawnFacingAwayFromWall,
            spawnWithBackFlushAgainstWall = hazard.spawnWithBackFlushAgainstWall,
            disallowSpawningNearEntrances = hazard.disallowSpawningNearEntrances,
            requireDistanceBetweenSpawns = hazard.requireDistanceBetweenSpawns,
        }, Levels.LevelTypes.All, _ => animationCurve);

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

    public static void LoadUnlockables(ConfigFile? configFile) {
        if (_assets is null || configFile is null) return;

        var allAssets = _assets.LoadAllAssets<UnlockableWithPrice>();

        var allHazardsWithDefaultWeight = allAssets.OfType<UnlockableWithPrice>();

        var unlockablesWithPrice = allHazardsWithDefaultWeight.ToList();

        RegisterAllUnlockables(unlockablesWithPrice, configFile);
    }

    private static void RegisterAllUnlockables(List<UnlockableWithPrice> unlockablesWithPrice, ConfigFile? configFile) {
        if (configFile is null) return;

        unlockablesWithPrice.ForEach(unlockable => RegisterUnlockable(unlockable, configFile));
    }

    private static void RegisterUnlockable(UnlockableWithPrice unlockable, ConfigFile? configFile) {
        if (configFile is null) return;

        if (unlockable.unlockableName is null) throw new NullReferenceException("Unlockable unlockableName cannot be null!");

        TestAccountCore.Logger.LogInfo($"Registering unlockable {unlockable.unlockableName}...");

        if (unlockable.spawnPrefab is null) throw new NullReferenceException($"({unlockable.unlockableName}) Spawn Prefab cannot be null!");


        var unlockableEnabled = configFile.Bind($"{unlockable.unlockableName}", "1. Enabled", true,
                                                $"If false, {unlockable.unlockableName} will not be registered.");

        if (!unlockableEnabled.Value) return;

        var alwaysUnlocked = configFile.Bind($"{unlockable.unlockableName}", "2. Always unlocked", false,
                                             $"If true, {unlockable.unlockableName} will always be unlocked. Otherwise you need to unlock it.");

        var price = configFile.Bind($"{unlockable.unlockableName}", "3. Price", unlockable.price,
                                    new ConfigDescription(
                                        $"Price to unlock {unlockable.unlockableName}. Obviously doesn't matter, if 'Always Unlocked' is true.",
                                        new AcceptableValueRange<int>(0, 100000)));

        if (ScriptableObject.CreateInstance(typeof(UnlockableItemDef)) is not UnlockableItemDef unlockableDef)
            throw new NullReferenceException($"({unlockable.unlockableName}) Could not create unlockable item!");

        unlockableDef.storeType = unlockable.storeType;

        unlockableDef.unlockable = new() {
            unlockableName = unlockable.unlockableName,
            alreadyUnlocked = alwaysUnlocked.Value,
            inStorage = false,
            alwaysInStock = true,
            canBeStored = true,
            maxNumber = 1,
            unlockableType = 1,
            hasBeenUnlockedByPlayer = alwaysUnlocked.Value,
            spawnPrefab = true,
            prefabObject = unlockable.spawnPrefab,
        };

        Unlockables.RegisterUnlockable(unlockableDef, price.Value, unlockable.storeType);

        NetworkPrefabs.RegisterNetworkPrefab(unlockable.spawnPrefab);

        TestAccountCore.Logger.LogInfo($"Fully registered unlockable {unlockable.unlockableName}!");
    }
}