using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Stitching.Execution;

internal sealed class HttpPostBatchRequestHandler : IRemoteBatchRequestHandler
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IErrorHandler _errorHandler;
    private readonly IHttpStitchingRequestInterceptor _requestInterceptor;
    private readonly NameString _targetSchema;

    public HttpPostBatchRequestHandler(
        IHttpClientFactory clientFactory,
        IErrorHandler errorHandler,
        IHttpStitchingRequestInterceptor requestInterceptor,
        NameString targetSchema)
    {
        _clientFactory = clientFactory;
        _errorHandler = errorHandler;
        _requestInterceptor = requestInterceptor;
        _targetSchema = targetSchema;
    }

    public Task<IResponseStream> ExecuteAsync(
        IEnumerable<IQueryRequest> requestBatch,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IResponseStream>(
            new ResponseStream(
                () => new HttpPostBatchStream(
                    _clientFactory,
                    _errorHandler,
                    _requestInterceptor,
                    _targetSchema,
                    requestBatch)));
}
