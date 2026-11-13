namespace Mocha;

/// <summary>
/// Collects the problems that the health checks of a message bus report.
/// </summary>
public sealed class MessageBusHealthContext
{
    private List<MessageBusHealthFailure>? _failures;

    /// <summary>
    /// Gets the reported problems in the order they were reported. The list is empty when the bus is healthy.
    /// </summary>
    public IReadOnlyList<MessageBusHealthFailure> Failures => _failures ?? (IReadOnlyList<MessageBusHealthFailure>)[];

    /// <summary>
    /// Reports a part of the message bus as unhealthy.
    /// </summary>
    /// <param name="name">The name of the unhealthy part, for example the name of a transport.</param>
    /// <param name="reason">The reason the part is unhealthy.</param>
    public void ReportUnhealthy(string name, string reason)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(reason);

        (_failures ??= []).Add(new MessageBusHealthFailure(name, reason));
    }
}
