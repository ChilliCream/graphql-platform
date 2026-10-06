namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering.Nodes;

internal sealed record FilterAndNode(IReadOnlyList<FilterNode> Children, int Start, int End) : FilterNode(Start, End);
