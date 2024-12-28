using System;
using System.Collections.Generic;
using System.Linq;
using LethalLib.Modules;

namespace TestAccountCore;

public static class ConfigParser {
    public static (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) ParseConfig(
        this string configMoonRarity, string enemyName) {
        Dictionary<Levels.LevelTypes, int> spawnRateByLevelType = [
        ];
        Dictionary<string, int> spawnRateByCustomLevelType = [
        ];

        foreach (var entry in configMoonRarity.Split(',').Select(configEntry => configEntry.Trim())) {
            if (string.IsNullOrWhiteSpace(entry)) continue;

            string[] entryParts = entry.Split(':');

            if (entryParts.Length != 2) continue;

            var name = entryParts[0];

            if (!int.TryParse(entryParts[1], out var spawnWeight)) continue;

            if (Enum.TryParse<Levels.LevelTypes>(name, true, out var levelType)) {
                spawnRateByLevelType[levelType] = spawnWeight;
                TestAccountCore.Logger.LogInfo($"Registered {enemyName}'s weight for level type {levelType} to {spawnWeight}");
                continue;
            }

            spawnRateByCustomLevelType[name] = spawnWeight;
            TestAccountCore.Logger.LogInfo($"Registered {enemyName}'s weight for custom level type {name} to {spawnWeight}");
        }

        return (spawnRateByLevelType, spawnRateByCustomLevelType);
    }
}