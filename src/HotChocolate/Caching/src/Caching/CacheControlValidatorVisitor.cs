using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Validation;

namespace HotChocolate.Caching;

internal sealed class CacheControlValidatorVisitor : TypeDocumentValidatorVisitor
{
    protected override ISyntaxVisitorAction Enter(
        DocumentNode node,
        IDocumentValidatorContext context)
    {
        // For each document we add a list of CacheControlResult.
        context.List.Push(new List<CacheControlResult>());

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        OperationDefinitionNode node,
        IDocumentValidatorContext context)
    {
        var results = (List<CacheControlResult>)context.List.Peek()!;

        // For each operation within a document we add one
        // CacheControlResult.
        results.Add(new CacheControlResult(node));

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
            // For each field we store the field and its return type.
            context.OutputFields.Push(of);
            context.Types.Push(of.Type);
            return Continue;
        }

        context.UnexpectedErrorsDetected = true;
        return Skip;
    }

    // todo: how does an interface field behave here?
    protected override ISyntaxVisitorAction Leave(
        FieldNode node,
        IDocumentValidatorContext context)
    {
        IOutputField field = context.OutputFields.Pop();
        IType fieldType = context.Types.Pop();

        var results = (List<CacheControlResult>)context.List.Peek()!;

        // The CacheControlResult of the current operation.
        CacheControlResult result = results.Last();
        int? lowestMaxAge = result.MaxAgeHasValue ? result.MaxAge : null;

        CacheControlDirective? directive = field.Directives["cacheControl"]
            .FirstOrDefault()?.ToObject<CacheControlDirective>();

        bool maxAgeSet = false, scopeSet = false;

        if (directive is not null)
        {
            // The @cacheControl directive was specified directly on the field,
            // so its settings take precedence over everything else.

            if (directive.MaxAge.HasValue && (!lowestMaxAge.HasValue || directive.MaxAge < lowestMaxAge.Value))
            {
                // The maxAge of the @cacheControl on this field is the lowest within 
                // the current operation.
                lowestMaxAge = result.MaxAge = directive.MaxAge.Value;
                maxAgeSet = true;
            }
            else if (directive.InheritMaxAge == true)
            {
                // We do not need to modify the result, we just treat it like a
                // maxAge value has been applied.
                maxAgeSet = true;
            }

            if (directive.Scope.HasValue && directive.Scope > result.Scope)
            {
                // The scope of the @cacheControl on this field is the most restrictive
                // within the current operation.
                result.Scope = directive.Scope.Value;
                scopeSet = true;
            }
        }

        if (!maxAgeSet || !scopeSet)
        {
            // Either scope or maxAge have not been specified by the @cacheControl
            // directive on the field directly, so we inspect the type to see
            // whether it contains a @cacheControl directive.

            if (fieldType is not IComplexOutputType type)
            {
                // We can not read directives from this field type.
                return Continue;
            }

            CacheControlDirective? typeDirective = type.Directives["cacheControl"]
                .FirstOrDefault()?.ToObject<CacheControlDirective>();

            if (typeDirective is null)
            {
                // The fieldType does not contain the @cacheControl directive.
                return Continue;
            }

            if (!maxAgeSet && typeDirective.MaxAge.HasValue &&
                (!lowestMaxAge.HasValue || typeDirective.MaxAge < lowestMaxAge.Value))
            {
                // The maxAge of the @cacheControl on the field type is the lowest within 
                // the current operation.
                result.MaxAge = typeDirective.MaxAge.Value;
            }

            if (!scopeSet && typeDirective.Scope.HasValue && typeDirective.Scope > result.Scope)
            {
                // The scope of the @cacheControl on the field type is the most restrictive
                // within the current operation.
                result.Scope = typeDirective.Scope.Value;
            }
        }

        return Continue;
    }
}
