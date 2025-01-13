using CookieCrumble.HotChocolate.Formatters;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using CoreFormatters = CookieCrumble.Formatters.SnapshotValueFormatters;

namespace CookieCrumble.HotChocolate;

public static class SnapshotExtensions
{
    public static void MatchSnapshot(
        this ISyntaxNode? value,
        string? postFix = null)
        => Snapshot.Match(
            value,
            postFix,
            extension: ".graphql",
            formatter: SnapshotValueFormatters.GraphQL);

    public static void MatchSnapshot(
        this ISchema? value,
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

    public static Snapshot AddResult(
        this Snapshot snapshot,
        IExecutionResult result,
        string? name = null)
    {
        if (result.ContextData is null)
        {
            snapshot.Add(result.ToJson(), name);
            return snapshot;
        }

        snapshot.Add(
            result.ToJson(),
            string.IsNullOrEmpty(name)
                ? "Result:"
                : $"{name} Result:");

        if (result.ContextData.TryGetValue("query", out var queryResult) &&
            queryResult is string queryString &&
            !string.IsNullOrWhiteSpace(queryString))
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
