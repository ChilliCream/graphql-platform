using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline;

/// <summary>
/// Normalizes an operation document, i.e. inlines all fragments into the selected
/// operation and removes statically excluded selections, so that consumers only ever
/// have to deal with a single, self-contained operation definition.
/// </summary>
public interface IOperationDocumentNormalizer
{
    /// <summary>
    /// Normalizes the operation document of the current request.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <returns>
    /// The normalized operation document.
    /// </returns>
    DocumentNode NormalizeDocument(RequestContext context);
}
