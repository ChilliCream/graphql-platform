using System.Collections.Immutable;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering.Nodes;

internal sealed record FilterPredicateNode(
    string Field,
    FilterComparisonOperator Operator,
    ImmutableArray<FilterValue> Values,
    int Start,
    int End) : FilterNode(Start, End);
