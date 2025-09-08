using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Validation;

namespace HotChocolate.Authorization;

internal sealed class AuthorizeValidationRule(AuthorizationCache cache) : IDocumentValidatorRule
{
    private readonly AuthorizeValidationVisitor _visitor = new();
    private readonly AuthorizationCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    public ushort Priority => ushort.MaxValue;

    public bool IsCacheable => false;

    public void Validate(DocumentValidatorContext context, DocumentNode document)
    {
        if (context.Schema.IsAuthorizedAtRequestLevel())
        {
            if (!_cache.TryGetDirectives(context.DocumentId.Value, out var directives))
            {
                directives = _cache.GetOrCreate(
                    context.DocumentId.Value,
                    static (_, d) => d.CollectDirectives(),
                    new CacheContext(context, _visitor, document));
            }

            // update context data with the array result.
            context.SetAuthorizeDirectives(directives.Value);
        }
    }

    internal sealed class CacheContext(
        DocumentValidatorContext context,
        AuthorizeValidationVisitor visitor,
        DocumentNode document)
    {
        public DocumentValidatorContext Context => context;

        public AuthorizeValidationVisitor Visitor => visitor;

        public DocumentNode Document => document;

        public ImmutableArray<AuthorizeDirective> CollectDirectives()
        {
            Visitor.Visit(document, context);
            return context.GetAuthorizeDirectives();
        }
    }
}
