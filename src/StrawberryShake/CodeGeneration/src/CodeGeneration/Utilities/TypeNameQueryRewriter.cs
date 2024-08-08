using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Utilities;
using static StrawberryShake.CodeGeneration.WellKnownNames;

namespace StrawberryShake.CodeGeneration.Utilities;

internal sealed class TypeNameQueryRewriter : SyntaxRewriter<TypeNameQueryRewriter.Context>
{
    private static readonly FieldNode _typeNameField = new(
        null,
        new NameNode(TypeName),
        null,
        Array.Empty<DirectiveNode>(),
        Array.Empty<ArgumentNode>(),
        null);

    protected override OperationDefinitionNode? RewriteOperationDefinition(
        OperationDefinitionNode node,
        Context context)
    {
        context.Nodes.Push(node);
        var rewritten = base.RewriteOperationDefinition(node, context);
        context.Nodes.Pop();
        return rewritten;
    }

    protected override FieldNode? RewriteField(
        FieldNode node,
        Context context)
    {
        context.Nodes.Push(node);
        var rewritten = base.RewriteField(node, context);
        context.Nodes.Pop();
        return rewritten;
    }

    protected override InlineFragmentNode? RewriteInlineFragment(
        InlineFragmentNode node,
        Context context)
    {
        context.Nodes.Push(node);
        var rewritten = base.RewriteInlineFragment(node, context);
        context.Nodes.Pop();
        return rewritten;
    }

    protected override FragmentDefinitionNode? RewriteFragmentDefinition(
        FragmentDefinitionNode node,
        Context context)
    {
        context.Nodes.Push(node);
        var rewritten = base.RewriteFragmentDefinition(node, context);
        context.Nodes.Pop();
        return rewritten;
    }

    protected override SelectionSetNode? RewriteSelectionSet(
        SelectionSetNode node,
        Context context)
    {
        var current = base.RewriteSelectionSet(node, context);

        if (current is not null &&
            context.Nodes.Peek() is FieldNode &&
            !current.Selections
                .OfType<FieldNode>()
                .Any(t => t.Alias is null && t.Name.Value.EqualsOrdinal(TypeName)))
        {
            var selections = current.Selections.ToList();
            selections.Insert(0, _typeNameField);
            current = current.WithSelections(selections);
        }

        return current;
    }

    public static DocumentNode? Rewrite(DocumentNode document)
    {
        var rewriter = new TypeNameQueryRewriter();
        return rewriter.RewriteDocument(document, new());
    }

    public class Context
    {
        public Stack<ISyntaxNode> Nodes { get; } = new();
    }
}
