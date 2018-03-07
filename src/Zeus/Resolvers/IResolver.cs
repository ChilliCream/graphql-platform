using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zeus.Resolvers
{
    public delegate Task<object> ResolverDelegate(
        IResolverContext context,
        CancellationToken cancellationToken);

    public delegate ResolverDelegate ResolverFactory(IServiceProvider serviceProvider);

    public interface IResolver
    {
        Task<object> ResolveAsync(IResolverContext context, CancellationToken cancellationToken);
    }

    public interface IResolver<TResult>
        : IResolver
    {
        new Task<TResult> ResolveAsync(IResolverContext context, CancellationToken cancellationToken);
    }
}
