namespace Mocha.Middlewares;

/// <summary>
/// Dispatches outgoing messages through the messaging pipeline to a supplied store.
/// </summary>
internal interface IMessageDispatcher
{
    /// <summary>
    /// Publishes a message to the supplied store using the caller's messaging scope.
    /// </summary>
    ValueTask PublishAsync<T>(
        T message,
        PublishOptions options,
        IDispatchMessageStore store,
        CancellationToken cancellationToken)
        where T : notnull;

    /// <summary>
    /// Sends a message to the supplied store using the caller's messaging scope.
    /// </summary>
    ValueTask SendAsync<T>(
        T message,
        SendOptions options,
        IDispatchMessageStore store,
        CancellationToken cancellationToken)
        where T : notnull;
}
