using HotChocolate.Language;
using HotChocolate.Validation;

namespace HotChocolate.Authorization;

internal sealed class AuthorizeValidationRule(AuthorizationCache cache) : IDocumentValidatorRule
{
    private readonly AuthorizeValidationVisitor _visitor = new();
    private readonly AuthorizationCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    public ushort Priority => ushort.MaxValue;

    public bool IsCacheable => false;

    public void Validate(IDocumentValidatorContext context, DocumentNode document)
    {
        if (context.Schema.ContextData.ContainsKey(WellKnownContextData.AuthorizationRequestPolicy))
        {
            if (!_cache.TryGetDirectives(context.DocumentId.Value, out var directives))
            {
                _visitor.Visit(document, context);
                directives = ((HashSet<AuthorizeDirective>)
                    context.ContextData[AuthContextData.Directives]!).ToArray();
                _cache.TryAddDirectives(context.DocumentId.Value, directives);
            }

            // update context data with the array result.
            context.ContextData[AuthContextData.Directives] = directives;
        }
    }
}
