namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// A blocked task with the IDs and statuses of the dependencies blocking it,
/// as returned by the structured (JSON) output of <c>task blocked</c>.
/// </summary>
internal sealed record TaskBlockedResult(
    string Id,
    int Priority,
    string Type,
    string Status,
    string Title,
    IReadOnlyList<string> Blockers);
