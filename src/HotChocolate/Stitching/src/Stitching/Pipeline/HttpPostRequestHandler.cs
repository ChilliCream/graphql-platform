using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Utilities;
using static HotChocolate.Stitching.Pipeline.RemoteRequestHelper;

#nullable enable

namespace HotChocolate.Stitching.Pipeline;

internal sealed class HttpPostRequestHandler : IRemoteRequestHandler
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IErrorHandler _errorHandler;
    private readonly IHttpStitchingRequestInterceptor _requestInterceptor;
    private readonly NameString _targetSchema;

    public HttpPostRequestHandler(
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

    public bool CanHandle(IQueryRequest request) => true;

    public async ValueTask<IExecutionResult> ExecuteAsync(
        IQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        using var writer = new ArrayWriter();

        using HttpRequestMessage requestMessage =
            await CreateRequestAsync(
                writer,
                request,
                _targetSchema,
                cancellationToken)
                .ConfigureAwait(false);

        return await FetchAsync(
            request,
            requestMessage,
            _targetSchema,
            cancellationToken)
            .ConfigureAwait(false);
    }

    private async ValueTask<HttpRequestMessage> CreateRequestAsync(
        ArrayWriter writer,
        IQueryRequest request,
        NameString targetSchema,
        CancellationToken cancellationToken = default)
    {
        HttpRequestMessage requestMessage =
            await CreateRequestMessageAsync(writer, request, cancellationToken)
                .ConfigureAwait(false);

        await _requestInterceptor
            .OnCreateRequestAsync(targetSchema, request, requestMessage, cancellationToken)
            .ConfigureAwait(false);

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
