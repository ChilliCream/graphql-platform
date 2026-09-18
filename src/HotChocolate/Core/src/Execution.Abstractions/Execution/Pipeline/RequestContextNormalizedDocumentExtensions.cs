using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

/// <summary>
/// Provides access to the normalized operation document on the <see cref="RequestContext"/>.
/// </summary>
public static class RequestContextNormalizedDocumentExtensions
{
    /// <summary>
    /// Gets the normalized operation document from the request context, normalizing the
    /// operation document on first access and caching the result on the request context.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <returns>
    /// The normalized operation document.
    /// </returns>
    public static DocumentNode GetNormalizedDocument(this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var documentInfo = context.OperationDocumentInfo;

        if (documentInfo.NormalizedDocument is { } normalizedDocument)
        {
            return normalizedDocument;
        }

        if (documentInfo.Document is null)
        {
            throw ThrowHelper.NormalizedDocument_NoDocument();
        }

        if (!documentInfo.IsValidated)
        {
            throw ThrowHelper.NormalizedDocument_DocumentNotValidated();
        }

        if (documentInfo.Id.IsEmpty)
        {
            throw ThrowHelper.NormalizedDocument_DocumentIdEmpty();
        }

        var normalizer = context.Schema.Services.GetRequiredService<IOperationDocumentNormalizer>();
        normalizedDocument = normalizer.NormalizeDocument(context);
        documentInfo.NormalizedDocument = normalizedDocument;
        return normalizedDocument;
    }

    /// <summary>
    /// Gets the normalized operation definition from the request context. The normalized
    /// document holds exactly one definition, the operation, at <c>Definitions[0]</c>.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <returns>
    /// The normalized operation definition.
    /// </returns>
    public static OperationDefinitionNode GetNormalizedOperation(this RequestContext context)
        => (OperationDefinitionNode)context.GetNormalizedDocument().Definitions[0];
}
