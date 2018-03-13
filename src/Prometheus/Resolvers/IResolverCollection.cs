using System;

namespace Prometheus.Resolvers
{
    public interface IResolverCollection
    {
        bool TryGetResolver(
            string typeName, string fieldName,
            out ResolverDelegate resolver);
    }
}
