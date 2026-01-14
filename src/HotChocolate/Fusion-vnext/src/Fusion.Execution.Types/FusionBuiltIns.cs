namespace HotChocolate.Fusion.Types;

internal static class FusionBuiltIns
{
    private const string Prefix = "fusion__";

    public const string FieldDefinition = "fusion__FieldDefinition";
    public const string FieldSelectionMap = "fusion__FieldSelectionMap";
    public const string FieldSelectionSet = "fusion__FieldSelectionSet";
    public const string SelectionPath = "fusion__FieldSelectionPath";
    public const string Schema = "fusion__Schema";

    public const string Type = "fusion__type";
    public const string Field = "fusion__field";
    public const string InputField = "fusion__inputField";
    public const string Requires = "fusion__requires";
    public const string Lookup = "fusion__lookup";
    public const string Implements = "fusion__implements";
    public const string UnionMember = "fusion__unionMember";
    public const string EnumValue = "fusion__enumValue";
    public const string Inaccessible = "fusion__inaccessible";
    public const string SchemaMetadata = "fusion__schema_metadata";

    public static bool IsBuiltInType(string typeName)
        => typeName.StartsWith(Prefix, StringComparison.Ordinal);

    public static bool IsBuiltInDirective(string directiveName)
        => directiveName.StartsWith(Prefix, StringComparison.Ordinal);
}
