namespace Mocha;

/// <summary>
/// Helpers for dispatching a <see cref="MessageBatch{TEvent}"/> to its handler.
/// </summary>
internal static class MessageBatchExtensions
{
    /// <summary>
    /// Creates a token source that is cancelled when <paramref name="processorToken"/> or the
    /// receive context of any message in the batch is cancelled.
    /// </summary>
    public static CancellationTokenSource LinkCancellation<TEvent>(
        this MessageBatch<TEvent> batch,
        CancellationToken processorToken)
    {
        var first = batch.Entries[0].Context.CancellationToken;
        List<CancellationToken>? others = null;

        for (var i = 1; i < batch.Count; i++)
        {
            var token = batch.Entries[i].Context.CancellationToken;
            if (token != first && others?.Contains(token) != true)
            {
                (others ??= []).Add(token);
            }
        }

        if (others is null)
        {
            return CancellationTokenSource.CreateLinkedTokenSource(processorToken, first);
        }

        others.Add(processorToken);
        others.Add(first);
        return CancellationTokenSource.CreateLinkedTokenSource([.. others]);
    }
}
