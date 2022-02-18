using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Utilities;
using static HotChocolate.Stitching.Execution.RemoteRequestHelper;

namespace HotChocolate.Stitching.Execution;

internal interface IRemoteBatchRequestHandler
{
    Task<IBatchQueryResult> ExecuteAsync(
        IEnumerable<IQueryRequest> requestBatch,
        CancellationToken cancellationToken = default);
}

public sealed class HttpPostOperationBatchRequestHandler : IRemoteBatchRequestHandler
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IErrorHandler _errorHandler;
    private readonly IHttpStitchingRequestInterceptor _requestInterceptor;
    private readonly NameString _targetSchema;

    public HttpPostOperationBatchRequestHandler(
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

    public async Task<IBatchQueryResult> ExecuteAsync(
        IEnumerable<IQueryRequest> requestBatch,
        CancellationToken cancellationToken = default)
    {
        using var writer = new ArrayWriter();

        using HttpRequestMessage requestMessage =
            await CreateRequestAsync(
                    writer,
                    requestBatch,
                    _targetSchema,
                    cancellationToken)
                .ConfigureAwait(false);
    }

    private async ValueTask<HttpRequestMessage> CreateRequestAsync(
        ArrayWriter writer,
        IEnumerable<IQueryRequest> requests,
        NameString targetSchema,
        CancellationToken cancellationToken = default)
    {
        HttpRequestMessage requestMessage =
            await CreateBatchRequestMessageAsync(writer, requests, cancellationToken)
                .ConfigureAwait(false);

        //await _requestInterceptor
        //    .OnCreateRequestAsync(targetSchema, request, requestMessage, cancellationToken)
        //    .ConfigureAwait(false);

        return requestMessage;
    }

    private async Task<IQueryResult> FetchAsync(
        IQueryRequest request,
        HttpRequestMessage requestMessage,
        NameString targetSchema,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpClient httpClient = _clientFactory.CreateClient(targetSchema);

            using HttpResponseMessage responseMessage = await httpClient
                .SendAsync(requestMessage, cancellationToken)
                .ConfigureAwait(false);

            IQueryResult result =
                responseMessage.IsSuccessStatusCode
                    ? await ParseResponseMessageAsync(responseMessage, cancellationToken)
                        .ConfigureAwait(false)
                    : await ParseErrorResponseMessageAsync(responseMessage, cancellationToken)
                        .ConfigureAwait(false);

            return await _requestInterceptor.OnReceivedResultAsync(
                    targetSchema,
                    request,
                    result,
                    responseMessage,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            IError error = _errorHandler.CreateUnexpectedError(ex)
                .SetCode(ErrorCodes.Stitching.UnknownRequestException)
                .Build();

            return QueryResultBuilder.CreateError(error);
        }
    }
}
