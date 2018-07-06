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
}
