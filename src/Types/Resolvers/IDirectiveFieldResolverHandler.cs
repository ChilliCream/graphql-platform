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
}
