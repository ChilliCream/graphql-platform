namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// A dependency edge identified by its task pair, without the edge's type or
/// timestamps.
/// </summary>
internal sealed record TaskDependencyReference(string TaskId, string DependsOnId);
