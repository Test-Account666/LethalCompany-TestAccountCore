using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using LethalLib.Modules;
using MonoMod.Utils;
using UnityEngine;

namespace TestAccountCore.Loaders;

public static class HazardLoader {
    private static readonly Lazy<Regex> _NumberPattern = new(() => new(@"\d* ", RegexOptions.Compiled));

    internal static void RegisterAllHazards(List<MapHazardWithDefaultWeight> hazardsWithDefaultWeight, ConfigFile? configFile) {
        if (configFile is null) return;

        hazardsWithDefaultWeight.ForEach(hazard => RegisterHazard(hazard, configFile));
    }

    private static void RegisterHazard(MapHazardWithDefaultWeight hazard, ConfigFile? configFile) {
        if (configFile is null) return;

        if (hazard.hazardName is null) throw new NullReferenceException("Map Hazard name cannot be null!");

        if (hazard.spawnableMapObject is null) throw new NullReferenceException($"({hazard.hazardName}) Map Hazard cannot be null!");


        var canHazardSpawn = configFile.Bind($"{hazard.hazardName}", "1. Enabled", true, $"If false, {hazard.hazardName} will not be registered.");

        if (!canHazardSpawn.Value) return;

        TestAccountCore.Logger.LogInfo($"Registering hazard {hazard.hazardName}...");

        if (string.IsNullOrWhiteSpace(hazard.spawnCurve)) {
            TestAccountCore.Logger.LogWarning($"Hazard {hazard.hazardName} is still using spawn amount! This is no longer supported!");
            return;
        }

        var defaultCurve = $"Vanilla - {hazard.spawnCurve} ; Modded - {hazard.spawnCurve}";

        var spawnCurveString = configFile.Bind($"{hazard.hazardName}", "2. Spawn Curve", defaultCurve,
                                               $"The spawn curve for {hazard.hazardName}. "
                                             + $"First number is between 0 and 1. The second one is the max amount.").Value;

        if (!spawnCurveString.Contains("-")) {
            TestAccountCore.Logger.LogWarning($"Looks like you didn't specify any moons for {hazard.hazardName}! Defaulting to 'All'! A valid example: '{
                defaultCurve}'");
            spawnCurveString = $"All - {spawnCurveString}";
        }

        spawnCurveString = spawnCurveString.Replace(" ", "");

        var levelSpawnCurveDictionary = new Dictionary<string, AnimationCurve>();

        var proceed = FillLevelSpawnCurveList(defaultCurve, spawnCurveString, levelSpawnCurveDictionary);

        if (!proceed) {
            TestAccountCore.Logger.LogWarning($"Failed to register hazard {hazard.hazardName}!");
            return;
        }

        var noSpawnAnimationCurve = new AnimationCurve(new(0, 0), new(1, 0));

        var hasAll = levelSpawnCurveDictionary.TryGetValue("ALL", out var allLevelSpawnCurve);
        var hasVanilla = levelSpawnCurveDictionary.TryGetValue("VANILLA", out var vanillaLevelSpawnCurve);
        var hasModded = levelSpawnCurveDictionary.TryGetValue("MODDED", out var moddedLevelSpawnCurve);

        MapHazardRegistry.RegisteredHazards[new() {
            prefabToSpawn = hazard.spawnableMapObject,
            spawnFacingWall = hazard.spawnFacingWall,
            spawnWithBackToWall = hazard.spawnWithBackToWall,
            spawnFacingAwayFromWall = hazard.spawnFacingAwayFromWall,
            spawnWithBackFlushAgainstWall = hazard.spawnWithBackFlushAgainstWall,
            disallowSpawningNearEntrances = hazard.disallowSpawningNearEntrances,
            requireDistanceBetweenSpawns = hazard.requireDistanceBetweenSpawns,
        }] = level => {
            var levelName = _NumberPattern.Value.Replace(level.PlanetName, "").ToLower();

            var spawnCurve = noSpawnAnimationCurve;

            if (hasAll) spawnCurve = allLevelSpawnCurve;

            var isVanilla = VanillaLevelMatcher.IsVanilla(levelName);

            if (hasVanilla && isVanilla) spawnCurve = vanillaLevelSpawnCurve;

            if (hasModded && !isVanilla) spawnCurve = moddedLevelSpawnCurve;

            foreach (var (spawnLevel, animationCurve) in levelSpawnCurveDictionary) {
                if (!levelName.StartsWith(spawnLevel.ToLower())) continue;

                spawnCurve = animationCurve;
                break;
            }

            spawnCurve ??= noSpawnAnimationCurve;

            return spawnCurve;
        };

        NetworkPrefabs.RegisterNetworkPrefab(hazard.spawnableMapObject);

        hazard.isRegistered = true;

        TestAccountCore.Logger.LogInfo($"Fully registered hazard {hazard.hazardName}!");
    }

