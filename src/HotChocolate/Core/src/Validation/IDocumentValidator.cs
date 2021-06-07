using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    /// <summary>
    /// The document validator will analyze if the query is valid in the current schema context.
    /// </summary>
    public interface IDocumentValidator
    {
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
        DocumentValidatorResult Validate(ISchema schema, DocumentNode document);

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
        /// <returns>
        /// The result of the document validation.
        /// </returns>
        DocumentValidatorResult Validate(
            ISchema schema,
            DocumentNode document,
            IEnumerable<KeyValuePair<string, object?>>? contextData);
    }
}
