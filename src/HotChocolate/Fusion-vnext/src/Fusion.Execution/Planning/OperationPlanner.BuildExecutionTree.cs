using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed partial class OperationPlanner
{
    /// <summary>
    /// Builds the actual execution plan from the provided <paramref name="planSteps"/>.
    /// </summary>
    private OperationPlan BuildExecutionPlan(
        Operation operation,
        OperationDefinitionNode operationDefinition,
        ImmutableList<OperationPlanStep> planSteps,
        bool isIntrospectionOnly)
    {
        if (isIntrospectionOnly)
        {
            var introspectionNode = new IntrospectionExecutionNode(1, [.. operation.RootSelectionSet.Selections]);
            introspectionNode.Seal();

            var nodes = ImmutableArray.Create<ExecutionNode>(introspectionNode);

            var plan = OperationPlan.Create(operation, nodes, nodes);
            OnAfterPlanCompleted(plan);
            return plan;
        }
        else
        {
            var completedSteps = new HashSet<int>();
            var completedNodes = new Dictionary<int, ExecutionNode>();
            var dependencyLookup = new Dictionary<int, HashSet<int>>();
            var hasVariables = operationDefinition.VariableDefinitions.Count > 0;

            planSteps = PrepareSteps(planSteps, operationDefinition, dependencyLookup);
            BuildExecutionNodes(planSteps, completedSteps, completedNodes, dependencyLookup, hasVariables);
            BuildDependencyStructure(completedNodes, dependencyLookup);

            var rootNodes = planSteps
                .Where(t => !dependencyLookup.ContainsKey(t.Id))
                .Select(t => completedNodes[t.Id])
                .ToImmutableArray();

            var allNodes = completedNodes
                .OrderBy(t => t.Key)
                .Select(t => t.Value)
                .ToImmutableArray();

            if (operation.HasIntrospectionFields())
            {
                var introspectionNode = new IntrospectionExecutionNode(
                    allNodes.Max(t => t.Id) + 1,
                    operation.GetIntrospectionSelections());
                rootNodes = rootNodes.Add(introspectionNode);
                allNodes = allNodes.Add(introspectionNode);
            }

            foreach (var node in allNodes)
            {
                node.Seal();
            }

            var plan = OperationPlan.Create(operation, rootNodes, allNodes);
            OnAfterPlanCompleted(plan);
            return plan;
        }
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
            updatedPlanSteps = updatedPlanSteps.Replace(step, AddVariableDefinitions(step));

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
                        description: null,
                        requirement.Type,
                        null,
                        []);
            }

            var rewrittenNode = s_forwardVariableRewriter.Rewrite(step.Definition, forwardVariableContext);
            if (rewrittenNode is OperationDefinitionNode rewrittenOperationNode
                && !ReferenceEquals(rewrittenOperationNode, step.Definition))
            {
                return step with { Definition = rewrittenOperationNode };
            }

            return step;
        }
    }

    private static void BuildExecutionNodes(
        ImmutableList<OperationPlanStep> planSteps,
        HashSet<int> completedSteps,
        Dictionary<int, ExecutionNode> completedNodes,
        Dictionary<int, HashSet<int>> dependencyLookup,
        bool hasVariables)
    {
        var readySteps = planSteps.Where(t => !dependencyLookup.ContainsKey(t.Id)).ToList();
        List<string>? variables = null;

        while (completedSteps.Count < planSteps.Count)
        {
            foreach (var step in readySteps)
            {
                if (!completedSteps.Add(step.Id))
                {
                    continue;
                }

                var requirements = Array.Empty<OperationRequirement>();

                if (!step.Requirements.IsEmpty)
                {
                    var temp = new List<OperationRequirement>();

                    foreach (var (_, requirement) in step.Requirements.OrderBy(t => t.Key))
                    {
                        temp.Add(requirement);
                    }

                    requirements = temp.ToArray();
                }

                variables?.Clear();

                if (hasVariables && step.Definition.VariableDefinitions.Count > 0)
                {
                    variables ??= [];

                    foreach (var variableDef in step.Definition.VariableDefinitions)
                    {
                        if (requirements.Any(r => r.Key == variableDef.Variable.Name.Value))
                        {
                            continue;
                        }

                        variables.Add(variableDef.Variable.Name.Value);
                    }
                }

                var operationNode = new OperationExecutionNode(
                    step.Id,
                    step.Definition.ToSourceText(),
                    step.SchemaName,
                    step.Target,
                    step.Source,
                    requirements,
                    variables?.Count > 0 ? variables.ToArray() : [],
                    GetResponseNamesFromPath(step.Definition, step.Source));

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

                dependencyNode.AddDependent(node);
                node.AddDependency(dependencyNode);
            }
        }
    }

    private static string[] GetResponseNamesFromPath(
        OperationDefinitionNode operationDefinition,
        SelectionPath path)
    {
        var selectionSet = GetSelectionSetNodeFromPath(operationDefinition, path);

        if (selectionSet is null)
        {
            return [];
        }

        var responseNames = new List<string>();

        var stack = new Stack<ISelectionNode>(selectionSet.Selections);

        while (stack.TryPop(out var selection))
        {
            switch (selection)
            {
                case FieldNode fieldNode:
                    responseNames.Add(fieldNode.Alias?.Value ?? fieldNode.Name.Value);
                    break;

                case InlineFragmentNode inlineFragmentNode:
                    foreach (var child in inlineFragmentNode.SelectionSet.Selections)
                    {
                        stack.Push(child);
                    }
                    break;
            }
        }

        return [.. responseNames];
    }

    private static SelectionSetNode? GetSelectionSetNodeFromPath(
        OperationDefinitionNode operationDefinition,
        SelectionPath path)
    {
        var current = operationDefinition.SelectionSet;

        if (path.IsRoot)
        {
            return current;
        }

        for (var i = path.Segments.Length - 1; i >= 0; i--)
        {
            var segment = path.Segments[i];

            switch (segment.Kind)
            {
                case SelectionPathSegmentKind.InlineFragment:
                {
                    var selection = current.Selections
                        .OfType<InlineFragmentNode>()
                        .FirstOrDefault(s => s.TypeCondition?.Name.Value == segment.Name);

                    if (selection is null)
                    {
                        return null;
                    }

                    current = selection.SelectionSet;
                    break;
                }
                case SelectionPathSegmentKind.Field:
                {
                    var selection = current.Selections
                        .OfType<FieldNode>()
                        .FirstOrDefault(s => s.Alias?.Value == segment.Name || s.Name.Value == segment.Name);

                    if (selection?.SelectionSet is null)
                    {
                        return null;
                    }

                    current = selection.SelectionSet;
                    break;
                }
            }
        }

        return current;
    }

    private void OnAfterPlanCompleted(OperationPlan plan)
    {
        if (_interceptors.Length == 1)
        {
            _interceptors[0].OnAfterPlanCompleted(plan);
        }
        else if (_interceptors.Length > 1)
        {
            foreach (var interceptor in _interceptors)
            {
                interceptor.OnAfterPlanCompleted(plan);
            }
        }
    }
}

