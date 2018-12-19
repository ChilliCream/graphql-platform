namespace HotChocolate.Types
{
    internal static class IntrospectionExtensions
    {
        public static bool IsIntrospectionType(this INamedType type)
        {
            return type.GetType().IsDefined(
                typeof(IntrospectionAttribute),
                false);
        }
    }
}
