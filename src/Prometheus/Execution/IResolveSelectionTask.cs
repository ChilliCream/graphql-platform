using System.Threading;
using System.Threading.Tasks;
using Prometheus.Abstractions;
using Prometheus.Resolvers;

namespace Prometheus.Execution
{
    internal interface IResolveSelectionTask
    {
        IResolverContext Context { get; }

        IOptimizedSelection Selection { get; }

        object Result { get; }

        Task ExecuteAsync(CancellationToken cancellationToken);

        void IntegrateResult(object result);
    }
}