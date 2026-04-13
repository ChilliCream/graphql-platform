namespace ChilliCream.Nitro.CommandLine.Commands.Schemas.Components;

/// <summary>
/// The analytical payload rendered by <c>nitro schema clients</c>. Captures the per-client
/// breakdown for a single coordinate on the resolved stage.
/// </summary>
internal sealed record CoordinateClientsResult
{
    public required string Coordinate { get; init; }

    public required bool IsDeprecated { get; init; }

    public required IReadOnlyList<CoordinateClientsResultEntry> Clients { get; init; }
}

internal sealed record CoordinateClientsResultEntry
{
    public required string Name { get; init; }

    public required string? ClientId { get; init; }

    public required long TotalVersions { get; init; }

    public required long TotalOperations { get; init; }

    public required long TotalRequests { get; init; }
}
