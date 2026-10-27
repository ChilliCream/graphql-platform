using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline;

/// <summary>
/// Produces a document containing only the selected operation, with fragment inlined
/// and statically excluded selections removed.
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
