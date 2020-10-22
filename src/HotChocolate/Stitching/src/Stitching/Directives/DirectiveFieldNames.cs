namespace HotChocolate.Stitching
{
    internal static class DirectiveFieldNames
    {
        public static NameString Source_Schema { get; } = "schema";

        public static NameString Source_Name { get; } = "name";

        public static NameString Delegate_Schema { get; } = "schema";

        public static NameString Delegate_Path { get; } = "path";

        public static NameString Computed_DependantOn { get; } = "dependantOn";

        public static NameString RemoveType_TypeName { get; } = "typeName";

        public static NameString RenameType_TypeName { get; } = "typeName";

        public static NameString RenameType_NewTypeName { get; } = "newTypeName";

        public static NameString RenameField_TypeName { get; } = "typeName";

        public static NameString RenameField_FieldName { get; } = "fieldName";

        public static NameString RenameField_NewFieldName { get; } = "newFieldName";
    }
}
