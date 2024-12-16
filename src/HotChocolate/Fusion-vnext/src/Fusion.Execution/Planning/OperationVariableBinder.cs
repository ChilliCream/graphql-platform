using HotChocolate.Fusion.Planning.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

internal static class OperationVariableBinder
{
    public static void BindOperationVariables(
        OperationDefinitionNode operationDefinition,
        RequestPlanNode operationPlan)
    {
        var operationBacklog = new Stack<OperationPlanNode>(operationPlan.Operations);
        var selectionBacklog = new Stack<SelectionPlanNode>();
        var variableDefinitions = operationDefinition.VariableDefinitions.ToDictionary(t => t.Variable.Name.Value);
        var usedVariables = new HashSet<string>();

        while (operationBacklog.TryPop(out var operation))
        {
            CollectAndBindUsedVariables(operation, variableDefinitions, usedVariables, selectionBacklog);

            foreach (var child in operation.Dependants)
            {
                operationBacklog.Push(child);
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

            foreach (var selection in node.Selections)
            {
                backlog.Push(selection);
            }
        }

        foreach (var variable in usedVariables)
        {
            if (variableDefinitions.TryGetValue(variable, out var variableDefinition))
            {
                operation.AddVariableDefinition(variableDefinition);
            }
        }

        foreach (var requirement in operation.Requirements.Values)
        {
            var variable = new VariableDefinitionNode(
                null,
                new VariableNode(requirement.Name),
                requirement.Type,
                null,
                Array.Empty<DirectiveNode>());

            operation.AddVariableDefinition(variable);
        }
    }
}
