namespace ChilliCream.Nitro.CommandLine.Commands.Schemas.Components;

/// <summary>
/// The analytical payload rendered by <c>nitro schema operations</c>. Flattens the
/// operations reported for every client that uses the coordinate.
/// </summary>
internal sealed record CoordinateOperationsResult
{
    public required string Coordinate { get; init; }

    public required bool IsDeprecated { get; init; }

    public required IReadOnlyList<CoordinateOperationsResultEntry> Operations { get; init; }
}

internal sealed record CoordinateOperationsResultEntry
{
    public required string OperationName { get; init; }

    public required string Hash { get; init; }

    public required string? Kind { get; init; }

    public required string ClientName { get; init; }

    public required double OperationsPerMinute { get; init; }

    public required long TotalCount { get; init; }

    public required double ErrorRate { get; init; }

    public required double AverageLatency { get; init; }
}