file static class Extensions
{
    private static readonly Encoding s_encoding = Encoding.UTF8;

    public static bool HasIntrospectionFields(this Operation operation)
    {
        foreach (var selection in operation.RootSelectionSet.Selections)
        {
            if (selection.Field.IsIntrospectionField)
            {
                return true;
            }
        }

        return false;
    }

    public static Selection[] GetIntrospectionSelections(this Operation operation)
    {
        var selections = new List<Selection>(operation.RootSelectionSet.Selections.Length);

        foreach (var selection in operation.RootSelectionSet.Selections)
        {
            if (selection.Field.IsIntrospectionField)
            {
                selections.Add(selection);
            }
        }

        return selections.ToArray();
    }

    public static OperationSourceText ToSourceText(this OperationDefinitionNode operation)
    {
        var sourceText = operation.ToString(indented: true);
        var sourceTextUtf8 = s_encoding.GetBytes(sourceText);
#if NET9_0_OR_GREATER
        var operationHash = Convert.ToHexStringLower(SHA256.HashData(sourceTextUtf8));
#else
        var operationHash = Convert.ToHexString(SHA256.HashData(sourceTextUtf8)).ToLowerInvariant();
#endif
        return new OperationSourceText(operation.Name!.Value, operation.Operation, sourceText, operationHash);
    }
}
