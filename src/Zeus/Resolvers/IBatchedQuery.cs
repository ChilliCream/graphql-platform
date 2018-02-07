using System.Threading;
using System.Threading.Tasks;

namespace Zeus.Resolvers
{
    public interface IBatchedQuery
    {
        Task ExecuteAsync(CancellationToken cancellationToken);
    }
}