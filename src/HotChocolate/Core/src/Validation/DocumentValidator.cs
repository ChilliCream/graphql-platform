using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    // Documents for which every non-cacheable rule reported a request independent result, with their schema.
    private readonly ConditionalWeakTable<DocumentNode, WeakReference<ISchemaDefinition>> _requestIndependentDocuments =
        new();
    private WeakReference<ISchemaDefinition>? _schemaReference;
    private volatile bool _hasRequestIndependentDocuments;
    private readonly int _maxAllowedErrors;
    private readonly int _maxLocationsPerError;
    private readonly int _maxAllowedFragmentVisits;

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
    /// <param name="maxLocationsPerError">
    /// The maximum number of locations that will be added to a validation error.
    /// </param>
    /// <param name="maxAllowedFragmentVisits">
    /// The maximum number of fragment visits allowed during validation.
    /// </param>
    internal DocumentValidator(
        ObjectPool<DocumentValidatorContext> contextPool,
        IDocumentValidatorRule[] rules,
        int maxAllowedErrors,
        int maxLocationsPerError,
        int maxAllowedFragmentVisits)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(contextPool);
        ArgumentOutOfRangeException.ThrowIfNegative(maxAllowedErrors);

        _contextPool = contextPool;
        _allRules = rules;
        _nonCacheableRules = [.. rules.Where(rule => !rule.IsCacheable)];
        _maxAllowedErrors = maxAllowedErrors > 0 ? maxAllowedErrors : 1;
        _maxLocationsPerError = maxLocationsPerError > 0 ? maxLocationsPerError : 1;
        _maxAllowedFragmentVisits = maxAllowedFragmentVisits > 0 ? maxAllowedFragmentVisits : 1;
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

        if (onlyNonCacheable
            && _hasRequestIndependentDocuments
            && _requestIndependentDocuments.TryGetValue(document, out var schemaReference)
            && schemaReference.TryGetTarget(out var validatedSchema)
            && ReferenceEquals(validatedSchema, schema))
        {
            return DocumentValidatorResult.OK;
        }

        var context = RentContext(schema, documentId, document, features);

        try
        {
            var isRequestIndependent = true;
            ref var start = ref MemoryMarshal.GetArrayDataReference(rules);
            ref var end = ref Unsafe.Add(ref start, rules.Length);

            while (Unsafe.IsAddressLessThan(ref start, ref end))
            {
                context.IsRequestIndependent = start.IsCacheable;
                start.Validate(context, document);
                isRequestIndependent &= context.IsRequestIndependent;

                if (context.FatalErrorDetected)
                {
                    break;
                }

                context.Reset();

                start = ref Unsafe.Add(ref start, 1)!;
            }

            if (context.Errors.Count > 0)
            {
                return new DocumentValidatorResult(context.Errors);
            }

            if (isRequestIndependent
                && _nonCacheableRules.Length > 0
                && !context.FatalErrorDetected
                && !context.UnexpectedErrorsDetected)
            {
                _requestIndependentDocuments.AddOrUpdate(document, GetSchemaReference(schema));
                _hasRequestIndependentDocuments = true;
            }

            return DocumentValidatorResult.OK;
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

    private WeakReference<ISchemaDefinition> GetSchemaReference(ISchemaDefinition schema)
    {
        var reference = _schemaReference;

        if (reference is null
            || !reference.TryGetTarget(out var current)
            || !ReferenceEquals(current, schema))
        {
            reference = new WeakReference<ISchemaDefinition>(schema);
            _schemaReference = reference;
        }

        return reference;
    }

    private DocumentValidatorContext RentContext(
        ISchemaDefinition schema,
        OperationDocumentId documentId,
        DocumentNode document,
        IFeatureCollection? features)
    {
        var context = _contextPool.Get();
        context.Initialize(schema, documentId, document, _maxAllowedErrors, _maxLocationsPerError, _maxAllowedFragmentVisits, features);
        return context;
    }

    private void ReturnContext(DocumentValidatorContext context)
    {
        _contextPool.Return(context);
    }
}
