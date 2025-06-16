using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Validation;

/// <summary>
/// The <see cref="DocumentValidator"/> is used to validate if a GraphQL operation document
/// is valid and can be executed.
/// </summary>
public sealed class DocumentValidator
{
    private readonly ObjectPool<DocumentValidatorContext> _contextPool;
    private readonly IDocumentValidatorRule[] _allRules;
    private readonly IDocumentValidatorRule[] _nonCacheableRules;
    private readonly int _maxAllowedErrors;

    /// <summary>
    /// Initializes a new instance of <see cref="DocumentValidator"/>.
    /// </summary>
    /// <param name="contextPool">
    /// The context pool.
    /// </param>
    /// <param name="rules">
    /// All registered validation rules.
    /// </param>
    /// <param name="maxAllowedErrors">
    /// The maximum number of errors that are allowed to be reported.
    /// </param>
    internal DocumentValidator(
        ObjectPool<DocumentValidatorContext> contextPool,
        IDocumentValidatorRule[] rules,
        int maxAllowedErrors)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(contextPool);
        ArgumentOutOfRangeException.ThrowIfNegative(maxAllowedErrors);

        _contextPool = contextPool;
        _allRules = rules;
        _nonCacheableRules = [.. rules.Where(rule => !rule.IsCacheable)];
        _maxAllowedErrors = maxAllowedErrors > 0 ? maxAllowedErrors : 1;
    }

    /// <summary>
    /// Gets the rules that are used to validate the GraphQL operation document.
    /// </summary>
    public ImmutableArray<IDocumentValidatorRule> Rules => ImmutableCollectionsMarshal.AsImmutableArray(_allRules);

    /// <summary>
    /// Gets a value indicating whether the document validator has non-cacheable rules.
    /// </summary>
    public bool HasNonCacheableRules => _nonCacheableRules.Length > 0;

    /// <summary>
    /// Validates the GraphQL operation <paramref name="document"/> against the given <paramref name="schema"/>.
    /// </summary>
    /// <param name="schema">
    /// The GraphQL schema.
    /// </param>
    /// <param name="document">
    /// The GraphQL operation document that shall be validated.
    /// </param>
    /// <returns>
    /// The result of the validation.
    /// </returns>
    public DocumentValidatorResult Validate(
        ISchemaDefinition schema,
        DocumentNode document)
        => Validate(schema, default, document, null);

    /// <summary>
    /// Validates the GraphQL operation <paramref name="document"/> against the given <paramref name="schema"/>.
    /// </summary>
    /// <param name="schema">
    /// The GraphQL schema.
    /// </param>
    /// <param name="documentId">
    /// The unique identifier of the document.
    /// </param>
    /// <param name="document">
    /// The GraphQL operation document that shall be validated.
    /// </param>
    /// <param name="features">
    /// A collection of features that are used to extend the validation context.
    /// </param>
    /// <param name="onlyNonCacheable">
    /// If set to <c>true</c> only non-cacheable rules will be executed.
    /// </param>
    /// <returns>
    /// The result of the validation.
    /// </returns>
    public DocumentValidatorResult Validate(
        ISchemaDefinition schema,
        OperationDocumentId documentId,
        DocumentNode document,
        IFeatureCollection? features = null,
        bool onlyNonCacheable = false)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(document);

        var rules = onlyNonCacheable ? _nonCacheableRules : _allRules;

        if (rules.Length == 0)
        {
            return DocumentValidatorResult.OK;
        }

        var context = RentContext(schema, documentId, document, features);

        try
        {
            ref var start = ref MemoryMarshal.GetArrayDataReference(rules);
            ref var end = ref Unsafe.Add(ref start, rules.Length);

            while (Unsafe.IsAddressLessThan(ref start, ref end))
            {
                start.Validate(context, document);

                if (context.FatalErrorDetected)
                {
                    break;
                }

                start = ref Unsafe.Add(ref start, 1)!;
            }

            return context.Errors.Count == 0
                ? DocumentValidatorResult.OK
                : new DocumentValidatorResult(context.Errors);
        }
        catch (MaxValidationErrorsException)
        {
            return new DocumentValidatorResult(context.Errors);
        }
        finally
        {
            ReturnContext(context);
        }
    }

    private DocumentValidatorContext RentContext(
        ISchemaDefinition schema,
        OperationDocumentId documentId,
        DocumentNode document,
        IFeatureCollection? features)
    {
        var context = _contextPool.Get();
        context.Initialize(schema, documentId, document, _maxAllowedErrors, features);
        return context;
    }

    private void ReturnContext(DocumentValidatorContext context)
    {
        context.Clear();
        _contextPool.Return(context);
    }
}
