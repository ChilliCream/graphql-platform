using System.Threading.Tasks;

namespace StrawberryShake
{
    public interface IOperationExecutor
    {
        Task<IOperationResult> ExecuteAsync(IOperation operation);

        Task<IOperationResult<T>> ExecuteAsync<T>(IOperation<T> operation);
    }
}
