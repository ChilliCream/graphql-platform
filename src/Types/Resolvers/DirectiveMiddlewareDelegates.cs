using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Resolvers
{
    public delegate Task OnBeforeInvokeResolverAsync(
        IResolverContext resolverContext,
        IDirectiveContext directiveContext,
        CancellationToken cancellationToken);

    public delegate Task<object> OnAfterInvokeResolverAsync(
        IResolverContext resolverContext,
        IDirectiveContext directiveContext,
        object resolverResult,
        CancellationToken cancellationToken);

    public delegate Task<object> AsyncDirectiveResolver(
        IResolverContext resolverContext,
        IDirectiveContext directiveContext,
        CancellationToken cancellationToken);
}
