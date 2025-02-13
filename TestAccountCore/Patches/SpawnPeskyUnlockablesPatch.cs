using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace TestAccountCore.Patches;

[HarmonyPatch(typeof(StartOfRound))]
public static class SpawnPeskyUnlockablesPatch {
    internal static readonly HashSet<UnlockableItem> AlwaysUnlockedItems = [
    ];

    private static readonly HashSet<int> _SpawnedUnlockables = [
    ];

    [HarmonyPatch(nameof(StartOfRound.Awake))]
    [HarmonyPostfix]
    public static void ResetSpawnedUnlockablesList() => _SpawnedUnlockables.Clear();

    [HarmonyPatch(nameof(StartOfRound.LoadUnlockables))]
    [HarmonyPostfix]
    public static void SpawnPeskyUnlockables() {
        var unlockablesList = StartOfRound.Instance.unlockablesList.unlockables;

        for (var index = 0; index < unlockablesList.Count; index++) {
            var unlockable = unlockablesList[index];

            if (!AlwaysUnlockedItems.Contains(unlockable) || _SpawnedUnlockables.Contains(index)) continue;

            StartOfRound.Instance.SpawnUnlockable(index);
        }
    }

    [HarmonyPatch(nameof(StartOfRound.SpawnUnlockable))]
    [HarmonyPostfix]
    public static void AddUnlockableToSpawnedList(int unlockableIndex) => _SpawnedUnlockables.Add(unlockableIndex);
}