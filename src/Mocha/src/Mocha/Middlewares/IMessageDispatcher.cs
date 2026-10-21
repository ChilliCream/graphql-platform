namespace Mocha.Middlewares;

/// <summary>
/// Prepares outgoing messages through the messaging pipeline without delivering them.
/// </summary>
internal interface IMessageDispatcher
{
    /// <summary>
    /// Prepares a publication using the caller's messaging scope.
    /// The envelope is valid only during the callback.
    /// </summary>
    ValueTask PreparePublishAsync<T>(
        T message,
        PublishOptions options,
        Action<MessageEnvelope, CancellationToken> onPrepared,
        CancellationToken cancellationToken)
        where T : notnull;

    /// <summary>
    /// Prepares a directed message using the caller's messaging scope.
    /// The envelope is valid only during the callback.
    /// </summary>
    ValueTask PrepareSendAsync<T>(
        T message,
        SendOptions options,
        Action<MessageEnvelope, CancellationToken> onPrepared,
        CancellationToken cancellationToken)
        where T : notnull;
}
