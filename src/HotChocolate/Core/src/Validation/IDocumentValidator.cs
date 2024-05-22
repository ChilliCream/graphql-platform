using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Validation;

/// <summary>
/// The document validator will analyze if the GraphQL request document is valid
/// in the current schema context.
/// </summary>
public interface IDocumentValidator
{
    /// <summary>
    /// Specifies that the validator needs to be invoked for
    /// every request and that the validation result cannot be
    /// fully cached.
    /// </summary>
    bool HasDynamicRules { get; }

    /// <summary>
    /// Validates the current document against the current schema context.
    /// </summary>
    /// <param name="schema">
    /// The schema.
    /// </param>
    /// <param name="document">
    /// The document to validate.
    /// </param>
    /// <param name="documentId">
    /// A unique string identifying this document.
    /// </param>
    /// <param name="contextData">
    /// Arbitrary execution context data that can be used during the document validation.
    /// </param>
    /// <param name="onlyNonCacheable">
    /// Defines that only rules shall be evaluated that are not cacheable.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// The result of the document validation.
    /// </returns>
    ValueTask<DocumentValidatorResult> ValidateAsync(
        ISchema schema,
        DocumentNode document,
        OperationDocumentId documentId,
        IDictionary<string, object?> contextData,
        bool onlyNonCacheable,
        CancellationToken cancellationToken = default);
}
