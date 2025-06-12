using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Language;

namespace HotChocolate.Execution;

/// <summary>
/// Provides extension methods for <see cref="RequestContext"/>.
/// </summary>
public static class RequestContextExtensions
{
    public static OperationDocumentId GetOperationDocumentId(this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.OperationDocumentInfo.Id;
    }

    public static bool IsOperationDocumentValid(this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.OperationDocumentInfo.IsValidated;
    }

    public static bool IsPersistedOperationDocument(this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.OperationDocumentInfo.IsPersisted;
    }

    public static bool TryGetOperationDocument(
        this RequestContext context,
        [NotNullWhen(true)] out DocumentNode? document,
        out OperationDocumentId documentId)
    {
        ArgumentNullException.ThrowIfNull(context);

        document = context.OperationDocumentInfo.Document;
        documentId = context.OperationDocumentInfo.Id;

        return document is not null;
    }
}
