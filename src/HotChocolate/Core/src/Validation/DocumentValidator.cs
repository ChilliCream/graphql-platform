using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Validation.Options;

namespace HotChocolate.Validation;

/// <summary>
/// The default document validator implementation.
/// </summary>
public sealed class DocumentValidator : IDocumentValidator
{
    private readonly DocumentValidatorContextPool _contextPool;
    private readonly IDocumentValidatorRule[] _allRules;
    private readonly IDocumentValidatorRule[] _nonCacheableRules;
    private readonly int _maxAllowedErrors;

    /// <summary>
    /// Creates a new instance of <see cref="DocumentValidator"/>.
    /// </summary>
    /// <param name="contextPool">
    /// The document validator context pool.
    /// </param>
    /// <param name="rules">
    /// The validation rules.
    /// </param>
    /// <param name="errorOptionsAccessor"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public DocumentValidator(
        DocumentValidatorContextPool contextPool,
        IEnumerable<IDocumentValidatorRule> rules,
        IErrorOptionsAccessor errorOptionsAccessor)
    {
        if (rules is null)
        {
            throw new ArgumentNullException(nameof(rules));
        }

        if (errorOptionsAccessor is null)
        {
            throw new ArgumentNullException(nameof(errorOptionsAccessor));
        }

        _contextPool = contextPool ?? throw new ArgumentNullException(nameof(contextPool));
        _allRules = rules.ToArray();
        _nonCacheableRules = _allRules.Where(t => !t.IsCacheable).ToArray();
        _maxAllowedErrors = errorOptionsAccessor.MaxAllowedErrors;
    }

    /// <inheritdoc />
    public bool HasDynamicRules => _nonCacheableRules.Length > 0;

    /// <inheritdoc />
    public DocumentValidatorResult Validate(
        ISchema schema,
        DocumentNode document) =>
        Validate(schema, document, new Dictionary<string, object?>());

    /// <inheritdoc />
    public DocumentValidatorResult Validate(
        ISchema schema,
        DocumentNode document,
        IDictionary<string, object?> contextData,
        bool onlyNonCacheable = false)
    {
        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (onlyNonCacheable && _nonCacheableRules.Length == 0)
        {
            return DocumentValidatorResult.Ok;
        }

        var context = _contextPool.Get();
        var rules = onlyNonCacheable ? _nonCacheableRules : _allRules;

        try
        {
            PrepareContext(schema, document, context, contextData);

            foreach (var rule in rules)
            {
                rule.Validate(context, document);
            }

            return context.Errors.Count > 0
                ? new DocumentValidatorResult(context.Errors)
                : DocumentValidatorResult.Ok;
        }
        catch (MaxValidationErrorsException)
        {
            Debug.Assert(context.Errors.Count > 0, "There must be at least 1 validation error.");
            return new DocumentValidatorResult(context.Errors);
        }
        finally
        {
            _contextPool.Return(context);
        }
    }

    private void PrepareContext(
        ISchema schema,
        DocumentNode document,
        DocumentValidatorContext context,
        IDictionary<string, object?> contextData)
    {
        context.Schema = schema;

        for (var i = 0; i < document.Definitions.Count; i++)
        {
            var definitionNode = document.Definitions[i];
            if (definitionNode.Kind is SyntaxKind.FragmentDefinition)
            {
                var fragmentDefinition = (FragmentDefinitionNode)definitionNode;
                context.Fragments[fragmentDefinition.Name.Value] = fragmentDefinition;
            }
        }

        context.MaxAllowedErrors = _maxAllowedErrors;
        context.ContextData = contextData;
    }
}
