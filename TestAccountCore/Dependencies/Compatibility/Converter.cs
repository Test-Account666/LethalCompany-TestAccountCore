namespace TestAccountCore.Dependencies.Compatibility;

internal static class Converter {
    internal static LobbyCompatibility.Enums.VersionStrictness Convert(this VersionStrictness versionStrictness) =>
        (LobbyCompatibility.Enums.VersionStrictness) versionStrictness;

    internal static LobbyCompatibility.Enums.CompatibilityLevel Convert(this CompatibilityLevel versionStrictness) =>
        (LobbyCompatibility.Enums.CompatibilityLevel) versionStrictness;
}