using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public interface IOperationBatchExecutor
    {
        Task<IResponseStream> ExecuteAsync(IEnumerable<IOperation> batch);
    }
}
