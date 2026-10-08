using System.Buffers;
using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

public record OperationPlanStep : PlanStep
{
    private const int StackAllocWordLimit = 32;

    public required OperationDefinitionNode Definition { get; init; }

    public required ITypeDefinition Type { get; init; }

    public required uint RootSelectionSetId { get; init; }

    public required ImmutableHashSet<uint> SelectionSets { get; init; }

    public required string? SchemaName { get; init; }

    public ExecutionNodeCondition[] Conditions { get; init; } = [];

    public ImmutableHashSet<int> Dependents { get; init; } = [];

    public ImmutableHashSet<ParentStepRef> ParentDependencies { get; init; } = [];

    public ImmutableDictionary<string, OperationRequirement> Requirements { get; init; }
#if NET10_0_OR_GREATER
        = [];
#else
        = ImmutableDictionary<string, OperationRequirement>.Empty;
#endif

    public required SelectionPath Target { get; init; }

    public required SelectionPath Source { get; init; }

    public Lookup? Lookup { get; init; }

    internal EventStreamPlan? EventStreamPlan { get; init; }

    public bool DependsOn(OperationPlanStep otherStep, ImmutableList<PlanStep> allSteps)
    {
        if (otherStep.Dependents.Contains(Id))
        {
            return true;
        }

        if (otherStep.Dependents.IsEmpty)
        {
            return false;
        }

        var wordCount = (Math.Max(allSteps.Count, otherStep.Id) >> 6) + 1;
        ulong[]? rented = null;
        var visited = wordCount <= StackAllocWordLimit
            ? stackalloc ulong[wordCount]
            : (rented = ArrayPool<ulong>.Shared.Rent(wordCount)).AsSpan(0, wordCount);
        visited.Clear();

        try
        {
            return DependsOnRecursive(otherStep, Id, allSteps, visited);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<ulong>.Shared.Return(rented);
            }
        }
    }

    private static bool DependsOnRecursive(
        OperationPlanStep currentStep,
        int targetId,
        ImmutableList<PlanStep> allSteps,
        Span<ulong> visited)
    {
        var word = currentStep.Id >> 6;
        var bit = 1UL << (currentStep.Id & 63);

        if ((visited[word] & bit) != 0)
        {
            return false;
        }

        visited[word] |= bit;

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
