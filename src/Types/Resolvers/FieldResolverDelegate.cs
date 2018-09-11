using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Resolvers
{
    public delegate Task<object> AsyncFieldResolverDelegate(
        IResolverContext context,
        CancellationToken cancellationToken);

    public delegate object FieldResolverDelegate(
        IResolverContext context,
        CancellationToken cancellationToken);

    public delegate Task<object> AsyncDirectiveFieldResolverDelegate(
        IDirectiveContext directiveContext,
        IResolverContext resolverContext,
        CancellationToken cancellationToken);

    public delegate object DirectiveFieldResolverDelegate(
        IDirectiveContext directiveContext,
        IResolverContext resolverContext,
        CancellationToken cancellationToken);
}
