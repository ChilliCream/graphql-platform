using System.Threading.Tasks;

namespace StrawberryShake
{
    public interface IOperationStreamExecutor
    {
        Task<IResponseStream<T>> ExecuteAsync<T>(IOperation<T> operation)
            where T : class;
    }
}
