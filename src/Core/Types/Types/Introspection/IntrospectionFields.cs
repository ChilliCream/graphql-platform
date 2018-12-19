namespace HotChocolate.Types.Introspection
{
    internal static class IntrospectionFields
    {
        public static NameString TypeName { get; } = "__typename";
        public static NameString Schema { get; } = "__schema";
        public static NameString Type { get; } = "__type";
    }
}
