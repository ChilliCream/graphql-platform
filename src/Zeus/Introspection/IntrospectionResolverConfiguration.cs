using Zeus.Resolvers;

namespace Zeus.Introspection
{
    internal static class IntrospectionResolverConfiguration
    {
        public static IResolverBuilder AddIntrospectionResolvers(this IResolverBuilder resolverBuilder)
        {
            return resolverBuilder.Add("Query", "__schema",
                c => IntrospectionResolvers.GetSchema(c));
        }
    }
}