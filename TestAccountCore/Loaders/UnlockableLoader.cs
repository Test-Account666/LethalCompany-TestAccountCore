using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using LethalLib.Extras;
using LethalLib.Modules;
using UnityEngine;

namespace TestAccountCore.Loaders;

public static class UnlockableLoader {
    internal static void RegisterAllUnlockables(List<UnlockableWithPrice> unlockablesWithPrice, ConfigFile? configFile) {
        if (configFile is null) return;

        unlockablesWithPrice.ForEach(unlockable => RegisterUnlockable(unlockable, configFile));
    }

    private static void RegisterUnlockable(UnlockableWithPrice unlockable, ConfigFile? configFile) {
        if (configFile is null) return;

        if (unlockable.unlockableName is null) throw new NullReferenceException("Unlockable unlockableName cannot be null!");

        if (unlockable.spawnPrefab is null) throw new NullReferenceException($"({unlockable.unlockableName}) Spawn Prefab cannot be null!");


        var unlockableEnabled = configFile.Bind($"{unlockable.unlockableName}", "1. Enabled", true,
                                                $"If false, {unlockable.unlockableName} will not be registered.");

        if (!unlockableEnabled.Value) return;

        TestAccountCore.Logger.LogInfo($"Registering unlockable {unlockable.unlockableName}...");

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