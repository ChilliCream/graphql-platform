using System;
using System.Threading;
using Prometheus.Abstractions;

namespace Prometheus.Resolvers
{
    public interface IResolverBuilder
    {
        IResolverBuilder Add(ResolverFactoryDelegate resolverFactory);
        IResolverCollection Build(ISchemaDocument schema, IServiceProvider serviceProvider);
    }
}