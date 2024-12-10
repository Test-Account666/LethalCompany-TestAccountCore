using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TestAccountCore;

[HarmonyPatch]
public static class MapHazardRegistry {
    public static readonly Dictionary<SpawnableMapObject, Func<SelectableLevel, AnimationCurve>> RegisteredHazards = [
    ];

    public static readonly HashSet<GameObject> HazardPrefabs = [
    ];

    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnMapObjects))]
    [HarmonyPrefix]
    public static void FillRandomMapObjects() {
        var randomMapObjects = Object.FindObjectsByType<RandomMapObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var mapHazard in RoundManager.Instance.currentLevel.spawnableMapObjects) {
            if (!HazardPrefabs.Contains(mapHazard.prefabToSpawn)) continue;

            foreach (var randomMapObject in randomMapObjects) randomMapObject.spawnablePrefabs.Add(mapHazard.prefabToSpawn);
        }
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
    [HarmonyPostfix]
    public static void RegisterHazards() {
        HazardPrefabs.Clear();

        var selectableLevels = StartOfRound.Instance.levels;

        foreach (var selectableLevel in selectableLevels) {
            List<SpawnableMapObject> spawnableMapObjects = [
                ..selectableLevel.spawnableMapObjects,
            ];

            foreach (var (mapHazard, spawnCurveFunc) in RegisteredHazards) {
                HazardPrefabs.Add(mapHazard.prefabToSpawn);

                var spawnableMapObject = new SpawnableMapObject {
                    prefabToSpawn = mapHazard.prefabToSpawn,
                    spawnFacingWall = mapHazard.spawnFacingWall,
                    spawnWithBackToWall = mapHazard.spawnWithBackToWall,
                    spawnFacingAwayFromWall = mapHazard.spawnFacingAwayFromWall,
                    spawnWithBackFlushAgainstWall = mapHazard.spawnWithBackFlushAgainstWall,
                    disallowSpawningNearEntrances = mapHazard.disallowSpawningNearEntrances,
                    requireDistanceBetweenSpawns = mapHazard.requireDistanceBetweenSpawns,
                    numberToSpawn = spawnCurveFunc(selectableLevel),
                };

                spawnableMapObjects.Add(spawnableMapObject);
            }

            selectableLevel.spawnableMapObjects = spawnableMapObjects.ToArray();
        }
    }
}