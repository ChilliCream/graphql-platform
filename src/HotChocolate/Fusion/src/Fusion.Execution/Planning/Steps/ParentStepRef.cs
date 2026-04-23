namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Identifies a dependency on a step that lives in an enclosing plan scope,
/// for example a deferred sub-plan step that depends on a step in its parent
/// plan. The referenced step is not part of the same plan scope as the step
/// declaring the dependency.
/// </summary>
/// <param name="StepId">
/// The identifier of the step in the parent plan scope that must complete
/// before the declaring step can execute.
/// </param>
public readonly record struct ParentStepRef(int StepId);
