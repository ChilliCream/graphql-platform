using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Utilities;

namespace StrawberryShake.CodeGeneration.Utilities;

internal sealed class FragmentRewriter : SyntaxRewriter<FragmentRewriter.Context>
{
    protected override FragmentDefinitionNode RewriteFragmentDefinition(
        FragmentDefinitionNode node,
        Context context)
    {
        if (context.Deferred.Contains(node.Name.Value))
        {
            var selections = node.SelectionSet.Selections.ToList();

            selections.Insert(
                0,
                new FieldNode(
                    null,
                    new("__typename"),
                    new NameNode($"_is{node.Name.Value}Fulfilled"),
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null));

            node = node.WithSelectionSet(node.SelectionSet.WithSelections(selections));
        }

        return base.RewriteFragmentDefinition(node, context)!;
    }

    public static DocumentNode Rewrite(DocumentNode document)
    {
        var context = new Context();

        SyntaxVisitor
            .Create(node =>
            {
                if (node is FragmentSpreadNode spread &&
                    spread.Directives.Any(t => t.Name.Value.EqualsOrdinal(WellKnownDirectives.Defer)))
                {
                    context.Deferred.Add(spread.Name.Value);
                }

                return SyntaxVisitor.Continue;
            })
            .Visit(document);

        var rewriter = new FragmentRewriter();
        return rewriter.RewriteDocument(document, context)!;
    }

    internal sealed class Context
    {
        public HashSet<string> Deferred { get; } = [];
    }
}
