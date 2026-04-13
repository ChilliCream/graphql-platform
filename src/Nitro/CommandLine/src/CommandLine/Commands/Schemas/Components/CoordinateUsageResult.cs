namespace ChilliCream.Nitro.CommandLine.Commands.Schemas.Components;

/// <summary>
/// The analytical payload rendered by <c>nitro schema usage</c>. One instance per
/// requested coordinate.
/// </summary>
internal sealed record CoordinateUsageResult
{
    public required string Coordinate { get; init; }

    public required bool IsDeprecated { get; init; }

    public required long TotalRequests { get; init; }

    public required long ClientCount { get; init; }

    public required long OperationCount { get; init; }

    public required DateTimeOffset? FirstSeen { get; init; }

    public required DateTimeOffset? LastSeen { get; init; }

    public required double? ErrorRate { get; init; }

    public required double? MeanDuration { get; init; }
}

/// <summary>
/// Wrapper for the multi-coordinate <c>nitro schema usage</c> payload. Keyed by coordinate
/// string for stable JSON shape. Single-coordinate invocations still produce a
/// one-element dictionary.
/// </summary>
internal sealed record CoordinateUsageResultSet
{
    public required IReadOnlyDictionary<string, CoordinateUsageResult> Coordinates { get; init; }
}
