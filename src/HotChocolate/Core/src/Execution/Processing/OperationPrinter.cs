using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal static class OperationPrinter
{
    public static string Print(IPreparedOperation2 operation)
    {
        var directives = operation.Definition.Directives;

        if (operation.IncludeConditions.Count > 0)
        {
            var temp = operation.Definition.Directives.ToList();
            directives = temp;

            for (var i = 0; i < operation.IncludeConditions.Count; i++)
            {
                var includeCondition = operation.IncludeConditions[i];
                long flag = 2 ^ i;

                var arguments = new List<ArgumentNode> { new("flag", new IntValueNode(flag)) };

                if (includeCondition.Skip is BooleanValueNode)
                {
                    arguments.Add(new ArgumentNode("skip", includeCondition.Skip));
                }

                if (includeCondition.Include is BooleanValueNode)
                {
                    arguments.Add(new ArgumentNode("include", includeCondition.Include));
                }

                temp.Add(new DirectiveNode("includeCondition", arguments));
            }
        }

        var selectionSet = Visit(operation, operation.SelectionVariants[0]);

        var operationDefinition = new OperationDefinitionNode(
            operation.Definition.Location,
            operation.Definition.Name,
            operation.Definition.Operation,
            operation.Definition.VariableDefinitions,
            directives,
            selectionSet);

        return new DocumentNode(new[] { operationDefinition }).ToString();
    }

    private static SelectionSetNode Visit(IPreparedOperation2 operation, ISelectionVariants2 selectionVariants)
    {
        var fragments = new List<InlineFragmentNode>();

        foreach (IObjectType objectType in selectionVariants.GetPossibleTypes())
        {
            var typeContext = (ObjectType)objectType;
            var selections = new List<ISelectionNode>();

            foreach (var selection in selectionVariants.GetSelectionSet(typeContext).Selections)
            {
                var selectionSetId = ((Selection2)selection).SelectionSetId;

                SelectionSetNode? selectionSetNode =
                    selection.SelectionSet is not null
                        ? Visit(operation, operation.SelectionVariants[selectionSetId])
                        : null;
                selections.Add(CreateSelection(selection, selectionSetNode));
            }

            fragments.Add(new InlineFragmentNode(
                null,
                new NamedTypeNode(typeContext.Name),
                Array.Empty<DirectiveNode>(),
                new SelectionSetNode(selections)));
        }

        return new SelectionSetNode(fragments);
    }

    private static FieldNode CreateSelection(ISelection2 selection, SelectionSetNode? selectionSet)
    {
        var directives = new List<DirectiveNode>();

        if (selection.IsConditional)
        {
            directives.Add(new DirectiveNode("conditional"));
        }

        directives.Add(CreateExecutionInfo(selection));

        return new FieldNode(
            null,
            selection.SyntaxNode.Name,
            selection.SyntaxNode.Alias,
            null,
            directives,
            selection.SyntaxNode.Arguments,
            selectionSet);
    }

    private static DirectiveNode CreateExecutionInfo(ISelection2 selection)
    {
        var arguments = new ArgumentNode[selection.IsInternal ? 4 : 3];
        arguments[0] = new ArgumentNode("id", new IntValueNode(selection.Id));
        arguments[1] = new ArgumentNode("kind", new EnumValueNode(selection.Strategy));

        if (selection.Type.IsListType())
        {
            if (selection.Type.NamedType().IsLeafType())
            {
                arguments[2] = new ArgumentNode("type", new EnumValueNode("LEAF_LIST"));
            }
            else
            {
                arguments[2] = new ArgumentNode("type", new EnumValueNode("COMPOSITE_LIST"));
            }
        }
        else if (selection.Type.IsCompositeType())
        {
            arguments[2] = new ArgumentNode("type", new EnumValueNode("COMPOSITE"));
        }
        else if (selection.Type.IsLeafType())
        {
            arguments[2] = new ArgumentNode("type", new EnumValueNode("LEAF"));
        }

        if (selection.IsInternal)
        {
            arguments[3] = new ArgumentNode("internal", BooleanValueNode.True);
        }

        return new DirectiveNode("__execute", arguments);
    }
}
