using System.Threading;
using System.Threading.Tasks;
using Zeus.Abstractions;
using Zeus.Resolvers;

namespace Zeus.Execution
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