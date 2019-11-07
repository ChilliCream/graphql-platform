using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Http.Subscriptions
{
    public class WebSocketOperationStreamExecutor
        : IOperationStreamExecutor
    {
        public Task<IResponseStream> ExecuteAsync<T>(IOperation operation, CancellationToken cancellationToken) where T : class
        {
            throw new System.NotImplementedException();
        }

        public Task<IResponseStream<T>> ExecuteAsync<T>(IOperation<T> operation, CancellationToken cancellationToken) where T : class
        {
            throw new System.NotImplementedException();
        }
    }
}
