namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// An immutable audit record for an actor takeover. Null role and reason values mean the takeover did not record those details.
/// </summary>
internal sealed class TakeoverRecord
{
    public const string Columns =
        "id AS Id, from_actor AS FromActor, to_actor AS ToActor, actor AS Actor, "
        + "created_at AS CreatedAt, forced AS Forced, role AS Role, reason AS Reason";

    public required string Id { get; init; }
    public required string FromActor { get; init; }
    public required string ToActor { get; init; }
    public required string Actor { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required bool Forced { get; init; }
    public string? Role { get; init; }
    public string? Reason { get; init; }
    public IReadOnlyList<TakeoverItem> Items { get; init; } = [];
}
