using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Describes the inputs and selected outputs of a lookup at one target in the operation plan.
/// </summary>
internal sealed class RequirementDependency
{
    private static readonly ConditionalWeakTable<IOperationPlan, RequirementDependency[]> s_dependencies = new();
    private readonly IOperationPlanNode _owner;

    private RequirementDependency(
        IOperationPlanNode owner,
        SelectionPath target,
        ImmutableArray<OperationRequirement> requirements,
        ResultSelectionSet selections)
    {
        _owner = owner;
        Target = target;
        Requirements = requirements;
        Selections = selections;
    }

    public SelectionPath Target { get; }

    public ImmutableArray<OperationRequirement> Requirements { get; }

    public ResultSelectionSet Selections { get; }

    public bool IsSkipped(IVariableValueCollection variables)
        => _owner switch
        {
            ExecutionNode node => ExecutionNode.IsSkipped(variables, node.Conditions, node.ConditionAlternatives),
            OperationDefinition op => ExecutionNode.IsSkipped(variables, op.Conditions, op.ConditionAlternatives),
            _ => false
        };

    /// <summary>
    /// Gets the lookup dependencies in input-before-output order, including operations within batches.
    /// </summary>
    public static ReadOnlySpan<RequirementDependency> GetDependencies(IOperationPlan plan)
        => s_dependencies.GetValue(plan, static key => Collect(key));

    private static RequirementDependency[] Collect(IOperationPlan plan)
    {
        var dependencies = new List<RequirementDependency>();
        var visited = new HashSet<IOperationPlanNode>(ReferenceEqualityComparer.Instance);
        foreach (var node in plan.AllNodes)
        {
            Visit(node);
        }

        return dependencies.ToArray();

        void Visit(IOperationPlanNode node)
        {
            if (!visited.Add(node))
            {
                return;
            }

            foreach (var dependency in node.Dependencies)
            {
                Visit(dependency);
            }

            foreach (var dependency in node.OptionalDependencies)
            {
                Visit(dependency);
            }

            switch (node)
            {
                case OperationExecutionNode op when !op.Requirements.IsEmpty:
                    dependencies.Add(new(node, op.Target, op.GetRequirementsArray(), op.ResultSelectionSet));
                    break;
                case ApolloOperationExecutionNode op when !op.Requirements.IsEmpty:
                    dependencies.Add(new(node, op.Target, op.GetRequirementsArray(), op.ResultSelectionSet));
                    break;
                case OperationBatchExecutionNode batch:
                    foreach (var operation in batch.Operations)
                    {
                        Visit(operation);
                    }
                    break;
                case ApolloOperationBatchExecutionNode batch:
                    foreach (var operation in batch.Operations)
                    {
                        Visit(operation);
                    }
                    break;
                case SingleOperationDefinition op when !op.Requirements.IsEmpty:
                    dependencies.Add(new(node, op.Target, op.GetRequirementsArray(), op.ResultSelectionSet));
                    break;
                case BatchOperationDefinition op when !op.Requirements.IsEmpty:
                    foreach (var target in op.Targets)
                    {
                        dependencies.Add(new(node, target, op.GetRequirementsArray(), op.ResultSelectionSet));
                    }
                    break;
            }
        }
    }
}
