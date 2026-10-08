namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering.Nodes;

internal sealed record FilterOrNode(IReadOnlyList<FilterNode> Children, int Start, int End) : FilterNode(Start, End);
