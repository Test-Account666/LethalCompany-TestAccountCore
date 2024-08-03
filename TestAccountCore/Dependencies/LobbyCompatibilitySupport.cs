using LobbyCompatibility.Features;
using TestAccountCore.Dependencies.Compatibility;

namespace TestAccountCore.Dependencies;

public static class LobbyCompatibilitySupport {
    internal static void Initialize() =>
        Initialize(MyPluginInfo.PLUGIN_GUID, new(MyPluginInfo.PLUGIN_VERSION), CompatibilityLevel.ClientOnly,
                   VersionStrictness.Minor);

    public static void Initialize(string pluginGuid, string pluginVersion, CompatibilityLevel compatibilityLevel,
                                  VersionStrictness versionStrictness) =>
        PluginHelper.RegisterPlugin(pluginGuid, new(pluginVersion), compatibilityLevel.Convert(), versionStrictness.Convert());
}