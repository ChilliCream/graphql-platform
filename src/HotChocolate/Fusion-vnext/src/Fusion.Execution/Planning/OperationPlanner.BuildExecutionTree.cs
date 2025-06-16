using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed partial class OperationPlanner
{
    /// <summary>
    /// Builds the actual execution plan from the provided <paramref name="planSteps"/>.
    /// </summary>
    private static OperationExecutionPlan BuildExecutionPlan(
        ImmutableList<OperationPlanStep> planSteps,
        OperationDefinitionNode originalOperation)
    {
        var completedSteps = new HashSet<int>();
        var completedNodes = new Dictionary<int, ExecutionNode>();
        var dependencyLookup = new Dictionary<int, HashSet<int>>();

        planSteps = PrepareSteps(planSteps, originalOperation, dependencyLookup);
        BuildExecutionNodes(planSteps, completedSteps, completedNodes, dependencyLookup);
        BuildDependencyStructure(completedNodes, dependencyLookup);

        var rootNodes = planSteps
            .Where(t => !dependencyLookup.ContainsKey(t.Id))
            .Select(t => completedNodes[t.Id])
            .ToImmutableArray();

        var allNodes = completedNodes
            .OrderBy(t => t.Key)
            .Select(t => t.Value)
            .ToImmutableArray();

        return new OperationExecutionPlan
        {
            Operation = originalOperation,
            RootNodes = rootNodes,
            AllNodes = allNodes
        };
    }

    private static ImmutableList<OperationPlanStep> PrepareSteps(
        ImmutableList<OperationPlanStep> planSteps,
        OperationDefinitionNode originalOperation,
        Dictionary<int, HashSet<int>> dependencyLookup)
    {
        var updatedPlanSteps = planSteps;
        var emptySelectionSetContext = new HasEmptySelectionSetVisitor.Context();
        var forwardVariableContext = new ForwardVariableRewriter.Context();

        foreach (var variableDef in originalOperation.VariableDefinitions)
        {
            forwardVariableContext.Variables[variableDef.Variable.Name.Value] = variableDef;
        }

        foreach (var step in planSteps)
        {
            // During the planing process we keep incomplete operation steps around
            // in order to inline requirements. If those do not materialize these
            // operation fragments need to be removed before we can build the
            // execution plan.
            if (IsEmptyOperation(step))
            {
                updatedPlanSteps = updatedPlanSteps.Remove(step);
                continue;
            }

            // The operation definition of the current OperationPlanStep do not yet
            // have variable definitions declared, so we need to traverse the operation definition
            // and look at what variables and requirements are used within the operation definition.
            updatedPlanSteps.Replace(step, AddVariableDefinitions(step));

            // Each PlanStep tracks dependant PlanSteps,
            // so PlanSteps that require data (lookup or field requirements)
            // from the current step.
            // For a simpler planing algorithm we are building a lookup in reverse,
            // that tracks the dependencies each node has.
            foreach (var dependent in step.Dependents)
            {
                if (!dependencyLookup.TryGetValue(dependent, out var dependencies))
                {
                    dependencies = [];
                    dependencyLookup[dependent] = dependencies;
                }

                dependencies.Add(step.Id);
            }
        }

        return updatedPlanSteps;

        bool IsEmptyOperation(OperationPlanStep step)
        {
            emptySelectionSetContext.HasEmptySelectionSet = false;
            s_hasEmptySelectionSetVisitor.Visit(step.Definition, emptySelectionSetContext);
            return emptySelectionSetContext.HasEmptySelectionSet;
        }

        OperationPlanStep AddVariableDefinitions(OperationPlanStep step)
        {
            forwardVariableContext.Reset();

            foreach (var (key, requirement) in step.Requirements.OrderBy(t => t.Key))
            {
                forwardVariableContext.Requirements[key] =
                    new VariableDefinitionNode(
                        null,
                        new VariableNode(null, new NameNode(key)),
                        requirement.Type,
                        null,
                        []);
            }

            if (s_forwardVariableRewriter.Rewrite(step.Definition, forwardVariableContext) is OperationDefinitionNode rewritten
                && !ReferenceEquals(rewritten, step.Definition))
            {
                return step with { Definition = rewritten };
            }

            return step;
        }
    }

    private static void BuildExecutionNodes(
        ImmutableList<OperationPlanStep> planSteps,
        HashSet<int> completedSteps,
        Dictionary<int, ExecutionNode> completedNodes,
        Dictionary<int, HashSet<int>> dependencyLookup)
    {
        var readySteps = planSteps.Where(t => !dependencyLookup.ContainsKey(t.Id)).ToList();

        while (completedSteps.Count < planSteps.Count)
        {
            foreach (var step in readySteps)
            {
                if (!completedSteps.Add(step.Id))
                {
                    continue;
                }

                var requirements = ImmutableArray<OperationRequirement>.Empty;

                if (!step.Requirements.IsEmpty)
                {
                    var builder = ImmutableArray.CreateBuilder<OperationRequirement>();

                    foreach (var (_, requirement) in step.Requirements.OrderBy(t => t.Key))
                    {
                        builder.Add(requirement);
                    }

                    requirements = builder.ToImmutable();
                }

                var operationNode = new OperationExecutionNode(
                    step.Id,
                    step.Definition,
                    step.SchemaName,
                    // TODO : fix path
                    SelectionPath.Root,
                    SelectionPath.Root,
                    [.. step.Requirements.Values]);

                completedNodes.Add(step.Id, operationNode);
            }

            readySteps.Clear();

            foreach (var step in planSteps)
            {
                if (dependencyLookup.TryGetValue(step.Id, out var stepDependencies)
                    && completedSteps.IsSupersetOf(stepDependencies))
                {
                    readySteps.Add(step);
                }
            }

            if (readySteps.Count == 0)
            {
                break;
            }
        }
    }

    private static void BuildDependencyStructure(
        Dictionary<int, ExecutionNode> completedNodes,
        Dictionary<int, HashSet<int>> dependencyLookup)
    {
        foreach (var (nodeId, stepDependencies) in dependencyLookup)
        {
            if (!completedNodes.TryGetValue(nodeId, out var entry)
                || entry is not OperationExecutionNode node)
            {
                continue;
            }

            foreach (var dependencyId in stepDependencies)
            {
                if (!completedNodes.TryGetValue(dependencyId, out entry)
                    || entry is not OperationExecutionNode dependencyNode)
                {
                    continue;
                }

                completedNodes[dependencyId] = dependencyNode with { Dependents = dependencyNode.Dependents.Add(node) };
                completedNodes[nodeId] = node with { Dependencies = node.Dependencies.Add(dependencyNode) };
            }
        }
    }
}
