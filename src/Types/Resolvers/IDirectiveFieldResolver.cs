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


}
