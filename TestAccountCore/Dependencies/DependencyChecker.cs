using System.Linq;
using BepInEx.Bootstrap;

namespace TestAccountCore.Dependencies;

public static class DependencyChecker {
    public static bool IsLobbyCompatibilityInstalled() =>
        Chainloader.PluginInfos.Values.Any(metadata => metadata.Metadata.GUID.Contains("LobbyCompatibility"));

    public static bool IsLethalLibInstalled() =>
        Chainloader.PluginInfos.Values.Any(metadata => metadata.Metadata.GUID.ToLowerInvariant().Contains("lethallib"));

    public static bool IsDawnLibInstalled() =>
        Chainloader.PluginInfos.Values.Any(metadata => metadata.Metadata.GUID.ToLowerInvariant().Contains("dawnlib"));
}