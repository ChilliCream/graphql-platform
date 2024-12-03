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
    {
        if(typeName == FieldDefinition ||
            typeName == FieldSelectionMap ||
            typeName == FieldSelectionSet)
        {
            return true;
        }

        return false;
    }

    public static bool IsBuiltInDirective(string directiveName)
    {
        if(directiveName == Type ||
            directiveName == Field ||
            directiveName == InputField ||
            directiveName == Requires ||
            directiveName == Lookup)
        {
            return true;
        }

        return false;
    }
}
