using System.Collections.Generic;
using System.Linq;

namespace TestAccountCore;

public static class ConfigParser {
    public static Dictionary<string, int> ParseConfig(this string configMoonRarity, string itemName) {
        Dictionary<string, int> spawnRateByCustomLevelType = [
        ];
        configMoonRarity = configMoonRarity.ToLower().Replace("modded:", "custom:");

        foreach (var entry in configMoonRarity.Split(',').Select(configEntry => configEntry.Trim())) {
            if (string.IsNullOrWhiteSpace(entry)) continue;

            var entryParts = entry.Split(':');
            if (entryParts.Length != 2) continue;

            var name = entryParts[0];
            if (!int.TryParse(entryParts[1], out var spawnWeight)) {
                TestAccountCore.Logger.LogWarning($"Invalid spawn weight for {name}: {entry}");
                continue;
            }

            spawnRateByCustomLevelType[name] = spawnWeight;
            TestAccountCore.Logger.LogInfo($"Registered {itemName}'s weight for custom level type {name} to {spawnWeight}");
        }

        return spawnRateByCustomLevelType;
    }
}