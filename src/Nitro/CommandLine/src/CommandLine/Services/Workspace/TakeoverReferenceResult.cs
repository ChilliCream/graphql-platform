namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Identifies a takeover that moved a mail message or task.
/// </summary>
internal sealed record TakeoverReferenceResult
{
    public required string Id { get; init; }
    public required string From { get; init; }
    public required string To { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    public static TakeoverReferenceResult FromRecord(TakeoverRecord record)
        => new()
        {
            Id = record.Id,
            From = record.FromActor,
            To = record.ToActor,
            CreatedAt = record.CreatedAt
        };
}
