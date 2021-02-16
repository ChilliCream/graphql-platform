namespace HotChocolate.Types
{
    internal static class IntrospectionExtensions
    {
        public static bool IsIntrospectionType(this IType type)
        {
            return type.GetType().IsDefined(typeof(IntrospectionAttribute), false);
        }
    }
}
