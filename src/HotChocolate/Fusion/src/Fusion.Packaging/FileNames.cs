namespace HotChocolate.Fusion.Packaging;

internal static class FileNames
{
    private const string GatewaySchemaFormat = "gateway/{0}/gateway.graphqls";
    private const string GatewaySettingsFormat = "gateway/{0}/gateway-settings.json";
    private const string SourceSchemaFormat = "source-schemas/{0}/schema.graphqls";
    private const string SourceSchemaSettingsFormat = "source-schemas/{0}/schema-settings.json";

    public const string ArchiveMetadata = "archive-metadata.json";
    public const string CompositionSettings = "composition-settings.json";
    public const string SignatureManifest = ".signature/manifest.json";
    public const string Signature = ".signature/signature.p7s";

    public static string GetGatewaySchemaPath(Version version)
        => string.Format(GatewaySchemaFormat, version);

    public static string GetGatewaySettingsPath(Version version)
        => string.Format(GatewaySettingsFormat, version);

    public static string GetSourceSchemaPath(string schemaName)
        => string.Format(SourceSchemaFormat, schemaName);

    public static string GetSourceSchemaSettingsPath(string schemaName)
        => string.Format(SourceSchemaSettingsFormat, schemaName);

    public static FileKind GetFileKind(string fileName)
    {
        switch (Path.GetFileName(fileName))
        {
            case "gateway.graphqls":
            case "schema.graphqls":
                return FileKind.Schema;

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

            default:
                return FileKind.Settings;
        }
    }
}
