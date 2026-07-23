namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Describes the transport-level capabilities that a <see cref="ISourceSchemaClient"/>
/// supports. Because this is a <see cref="FlagsAttribute"/> enum, values can be combined
/// to advertise multiple capabilities at once.
/// </summary>
[Flags]
public enum SourceSchemaClientCapabilities
{
    None = 0,

    /// <summary>
    /// The client supports variable batching, where a single request carries multiple
    /// sets of variables so the downstream service can resolve them in one round-trip.
    /// </summary>
    VariableBatching = 1 << 1,

    /// <summary>
    /// The client supports request batching, where multiple independent GraphQL
    /// operations are sent as an array in a single HTTP request.
    /// </summary>
    RequestBatching = 1 << 2,

    /// <summary>
    /// The client supports alias batching, where each row of a batch is rewritten into an
    /// aliased copy of the root selections within a single spec-conformant GraphQL request.
    /// Unlike <see cref="VariableBatching"/> and <see cref="RequestBatching"/>, this mode
    /// requires no protocol extension from the downstream service.
    /// </summary>
    AliasBatching = 1 << 3,

    /// <summary>
    /// Combines the protocol-extension batching capabilities (<see cref="VariableBatching"/>
    /// and <see cref="RequestBatching"/>). <see cref="AliasBatching"/> is deliberately excluded
    /// because it supersedes those modes and is selected through an explicit opt-in rather than
    /// advertised as a default capability.
    /// </summary>
    All = VariableBatching | RequestBatching
}
