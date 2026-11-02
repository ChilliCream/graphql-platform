namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

internal readonly record struct KeyCandidate(string Path, int CompletionLength, int SegmentCount, int Distance);
