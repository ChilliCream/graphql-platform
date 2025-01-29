using CookieCrumble.Formatters;

namespace CookieCrumble.HotChocolate.Formatters;

/// <summary>
/// Provides access to well-known snapshot value formatters.
/// </summary>
public static class SnapshotValueFormatters
{
    public static ISnapshotValueFormatter ExecutionResult { get; } =
        new ExecutionResultSnapshotValueFormatter();

    public static ISnapshotValueFormatter GraphQL { get; } =
        new GraphQLSnapshotValueFormatter();

    public static ISnapshotValueFormatter GraphQLHttp { get; } =
        new GraphQLHttpResponseFormatter();

    public static ISnapshotValueFormatter OperationResult { get; } =
        new OperationResultSnapshotValueFormatter();

    public static ISnapshotValueFormatter Schema { get; } =
        new SchemaSnapshotValueFormatter();

    public static ISnapshotValueFormatter SchemaError { get; } =
        new SchemaErrorSnapshotValueFormatter();
}
