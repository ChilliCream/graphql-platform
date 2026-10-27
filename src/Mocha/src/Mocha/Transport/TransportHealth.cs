namespace Mocha;

/// <summary>
/// The health status of a messaging transport or one of its components.
/// </summary>
public enum TransportHealthStatus
{
    /// <summary>
    /// The component works as expected.
    /// </summary>
    Healthy,

    /// <summary>
    /// The component does not work right now and is trying to recover.
    /// </summary>
    Degraded,

    /// <summary>
    /// The component does not work and cannot recover on its own.
    /// </summary>
    Unhealthy
}

/// <summary>
/// The health of one component of a messaging transport, such as a connection or a receive endpoint.
/// </summary>
public sealed class TransportHealthEntry
{
    /// <summary>
    /// Gets the name of the component, unique within its transport.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the health status of the component.
    /// </summary>
    public required TransportHealthStatus Status { get; init; }

    /// <summary>
    /// Gets a short description of the component's current state.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the time at which the component entered its current status, or <c>null</c> when it is healthy.
    /// </summary>
    public DateTimeOffset? Since { get; init; }

    /// <summary>
    /// Gets the last error the component reported, or <c>null</c> when there is none.
    /// </summary>
    public Exception? Exception { get; init; }
}

/// <summary>
/// The health of a messaging transport, made up of the health of its components.
/// </summary>
public sealed class TransportHealth
{
    /// <summary>
    /// Creates the health of a transport from the health of its components.
    /// </summary>
    /// <param name="entries">The health of each component of the transport.</param>
    public TransportHealth(IReadOnlyList<TransportHealthEntry> entries)
    {
        Entries = entries;

        var status = TransportHealthStatus.Healthy;
        foreach (var entry in entries)
        {
            if (entry.Status > status)
            {
                status = entry.Status;
            }
        }

        Status = status;
    }

    /// <summary>
    /// Gets the health of a transport that reports no components.
    /// </summary>
    public static TransportHealth Healthy { get; } = new([]);

    /// <summary>
    /// Gets the worst status of all components, or <see cref="TransportHealthStatus.Healthy"/> when there are none.
    /// </summary>
    public TransportHealthStatus Status { get; }

    /// <summary>
    /// Gets the health of each component of the transport.
    /// </summary>
    public IReadOnlyList<TransportHealthEntry> Entries { get; }
}
