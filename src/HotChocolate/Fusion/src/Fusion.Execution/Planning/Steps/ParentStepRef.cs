namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Identifies a dependency on a step in an enclosing plan scope.
/// </summary>
/// <param name="StepId">
/// The identifier of the step in the parent plan scope that must complete
/// before the declaring step can execute.
/// </param>
public readonly record struct ParentStepRef(int StepId);
