namespace Mocha.Transport.RabbitMQ;

internal static class RequestTaskExtensions
{
    /// <summary>
    /// Waits for a request to the broker, and stops waiting when <paramref name="cancellationToken"/> is
    /// cancelled. RabbitMQ.Client does not end a request with a token once the request is sent, so the
    /// abandoned request stays in flight, and its channel must not be used again.
    /// </summary>
    public static Task WaitOrAbandonAsync(this Task request, CancellationToken cancellationToken)
    {
        return request.IsCompleted || !cancellationToken.CanBeCanceled
            ? request
            : WaitAsync(request, cancellationToken);

        static async Task WaitAsync(Task request, CancellationToken cancellationToken)
        {
            try
            {
                await request.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                ObserveFailure(request);
                throw;
            }
        }
    }

    /// <inheritdoc cref="WaitOrAbandonAsync(Task, CancellationToken)" />
    public static Task<T> WaitOrAbandonAsync<T>(this Task<T> request, CancellationToken cancellationToken)
    {
        return request.IsCompleted || !cancellationToken.CanBeCanceled
            ? request
            : WaitAsync(request, cancellationToken);

        static async Task<T> WaitAsync(Task<T> request, CancellationToken cancellationToken)
        {
            try
            {
                return await request.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                ObserveFailure(request);
                throw;
            }
        }
    }

    // A failure of the abandoned request is read, so it does not surface as an unobserved task exception.
    private static void ObserveFailure(Task request)
        => request.ContinueWith(
            static task => _ = task.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
}
