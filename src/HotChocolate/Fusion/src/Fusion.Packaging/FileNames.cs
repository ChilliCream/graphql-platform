namespace HotChocolate.Fusion.Packaging;

internal static class FileNames
{
    private const string GatewaySchemaFormat = "gateway/{0}/gateway.graphqls";
    private const string GatewaySettingsFormat = "gateway/{0}/gateway-settings.json";
    private const string SourceSchemaFormat = "source-schemas/{0}/schema.graphqls";
    private const string SourceSchemaExtensionsFormat = "source-schemas/{0}/schema-extensions.graphqls";
    private const string SourceSchemaSettingsFormat = "source-schemas/{0}/schema-settings.json";
    private const string RegoPolicyFormat = "policies/rego/{0}/{1}.rego";
    private const string RegoPolicyRequirementsFormat = "policies/rego/{0}/{1}.graphql";

    public const string RegoPolicies = "policies/rego/";
    public const string ArchiveMetadata = "archive-metadata.json";
    public const string CompositionSettings = "composition-settings.json";
    public const string SignatureManifest = ".signature/manifest.json";
    public const string Signature = ".signature/signature.p7s";
    public const string LegacyArchive = "legacy-v1-archive.fgp";

    public static string GetGatewaySchemaPath(Version version)
        => string.Format(GatewaySchemaFormat, version);

    public static string GetGatewaySettingsPath(Version version)
        => string.Format(GatewaySettingsFormat, version);

    public static string GetSourceSchemaPath(string schemaName)
        => string.Format(SourceSchemaFormat, schemaName);

    public static string GetSourceSchemaExtensionsPath(string schemaName)
        => string.Format(SourceSchemaExtensionsFormat, schemaName);

    public static string GetSourceSchemaSettingsPath(string schemaName)
        => string.Format(SourceSchemaSettingsFormat, schemaName);

    public static string GetRegoPolicyPath(Version version, string policyName)
        => string.Format(RegoPolicyFormat, version, policyName);

    public static string GetRegoPolicyRequirementsPath(Version version, string policyName)
        => string.Format(RegoPolicyRequirementsFormat, version, policyName);

    public static FileKind GetFileKind(string fileName)
    {
        switch (Path.GetFileName(fileName))
        {
            case "gateway.graphqls":
            case "schema.graphqls":
            case "schema-extensions.graphqls":
            case var name when name.EndsWith(".graphql", StringComparison.Ordinal):
                return FileKind.Schema;

            case var name when name.EndsWith(".rego", StringComparison.Ordinal):
                return FileKind.Policy;

            case "schema-settings.json":
            case "gateway-settings.json":
            case "composition-settings.json":
                return FileKind.Settings;

            case "archive-metadata.json":
                return FileKind.Metadata;

            case "manifest.json":
                return FileKind.Manifest;

            case "signature.json":
                return FileKind.Signature;

            case "legacy-v1-archive.fgp":
                return FileKind.LegacyArchive;

            default:
                return FileKind.Settings;
        }
    }
}
