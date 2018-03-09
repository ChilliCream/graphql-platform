using System;
using System.Threading;
using Zeus.Abstractions;

namespace Zeus.Resolvers
{
    public interface IResolverBuilder
    {
        IResolverBuilder Add(ResolverFactoryDelegate resolverFactory);
        IResolverCollection Build(ISchemaDocument schema, IServiceProvider serviceProvider);
    }
}