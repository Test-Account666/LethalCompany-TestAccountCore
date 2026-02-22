using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TestAccountCore.Dependencies;

namespace TestAccountCore;

[BepInDependency("BMX.LobbyCompatibility", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("com.github.teamxiaolan.dawnlib", BepInDependency.DependencyFlags.SoftDependency)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class TestAccountCore : BaseUnityPlugin {
    internal static Harmony? harmony;
    internal new static ManualLogSource Logger { get; private set; } = null!;

    private void Awake() {
        Logger = base.Logger;

        harmony ??= new(MyPluginInfo.PLUGIN_GUID);

        if (DependencyChecker.IsDawnLibInstalled()) {
            harmony.PatchAll(typeof(HallwayHazardRegistry));
            harmony.PatchAll(typeof(SpawnTheUnlockablesGodDammit));
        }

        if (DependencyChecker.IsLobbyCompatibilityInstalled()) {
            Logger.LogInfo("Found LobbyCompatibility Mod, initializing support :)");
            LobbyCompatibilitySupport.Initialize();
        }

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    /*TODO:
       List<NamespacedConfigWeight> Moons = NamespacedConfigWeight.ConvertManyFromString(Config.MoonSpawnWeights.Value);
       List<NamespacedConfigWeight> Interiors = NamespacedConfigWeight.ConvertManyFromString(Config.InteriorSpawnWeights.Value);
       List<NamespacedConfigWeight> Weathers = NamespacedConfigWeight.ConvertManyFromString(Config.WeatherSpawnWeights.Value);
       SpawnWeights.SetupSpawnWeightsPreset(Moons, Interiors, Weathers);
     */
}