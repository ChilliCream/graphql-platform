namespace Mocha.Middlewares;

/// <summary>
/// Prepares outgoing messages through the messaging pipeline without delivering them.
/// </summary>
internal interface IMessageDispatcher
{
    /// <summary>
    /// Prepares a publication using the caller's messaging scope.
    /// </summary>
    ValueTask<PreparedMessage> PreparePublishAsync<T>(
        T message,
        PublishOptions options,
        CancellationToken cancellationToken)
        where T : notnull;

    /// <summary>
    /// Prepares a directed message using the caller's messaging scope.
    /// </summary>
    ValueTask<PreparedMessage> PrepareSendAsync<T>(
        T message,
        SendOptions options,
        CancellationToken cancellationToken)
        where T : notnull;
}
