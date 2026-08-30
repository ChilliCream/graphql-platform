namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// A label row identified by its task and label text.
/// </summary>
internal sealed record TaskOrphanLabel(string TaskId, string Label);
