namespace HotChocolate.Stitching
{
    internal static class DirectiveFieldNames
    {
        public static string Source_Schema { get; } = "schema";

        public static string Source_Name { get; } = "name";

        public static string Delegate_Schema { get; } = "schema";

        public static string Delegate_Path { get; } = "path";

        public static string Computed_DependantOn { get; } = "dependantOn";

        public static string RemoveType_TypeName { get; } = "typeName";

        public static string RenameType_TypeName { get; } = "typeName";

        public static string RenameType_NewTypeName { get; } = "newTypeName";

        public static string RenameField_TypeName { get; } = "typeName";

        public static string RenameField_FieldName { get; } = "fieldName";

        public static string RenameField_NewFieldName { get; } = "newFieldName";
    }
}
