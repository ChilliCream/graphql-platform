#nullable enable

namespace HotChocolate.Types.Relay;

/// <summary>
/// Represents options for adding a query root field to mutation payloads.
/// </summary>
public class MutationPayloadOptions
{
    /// <summary>
    /// The name of the query field on a mutation payload (default: query).
    /// </summary>
    public string? QueryFieldName { get; set; }

    /// <summary>
    /// A predicate that defines if the query field shall be added to
    /// the specified payload type.
    /// </summary>
    public Func<INamedType, bool> MutationPayloadPredicate { get; set; } =
        type => type.Name.EndsWith("Payload", StringComparison.Ordinal);
}
