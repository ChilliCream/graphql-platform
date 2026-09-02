namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The optional criteria for querying takeover audit records. Every supplied criterion applies as an AND condition.
/// </summary>
internal sealed record TakeoverFilter
{
    public string? Actor { get; init; }
    public string? MessageId { get; init; }
    public string? TaskId { get; init; }
    public int? Limit { get; init; }
}
