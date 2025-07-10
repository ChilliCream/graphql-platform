using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

public record OperationPlanStep : PlanStep
{
    public required OperationDefinitionNode Definition { get; init; }

    public required ITypeDefinition Type { get; init; }

    public required uint RootSelectionSetId { get; init; }

    public required ImmutableHashSet<uint> SelectionSets { get; init; }

    public required string SchemaName { get; init; }

    public ImmutableHashSet<int> Dependents { get; init; } = [];

    public ImmutableDictionary<string, OperationRequirement> Requirements { get; init; }
#if NET10_0_OR_GREATER
        = [];
#else
        = ImmutableDictionary<string, OperationRequirement>.Empty;
#endif

    public required SelectionPath Target { get; init; }

    public required SelectionPath Source { get; init; }

    public bool DependsOn(OperationPlanStep otherStep, ImmutableList<PlanStep> allSteps)
        => DependsOnRecursive(otherStep, Id, allSteps, []);

    private static bool DependsOnRecursive(
        OperationPlanStep currentStep,
        int targetId,
        ImmutableList<PlanStep> allSteps,
        HashSet<int> visited)
    {
        if (!visited.Add(currentStep.Id))
        {
            return false;
        }

        if (currentStep.Dependents.Contains(targetId))
        {
            return true;
        }

        foreach (var dependentId in currentStep.Dependents)
        {
            var dependentStep = allSteps.ById(dependentId);
            if (dependentStep is OperationPlanStep dependentOpStep
                && DependsOnRecursive(dependentOpStep, targetId, allSteps, visited))
            {
                return true;
            }
        }

        return false;
    }
}
