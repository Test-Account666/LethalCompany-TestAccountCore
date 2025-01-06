using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx.Configuration;
using TestAccountCore.Loaders;
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
            TestAccountCore.Logger.LogFatal(new StringBuilder($"Asset bundle '{assetBundleName}' not found at {assetBundlePath}.").Append(" ")
                                                .Append("Check if the asset bundle is in the same directory as the plugin.").ToString());
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
        LoadItemsAndReturn(configFile);
    }

    // ReSharper disable once UnusedMethodReturnValue.Global
    public static List<ItemWithDefaultWeight> LoadItemsAndReturn(ConfigFile? configFile) {
        if (_assets is null || configFile is null)
            return [
            ];

        var allAssets = _assets.LoadAllAssets<ItemWithDefaultWeight>();

        var allItemsWithDefaultWeight = allAssets.OfType<ItemWithDefaultWeight>();

        var itemsWithDefaultWeight = allItemsWithDefaultWeight.ToList();

        ScrapLoader.RegisterAllScrap(itemsWithDefaultWeight, configFile);

        return itemsWithDefaultWeight;
    }


    public static void LoadHazards(ConfigFile? configFile) {
        LoadHazardsAndReturn(configFile);
    }

    // ReSharper disable once UnusedMethodReturnValue.Global
    public static List<MapHazardWithDefaultWeight> LoadHazardsAndReturn(ConfigFile? configFile) {
        if (_assets is null || configFile is null)
            return [
            ];

        var allAssets = _assets.LoadAllAssets<MapHazardWithDefaultWeight>();

        var allHazardsWithDefaultWeight = allAssets.OfType<MapHazardWithDefaultWeight>();

        var hazardsWithDefaultWeight = allHazardsWithDefaultWeight.ToList();

        HazardLoader.RegisterAllHazards(hazardsWithDefaultWeight, configFile);

        return hazardsWithDefaultWeight;
    }

    public static void LoadUnlockables(ConfigFile? configFile) {
        LoadUnlockablesAndReturn(configFile);
    }

    // ReSharper disable once UnusedMethodReturnValue.Global
    public static List<UnlockableWithPrice> LoadUnlockablesAndReturn(ConfigFile? configFile) {
        if (_assets is null || configFile is null)
            return [
            ];

        var allAssets = _assets.LoadAllAssets<UnlockableWithPrice>();

        var allHazardsWithDefaultWeight = allAssets.OfType<UnlockableWithPrice>();

        var unlockablesWithPrice = allHazardsWithDefaultWeight.ToList();

        UnlockableLoader.RegisterAllUnlockables(unlockablesWithPrice, configFile);

        return unlockablesWithPrice;
    }

    public static void LoadShopItems(ConfigFile? configFile) {
        LoadShopItemsAndReturn(configFile);
    }

    // ReSharper disable once UnusedMethodReturnValue.Global
    public static List<ShopItemWithDefaultPrice> LoadShopItemsAndReturn(ConfigFile? configFile) {
        if (_assets is null || configFile is null)
            return [
            ];

        var allAssets = _assets.LoadAllAssets<ShopItemWithDefaultPrice>();

        var allItemsWithPrice = allAssets.OfType<ShopItemWithDefaultPrice>();

        var itemsWithDefaultPrice = allItemsWithPrice.ToList();

        ShopItemLoader.RegisterAllShopItems(itemsWithDefaultPrice, configFile);

        return itemsWithDefaultPrice;
    }

    public static void LoadEnemies(ConfigFile? configFile) {
        LoadEnemiesAndReturn(configFile);
    }

    // ReSharper disable once UnusedMethodReturnValue.Global
    public static List<EnemyWithDefaultWeight> LoadEnemiesAndReturn(ConfigFile? configFile) {
        if (_assets is null || configFile is null)
            return [
            ];

        var allAssets = _assets.LoadAllAssets<EnemyWithDefaultWeight>();

        var allEnemiesWithWeight = allAssets.OfType<EnemyWithDefaultWeight>();

        var enemiesWithWeight = allEnemiesWithWeight.ToList();

        EnemyLoader.RegisterAllEnemies(enemiesWithWeight, configFile);

        return enemiesWithWeight;
    }

    public static void UnloadBundle(bool unloadAllLoadedObjects = false) => _assets?.Unload(unloadAllLoadedObjects);
}