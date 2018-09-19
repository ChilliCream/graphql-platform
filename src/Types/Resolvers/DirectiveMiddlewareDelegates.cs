using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Resolvers
{
    public delegate void OnBeforeInvokeResolver(
        IDirectiveContext directiveContext,
        IResolverContext resolverContext,
        CancellationToken cancellationToken);

    public delegate object OnAfterInvokeResolver(
        IDirectiveContext directiveContext,
        IResolverContext resolverContext,
        object resolverResult,
        CancellationToken cancellationToken);

    public delegate Task OnBeforeInvokeResolverAsync(
        IDirectiveContext directiveContext,
        IResolverContext resolverContext,
        CancellationToken cancellationToken);

    public delegate Task<object> OnAfterInvokeResolverAsync(
        IDirectiveContext directiveContext,
        IResolverContext resolverContext,
        object resolverResult,
        CancellationToken cancellationToken);

    public delegate Task<object> AsyncDirectiveResolver(
        IDirectiveContext directiveContext,
        IResolverContext resolverContext,
        CancellationToken cancellationToken);

    public delegate object DirectiveResolver(
        IDirectiveContext directiveContext,
        IResolverContext resolverContext,
        CancellationToken cancellationToken);
}
