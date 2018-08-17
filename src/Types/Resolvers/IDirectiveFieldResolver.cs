using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Resolvers
{
    public interface IDirectiveFieldResolver
    {
        Task<object> ResolveAsync(
            IDirectiveContext directiveContext,
            IResolverContext resolverContext,
            CancellationToken cancellationToken);
    }

    public interface IDirectiveFieldResolverHandler // TODO: naming is not quite good
    {
        void OnBeforeInvoke(
            IDirectiveContext directiveContext,
            IResolverContext context);

        Task<object> OnAfterInvokeAsync(
            IDirectiveContext directiveContext,
            IResolverContext context,
            object resolverResult,
            CancellationToken cancellationToken);
    }
}
