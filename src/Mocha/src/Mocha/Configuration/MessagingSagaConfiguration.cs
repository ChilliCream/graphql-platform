using System.ComponentModel;
using Mocha.Sagas;

namespace Mocha;

/// <summary>
/// Pre-built saga configuration emitted by the source generator.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MessagingSagaConfiguration
{
    /// <summary>
    /// The CLR type of the saga.
    /// </summary>
    public required Type SagaType { get; init; }

    /// <summary>
    /// The pre-built state serializer for this saga.
    /// </summary>
    public required ISagaStateSerializer StateSerializer { get; init; }
}
