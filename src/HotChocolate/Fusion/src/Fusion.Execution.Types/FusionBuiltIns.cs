namespace HotChocolate.Fusion.Types;

internal static class FusionBuiltIns
{
    private const string Prefix = "fusion__";

    public const string FieldDefinition = "fusion__FieldDefinition";
    public const string FieldSelectionMap = "fusion__FieldSelectionMap";
    public const string FieldSelectionSet = "fusion__FieldSelectionSet";
    public const string SelectionPath = "fusion__FieldSelectionPath";
    public const string NodeResolution = "fusion__NodeResolution";
    public const string Schema = "fusion__Schema";

    public const string Type = "fusion__type";
    public const string Field = "fusion__field";
    public const string InputField = "fusion__inputField";
    public const string Requires = "fusion__requires";
    public const string Lookup = "fusion__lookup";
    public const string Implements = "fusion__implements";
    public const string InterfaceObject = "fusion__interfaceObject";
    public const string UnionMember = "fusion__unionMember";
    public const string EnumValue = "fusion__enumValue";
    public const string Execution = "fusion__execution";
    public const string CostOptions = "fusion__costOptions";
    public const string LegacyCostOptions = "fusion__cost_options";
    public const string Inaccessible = "fusion__inaccessible";
    public const string GatewayField = "fusion__gatewayField";
    public const string LegacyGatewayField = "fusion__gateway_field";
    public const string SchemaMetadata = "fusion__schemaMetadata";
    public const string LegacySchemaMetadata = "fusion__schema_metadata";
    public const string EventStream = "fusion__eventStream";
    public const string Tag = "fusion__tag";

    public static bool IsBuiltInType(string typeName)
        => typeName.StartsWith(Prefix, StringComparison.Ordinal);

    public static bool IsBuiltInDirective(string directiveName)
        => directiveName.StartsWith(Prefix, StringComparison.Ordinal);

    public static bool IsCostOptionsDirective(string directiveName)
        => directiveName.Equals(CostOptions, StringComparison.Ordinal)
            || directiveName.Equals(LegacyCostOptions, StringComparison.Ordinal);

    public static bool IsGatewayFieldDirective(string directiveName)
        => directiveName.Equals(GatewayField, StringComparison.Ordinal)
            || directiveName.Equals(LegacyGatewayField, StringComparison.Ordinal);

    public static bool IsSchemaMetadataDirective(string directiveName)
        => directiveName.Equals(SchemaMetadata, StringComparison.Ordinal)
            || directiveName.Equals(LegacySchemaMetadata, StringComparison.Ordinal);
}
