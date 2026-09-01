namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// The subset of a task's columns returned by the structured (JSON) output of
/// the list-style task query commands.
/// </summary>
internal sealed record TaskSummaryResult(string Id, int Priority, string Type, string Status, string Title);
