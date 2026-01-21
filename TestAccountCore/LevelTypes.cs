using System.Collections.Generic;

namespace TestAccountCore;

public enum LevelTypes {
    ALL = 0,
    VANILLA = 1,
    MODDED = 2,
    UNKNOWN = 3,
}

public static class VanillaLevelMatcher {
    public static readonly List<string> VanillaLevels = [
        "EXPERIMENTATION", "ASSURANCE", "VOW", "GORDION", "MARCH", "ADAMANCE", "REND", "DINE", "OFFENSE", "TITAN", "ARTIFICE",
        "LIQUIDATION", "EMBRION",
    ];

    public static bool IsVanilla(string level) => VanillaLevels.Contains(level.ToUpper());
}

public static class LevelTypeMatcher {
    public static LevelTypes FromString(string levelType) {
        if ("all".StartsWith(levelType.ToLower())) return LevelTypes.ALL;
        if ("vanilla".StartsWith(levelType.ToLower())) return LevelTypes.VANILLA;
        if ("modded".StartsWith(levelType.ToLower())) return LevelTypes.MODDED;

        return "custom".StartsWith(levelType.ToLower())? LevelTypes.MODDED : LevelTypes.UNKNOWN;
    }
}