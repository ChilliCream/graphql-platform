using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline;

/// <summary>
/// Provides access to the normalized operation document on the <see cref="RequestContext"/>,
/// i.e. the document produced by a document normalization pipeline stage.
/// </summary>
internal static class RequestContextNormalizedDocumentExtensions
{
    /// <summary>
    /// Gets the normalized operation document from the request context.
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

        return context.OperationDocumentInfo.NormalizedDocument
            ?? throw new InvalidOperationException("The normalized document was not set.");
    }

    /// <summary>
    /// Tries to get the normalized operation document from the request context.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <param name="document">
    /// The normalized operation document, if one is available.
    /// </param>
    /// <returns>
    /// <c>true</c> if a normalized operation document is available, otherwise <c>false</c>.
    /// </returns>
    public static bool TryGetNormalizedDocument(
        this RequestContext context,
        [NotNullWhen(true)] out DocumentNode? document)
    {
        ArgumentNullException.ThrowIfNull(context);

        document = context.OperationDocumentInfo.NormalizedDocument;
        return document is not null;
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
