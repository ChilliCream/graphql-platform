using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Validation;

namespace HotChocolate.Authorization;

internal sealed class AuthorizeValidationRule : IDocumentValidatorRule
{
    private readonly AuthorizeValidationVisitor _visitor = new();
    private readonly AuthorizationCache _cache;

    public AuthorizeValidationRule(AuthorizationCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public bool IsCacheable => true;

    public void Validate(IDocumentValidatorContext context, DocumentNode document)
    {
        if (context.Schema.ContextData.ContainsKey(WellKnownContextData.AuthorizationRequestPolicy))
        {
            if (!_cache.TryGetDirectives(context.DocumentId, out var directives))
            {
                _visitor.Visit(document, context);
                directives = ((HashSet<AuthorizeDirective>)
                    context.ContextData[AuthContextData.Directives]!).ToArray();
                _cache.TryAddDirectives(context.DocumentId, directives);
            }

            // update context data with the array result.
            context.ContextData[AuthContextData.Directives] = directives;
        }
    }
}
