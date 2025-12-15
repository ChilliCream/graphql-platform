using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Adapters.OpenApi;

public static class FragmentReferenceFinder
{
    private static readonly Visitor s_visitor = new();

    public static FragmentReferenceFinderResult Find(DocumentNode document, OperationDefinitionNode operation)
    {
        var localFragmentLookup = CreateLocalFragmentLookup(document);

        var context = new VisitorContext(localFragmentLookup);
        s_visitor.Visit(document, context);

        return new FragmentReferenceFinderResult(localFragmentLookup, context.ExternalFragmentReferences);
    }

    public static FragmentReferenceFinderResult Find(DocumentNode document, FragmentDefinitionNode fragment)
    {
        var localFragmentLookup = CreateLocalFragmentLookup(document);

        localFragmentLookup.Remove(fragment.Name.Value);

        var context = new VisitorContext(localFragmentLookup);
        s_visitor.Visit(document, context);

        return new FragmentReferenceFinderResult(localFragmentLookup, context.ExternalFragmentReferences);
    }

    public sealed record FragmentReferenceFinderResult(
        Dictionary<string, FragmentDefinitionNode> Local,
        HashSet<string> External);

    private static Dictionary<string, FragmentDefinitionNode> CreateLocalFragmentLookup(DocumentNode document)
    {
        return document.Definitions
            .OfType<FragmentDefinitionNode>()
            .ToDictionary(x => x.Name.Value);
    }

    private sealed class Visitor : SyntaxVisitor<VisitorContext>
    {
        protected override ISyntaxVisitorAction Enter(ISyntaxNode node, VisitorContext context)
        {
            if (node is FragmentSpreadNode fragmentSpread)
            {
                var fragmentName = fragmentSpread.Name.Value;
                if (!context.LocalFragmentLookup.ContainsKey(fragmentName))
                {
                    context.ExternalFragmentReferences.Add(fragmentName);
                }
            }

            return Continue;
        }
    }

    private sealed class VisitorContext(
        Dictionary<string, FragmentDefinitionNode> localFragmentLookup)
    {
        public HashSet<string> ExternalFragmentReferences { get; } = [];

        public Dictionary<string, FragmentDefinitionNode> LocalFragmentLookup { get; } = localFragmentLookup;
    }
}
