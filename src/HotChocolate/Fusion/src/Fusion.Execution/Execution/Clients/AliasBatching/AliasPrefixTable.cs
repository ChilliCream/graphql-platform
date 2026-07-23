using System.Collections.Immutable;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

/// <summary>
/// Describes the alias and variable prefixing applied when several source schema
/// requests are merged into a single alias batched operation. The table is laid
/// out as parallel arrays so the variable merger and the response reader can both
/// iterate it without allocating per entry.
/// </summary>
/// <remarks>
/// The table carries two independent groups of rows:
/// <list type="bullet">
/// <item>
/// Variable rows map a prefixed variable name in the merged document back to the
/// original variable name within a specific inbound request and variable row, so
/// the merger can write <c>prefixedName -&gt; requests[op].Variables[row].Values[originalName]</c>.
/// </item>
/// <item>
/// Root selection rows map a root alias in the merged document back to the inbound
/// request and variable row it belongs to, so the response reader can split the
/// merged response into per (operation, row) results.
/// </item>
/// </list>
/// </remarks>
internal sealed class AliasPrefixTable
{
    /// <summary>
    /// Gets the inbound request index for each variable row.
    /// </summary>
    public required ImmutableArray<int> VariableOperationIndices { get; init; }

    /// <summary>
    /// Gets the inbound variable row index for each variable row.
    /// </summary>
    public required ImmutableArray<int> VariableRowIndices { get; init; }

    /// <summary>
    /// Gets the original variable name (without the leading <c>$</c>) for each variable row.
    /// </summary>
    public required ImmutableArray<string> OriginalVariableNames { get; init; }

    /// <summary>
    /// Gets the prefixed variable name (without the leading <c>$</c>) emitted into
    /// the merged document for each variable row.
    /// </summary>
    public required ImmutableArray<string> PrefixedVariableNames { get; init; }

    /// <summary>
    /// Gets the number of variable rows in this table.
    /// </summary>
    public int VariableCount => PrefixedVariableNames.Length;

    /// <summary>
    /// Gets the inbound request index for each root selection.
    /// </summary>
    public required ImmutableArray<int> RootOperationIndices { get; init; }

    /// <summary>
    /// Gets the inbound variable row index for each root selection.
    /// </summary>
    public required ImmutableArray<int> RootRowIndices { get; init; }

    /// <summary>
    /// Gets the root alias emitted into the merged document for each root selection.
    /// </summary>
    public required ImmutableArray<string> RootAliases { get; init; }

    /// <summary>
    /// Gets the number of root selections in this table.
    /// </summary>
    public int RootCount => RootAliases.Length;
}
