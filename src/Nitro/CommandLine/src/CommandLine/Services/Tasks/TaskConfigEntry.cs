namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// One key-value pair from the workspace configuration, as returned by
/// <see cref="ITaskStore.ListConfigAsync"/>.
/// </summary>
internal sealed record TaskConfigEntry(string Key, string Value);
