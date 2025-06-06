using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Language;

namespace HotChocolate.Execution;

/// <summary>
/// Provides extension methods for <see cref="RequestContext"/>.
/// </summary>
public static class RequestContextExtensions
{
    /// <summary>
    /// Gets the operation document info from the request context.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <returns>The operation document info.</returns>
    public static OperationDocumentInfo GetOperationDocumentInfo(
        this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Features.GetOrSet<OperationDocumentInfo>();
    }

    public static OperationDocumentId GetOperationDocumentId(this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Features.GetOrSet<OperationDocumentInfo>().Id;
    }


    public static bool IsOperationDocumentValid(this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Features.GetOrSet<OperationDocumentInfo>().IsValidated;
    }

    public static bool IsPersistedOperationDocument(this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Features.GetOrSet<OperationDocumentInfo>().IsPersisted;
    }

    public static bool TryGetOperationDocument(
        this RequestContext context,
        [NotNullWhen(true)] out DocumentNode? document,
        out OperationDocumentId documentId)
    {
        ArgumentNullException.ThrowIfNull(context);

        document = context.Features.GetOrSet<OperationDocumentInfo>().Document;
        documentId = context.Features.GetOrSet<OperationDocumentInfo>().Id;

        return document is not null;
    }

    public static void SetOperationDocumentId(this RequestContext context, OperationDocumentId documentId)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Features.GetOrSet<OperationDocumentInfo>().Id = documentId;
    }
}
