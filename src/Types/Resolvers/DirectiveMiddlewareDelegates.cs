using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    public delegate Task OnBeforeInvokeResolverAsync(
        IResolverContext resolverContext,
        IDirective directiveContext,
        CancellationToken cancellationToken);

    public delegate Task<object> OnAfterInvokeResolverAsync(
        IResolverContext resolverContext,
        IDirective directiveContext,
        object resolverResult,
        CancellationToken cancellationToken);

    public delegate Task<object> OnInvokeResolverAsync(
        IResolverContext resolverContext,
        IDirective directiveContext,
        CancellationToken cancellationToken);
}
