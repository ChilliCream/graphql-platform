namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The details required to record an actor takeover. Null role and reason values omit those optional audit details.
/// </summary>
internal sealed record TakeoverRecordCreation
{
    public required string FromActor { get; init; }
    public required string ToActor { get; init; }
    public required string Actor { get; init; }
    public required bool Forced { get; init; }
    public string? Role { get; init; }
    public string? Reason { get; init; }
}
