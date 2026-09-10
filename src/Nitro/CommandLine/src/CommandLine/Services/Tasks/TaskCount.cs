namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// One group and how many non-tombstone tasks fall into it, as returned by
/// <see cref="ITaskStore.CountTasksByAsync"/>.
/// </summary>
internal sealed record TaskCount(string Value, int Count);
