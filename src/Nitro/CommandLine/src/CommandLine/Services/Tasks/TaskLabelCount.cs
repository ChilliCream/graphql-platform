namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// A label and how many non-tombstone tasks carry it, as returned by
/// <see cref="ITaskStore.GetLabelCountsAsync"/>.
/// </summary>
internal sealed record TaskLabelCount(string Label, int Count);
