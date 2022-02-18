using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Stitching.Pipeline;

internal sealed class RemoteRequestMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IReadOnlyList<IRemoteRequestHandler> _requestHandlers;

    public RemoteRequestMiddleware(
        RequestDelegate next,
        [SchemaService] IReadOnlyList<IRemoteRequestHandler> requestHandlers)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _requestHandlers = requestHandlers;
    }

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        IQueryRequest request = context.Request;
        CancellationToken ct = context.RequestAborted;

        IRemoteRequestHandler? requestHandler =
            _requestHandlers.FirstOrDefault(handler => handler.CanHandle(request));

        if (requestHandler is null)
        {
            throw new NotSupportedException(
                "The specified request is not supported by the downstream service.");
        }

        context.Result = await requestHandler.ExecuteAsync(request, ct).ConfigureAwait(false);
        await _next.Invoke(context).ConfigureAwait(false);
    }
}
