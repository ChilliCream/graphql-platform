using System.Collections;
using HotChocolate.Fusion.Planning.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

internal static class OperationVariableBinder
{
    public static void BindOperationVariables(
        OperationDefinitionNode operationDefinition,
        RootPlanNode rootPlanNode)
    {
        var planNodeBacklog = new Queue<PlanNode>(rootPlanNode.Nodes);
        var selectionBacklog = new Stack<SelectionPlanNode>();
        var variableDefinitions = operationDefinition.VariableDefinitions.ToDictionary(t => t.Variable.Name.Value);
        var usedVariables = new HashSet<string>();

        while (planNodeBacklog.TryDequeue(out var planNode))
        {
            if (planNode is OperationPlanNode operation)
            {
                CollectAndBindUsedVariables(operation, variableDefinitions, usedVariables, selectionBacklog);
            }

            if (planNode is IPlanNodeProvider planNodeProvider)
            {
                foreach (var childNode in planNodeProvider.Nodes)
                {
                    if (childNode is ConditionPlanNode conditionPlanNode)
                    {
                        foreach(var conditionChild in conditionPlanNode.Nodes)
                        {
                            planNodeBacklog.Enqueue(conditionChild);
                        }
                    }
                    else
                    {
                        planNodeBacklog.Enqueue(childNode);
                    }
                }
            }
        }
    }

    private static void CollectAndBindUsedVariables(
        OperationPlanNode operation,
        Dictionary<string, VariableDefinitionNode> variableDefinitions,
        HashSet<string> usedVariables,
        Stack<SelectionPlanNode> backlog)
    {
        usedVariables.Clear();
        backlog.Clear();
        backlog.Push(operation);

        while (backlog.TryPop(out var node))
        {
            if (node is FieldPlanNode field)
            {
                foreach (var argument in field.Arguments)
                {
                    if (argument.Value is VariableNode variable)
                    {
                        usedVariables.Add(variable.Name.Value);
                    }
                }
            }

            foreach (var directive in node.Directives)
            {
                foreach (var argument in directive.Arguments)
                {
                    if (argument.Value is VariableNode variable)
                    {
                        usedVariables.Add(variable.Name.Value);
                    }
                }
            }

            foreach (var condition in node.Conditions)
            {
                usedVariables.Add(condition.VariableName);
            }

            foreach (var selection in node.Selections)
            {
                backlog.Push(selection);
            }
        }

        foreach (var variable in usedVariables)
        {
            operation.AddVariableDefinition(variableDefinitions[variable]);
        }
    }
}
