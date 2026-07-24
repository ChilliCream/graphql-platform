using System.Collections.Immutable;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

/// <summary>
/// Represents the result of merging one or more source schema requests into a single
/// alias batched GraphQL operation. Instances are immutable and safe to cache and
/// reuse across requests that share the same operation hashes and row counts.
/// </summary>
internal sealed class AliasBatchedOperation
{
    /// <summary>
    /// Gets the merged operation rendered as GraphQL source text. The text is rendered
    /// deterministically so it can serve as a stable cache value.
    /// </summary>
    public required string SourceText { get; init; }

    /// <summary>
    /// Gets the prefix table that maps the merged document's aliases and prefixed
    /// variable names back to the inbound requests and variable rows.
    /// </summary>
    public required AliasPrefixTable Prefixes { get; init; }

    /// <summary>
    /// Gets the original response name for each root selection, aligned with
    /// <see cref="AliasPrefixTable.RootAliases"/>. The response reader uses these names
    /// to rebuild per-row results with their original field names instead of the aliases.
    /// </summary>
    public required ImmutableArray<string> RootResponseNames { get; init; }
}
