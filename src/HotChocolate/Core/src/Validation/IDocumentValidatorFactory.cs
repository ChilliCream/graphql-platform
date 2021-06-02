namespace HotChocolate.Validation
{
    /// <summary>
    /// The <see cref="IDocumentValidatorFactory" /> can create a validator for a specified schema.
    /// </summary>
    public interface IDocumentValidatorFactory
    {
        /// <summary>
        /// Creates a GraphQL document validator for the specified schema.
        /// </summary>
        /// <param name="schemaName">
        /// The name of the schema for which a document validator shall be created.
        /// </param>
        /// <returns>
        /// Returns a the document validator for the specified schema.
        /// </returns>
        IDocumentValidator CreateValidator(NameString schemaName = default);
    }
}
