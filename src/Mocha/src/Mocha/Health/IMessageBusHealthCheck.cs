namespace Mocha;

/// <summary>
/// Checks one part of the message bus and reports each problem it finds.
/// </summary>
public interface IMessageBusHealthCheck
{
    /// <summary>
    /// Checks the health of one part of the message bus.
    /// </summary>
    /// <param name="context">The context that collects the problems the check finds.</param>
    /// <param name="cancellationToken">The token that cancels the check.</param>
    ValueTask CheckAsync(MessageBusHealthContext context, CancellationToken cancellationToken);
}
