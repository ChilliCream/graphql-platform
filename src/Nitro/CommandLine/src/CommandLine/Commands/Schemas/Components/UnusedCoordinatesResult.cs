namespace ChilliCream.Nitro.CommandLine.Commands.Schemas.Components;

/// <summary>
/// The analytical payload rendered by <c>nitro schema unused</c>. Lists the coordinates
/// that had zero usage in the requested window, ordered by <c>totalRequests</c> ascending
/// on the server and filtered client-side.
/// </summary>
internal sealed record UnusedCoordinatesResult
{
    public required IReadOnlyList<UnusedCoordinatesResultEntry> Coordinates { get; init; }

    public required int Limit { get; init; }
}

internal sealed record UnusedCoordinatesResultEntry
{
    public required string Coordinate { get; init; }

    public required bool IsDeprecated { get; init; }

    public required long TotalRequests { get; init; }

    public required long ClientCount { get; init; }

    public required long OperationCount { get; init; }

    public required DateTimeOffset? LastSeen { get; init; }
}
