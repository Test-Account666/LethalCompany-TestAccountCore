using System;
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
        if (_assets is null || configFile is null) return;

        var allAssets = _assets.LoadAllAssets<ItemWithDefaultWeight>();

        var allItemsWithDefaultWeight = allAssets.OfType<ItemWithDefaultWeight>();

        var itemsWithDefaultWeight = allItemsWithDefaultWeight.ToList();

        ScrapLoader.RegisterAllScrap(itemsWithDefaultWeight, configFile);
    }


    public static void LoadHazards(ConfigFile? configFile) {
        if (_assets is null || configFile is null) return;

        var allAssets = _assets.LoadAllAssets<MapHazardWithDefaultWeight>();

        var allHazardsWithDefaultWeight = allAssets.OfType<MapHazardWithDefaultWeight>();

        var hazardsWithDefaultWeight = allHazardsWithDefaultWeight.ToList();

        HazardLoader.RegisterAllHazards(hazardsWithDefaultWeight, configFile);
    }

    public static void LoadUnlockables(ConfigFile? configFile) {
        if (_assets is null || configFile is null) return;

        var allAssets = _assets.LoadAllAssets<UnlockableWithPrice>();

        var allHazardsWithDefaultWeight = allAssets.OfType<UnlockableWithPrice>();

        var unlockablesWithPrice = allHazardsWithDefaultWeight.ToList();

        UnlockableLoader.RegisterAllUnlockables(unlockablesWithPrice, configFile);
    }

    public static void LoadShopItems(ConfigFile? configFile) {
        if (_assets is null || configFile is null) return;

        var allAssets = _assets.LoadAllAssets<ShopItemWithDefaultPrice>();

        var allItemsWithPrice = allAssets.OfType<ShopItemWithDefaultPrice>();

        var itemsWithDefaultPrice = allItemsWithPrice.ToList();

        ShopItemLoader.RegisterAllShopItems(itemsWithDefaultPrice, configFile);
    }
}