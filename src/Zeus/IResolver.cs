using System.Threading;
using System.Threading.Tasks;

namespace Zeus
{
    public interface IResolver
    {
        Task<object> ResolveAsync(IResolverContext context, CancellationToken cancellationToken);
    }


}
