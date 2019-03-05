namespace HotChocolate.Stitching
{
    internal static class DirectiveFieldNames
    {
        public static NameString Source_Schema { get; } = "schema";

        public static NameString Source_Name { get; } = "name";

        public static NameString Delegate_Schema { get; } = "schema";

        public static NameString Delegate_Path { get; } = "path";

        public static NameString Computed_DependantOn { get; } = "dependantOn";
    }
}
