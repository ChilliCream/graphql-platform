using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

/// <summary>
/// Provides access to the normalized operation document on the <see cref="RequestContext"/>.
/// </summary>
public static class RequestContextNormalizedDocumentExtensions
{
    /// <summary>
    /// Gets the request's document with fragments inlined and statically excluded selections
    /// removed. The result contains only the selected operation.
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
    /// Gets the selected operation with fragments inlined and statically excluded selections removed.
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
