namespace HotChocolate.Execution.Pipeline;

internal sealed class ReadRequestPropertiesMiddleware(RequestDelegate next)
{
    public ValueTask InvokeAsync(RequestContext context)
    {
        if (!context.Request.DocumentId.IsEmpty)
        {
            context.OperationDocumentInfo.Id = context.Request.DocumentId;
        }

        if (!context.Request.DocumentHash.IsEmpty)
        {
            context.OperationDocumentInfo.Hash = context.Request.DocumentHash;
        }

        return next(context);
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (_, next) => new ReadRequestPropertiesMiddleware(next).InvokeAsync,
            nameof(ReadRequestPropertiesMiddleware));
}
