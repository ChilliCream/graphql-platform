using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Stitching.Properties;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.Execution;

internal sealed class RemoteRequestExecutor : IRequestExecutor
{
    private readonly AutoUpdateRequestExecutorProxy _innerExecutor;
    private readonly IReadOnlyList<IRemoteRequestHandler> _requestHandlers;
    private readonly IRemoteBatchRequestHandler _batchRequestHandler;

    public RemoteRequestExecutor(AutoUpdateRequestExecutorProxy innerExecutor)
    {
        _innerExecutor = innerExecutor ??
            throw new ArgumentNullException(nameof(innerExecutor));
        _requestHandlers =
            innerExecutor.Services.GetServices<IRemoteRequestHandler>().ToArray();
        _batchRequestHandler =
            innerExecutor.Services.GetRequiredService<IRemoteBatchRequestHandler>();
    }

    public ISchema Schema => _innerExecutor.Schema;

    public IServiceProvider Services => _innerExecutor.Services;

    public ulong Version => _innerExecutor.Version;

    public Task<IExecutionResult> ExecuteAsync(
        IQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        IRemoteRequestHandler? requestHandler =
            _requestHandlers.FirstOrDefault(handler => handler.CanHandle(request));

        if (requestHandler is null)
        {
            throw new NotSupportedException(
                StitchingResources.RemoteRequestMiddleware_Request_Not_Supported);
        }

        return requestHandler.ExecuteAsync(request, cancellationToken);
    }

    public Task<IResponseStream> ExecuteBatchAsync(
        IReadOnlyList<IQueryRequest> requestBatch,
        CancellationToken cancellationToken = default)
        => _batchRequestHandler.ExecuteAsync(requestBatch, cancellationToken);
}
