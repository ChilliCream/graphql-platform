using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public record NodePlanStep : PlanStep
{
    public required string ResponseName { get; init; }

    public required IValueNode IdValue { get; init; }

    public required OperationDefinitionNode FallbackQuery { get; init; }

    public Dictionary<string, OperationPlanStep> Branches { get; } = new();

    public void AddBranch(string objectTypeName, OperationPlanStep step)
        => Branches[objectTypeName] = step;
}
