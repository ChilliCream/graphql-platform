using System.ComponentModel;

namespace Mocha;

/// <summary>
/// Pre-built handler configuration emitted by the source generator.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MessagingHandlerConfiguration
{
    /// <summary>
    /// The concrete handler implementation type.
    /// </summary>
    public required Type HandlerType { get; init; }

    /// <summary>
    /// Factory that creates the consumer instance for this handler.
    /// </summary>
    public required Func<Action<IConsumerDescriptor>?, Consumer> Factory { get; init; }
}
