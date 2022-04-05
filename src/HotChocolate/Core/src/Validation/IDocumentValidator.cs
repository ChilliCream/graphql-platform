using System.Collections.Generic;
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
    /// <returns>
    /// The result of the document validation.
    /// </returns>
    DocumentValidatorResult Validate(
        ISchema schema,
        DocumentNode document);

    /// <summary>
    /// Validates the current document against the current schema context.
    /// </summary>
    /// <param name="schema">
    /// The schema.
    /// </param>
    /// <param name="document">
    /// The document to validate.
    /// </param>
    /// <param name="contextData">
    /// Arbitrary execution context data that can be used during the document validation.
    /// </param>
    /// <param name="onlyNonCacheable">
    /// Defines that only rules shall be evaluated that are not cacheable.
    /// </param>
    /// <returns>
    /// The result of the document validation.
    /// </returns>
    DocumentValidatorResult Validate(
        ISchema schema,
        DocumentNode document,
        IDictionary<string, object?> contextData,
        bool onlyNonCacheable = false);
}
