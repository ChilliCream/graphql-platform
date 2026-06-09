using CookieCrumble.HotChocolate.Formatters;
using HotChocolate;
using HotChocolate.Execution;
using CoreFormatters = CookieCrumble.Formatters.SnapshotValueFormatters;

namespace CookieCrumble.HotChocolate;

public static class SnapshotExtensions
{
    public static void MatchSnapshot(
        this ISchemaDefinition? value,
        string? postFix = null)
        => Snapshot.Match(
            value,
            postFix,
            extension: ".graphql",
            formatter: SnapshotValueFormatters.Schema);

    public static void MatchSnapshot(
        this IExecutable? value,
        string? postFix = null)
        => Snapshot.Match(
            value,
            postFix,
            formatter: CoreFormatters.PlainText);

    /// <summary>
    /// Matches a snapshot of an execution result, merging an incrementally delivered
    /// (<c>@defer</c>/<c>@stream</c>) response into its final aggregated form. The snapshot
    /// is identical whether the transport delivered the response across several payloads or
    /// as a single bundled payload, so it does not depend on delivery timing.
    /// </summary>
    public static void MatchAggregatedSnapshot(
        this IExecutionResult? value,
        string? postFix = null)
        => Snapshot.Match(
            value,
            postFix,
            formatter: SnapshotValueFormatters.ExecutionResultAggregated);

    /// <summary>
    /// Markdown counterpart of <see cref="MatchAggregatedSnapshot"/>: matches a markdown
    /// snapshot of an execution result, merging an incrementally delivered
    /// (<c>@defer</c>/<c>@stream</c>) response into its final aggregated form so the snapshot
    /// does not depend on whether the transport delivered it incrementally or bundled.
    /// </summary>
    public static void MatchAggregatedMarkdownSnapshot(
        this IExecutionResult? value,
        object? postFix = null,
        string? extension = null)
        => Snapshot.Create(postFix?.ToString(), extension)
            .Add(value, formatter: SnapshotValueFormatters.ExecutionResultAggregated)
            .MatchMarkdown();

    public static Snapshot AddResult(
        this Snapshot snapshot,
        IExecutionResult result,
        string? name = null)
    {
        if (result.ContextData.IsEmpty)
        {
            snapshot.Add(result.ToJson(), name);
            return snapshot;
        }

        snapshot.Add(
            result.ToJson(),
            string.IsNullOrEmpty(name)
                ? "Result:"
                : $"{name} Result:");

        if (result.ContextData.TryGetValue("query", out var queryResult)
            && queryResult is string queryString
            && !string.IsNullOrWhiteSpace(queryString))
        {
            snapshot.Add(
                queryString,
                string.IsNullOrEmpty(name)
                    ? "Query:"
                    : $"{name} Query:",
                CoreFormatters.PlainText);
        }

        if (result.ContextData.TryGetValue("sql", out var sql))
        {
            snapshot.Add(
                sql,
                string.IsNullOrEmpty(name)
                    ? "SQL:"
                    : $"{name} SQL:",
                CoreFormatters.PlainText);
        }

        if (result.ContextData.TryGetValue("expression", out var expression))
        {
            snapshot.Add(
                expression,
                string.IsNullOrEmpty(name)
                    ? "Expression:"
                    : $"{name} Expression:",
                CoreFormatters.PlainText);
        }

        if (result.ContextData.TryGetValue("ex", out var exception))
        {
            snapshot.Add(exception, "Exception:");
        }

        return snapshot;
    }
}
