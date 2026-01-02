using System.Collections.Generic;
using System.Linq;
using DunGen;
using HarmonyLib;
using LethalLib.Modules;
using static TestAccountCore.VanillaLevelMatcher;
using Dungeon = DunGen.Dungeon;

namespace TestAccountCore;

[HarmonyPatch(typeof(Dungeon), nameof(Dungeon.SpawnDoorPrefab))]
public static class HallwayHazardRegistry {
    private static readonly List<RegisteredHazard> _REGISTERED_HAZARDS = [
    ];

    public static void RegisterHazard(HallwayHazardWithDefaultWeight hazard,
        (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) spawnWeights) {
        _REGISTERED_HAZARDS.Add(new(hazard, spawnWeights));
    }

    [HarmonyPatch]
    [HarmonyPrefix]
    public static void InjectHazards(Doorway a, Doorway b) {
        AddHazardsForDoorway(a, 0.05f);
        AddHazardsForDoorway(b, 0.07f);
    }

    private static void AddHazardsForDoorway(Doorway doorway, float multiplier) {
        if (doorway == null || doorway.ConnectorPrefabWeights == null) return;

        var connectors = doorway.ConnectorPrefabWeights;
        var matchCount = connectors.Count(weight => weight?.GameObject != null && weight.GameObject.name == "BigDoorSpawn");

        TestAccountCore.Logger.LogDebug($"Hazard injection: {matchCount} matches, {_REGISTERED_HAZARDS.Count} hazards");

        if (matchCount == 0) return;

        var toAdd = new List<GameObjectWeight>(matchCount * _REGISTERED_HAZARDS.Count);

        toAdd.AddRange(
            from hazard in _REGISTERED_HAZARDS
            from repeat in Enumerable.Repeat(new GameObjectWeight {
                Weight = multiplier * GetSpawnWeight(hazard, RoundManager.Instance.currentLevel.PlanetName),
                GameObject = hazard.Hazard.spawnHazardPrefab,
            }, matchCount)
            select repeat
        );

        connectors.AddRange(toAdd);
    }

    private static int GetSpawnWeight(RegisteredHazard hazard, string levelType) {
        var spawnWeight = GetVanillaWeight(hazard, levelType);

        if (spawnWeight == -1 || !IsVanilla(levelType)) {
            var moddedSpawnWeight = GetModdedWeight(hazard, levelType);
            if (moddedSpawnWeight != -1) return moddedSpawnWeight;
        }

        if (spawnWeight != -1) return spawnWeight;

        TestAccountCore.Logger.LogWarning($"Failed to find spawn weight for {hazard!.Hazard.hazardName}!");
        return 0;
    }

    private static int GetModdedWeight(RegisteredHazard hazard, string levelType) {
        var spawnWeight = -1;

        var spawnWeights = hazard.SpawnWeights;

        foreach (var (levelName, weight) in spawnWeights.spawnRateByCustomLevelType) {
            if (!levelType.ToLower().Contains(levelName.ToLower())) continue;
            spawnWeight = weight;
            break;
        }

        return spawnWeight;
    }

    private static int GetVanillaWeight(RegisteredHazard hazard, string levelType) {
        var spawnWeight = -1;

        var spawnWeights = hazard.SpawnWeights;

        foreach (var (levelTypes, weight) in spawnWeights.spawnRateByLevelType) {
            switch (levelTypes) {
                case Levels.LevelTypes.None: continue;
                case Levels.LevelTypes.All when spawnWeight == -1:
                case Levels.LevelTypes.Vanilla when spawnWeight == -1 && IsVanilla(levelType):
                case Levels.LevelTypes.Modded when spawnWeight == -1 && !IsVanilla(levelType):
                    spawnWeight = weight;
                    continue;
                default:
                    if (!levelType.ToLower().Contains(levelTypes.ToString().ToLower())) continue;
                    spawnWeight = weight;
                    continue;
            }
        }

        return spawnWeight;
    }

    private readonly struct RegisteredHazard(
        HallwayHazardWithDefaultWeight hazard,
        (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) spawnWeights) {
        public HallwayHazardWithDefaultWeight Hazard { get; } = hazard;

        public (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) SpawnWeights {
            get;
        } = spawnWeights;
    }
}