using System;
using System.Threading;

namespace Zeus.Resolvers
{
    public interface IResolverBuilder
    {
        IResolverBuilder Add(
            string typeName, string fieldName,
            ResolverFactory resolverFactory);

        IResolverCollection Build();
    }
}