using Prometheus.Resolvers;

namespace Prometheus.Introspection
{
    internal static class IntrospectionResolverConfiguration
    {
        public static IResolverBuilder AddIntrospectionResolvers(this IResolverBuilder resolverBuilder)
        {
            return resolverBuilder.Add("Query", "__schema",
                c => IntrospectionResolvers.GetSchema());
        }
    }
}