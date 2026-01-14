using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Dawn;

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

        var section = $"{unlockable.unlockableName} - Unlockable";

        var unlockableEnabled = configFile.Bind(section, "1. Enabled", true,
            $"If false, {unlockable.unlockableName} will not be registered.");

        if (!unlockableEnabled.Value) return;

        TestAccountCore.Logger.LogInfo($"Registering unlockable {unlockable.unlockableName}...");

        var alwaysUnlocked = configFile.Bind(section, "2. Always unlocked", false,
            $"If true, {unlockable.unlockableName} will always be unlocked. Otherwise you need to unlock it.");

        var price = configFile.Bind(section, "3. Price", unlockable.price,
            new ConfigDescription($"Price to unlock {unlockable.unlockableName}. Obviously doesn't matter, if 'Always Unlocked' is true.",
                new AcceptableValueRange<int>(0, 100000)));

        var unlockableItem = new UnlockableItem {
            unlockableName = unlockable.unlockableName,
            alreadyUnlocked = alwaysUnlocked.Value,
            inStorage = false,
            alwaysInStock = true,
            canBeStored = true,
            IsPlaceable = false,
            maxNumber = 1,
            unlockableType = 1,
            spawnPrefab = true,
            prefabObject = unlockable.spawnPrefab,
            luckValue = unlockable.luckValue,
        };

        var namespacedKey = NamespacedKey<DawnUnlockableItemInfo>.From("testaccountcore",
            $"unlockable{unlockable.unlockableName.ToLower()}");

        DawnLib.DefineUnlockable(namespacedKey, unlockableItem, unlockableBuilder => {
            unlockableBuilder.SetCost(price.Value);
            unlockableBuilder.DefinePlaceableObject(_ => {});
        });

        DawnLib.RegisterNetworkPrefab(unlockable.spawnPrefab);

        unlockable.isRegistered = true;

        TestAccountCore.Logger.LogInfo($"Fully registered unlockable {unlockable.unlockableName}!");
    }
}