using System.Collections.Generic;
using System.Linq;
using Dawn;
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

    public static void RegisterHazard(HallwayHazardWithDefaultWeight hazard, Dictionary<string, int> spawnWeights) {
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
            let weight = hazard.GetWeight(RoundManager.Instance.currentLevel.PlanetName)
            from repeat in Enumerable.Repeat(new GameObjectWeight {
                Weight = multiplier * weight,
                GameObject = hazard.Hazard.spawnHazardPrefab,
            }, matchCount)
            select repeat
        );

        connectors.AddRange(toAdd);
    }

    private readonly struct RegisteredHazard(HallwayHazardWithDefaultWeight hazard, Dictionary<string, int> spawnWeights) {
        public HallwayHazardWithDefaultWeight Hazard { get; } = hazard;

        public Dictionary<string, int> SpawnWeights { get; } = spawnWeights;

        public int GetWeight(string level) {
            var randomKey = NamespacedKey.ForceParse(level, true);
            if (randomKey is not NamespacedKey<DawnMoonInfo> moonKey) {
                TestAccountCore.Logger.LogWarning($"(Hazard: {Hazard.hazardName}) Couldn't find weight for key {randomKey}! Is it a moon?");
                return -1;
            }

            var found = -1;

            foreach (var (key, weight) in SpawnWeights) {
                if (level.ToLower().Contains(key)) return weight;
                switch (key) {
                    case "all":
                    case "modded" when moonKey.IsModded():
                    case "vanilla" when moonKey.IsVanilla():
                        found = weight;
                        continue;
                }
            }

            if (found != -1) return found;
            TestAccountCore.Logger.LogWarning($"(Hazard: {Hazard.hazardName}) Couldn't find weight for key {moonKey}!");
            return 0;
        }
    }
}