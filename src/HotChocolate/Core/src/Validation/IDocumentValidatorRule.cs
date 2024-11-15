using HotChocolate.Language;

namespace HotChocolate.Validation;

/// <summary>
/// A validation rule inspects a GraphQL document for a certain set of errors.
/// </summary>
public interface IDocumentValidatorRule
{
    /// <summary>
    /// Gets the priority of this rule. Rules with a lower priority are executed first.
    /// </summary>
    ushort Priority { get; }

    /// <summary>
    /// Defines if the result of this rule can be cached and reused on consecutive
    /// validations of the same GraphQL request document.
    /// </summary>
    bool IsCacheable { get; }

    /// <summary>
    /// Validates the document.
    /// </summary>
    /// <param name="context">
    /// The validation context.
    /// </param>
    /// <param name="document">
    /// The GraphQL document that shall be inspected.
    /// </param>
    void Validate(IDocumentValidatorContext context, DocumentNode document);
}
