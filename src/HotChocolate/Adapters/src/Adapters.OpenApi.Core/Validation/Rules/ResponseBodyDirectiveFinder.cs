using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Adapters.OpenApi.Validation;

internal sealed class ResponseBodyDirectiveFinder()
    : SyntaxVisitor<ResponseBodyDirectiveFinder.Context>(
        new SyntaxVisitorOptions { VisitDirectives = true })
{
    protected override ISyntaxVisitorAction Enter(ISyntaxNode node, Context context)
    {
        if (node is InlineFragmentNode { TypeCondition: not null })
        {
            context.TypedInlineFragmentDepth++;
        }
        else if (node is DirectiveNode directive
            && directive.Name.Value == WellKnownDirectiveNames.ResponseBody)
        {
            context.Count++;

            if (context.TypedInlineFragmentDepth > 0)
            {
                context.HasResponseBodyInTypedInlineFragment = true;
            }
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(ISyntaxNode node, Context context)
    {
        if (node is InlineFragmentNode { TypeCondition: not null })
        {
            context.TypedInlineFragmentDepth--;
        }

        return Continue;
    }

    public sealed class Context
    {
        public int Count { get; set; }

        public int TypedInlineFragmentDepth { get; set; }

        public bool HasResponseBodyInTypedInlineFragment { get; set; }
    }
}
