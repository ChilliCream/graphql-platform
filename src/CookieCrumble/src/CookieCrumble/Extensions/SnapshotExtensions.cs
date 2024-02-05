using CookieCrumble.Formatters;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace CookieCrumble;

public static class SnapshotExtensions
{
    public static void MatchInlineSnapshot(
        this object? value,
        string snapshot,
        ISnapshotValueFormatter? formatter = null)
        => Snapshot.Create().Add(value, formatter: formatter).MatchInline(snapshot);

    public static void MatchSnapshot(this Snapshot value)
        => value.Match();
    
    public static void MatchSnapshot(
        this object? value,
        object? postFix = null,
        string? extension = null,
        ISnapshotValueFormatter? formatter = null)
        => Snapshot.Match(value, postFix?.ToString(), extension, formatter);

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
            formatter: SnapshotValueFormatters.PlainText);

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
        snapshot.SetPostFix(TestEnvironment.TargetFramework);

        if (result.ContextData.TryGetValue("query", out var queryResult) &&
            queryResult is string queryString &&
            !string.IsNullOrWhiteSpace(queryString))
        {
            snapshot.Add(
                queryString,
                string.IsNullOrEmpty(name)
                    ? "Query:"
                    : $"{name} Query:",
                SnapshotValueFormatters.PlainText);
        }

        if (result.ContextData.TryGetValue("sql", out var sql))
        {
            snapshot.Add(
                sql,
                string.IsNullOrEmpty(name)
                    ? "SQL:"
                    : $"{name} SQL:",
                SnapshotValueFormatters.PlainText);
        }

        if (result.ContextData.TryGetValue("expression", out var expression))
        {
            snapshot.Add(
                expression,
                string.IsNullOrEmpty(name)
                    ? "Expression:"
                    : $"{name} Expression:",
                SnapshotValueFormatters.PlainText);
        }

        if (result.ContextData.TryGetValue("ex", out var exception))
        {
            snapshot.Add(exception, "Exception:");
        }

        return snapshot;
    }
}
