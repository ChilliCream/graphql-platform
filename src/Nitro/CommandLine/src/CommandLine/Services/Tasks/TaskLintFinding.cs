namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// One workspace quality finding reported by <c>task lint</c>: a task, the
/// short rule slug that flagged it, and a human-readable message.
/// </summary>
internal sealed record TaskLintFinding(string TaskId, string Rule, string Message);
