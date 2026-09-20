namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

public enum TelemetryFilterSignal
{
    Traces,
    Logs
}

internal enum FilterValueKind
{
    String,
    Number,
    Boolean
}

internal enum FilterComparisonOperator
{
    Equal,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    In,
    Range,
    Exists
}

internal abstract record FilterNode(int Start, int End);

internal sealed record FilterAndNode(IReadOnlyList<FilterNode> Children, int Start, int End)
    : FilterNode(Start, End);

internal sealed record FilterOrNode(IReadOnlyList<FilterNode> Children, int Start, int End)
    : FilterNode(Start, End);

internal sealed record FilterNotNode(FilterNode Child, int Start, int End)
    : FilterNode(Start, End);

internal sealed record FilterPredicateNode(
    string Field,
    FilterComparisonOperator Operator,
    IReadOnlyList<FilterValue> Values,
    int Start,
    int End)
    : FilterNode(Start, End);

internal sealed record FilterTermNode(string Text, int Start, int End)
    : FilterNode(Start, End);

internal readonly record struct FilterValue(
    FilterValueKind Kind,
    string Text,
    bool IsQuoted,
    bool HasWildcard,
    int Start,
    int End);
