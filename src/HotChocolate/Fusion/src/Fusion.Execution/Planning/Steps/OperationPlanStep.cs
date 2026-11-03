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
    private const int StackAllocIdLimit = 256;

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
        ulong[]? rentedVisited = null;
        var visited = wordCount <= StackAllocWordLimit
            ? stackalloc ulong[wordCount]
            : (rentedVisited = ArrayPool<ulong>.Shared.Rent(wordCount)).AsSpan(0, wordCount);
        visited.Clear();

        var stackCapacity = allSteps.Count + 1;
        int[]? rentedStack = null;
        var stack = stackCapacity <= StackAllocIdLimit
            ? stackalloc int[stackCapacity]
            : (rentedStack = ArrayPool<int>.Shared.Rent(stackCapacity)).AsSpan(0, stackCapacity);

        try
        {
            return DependsOnIterative(otherStep, Id, allSteps, visited, stack);
        }
        finally
        {
            if (rentedVisited is not null)
            {
                ArrayPool<ulong>.Shared.Return(rentedVisited);
            }

            if (rentedStack is not null)
            {
                ArrayPool<int>.Shared.Return(rentedStack);
            }
        }
    }

    private static bool DependsOnIterative(
        OperationPlanStep startStep,
        int targetId,
        ImmutableList<PlanStep> allSteps,
        Span<ulong> visited,
        Span<int> stack)
    {
        var count = 0;
        var currentStep = startStep;
        visited[startStep.Id >> 6] |= 1UL << (startStep.Id & 63);

        while (true)
        {
            if (currentStep.Dependents.Contains(targetId))
            {
                return true;
            }

            foreach (var dependentId in currentStep.Dependents)
            {
                if (dependentId < 1 || dependentId > allSteps.Count)
                {
                    continue;
                }

                var word = dependentId >> 6;
                var bit = 1UL << (dependentId & 63);

                if ((visited[word] & bit) != 0)
                {
                    continue;
                }

                visited[word] |= bit;
                stack[count++] = dependentId;
            }

            while (true)
            {
                if (count == 0)
                {
                    return false;
                }

                if (allSteps.ById(stack[--count]) is OperationPlanStep nextStep)
                {
                    currentStep = nextStep;
                    break;
                }
            }
        }
    }
}
