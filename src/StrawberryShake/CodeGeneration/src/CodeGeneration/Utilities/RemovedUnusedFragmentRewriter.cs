using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace StrawberryShake.CodeGeneration.Utilities;

internal sealed class RemovedUnusedFragmentRewriter
    : SyntaxRewriter<RemovedUnusedFragmentRewriter.Context>
{
    protected override DocumentNode RewriteDocument(DocumentNode node, Context context)
    {
        var definitions = node.Definitions.ToList();

        foreach (var fragmentDefinition in
            node.Definitions.OfType<FragmentDefinitionNode>())
        {
            if (!context.Used.Contains(fragmentDefinition.Name.Value))
            {
                definitions.Remove(fragmentDefinition);
            }
        }

        return node.Definitions.Count > definitions.Count
            ? node.WithDefinitions(definitions)
            : node;
    }

    public static DocumentNode Rewrite(DocumentNode document)
    {
        var context = new Context();

        SyntaxVisitor
            .Create(node =>
            {
                if (node is FragmentSpreadNode spread)
                {
                    context.Used.Add(spread.Name.Value);
                }

                return SyntaxVisitor.Continue;
            })
            .Visit(document);

        var rewriter = new RemovedUnusedFragmentRewriter();
        return rewriter.RewriteDocument(document, context);
    }

    internal sealed class Context
    {
        public HashSet<string> Used { get; } = [];
    }
}
