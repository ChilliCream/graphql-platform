using Zeus.Resolvers;

namespace Zeus.Introspection
{
    internal static class IntrospectionResolvers
    {
        public static __Schema GetSchema(IResolverContext context)
        {
            return new __Schema(context.Schema);
        }

        public static __Type GetType(string name)
        {
            return null;
        }
    }
}