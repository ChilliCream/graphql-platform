using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// This operation printer is made for testing purposes.
/// It prints the compiled operation with execution info directives.
/// </summary>
internal static class OperationPrinter
{
    public static string Print(Operation operation)
    {
        var definitions = new List<IDefinitionNode>();
        var context = new PrintContext(operation, definitions);

        // Create the root selection set wrapped in an inline fragment for the root type
        var selectionSet = VisitRootSelectionSet(context, operation.RootSelectionSet);

        var operationDefinition = new OperationDefinitionNode(
            operation.Definition.Location,
            operation.Definition.Name,
            operation.Definition.Description,
            operation.Definition.Operation,
            operation.Definition.VariableDefinitions,
            operation.Definition.Directives,
            selectionSet);
        definitions.Insert(0, operationDefinition);

        return new DocumentNode(definitions).ToString();
    }

    private static SelectionSetNode VisitRootSelectionSet(PrintContext context, SelectionSet selectionSet)
    {
        // Wrap the root selections in an inline fragment for the root type
        var selections = new List<ISelectionNode>();
        CreateSelectionsForSelectionSet(context, selectionSet, selections);

        var inlineFragment = new InlineFragmentNode(
            null,
            new NamedTypeNode(selectionSet.Type.Name),
            [],
            new SelectionSetNode(selections));

        return new SelectionSetNode([inlineFragment]);
    }

    private static void CreateSelectionsForSelectionSet(
        PrintContext context,
        SelectionSet selectionSet,
        List<ISelectionNode> selections)
    {
        foreach (var selection in selectionSet.Selections)
        {
            SelectionSetNode? childSelectionSet = null;

            if (selection.HasSelections)
            {
                childSelectionSet = VisitSelectionWithChildren(context, selection);
            }

            selections.Add(CreateFieldSelection(selection, childSelectionSet));
        }
    }

    private static SelectionSetNode VisitSelectionWithChildren(PrintContext context, Selection selection)
    {
        var fragments = new List<InlineFragmentNode>();
        var possibleTypes = context.Operation.GetPossibleTypes(selection);

        foreach (var objectType in possibleTypes)
        {
            if (objectType is not ObjectType concreteType)
            {
                continue;
            }

            var childSelectionSet = context.Operation.GetSelectionSet(selection, concreteType);
            var childSelections = new List<ISelectionNode>();

            CreateSelectionsForSelectionSet(context, childSelectionSet, childSelections);

            fragments.Add(new InlineFragmentNode(
                null,
                new NamedTypeNode(concreteType.Name),
                [],
                new SelectionSetNode(childSelections)));
        }

        return new SelectionSetNode(fragments);
    }

    private static FieldNode CreateFieldSelection(
        Selection selection,
        SelectionSetNode? selectionSet)
    {
        var directives = new List<DirectiveNode>();

        if (selection.IsConditional)
        {
            directives.Add(new DirectiveNode("conditional"));
        }

        directives.Add(CreateExecutionInfo(selection));

        return new FieldNode(
            null,
            selection.SyntaxNodes[0].Node.Name,
            selection.SyntaxNodes[0].Node.Alias,
            directives,
            selection.SyntaxNodes[0].Node.Arguments,
            selectionSet);
    }

    private static DirectiveNode CreateExecutionInfo(Selection selection)
    {
        var argumentCount = 3;

        if (selection.IsInternal)
        {
            argumentCount++;
        }

        if (selection.HasDeferUsage)
        {
            argumentCount++;
        }

        var index = 0;
        var arguments = new ArgumentNode[argumentCount];
        arguments[index++] = new ArgumentNode("id", new IntValueNode(selection.Id));
        arguments[index++] = new ArgumentNode("kind", new EnumValueNode(selection.Strategy.ToString().ToUpperInvariant()));

        if (selection.IsList)
        {
            if (selection.IsLeaf)
            {
                arguments[index++] = new ArgumentNode("type", new EnumValueNode("LEAF_LIST"));
            }
            else
            {
                arguments[index++] = new ArgumentNode("type", new EnumValueNode("COMPOSITE_LIST"));
            }
        }
        else if (selection.Type.IsCompositeType())
        {
            arguments[index++] = new ArgumentNode("type", new EnumValueNode("COMPOSITE"));
        }
        else if (selection.IsLeaf)
        {
            arguments[index++] = new ArgumentNode("type", new EnumValueNode("LEAF"));
        }

        if (selection.IsInternal)
        {
            arguments[index++] = new ArgumentNode("internal", BooleanValueNode.True);
        }

        if (selection.HasDeferUsage)
        {
            arguments[index++] = new ArgumentNode("isDeferred", BooleanValueNode.True);
        }

        return new DirectiveNode("__execute", arguments);
    }

    private sealed class PrintContext(Operation operation, List<IDefinitionNode> definitions)
    {
        public Operation Operation { get; } = operation;

        public List<IDefinitionNode> Definitions { get; } = definitions;
    }
}
