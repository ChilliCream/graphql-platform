using Microsoft.Extensions.Primitives;

namespace Mocha;

/// <summary>
/// Provides the current message bus topology snapshot and a coarse invalidation signal.
/// </summary>
public interface IMessageBusTopology
{
    /// <summary>
    /// Gets the current message bus topology snapshot.
    /// </summary>
    MessageBusDescription Description { get; }

    /// <summary>
    /// Gets a change token that fires when the topology snapshot may have changed.
    /// </summary>
    IChangeToken GetChangeToken();
}
