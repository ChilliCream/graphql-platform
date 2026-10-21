namespace Mocha.Middlewares;

/// <summary>
/// Prepares outgoing messages through the messaging pipeline for an explicit destination.
/// </summary>
public interface IMessageDispatcher
{
    /// <summary>
    /// Prepares a publication for the specified target using the caller's messaging scope.
    /// </summary>
    ValueTask PublishAsync<T>(
        T message,
        PublishOptions options,
        IMessageDispatchTarget target,
        CancellationToken cancellationToken)
        where T : notnull;

    /// <summary>
    /// Prepares a directed message for the specified target using the caller's messaging scope.
    /// </summary>
    ValueTask SendAsync<T>(
        T message,
        SendOptions options,
        IMessageDispatchTarget target,
        CancellationToken cancellationToken)
        where T : notnull;
}
