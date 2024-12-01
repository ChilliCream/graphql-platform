using System.Collections.Immutable;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class InlineFragmentOperationRewriter(CompositeSchema schema)
{
    public DocumentNode RewriteDocument(DocumentNode document, string? operationName)
    {
        var operation = document.GetOperation(operationName);
        var operationType = schema.GetOperationType(operation.Operation);
        var fragmentLookup = CreateFragmentLookup(document);
        var context = new Context(operationType, fragmentLookup);

        RewriteFields(operation.SelectionSet, context);

        var newSelectionSet = new SelectionSetNode(
            null,
            context.Selections.ToImmutable());

        var newOperation = new OperationDefinitionNode(
            null,
            operation.Name,
            operation.Operation,
            operation.VariableDefinitions,
            RewriteDirectives(operation.Directives),
            newSelectionSet);

        return new DocumentNode(ImmutableArray<IDefinitionNode>.Empty.Add(newOperation));
    }

    private void RewriteFields(SelectionSetNode selectionSet, Context context)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    RewriteField(field, context);
                    break;

                case InlineFragmentNode inlineFragment:
                    RewriteInlineFragment(inlineFragment, context);
                    break;

                case FragmentSpreadNode fragmentSpread:
                    InlineFragmentDefinition(fragmentSpread, context);
                    break;
            }
        }
    }

    private void RewriteField(FieldNode fieldNode, Context context)
    {
        if (fieldNode.SelectionSet is null)
        {
            var node = fieldNode.WithLocation(null);

            if (context.Visited.Add(node))
            {
                context.Selections.Add(node);
            }
        }
        else
        {
            var field = ((CompositeComplexType)context.Type).Fields[fieldNode.Name.Value];
            var fieldContext = context.Branch(field.Type.NamedType());

            RewriteFields(fieldNode.SelectionSet, fieldContext);

            var newSelectionSetNode = new SelectionSetNode(
                null,
                fieldContext.Selections.ToImmutable());

            var newFieldNode = new FieldNode(
                null,
                fieldNode.Name,
                fieldNode.Alias,
                RewriteDirectives(fieldNode.Directives),
                RewriteArguments(fieldNode.Arguments),
                newSelectionSetNode);

            if (context.Visited.Add(newFieldNode))
            {
                context.Selections.Add(newFieldNode);
            }
        }
    }

    private void RewriteInlineFragment(InlineFragmentNode inlineFragment, Context context)
    {
        if ((inlineFragment.TypeCondition is  null
            || inlineFragment.TypeCondition.Name.Value.Equals(context.Type.Name, StringComparison.Ordinal))
            && inlineFragment.Directives.Count == 0)
        {
            RewriteFields(inlineFragment.SelectionSet, context);
            return;
        }

        var typeCondition = inlineFragment.TypeCondition is null
            ? context.Type
            : schema.GetType(inlineFragment.TypeCondition.Name.Value);

        var inlineFragmentContext = context.Branch(typeCondition);

        RewriteFields(inlineFragment.SelectionSet, inlineFragmentContext);

        var newSelectionSetNode = new SelectionSetNode(
            null,
            inlineFragmentContext.Selections.ToImmutable());

        var newInlineFragment = new InlineFragmentNode(
            null,
            inlineFragment.TypeCondition,
            RewriteDirectives(inlineFragment.Directives),
            newSelectionSetNode);

        context.Selections.Add(newInlineFragment);
    }

    private void InlineFragmentDefinition(
        FragmentSpreadNode fragmentSpread,
        Context context)
    {
        var fragmentDefinition = context.GetFragmentDefinition(fragmentSpread.Name.Value);
        var typeCondition = schema.GetType(fragmentDefinition.TypeCondition.Name.Value);

        if (fragmentSpread.Directives.Count == 0
            && typeCondition.IsAssignableFrom(context.Type))
        {
            RewriteFields(fragmentDefinition.SelectionSet, context);
        }
        else
        {
            var fragmentContext = context.Branch(typeCondition);

            RewriteFields(fragmentDefinition.SelectionSet, context);

            var selectionSet = new SelectionSetNode(
                null,
                fragmentContext.Selections.ToImmutable());

            var inlineFragment = new InlineFragmentNode(
                null,
                new NamedTypeNode(typeCondition.Name),
                RewriteDirectives(fragmentSpread.Directives),
                selectionSet);

            if (context.Visited.Add(inlineFragment))
            {
                context.Selections.Add(inlineFragment);
            }
        }
    }

    private IReadOnlyList<DirectiveNode> RewriteDirectives(IReadOnlyList<DirectiveNode> directives)
    {
        if (directives.Count == 0)
        {
            return directives;
        }

        if (directives.Count == 1)
        {
            var directive = directives[0];
            var newDirective = new DirectiveNode(directive.Name.Value, RewriteArguments(directive.Arguments));
            return ImmutableArray<DirectiveNode>.Empty.Add(newDirective);
        }

        var buffer = new DirectiveNode[directives.Count];
        for (var i = 0; i < buffer.Length; i++)
        {
            var directive = directives[i];
            buffer[i] = new DirectiveNode(directive.Name.Value, RewriteArguments(directive.Arguments));
        }
        return ImmutableArray.Create(buffer);
    }

    private IReadOnlyList<ArgumentNode> RewriteArguments(IReadOnlyList<ArgumentNode> arguments)
    {
        if (arguments.Count == 0)
        {
            return arguments;
        }

        if (arguments.Count == 1)
        {
            return ImmutableArray<ArgumentNode>.Empty.Add(arguments[0].WithLocation(null));
        }

        var buffer = new ArgumentNode[arguments.Count];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = arguments[i].WithLocation(null);
        }
        return ImmutableArray.Create(buffer);
    }

    private Dictionary<string, FragmentDefinitionNode> CreateFragmentLookup(DocumentNode document)
    {
        var lookup = new Dictionary<string, FragmentDefinitionNode>();

        foreach (var definition in document.Definitions)
        {
            if (definition is FragmentDefinitionNode fragmentDef)
            {
                lookup.Add(fragmentDef.Name.Value, fragmentDef);
            }
        }

        return lookup;
    }

    public readonly ref struct Context(
        ICompositeNamedType type,
        Dictionary<string, FragmentDefinitionNode> fragments)
    {
        public ICompositeNamedType Type { get; } = type;

        public ImmutableArray<ISelectionNode>.Builder Selections { get; } =
            ImmutableArray.CreateBuilder<ISelectionNode>();

        public HashSet<ISelectionNode> Visited { get; } = new(SyntaxComparer.BySyntax);

        public FragmentDefinitionNode GetFragmentDefinition(string name)
            => fragments[name];

        public Context Branch(ICompositeNamedType type)
            => new(type, fragments);
    }
}
