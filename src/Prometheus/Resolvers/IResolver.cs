using System;
using System.Threading;
using System.Threading.Tasks;
using Prometheus.Abstractions;

namespace Prometheus.Resolvers
{
    public delegate Task<object> ResolverDelegate(
        IResolverContext context,
        CancellationToken cancellationToken);

    public delegate void ResolverFactoryDelegate(
        ISchemaDocument schema,
        IServiceProvider serviceProvider,
        RegisterResolverDelegate registerResolver);

    public delegate void RegisterResolverDelegate(
        string typeName, string fieldName,
        ResolverDelegate resolverDelegate);

    public interface IResolver
    {
        Task<object> ResolveAsync(
            IResolverContext context,
            CancellationToken cancellationToken);
    }

    public interface IResolver<TResult>
        : IResolver
    {
        new Task<TResult> ResolveAsync(
            IResolverContext context,
            CancellationToken cancellationToken);
    }
}