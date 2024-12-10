using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TestAccountCore.Dependencies;

namespace TestAccountCore;

[BepInDependency("BMX.LobbyCompatibility", BepInDependency.DependencyFlags.SoftDependency)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class TestAccountCore : BaseUnityPlugin {
    internal static Harmony? harmony;
    internal new static ManualLogSource Logger { get; private set; } = null!;

    private void Awake() {
        Logger = base.Logger;

        harmony ??= new(MyPluginInfo.PLUGIN_GUID);

        harmony.PatchAll(typeof(MapHazardRegistry));

        if (DependencyChecker.IsLobbyCompatibilityInstalled()) {
            Logger.LogInfo("Found LobbyCompatibility Mod, initializing support :)");
            LobbyCompatibilitySupport.Initialize();
        }

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }
}