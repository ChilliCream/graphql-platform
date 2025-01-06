namespace HotChocolate.Fusion.Types;

internal static class FusionTypes
{
    public const string FieldDefinition = "fusion__FieldDefinition";
    public const string FieldSelectionMap = "fusion__FieldSelectionMap";
    public const string FieldSelectionSet = "fusion__FieldSelectionSet";

    public const string Type = "fusion__type";
    public const string Field = "fusion__field";
    public const string InputField = "fusion__inputField";
    public const string Requires = "fusion__requires";
    public const string Lookup = "fusion__lookup";

    public static bool IsBuiltInType(string typeName)
        => typeName is FieldDefinition or FieldSelectionMap or FieldSelectionSet;

    public static bool IsBuiltInDirective(string directiveName)
        => directiveName is Type or Field or InputField or Requires or Lookup;
}
