namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

internal sealed record FilterNotNode(FilterNode Child, int Start, int End)
    : FilterNode(Start, End);
