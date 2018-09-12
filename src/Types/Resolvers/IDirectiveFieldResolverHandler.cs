using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Resolvers
{
    public interface IDirectiveFieldResolverHandler // TODO: naming is not quite good
    {
        Task OnBeforeInvokeAsync(
            IDirectiveContext directiveContext,
            IResolverContext context);


        Task<object> OnAfterInvokeAsync(
            IDirectiveContext directiveContext,
            IResolverContext context,
            object resolverResult,
            CancellationToken cancellationToken);
    }

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
