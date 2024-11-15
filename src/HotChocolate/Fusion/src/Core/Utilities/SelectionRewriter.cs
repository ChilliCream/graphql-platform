using System.Collections.Immutable;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Utilities;

public sealed class SelectionRewriter
{
    public DocumentNode RewriteDocument(DocumentNode document, string? operationName)
    {
        var operation = document.GetOperation(operationName);
        var context = new Context();

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
                    context.Selections.Add(fragmentSpread);
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
            var fieldContext = new Context();

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
        if ((inlineFragment.TypeCondition is null ||
                inlineFragment.TypeCondition.Name.Value.Equals(context.Type, StringComparison.Ordinal)) &&
            inlineFragment.Directives.Count == 0)
        {
            RewriteFields(inlineFragment.SelectionSet, context);
            return;
        }

        var inlineFragmentContext = new Context(inlineFragment.TypeCondition?.Name.Value);

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

    private class Context(string? typeName = null)
    {
        public string? Type => typeName;

        public ImmutableArray<ISelectionNode>.Builder Selections { get; } =
            ImmutableArray.CreateBuilder<ISelectionNode>();

        public HashSet<ISelectionNode> Visited { get; } = new(SyntaxComparer.BySyntax);
    }
}
