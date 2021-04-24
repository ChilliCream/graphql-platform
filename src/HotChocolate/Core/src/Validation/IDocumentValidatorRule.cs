using HotChocolate.Language;

namespace HotChocolate.Validation
{
    /// <summary>
    /// A validation rule inspects a GraphQL document for a certain set of errors.
    /// </summary>
    public interface IDocumentValidatorRule
    {
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
}
