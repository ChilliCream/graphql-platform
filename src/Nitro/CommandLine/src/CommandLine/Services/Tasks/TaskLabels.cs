namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// Labels belonging to one task, ordered by label.
/// </summary>
internal sealed record TaskLabels(string TaskId, IReadOnlyList<string> Labels);
