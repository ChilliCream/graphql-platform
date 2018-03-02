using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zeus.Execution
{
    public interface IOperationExecuter
    {
        Task<IReadOnlyDictionary<string, object>> ExecuteAsync(
            IOptimizedOperation operation,
            IVariableCollection variables,
            object initialValue,
            CancellationToken cancellationToken);
    }
}