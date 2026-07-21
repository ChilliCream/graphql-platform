using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Adapters.OpenApi.Validation;

internal sealed class HoistDirectiveFinder()
    : SyntaxVisitor<HoistDirectiveFinder.Context>(
        new SyntaxVisitorOptions { VisitDirectives = true })
{
    protected override ISyntaxVisitorAction Enter(ISyntaxNode node, Context context)
    {
        if (node is DirectiveNode directive
            && directive.Name.Value == WellKnownDirectiveNames.Hoist)
        {
            context.Count++;
        }

        return Continue;
    }

    public sealed class Context
    {
        public int Count { get; set; }
    }
}
