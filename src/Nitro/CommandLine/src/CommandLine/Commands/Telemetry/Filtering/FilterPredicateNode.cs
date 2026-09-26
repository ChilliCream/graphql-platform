namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

internal sealed record FilterPredicateNode(
    string Field,
    FilterComparisonOperator Operator,
    IReadOnlyList<FilterValue> Values,
    int Start,
    int End)
    : FilterNode(Start, End);
