using System.Collections.Generic;
using HarmonyLib;

namespace TestAccountCore;

[HarmonyPatch(typeof(StartOfRound))]
public static class SpawnTheUnlockablesGodDammit {
    public static List<UnlockableItem> AllUnlockedItems = [];

    [HarmonyPatch(nameof(StartOfRound.LoadUnlockables))]
    [HarmonyPrefix]
    public static void SpawnFuckingUnlockables() => DoYourWorst();

    [HarmonyPatch(nameof(StartOfRound.ResetShipFurniture))]
    [HarmonyPostfix]
    public static void RespawnTheGodDamnUnlockables() => DoYourWorst();

    private static void DoYourWorst() {
        var instance = StartOfRound.Instance;
        if (!instance || (!instance.IsHost && !instance.IsServer)) return;

        var dontYouDareExplodeList = instance.unlockablesList.unlockables;

        for (var index = 0; index < dontYouDareExplodeList.Count; index++) {
            var iHateThisUnlockable = dontYouDareExplodeList[index];
            if (!AllUnlockedItems.Contains(iHateThisUnlockable)) continue;
            if (iHateThisUnlockable.hasBeenUnlockedByPlayer || iHateThisUnlockable.alreadyUnlocked) continue;
            if (instance.SpawnedShipUnlockables.ContainsKey(index)) continue;

            instance.SpawnUnlockable(index, false);
            iHateThisUnlockable.hasBeenUnlockedByPlayer = true;
        }
    }
}