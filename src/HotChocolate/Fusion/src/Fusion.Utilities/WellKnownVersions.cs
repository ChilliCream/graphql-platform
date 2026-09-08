namespace HotChocolate.Fusion;

public static class WellKnownVersions
{
    public static readonly Version LatestGatewayFormatVersion = new(2, 0, 0);

    /// <summary>
    /// The manifest-indexed Rego bundle format: <c>policies/rego/2.0.0/manifest.json</c> plus
    /// package modules, shared library modules, and a single root data mount.
    /// </summary>
    public static readonly Version RegoPolicyBundleFormatVersion = new(2, 0, 0);

    /// <summary>
    /// The highest Rego policy format version this runtime understands. A runtime rejects an
    /// archive whose only Rego policy formats exceed this version instead of loading no policies.
    /// </summary>
    public static readonly Version LatestRegoPolicyFormatVersion = RegoPolicyBundleFormatVersion;

    public static readonly Version LatestSourceSchemaVersion = new(2, 0, 0);

    public static readonly Version LegacyGatewayFormatVersion = new(1, 0, 0);
}
