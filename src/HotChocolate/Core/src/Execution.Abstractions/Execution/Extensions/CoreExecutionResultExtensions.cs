using static HotChocolate.Execution.ExecutionResultKind;
using static HotChocolate.ExecutionAbstractionsResources;

namespace HotChocolate.Execution;

/// <summary>
/// Helper methods for <see cref="IExecutionResult"/>.
/// </summary>
public static class CoreExecutionResultExtensions
{
    extension(IExecutionResult? result)
    {
        /// <summary>
        /// Expects a single GraphQL operation result.
        /// </summary>
        public OperationResult ExpectOperationResult()
        {
            if (result is OperationResult qr)
            {
                return qr;
            }

            throw new ArgumentException(ExecutionResultExtensions_ExpectOperationResult_NotOperationResult);
        }

        /// <summary>
        /// Expects a batch of operation results.
        /// </summary>
        public OperationResultBatch ExpectOperationResultBatch()
        {
            if (result is OperationResultBatch qr)
            {
                return qr;
            }

            throw new ArgumentException(ExecutionResultExtensions_ExpectOperationResultBatch_NotOperationResultBatch);
        }

        /// <summary>
        /// Expect a stream result.
        /// </summary>
        public ResponseStream ExpectResponseStream()
        {
            if (result is ResponseStream rs)
            {
                return rs;
            }

            throw new ArgumentException(ExecutionResultExtensions_ExpectResponseStream_NotResponseStream);
        }
    }

    extension(IExecutionResult result)
    {
        /// <summary>
        /// Registers a cleanup task for execution resources bound to this execution result.
        /// </summary>
        /// <param name="clean">
        /// A cleanup task that will be executed when this result is disposed.
        /// </param>
        public void RegisterForCleanup(Action clean)
        {
            ArgumentNullException.ThrowIfNull(clean);

            result.RegisterForCleanup(() =>
            {
                clean();
                return default;
            });
        }

        /// <summary>
        /// Registers a resource that needs to be disposed when the result is being disposed.
        /// </summary>
        /// <param name="disposable">
        /// The resource that needs to be disposed.
        /// </param>
        public void RegisterForCleanup(IDisposable disposable)
        {
            ArgumentNullException.ThrowIfNull(disposable);

            result.RegisterForCleanup(disposable.Dispose);
        }

        /// <summary>
        /// Registers a resource that needs to be disposed when the result is being disposed.
        /// </summary>
        /// <param name="disposable">
        /// The resource that needs to be disposed.
        /// </param>
        public void RegisterForCleanup(IAsyncDisposable disposable)
        {
            ArgumentNullException.ThrowIfNull(disposable);

            result.RegisterForCleanup(disposable.DisposeAsync);
        }

        /// <summary>
        /// Defines if the specified <see cref="IExecutionResult"/> is a response stream.
        /// </summary>
        /// <returns>
        /// A boolean that specifies if the <see cref="IExecutionResult"/> is a response stream.
        /// </returns>
        public bool IsStreamResult()
            => result.Kind is BatchResult or DeferredResult or SubscriptionResult;
    }
}
