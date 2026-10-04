namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// One dependency to create between a new task and an existing one.
/// </summary>
internal sealed record TaskDependencyRequest(string DependsOnId, string Type);
