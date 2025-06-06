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
}
