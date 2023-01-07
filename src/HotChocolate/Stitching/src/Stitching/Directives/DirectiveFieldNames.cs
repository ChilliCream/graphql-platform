namespace HotChocolate.Stitching;

internal static class DirectiveFieldNames
{
    public static string Source_Schema => "schema";

    public static string Source_Name => "name";

    public static string Delegate_Schema => "schema";

    public static string Delegate_Path => "path";

    public static string Computed_DependantOn => "dependantOn";

    public static string RemoveType_TypeName => "typeName";

    public static string RenameType_TypeName => "typeName";

    public static string RenameType_NewTypeName => "newTypeName";

    public static string RenameField_TypeName => "typeName";

    public static string RenameField_FieldName => "fieldName";

    public static string RenameField_NewFieldName => "newFieldName";
}
