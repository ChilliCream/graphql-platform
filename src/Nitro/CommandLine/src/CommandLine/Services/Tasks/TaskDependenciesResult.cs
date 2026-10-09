namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// A task's outgoing dependencies and incoming dependents, as returned by the
/// structured (JSON) output of <c>agent tasks dep list</c>.
/// </summary>
internal sealed record TaskDependenciesResult(
    IReadOnlyList<TaskDependencyDetail> Dependencies,
    IReadOnlyList<TaskDependentDetail> Dependents);
