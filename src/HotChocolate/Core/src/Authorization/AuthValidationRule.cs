using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Validation;

namespace HotChocolate.Authorization;

internal class AuthValidationRule : IDocumentValidatorRule
{
    private readonly AuthDocumentValidatorVisitor _visitor = new();
    private readonly IAuthorizationCache _cache;

    public AuthValidationRule(IAuthorizationCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public bool IsCacheable => false;

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
            context.ContextData[""] = directives;
        }
    }
}
