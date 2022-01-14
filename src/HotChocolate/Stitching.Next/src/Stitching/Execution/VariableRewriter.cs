using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Execution;

internal sealed class VariableRewriter : QuerySyntaxRewriter<OperationPlanerContext>
{
    protected override OperationDefinitionNode RewriteOperationDefinition(
        OperationDefinitionNode node,
        OperationPlanerContext context)
    {
        if (context.VariableDefinitions.Count == 0)
        {
            foreach (var varDef in context.Operation.Definition.VariableDefinitions)
            {
                context.VariableDefinitions.Add(varDef.Variable.Name.Value, varDef);
            }
        }

        context.Processed.Clear();

        foreach (var varDef in node.VariableDefinitions)
        {
            context.Processed.Add(varDef.Variable.Name.Value);
        }

        node = base.RewriteOperationDefinition(node, context);

        if (context.Variables.Count > 0)
        {
            List<VariableDefinitionNode>? variables = null;

            foreach (var variableName in context.Variables)
            {
                if (context.Processed.Add(variableName))
                {
                    if (variables is null)
                    {
                        variables = new List<VariableDefinitionNode>();
                        variables.AddRange(node.VariableDefinitions);
                    }

                    variables.Add(context.VariableDefinitions[variableName]);
                }
            }

            if (variables is not null)
            {
                node = node.WithVariableDefinitions(variables);
            }
        }

        return node;
    }

    protected override VariableDefinitionNode RewriteVariableDefinition(
        VariableDefinitionNode node,
        OperationPlanerContext context)
        => node;

    protected override VariableNode RewriteVariable(
        VariableNode node,
        OperationPlanerContext context)
    {
        VariableNode variable = base.RewriteVariable(node, context);
        context.Variables.Add(variable.Name.Value);
        return variable;
    }
}
