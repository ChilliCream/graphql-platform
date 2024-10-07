using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Execution;
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
    private readonly IValidationResultAggregator[] _aggregators;
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
    /// <param name="resultAggregators">
    /// The result aggregators.
    /// </param>
    /// <param name="errorOptions">
    /// The error options.
    /// </param>
    public DocumentValidator(
        DocumentValidatorContextPool contextPool,
        IEnumerable<IDocumentValidatorRule> rules,
        IEnumerable<IValidationResultAggregator> resultAggregators,
        IErrorOptionsAccessor errorOptions)
    {
        if (rules is null)
        {
            throw new ArgumentNullException(nameof(rules));
        }

        if (errorOptions is null)
        {
            throw new ArgumentNullException(nameof(errorOptions));
        }

        _contextPool = contextPool ?? throw new ArgumentNullException(nameof(contextPool));
        _allRules = rules.ToArray();
        _nonCacheableRules = _allRules.Where(t => !t.IsCacheable).ToArray();
        _aggregators = resultAggregators.ToArray();
        _maxAllowedErrors = errorOptions.MaxAllowedErrors;

        Array.Sort(_allRules, (a, b) => a.Priority.CompareTo(b.Priority));
        Array.Sort(_nonCacheableRules, (a, b) => a.Priority.CompareTo(b.Priority));
    }

    /// <inheritdoc />
    public bool HasDynamicRules => _nonCacheableRules.Length > 0 || _aggregators.Length > 0;

    /// <inheritdoc />
    public ValueTask<DocumentValidatorResult> ValidateAsync(
        ISchema schema,
        DocumentNode document,
        OperationDocumentId documentId,
        IDictionary<string, object?> contextData,
        bool onlyNonCacheable,
        CancellationToken cancellationToken = default)
    {
        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (documentId.IsEmpty)
        {
            throw new ArgumentNullException(nameof(documentId));
        }

        if (onlyNonCacheable && _nonCacheableRules.Length == 0 && _aggregators.Length == 0)
        {
            return new(DocumentValidatorResult.Ok);
        }

        var context = _contextPool.Get();
        var rules = onlyNonCacheable ? _nonCacheableRules : _allRules;
        var handleCleanup = true;

        try
        {
            PrepareContext(schema, document, documentId, context, contextData);

            var length = rules.Length;
            ref var start = ref MemoryMarshal.GetArrayDataReference(rules);

            for (var i = 0; i < length; i++)
            {
                Unsafe.Add(ref start, i).Validate(context, document);

                if (context.FatalErrorDetected)
                {
                    break;
                }
            }

            if (_aggregators.Length == 0)
            {
                return new(
                    context.Errors.Count > 0
                        ? new DocumentValidatorResult(context.Errors)
                        : DocumentValidatorResult.Ok);
            }
            else
            {
                handleCleanup = false;
                return RunResultAggregators(context, document, cancellationToken);
            }
        }
        catch (MaxValidationErrorsException)
        {
            Debug.Assert(context.Errors.Count > 0, "There must be at least 1 validation error.");
            return new(new DocumentValidatorResult(context.Errors));
        }
        finally
        {
            if (handleCleanup)
            {
                _contextPool.Return(context);
            }
        }
    }

    private async ValueTask<DocumentValidatorResult> RunResultAggregators(
        DocumentValidatorContext context,
        DocumentNode document,
        CancellationToken ct)
    {
        var aggregators = _aggregators;
        var length = aggregators.Length;

        try
        {
            for (var i = 0; i < length; i++)
            {
                await aggregators[i].AggregateAsync(context, document, ct).ConfigureAwait(false);
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
        OperationDocumentId documentId,
        DocumentValidatorContext context,
        IDictionary<string, object?> contextData)
    {
        context.Schema = schema;
        context.DocumentId = documentId;

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
