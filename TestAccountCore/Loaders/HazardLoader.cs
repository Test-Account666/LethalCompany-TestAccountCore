using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using Dawn;
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

        var section = $"{hazard.hazardName} - Hazard";

        var canHazardSpawn = configFile.Bind(section, "1. Enabled", true, $"If false, {hazard.hazardName} will not be registered.");

        if (!canHazardSpawn.Value) return;

        TestAccountCore.Logger.LogInfo($"Registering hazard {hazard.hazardName}...");

        if (string.IsNullOrWhiteSpace(hazard.spawnCurve)) {
            TestAccountCore.Logger.LogWarning($"Hazard {hazard.hazardName} is still using spawn amount! This is no longer supported!");
            return;
        }

        var defaultCurve = $"Vanilla - {hazard.spawnCurve} ; Modded - {hazard.spawnCurve}";

        var spawnCurveString = configFile.Bind(section, "2. Spawn Curve", defaultCurve,
            $"The spawn curve for {hazard.hazardName}. "
            + $"First number is between 0 and 1. The second one is the max amount.").Value;

        spawnCurveString = spawnCurveString.ToUpper();

        if (!spawnCurveString.Contains("-")) {
            TestAccountCore.Logger.LogWarning(
                $"Looks like you didn't specify any moons for {hazard.hazardName}! Defaulting to 'All'! A valid example: '{
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

        var namespacedKey = NamespacedKey<DawnMapObjectInfo>.From("testaccountcore", "hazard" + hazard.hazardName.ToLower());

        DawnLib.DefineMapObject(namespacedKey, hazard.spawnableMapObject!,
            infoBuilder => {
                infoBuilder.DefineInside(insideBuilder => {
                    insideBuilder.OverrideDisallowSpawningNearEntrances(hazard.disallowSpawningNearEntrances);
                    insideBuilder.OverrideRequireDistanceBetweenSpawns(hazard.requireDistanceBetweenSpawns);
                    insideBuilder.OverrideSpawnFacingAwayFromWall(hazard.spawnFacingAwayFromWall);
                    insideBuilder.OverrideSpawnFacingWall(hazard.spawnFacingWall);
                    insideBuilder.OverrideSpawnWithBackFlushAgainstWall(hazard.spawnWithBackFlushAgainstWall);
                    insideBuilder.OverrideSpawnWithBackToWall(hazard.spawnWithBackToWall);

                    insideBuilder.SetWeights(curveBuilder => {
                        foreach (var (moon, weight) in levelSpawnCurveDictionary) {
                            if (moon is null) continue;
                            if (moon.Equals("ALL")) {
                                curveBuilder.SetGlobalCurve(weight);
                                continue;
                            }

                            var foundKey = NamespacedKey.ForceParse(moon, true);
                            if (foundKey == null!) {
                                TestAccountCore.Logger.LogError($"Could not parse key {moon} for hazard {hazard.hazardName}");
                                continue;
                            }

                            if (foundKey is not NamespacedKey<DawnMoonInfo> moonKey) {
                                curveBuilder.AddTagCurve(foundKey, weight);
                                continue;
                            }

                            curveBuilder.AddCurve(moonKey, weight);
                        }
                    });
                });
            });

        DawnLib.RegisterNetworkPrefab(hazard.spawnableMapObject);

        hazard.isRegistered = true;

        TestAccountCore.Logger.LogInfo($"Fully registered hazard {hazard.hazardName}!");
    }

    private static bool FillLevelSpawnCurveList(string defaultCurve, string spawnCurveString,
        Dictionary<string, AnimationCurve> levelSpawnCurveDictionary) {
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
                _ => levelName,
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