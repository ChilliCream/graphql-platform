using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Validation;

namespace HotChocolate.Caching;

// todo: rework this
internal sealed class CacheControlValidatorVisitor : TypeDocumentValidatorVisitor
{
    private readonly IQueryCacheSettings _settings;

    public CacheControlValidatorVisitor(IQueryCacheSettings settings)
    {
        _settings = settings;
    }

    protected override ISyntaxVisitorAction Enter(
        DocumentNode node,
        IDocumentValidatorContext context)
    {
        context.List.Push(new List<CacheControlResult>());

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        OperationDefinitionNode node,
        IDocumentValidatorContext context)
    {
        var results = (List<CacheControlResult>)context.List.Peek()!;

        results.Add(new CacheControlResult(node));

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(
        OperationDefinitionNode node,
        IDocumentValidatorContext context)
    {
        var results = (List<CacheControlResult>)context.List.Peek()!;

        CacheControlResult result = results.Last();

        if (!result.MaxAge.HasValue)
        {
            result.MaxAge = _settings.DefaultMaxAge;
        }

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        IDocumentValidatorContext context)
    {
        if (context.Types.TryPeek(out IType? type) &&
            type.NamedType() is IComplexOutputType ot &&
            ot.Fields.TryGetField(node.Name.Value, out IOutputField? of))
        {
            // context.List.Push(new List<Expression>());
            context.OutputFields.Push(of);
            context.Types.Push(of.Type);
            return Continue;
        }

        context.UnexpectedErrorsDetected = true;
        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(
        FieldNode node,
        IDocumentValidatorContext context)
    {
        IOutputField field = context.OutputFields.Pop();
        context.Types.Pop();

        var results = (List<CacheControlResult>)context.List.Peek()!;

        CacheControlResult result = results.Last();

        CacheControlDirective? directive = field.Directives["cacheControl"]
            .FirstOrDefault()?.ToObject<CacheControlDirective>();

        if (directive is not null)
        {
            // we want to determine the lowest cache time, i.e. MaxAge
            if (directive.MaxAge.HasValue)
            {
                if (!result.MaxAge.HasValue || directive.MaxAge < result.MaxAge)
                {
                    result.MaxAge = directive.MaxAge.Value;
                }
            }
            else
            {
                result.MaxAge = _settings.DefaultMaxAge;
            }

            // we want to determine the most restrictive cache scope
            if (directive.Scope.HasValue && directive.Scope > result.Scope)
            {
                result.Scope = directive.Scope.Value;
            }
        }
        else
        {
            result.MaxAge = _settings.DefaultMaxAge;
        }

        return Continue;
    }
}
