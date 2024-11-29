using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// This operation printer is made for testing purposes.
/// </summary>
internal static class OperationPrinter
{
    public static string Print(Operation operation)
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

                var arguments = new List<ArgumentNode> { new("flag", new IntValueNode(flag)), };

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

        var definitions = new List<IDefinitionNode>();
        var context = new PrintContext(operation, operation.SelectionVariants[0], definitions);

        var selectionSet = Visit(context);

        var operationDefinition = new OperationDefinitionNode(
            operation.Definition.Location,
            operation.Definition.Name,
            operation.Definition.Operation,
            operation.Definition.VariableDefinitions,
            directives,
            selectionSet);
        definitions.Insert(0, operationDefinition);

        return new DocumentNode(definitions).ToString();
    }

    private static SelectionSetNode Visit(PrintContext context)
    {
        var fragments = new List<InlineFragmentNode>();

        foreach (var objectType in context.SelectionVariants.GetPossibleTypes())
        {
            var typeContext = (ObjectType)objectType;
            var selectionSet = context.SelectionVariants.GetSelectionSet(typeContext);
            var selections = new List<ISelectionNode>();

            fragments.Add(new InlineFragmentNode(
                null,
                new NamedTypeNode(typeContext.Name),
                Array.Empty<DirectiveNode>(),
                CreateSelectionSet(context, selectionSet, selections)));

            foreach (var fragment in selectionSet.Fragments)
            {
                if (context.GetOrCreateFragmentName(fragment.SelectionSet.Id, out var fragmentName))
                {
                    var index = context.Definitions.Count;
                    context.Definitions.Add(default!);

                    context.Definitions[index] =
                        new FragmentDefinitionNode(
                            null,
                            new(fragmentName),
                            Array.Empty<VariableDefinitionNode>(),
                            new NamedTypeNode(typeContext.Name),
                            Array.Empty<DirectiveNode>(),
                            CreateSelectionSet(context, fragment.SelectionSet, []));
                }

                selections.Add(
                    new FragmentSpreadNode(
                        null,
                        new(fragmentName),
                        new[] { new DirectiveNode("defer"), }));
            }
        }

        return new SelectionSetNode(fragments);
    }

    private static SelectionSetNode CreateSelectionSet(
        PrintContext context,
        ISelectionSet selectionSet,
        List<ISelectionNode> selections)
    {
        foreach (var selection in selectionSet.Selections)
        {
            var selectionSetId = ((Selection)selection).SelectionSetId;
            SelectionSetNode? selectionSetNode = null;

            if (selection.SelectionSet is not null)
            {
                var childSelectionSet = context.Operation.SelectionVariants[selectionSetId];
                selectionSetNode = Visit(context.Branch(childSelectionSet));
            }

            selections.Add(CreateSelection(selection, selectionSetNode));
        }

        return new SelectionSetNode(selections);
    }

    private static FieldNode CreateSelection(
        ISelection selection,
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
            selection.SyntaxNode.Name,
            selection.SyntaxNode.Alias,
            directives,
            selection.SyntaxNode.Arguments,
            selectionSet);
    }

    private static DirectiveNode CreateExecutionInfo(ISelection selection)
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

    private sealed class PrintContext
    {
        private readonly GlobalState _state;

        public PrintContext(
            Operation operation,
            ISelectionVariants selectionVariants,
            List<IDefinitionNode> definitions)
        {
            Operation = operation;
            SelectionVariants = selectionVariants;
            Definitions = definitions;
            _state = new();
        }

        private PrintContext(
            Operation operation,
            ISelectionVariants selectionVariants,
            List<IDefinitionNode> definitions,
            GlobalState state)
        {
            Operation = operation;
            SelectionVariants = selectionVariants;
            Definitions = definitions;
            _state = state;
        }

        public Operation Operation { get; }

        public ISelectionVariants SelectionVariants { get; }

        public List<IDefinitionNode> Definitions { get; }

        public bool GetOrCreateFragmentName(int selectionSetId, out string fragmentName)
        {
            if (!_state.FragmentNames.TryGetValue(selectionSetId, out var name))
            {
                name = $"Fragment_{_state.FragmentId++}";
                _state.FragmentNames.Add(selectionSetId, name);
                fragmentName = name;
                return true;
            }

            fragmentName = name;
            return false;
        }

        public PrintContext Branch(ISelectionVariants selectionVariants)
            => new(Operation, selectionVariants, Definitions, _state);

        private sealed class GlobalState
        {
            public int FragmentId;
            public readonly Dictionary<int, string> FragmentNames = new();
        }
    }
}
