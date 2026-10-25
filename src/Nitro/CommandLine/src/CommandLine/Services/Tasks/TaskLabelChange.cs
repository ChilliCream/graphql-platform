namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// One label from an <see cref="ITaskStore.AddLabelAsync"/> call and whether
/// it was newly added or already present.
/// </summary>
internal sealed record TaskLabelChange(string Label, bool Added);
