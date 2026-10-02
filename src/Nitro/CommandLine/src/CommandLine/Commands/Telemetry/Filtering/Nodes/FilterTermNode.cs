namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering.Nodes;

internal sealed record FilterTermNode(string Text, int Start, int End) : FilterNode(Start, End);
