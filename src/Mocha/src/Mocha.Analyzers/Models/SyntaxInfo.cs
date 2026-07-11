using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers;

/// <summary>
/// Represents the base record for all syntax-derived information models
/// collected during source generation.
/// </summary>
public abstract record SyntaxInfo
{
    /// <summary>
    /// Gets the key used to establish a deterministic ordering of syntax information entries.
    /// </summary>
    public abstract string OrderByKey { get; }

    /// <summary>
    /// Gets the collection of diagnostics associated with this syntax information entry.
    /// Uses <see cref="DiagnosticInfo"/> instead of Roslyn's <c>Diagnostic</c> to maintain
    /// value equality and avoid rooting old compilations in memory.
    /// </summary>
    public ImmutableEquatableArray<DiagnosticInfo> Diagnostics { get; init; } =
        ImmutableEquatableArray<DiagnosticInfo>.Empty;
}
