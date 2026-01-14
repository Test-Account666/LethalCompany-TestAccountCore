using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using Dawn;
using HarmonyLib;
using LethalLib.Modules;

namespace TestAccountCore.Loaders;

public static class HallwayHazardLoader {
    internal static void RegisterAllHazards(List<HallwayHazardWithDefaultWeight> hazardsWithDefaultWeight, ConfigFile? configFile) {
        if (configFile is null) return;

        hazardsWithDefaultWeight.ForEach(item => RegisterHazard(item, configFile));
    }

    private static void RegisterHazard(HallwayHazardWithDefaultWeight hazard, ConfigFile? configFile) {
        if (configFile is null) return;

        if (hazard.hazardName is null) {
            TestAccountCore.Logger.LogError($"Hallway Hazard name cannot be null! ({hazard})");
            return;
        }

        if (hazard.hazardPrefab is null) {
            TestAccountCore.Logger.LogError($"Hallway Hazard {hazard.hazardName} has no prefab!");
            return;
        }

        if (hazard.spawnHazardPrefab is null) {
            TestAccountCore.Logger.LogError($"Hallway Hazard {hazard.hazardName} has no spawn prefab!");
            return;
        }

        var section = $"{hazard.hazardName} - Hallway Hazard";

        var canHazardSpawn = configFile.Bind(section, "1. Enabled", true,
            $"If false, {hazard.hazardName} will not be registered. This is different from a spawn weight of 0!");

        if (!canHazardSpawn.Value) return;

        TestAccountCore.Logger.LogInfo($"Registering hallway hazard {hazard.hazardName}...");

        var configMoonRarity = configFile.Bind(section, "2. Moon Spawn Weight",
            $"Vanilla:{hazard.defaultWeight}, Modded:{hazard.defaultWeight}",
            $"Defines the spawn weight per moon. e.g. Assurance:{hazard.defaultWeight}");

        var parsedConfig = configMoonRarity.Value.ParseConfig(hazard.hazardName);
        DawnLib.RegisterNetworkPrefab(hazard.hazardPrefab);

        HallwayHazardRegistry.RegisterHazard(hazard, parsedConfig);
        hazard.isRegistered = true;

        TestAccountCore.Logger.LogInfo($"Fully registered hallway hazard {hazard.hazardName}!");
    }
}