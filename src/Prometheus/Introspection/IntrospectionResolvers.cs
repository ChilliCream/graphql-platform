using Prometheus.Resolvers;

namespace Prometheus.Introspection
{
    internal static class IntrospectionResolvers
    {
        public static __Schema GetSchema()
        {
            return new __Schema();
        }

        public static __Type GetType(string name)
        {
            return null;
        }
    }
}