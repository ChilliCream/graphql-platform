using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zeus.Execution
{
    public interface IOperationExecuter
    {
        Task<IDictionary<string, object>> ExecuteAsync(
            IOptimizedOperation operation,
            IVariableCollection variables,
            object initialValue,
            CancellationToken cancellationToken);
    }
}