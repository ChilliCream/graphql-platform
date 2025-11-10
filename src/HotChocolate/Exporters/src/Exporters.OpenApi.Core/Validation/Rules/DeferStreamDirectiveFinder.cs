using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Exporters.OpenApi.Validation;

internal sealed class DeferStreamDirectiveFinder()
    : SyntaxVisitor<DeferStreamDirectiveFinder.DeferStreamFinderContext>(
        new SyntaxVisitorOptions { VisitDirectives = true })
{
    protected override ISyntaxVisitorAction Enter(ISyntaxNode node, DeferStreamFinderContext context)
    {
        if (context.FoundDirective is not null)
        {
            return Break;
        }

        if (node is DirectiveNode directive)
        {
            var directiveName = directive.Name.Value;
            if (directiveName is "defer" or "stream")
            {
                context.FoundDirective = directiveName;
                return Break;
            }
        }

        return Continue;
    }

    public sealed class DeferStreamFinderContext
    {
        public string? FoundDirective { get; set; }
    }
}
