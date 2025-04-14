using System.Collections.Immutable;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Fusion.Planning;

public class TestMe
{
    private static readonly HasEmptySelectionSetVisitor _hasEmptySelectionSetVisitor = new();
    private static readonly ForwardVariableRewriter _forwardVariableRewriter = new();

    public ExecutionPlan BuildExecutionTree(
        OperationDefinitionNode originalOperation,
        ImmutableList<OperationPlanStep> planSteps)
    {
        var dependencies = new Dictionary<int, HashSet<int>>();
        var updatedPlanSteps = planSteps;
        var emptySelectionSetContext = new HasEmptySelectionSetVisitor.Context();
        var forwardVariableContext = new ForwardVariableRewriter.Context();

        foreach (var variableDef in originalOperation.VariableDefinitions)
        {
            forwardVariableContext.Variables[variableDef.Variable.Name.Value] = variableDef;
        }

        foreach (var step in planSteps)
        {
            emptySelectionSetContext.HasEmptySelectionSet = false;
            _hasEmptySelectionSetVisitor.Visit(step.Definition, emptySelectionSetContext);

            if (emptySelectionSetContext.HasEmptySelectionSet)
            {
                updatedPlanSteps = updatedPlanSteps.Remove(step);
                continue;
            }

            forwardVariableContext.UsedVariables.Clear();
            forwardVariableContext.Requirements.Clear();

            foreach (var(key, requirement) in step.Requirements.OrderBy(t => t.Key))
            {
                foreach (var argument in requirement.Arguments)
                {
                    var requirementKey = $"{key}_{argument.Name}";
                    forwardVariableContext.Requirements[key] =
                        new VariableDefinitionNode(
                            null,
                            new VariableNode(null, new NameNode(requirementKey)),
                            argument.Type,
                            null,
                            Array.Empty<DirectiveNode>());
                }
            }

            if (_forwardVariableRewriter.Rewrite(step.Definition, forwardVariableContext) is OperationDefinitionNode rewritten
                && !ReferenceEquals(rewritten, step.Definition))
            {
                updatedPlanSteps = updatedPlanSteps.Replace(step, step with { Definition = rewritten });
            }

            foreach (var dependent in step.Dependents)
            {
                if (!dependencies.TryGetValue(dependent, out var stepDependencies))
                {
                    stepDependencies = [];
                    dependencies[dependent] = stepDependencies;
                }

                stepDependencies.Add(step.Id);
            }
        }

        planSteps = updatedPlanSteps;

        var completedSteps = new HashSet<int>();
        var completedNodes = new Dictionary<int, ExecutionNode>();
        var readySteps = planSteps.Where(t => !dependencies.ContainsKey(t.Id)).ToList();

        while (completedSteps.Count < planSteps.Count)
        {
            foreach (var step in readySteps)
            {
                if (!completedSteps.Add(step.Id))
                {
                    continue;
                }

                var operationNode = new OperationExecutionNode
                {
                    Id = step.Id, Definition = step.Definition, SchemaName = step.SchemaName
                };

                completedNodes.Add(step.Id, operationNode);
            }

            readySteps.Clear();

            foreach (var step in planSteps)
            {
                if (dependencies.TryGetValue(step.Id, out var stepDependencies)
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

        foreach (var (nodeId, stepDependencies) in dependencies)
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

        var rootNodes = planSteps
            .Where(t => !dependencies.ContainsKey(t.Id))
            .Select(t => completedNodes[t.Id])
            .ToImmutableArray();

        return new ExecutionPlan
        {
            RootNodes = rootNodes,
            AllNodes = [..completedNodes.OrderBy(t => t.Key).Select(t => t.Value)]
        };
    }

    private sealed class HasEmptySelectionSetVisitor : SyntaxWalker<HasEmptySelectionSetVisitor.Context>
    {
        protected override ISyntaxVisitorAction Enter(
            ISyntaxNode node,
            Context context)
        {
            if (node is SelectionSetNode { Selections.Count: 0 })
            {
                context.HasEmptySelectionSet = true;
                return Break;
            }

            return base.Enter(node, context);
        }


        public sealed class Context
        {
            public bool HasEmptySelectionSet { get; set; }
        }
    }

    private sealed class ForwardVariableRewriter : SyntaxRewriter<ForwardVariableRewriter.Context>
    {
        protected override Context OnEnter(ISyntaxNode node, Context context)
        {
            if (node is VariableNode variableNode)
            {
                context.UsedVariables.Add(variableNode.Name.Value);
            }

            return base.OnEnter(node, context);
        }

        protected override OperationDefinitionNode? RewriteOperationDefinition(OperationDefinitionNode node,
            Context context)
        {
            var rewritten = base.RewriteOperationDefinition(node, context);

            if (rewritten is null || context.UsedVariables.Count == 0)
            {
                return rewritten;
            }

            var variableDefinitions = new List<VariableDefinitionNode>();

            foreach (var variableDef in node.VariableDefinitions)
            {
                if (!context.UsedVariables.Contains(variableDef.Variable.Name.Value))
                {
                    variableDefinitions.Add(variableDef);
                }
            }

            foreach (var requirement in context.Requirements)
            {
                if (!context.UsedVariables.Contains(requirement.Key))
                {
                    variableDefinitions.Add(requirement.Value);
                }
            }

            return rewritten.WithVariableDefinitions(variableDefinitions);
        }


        public sealed class Context
        {
            public OrderedDictionary<string, VariableDefinitionNode> Variables { get; } = new();
            public OrderedDictionary<string, VariableDefinitionNode> Requirements { get; } = new();
            public HashSet<string> UsedVariables { get; } = new();
        }
    }
}

public abstract record ExecutionNode
{
    public required int Id { get; init; }
}

public record ExecutionPlan
{
    public ImmutableArray<ExecutionNode> RootNodes { get; init; } = [];

    public ImmutableArray<ExecutionNode> AllNodes { get; init; } = [];
}

public record OperationExecutionNode : ExecutionNode
{
    public required OperationDefinitionNode Definition { get; init; }

    public required string SchemaName { get; init; }

    public ImmutableArray<ExecutionNode> Dependencies { get; init; } = [];

    public ImmutableArray<ExecutionNode> Dependents { get; init; } = [];

    public ImmutableArray<OperationRequirement> Requirements { get; init; } = [];
}

public record OperationRequirement(
    string Key,
    SelectionPath Path,
    FieldPath Map);
