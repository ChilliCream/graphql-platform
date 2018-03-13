using System.Threading;
using System.Threading.Tasks;

namespace Prometheus.Resolvers
{
    public interface IBatchedQuery
    {
        Task ExecuteAsync(CancellationToken cancellationToken);
    }
}