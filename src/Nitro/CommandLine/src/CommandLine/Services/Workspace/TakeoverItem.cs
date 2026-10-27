namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// A mail or task item related to an actor takeover.
/// </summary>
internal sealed record TakeoverItem
{
    public required string Kind { get; init; }
    public required string ItemId { get; init; }
}