    private static bool FillLevelSpawnCurveList(string defaultCurve, string spawnCurveString, Dictionary<string, AnimationCurve> levelSpawnCurveDictionary) {
        var levelSpawnSplit = spawnCurveString.Split(";", StringSplitOptions.RemoveEmptyEntries);

        if (levelSpawnSplit.Length < 1)
            try {
                throw new InvalidDataException($"'{spawnCurveString}' is invalid! Valid Example: '{defaultCurve}'");
            } catch (Exception exception) {
                TestAccountCore.Logger.LogError($"Map Hazard spawn curve is not correctly configured: {exception.Message}");

                exception.LogDetailed();
                return false;
            }

        foreach (var levelSpawn in levelSpawnSplit) {
            var levelCurveSplit = levelSpawn.Split("-", StringSplitOptions.RemoveEmptyEntries);

            if (levelCurveSplit.Length <= 1)
                try {
                    throw new InvalidDataException($"'{spawnCurveString}' is invalid! Point: {levelSpawn}");
                } catch (Exception exception) {
                    TestAccountCore.Logger.LogError($"Map Hazard spawn curve is not correctly configured: {exception.Message}");

                    exception.LogDetailed();
                    return false;
                }

            var levelName = levelCurveSplit[0];
            var curveString = levelCurveSplit[1];

            var keyFrames = new List<Keyframe>();

            var proceed = ReadSpawnCurveString(curveString, keyFrames);

            if (!proceed) return false;

            var levelType = LevelTypeMatcher.FromString(levelName);

            levelName = levelType switch {
                LevelTypes.ALL => "ALL",
                LevelTypes.VANILLA => "VANILLA",
                LevelTypes.MODDED => "MODDED",
                var _ => levelName,
            };

            var animationCurve = new AnimationCurve(keyFrames.ToArray());

            levelSpawnCurveDictionary[levelName] = animationCurve;
        }

        return true;
    }

    private static bool ReadSpawnCurveString(string spawnCurveString, List<Keyframe> keyFrames) {
        foreach (var keyframeString in spawnCurveString.Split(",", StringSplitOptions.RemoveEmptyEntries)) {
            var keyframe = keyframeString.Split(':', StringSplitOptions.RemoveEmptyEntries);

            if (keyframe.Length <= 1)
                try {
                    throw new InvalidDataException($"'{spawnCurveString}' is invalid! Point: {keyframeString}");
                } catch (Exception exception) {
                    TestAccountCore.Logger.LogError($"Map Hazard spawn curve is not correctly configured: {exception.Message}");

                    exception.LogDetailed();
                    return false;
                }

            var parsedTime = float.TryParse(keyframe[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var time);

            if (!parsedTime) {
                try {
                    throw new InvalidDataException($"'{spawnCurveString}' is invalid!"
                                                 + $" Point: {keyframeString} (Could not parse time '{keyframe[0]}' as float)");
                } catch (Exception exception) {
                    TestAccountCore.Logger.LogError($"Map Hazard spawn curve is not correctly configured: {exception.Message}");

                    exception.LogDetailed();
                    return false;
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
                    return false;
                }
            }


            keyFrames.Add(new(time, amount));
        }

        return true;
    }
}