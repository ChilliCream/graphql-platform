using System.Threading;
using System.Threading.Tasks;

namespace Zeus
{
    public interface IResolver<TResult>
        : IResolver
    {
        new Task<TResult> ResolveAsync(IResolverContext context, CancellationToken cancellationToken);
    }


}
