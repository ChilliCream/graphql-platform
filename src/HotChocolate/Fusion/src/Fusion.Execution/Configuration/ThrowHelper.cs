namespace HotChocolate.Fusion.Configuration;

internal static class ThrowHelper
{
    public static InvalidOperationException UnsupportedExecutionSchemaVersion(
        Version executionSchemaVersion,
        Version maxSupportedExecutionSchemaVersion)
        => new(
            $"The Fusion archive requires execution schema version {executionSchemaVersion}, but this gateway supports up to {maxSupportedExecutionSchemaVersion}. Upgrade the gateway to load this archive.");
}
