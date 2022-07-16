using CookieCrumble.Formatters;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace CookieCrumble;

public static class SnapshotExtensions
{
    public static void MatchSnapshot(
        this object? value,
        string? postFix = null,
        string? extension = null,
        ISnapshotValueFormatter? formatter = null)
        => Snapshot.Match(value, postFix, extension, formatter);

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

    public static Snapshot AddSqlFrom(
        this Snapshot snapshot,
        IExecutionResult result,
        string? name = null)
    {
        snapshot.SetPostFix(TestEnvironment.TargetFramework);
        snapshot.Add(result.ToJson(), string.IsNullOrEmpty(name) ? "Result:" : $"{name} Result:");

        if (result.ContextData is null)
        {
            return snapshot;
        }

        if (result.ContextData.TryGetValue("sql", out var sql))
        {
            snapshot.Add(
                sql,
                string.IsNullOrEmpty(name) ? "SQL:" : $"{name} SQL:",
                SnapshotValueFormatters.PlainText);
        }

        if (result.ContextData.TryGetValue("expression", out var expression))
        {
            snapshot.Add(
                expression,
                string.IsNullOrEmpty(name) ? "Expression:" : $"{name} Expression:",
                SnapshotValueFormatters.PlainText);
        }

        return snapshot;
    }

    public static Snapshot AddExceptionFrom(
        this Snapshot snapshot,
        IExecutionResult result)
    {
        snapshot.Add(result, "Result:");
        if (result.ContextData is { } &&
            result.ContextData.TryGetValue("ex", out var queryResult))
        {
            snapshot.Add(queryResult, "Exception:");
        }
        return snapshot;
    }
}
