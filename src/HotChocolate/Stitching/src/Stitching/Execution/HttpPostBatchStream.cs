using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Utilities;
using Microsoft.AspNetCore.WebUtilities;

namespace HotChocolate.Stitching.Execution;

internal sealed class HttpPostBatchStream : IAsyncEnumerable<IQueryResult>
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IErrorHandler _errorHandler;
    private readonly IHttpStitchingRequestInterceptor _requestInterceptor;
    private readonly NameString _targetSchema;
    private readonly IEnumerable<IQueryRequest> _requestBatch;

    public HttpPostBatchStream(
        IHttpClientFactory clientFactory,
        IErrorHandler errorHandler,
        IHttpStitchingRequestInterceptor requestInterceptor,
        NameString targetSchema,
        IEnumerable<IQueryRequest> requestBatch)
    {
        _clientFactory = clientFactory;
        _errorHandler = errorHandler;
        _requestInterceptor = requestInterceptor;
        _targetSchema = targetSchema;
        _requestBatch = requestBatch;
    }


    public async IAsyncEnumerator<IQueryResult> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
    {
        using var writer = new ArrayWriter();

        using HttpClient httpClient = _clientFactory.CreateClient(_targetSchema);

        using HttpRequestMessage requestMessage =
            await CreateRequestAsync(
                    writer,
                    _requestBatch,
                    _targetSchema,
                    cancellationToken)
                .ConfigureAwait(false);

        using HttpResponseMessage responseMessage =
            await httpClient.SendAsync(requestMessage, cancellationToken)
                .ConfigureAwait(false);

#if NET5_0_OR_GREATER
        await using Stream stream = await responseMessage.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
#else
        using Stream stream = await responseMessage.Content
            .ReadAsStreamAsync()
            .ConfigureAwait(false);
#endif

        if (responseMessage.Content.Headers.ContentType?.MediaType is "multipart/mixed")
        {
            var boundary =
                responseMessage.Content.Headers.ContentType.Parameters.First(t =>
                    StringExtensions.EqualsOrdinal(t.Name, "boundary")).Value;

            var multiPartReader = new MultipartReader(boundary, stream);

            MultipartSection? section;

            do
            {
                section =
                    await multiPartReader.ReadNextSectionAsync(cancellationToken)
                        .ConfigureAwait(false);

#if NET5_0_OR_GREATER
                await using Stream body = section.Body;
#else
                using Stream body = section.Body;
#endif

                yield return await RemoteRequestHelper.ParseResultAsync(body, cancellationToken).ConfigureAwait(false);

            }
            while(section is not null);
        }
        else
        {
            IReadOnlyList<IQueryResult> queryResults =
                await RemoteRequestHelper.ParseBatchResultAsync(stream, cancellationToken).ConfigureAwait(false);

            foreach (IQueryResult queryResult in queryResults)
            {
                yield return queryResult;
            }
        }
    }

    private async ValueTask<HttpRequestMessage> CreateRequestAsync(
        ArrayWriter writer,
        IEnumerable<IQueryRequest> requests,
        NameString targetSchema,
        CancellationToken cancellationToken = default)
    {
        HttpRequestMessage requestMessage =
            await RemoteRequestHelper.CreateBatchRequestMessageAsync(writer, requests, cancellationToken)
                .ConfigureAwait(false);

        //await _requestInterceptor
        //    .OnCreateRequestAsync(targetSchema, request, requestMessage, cancellationToken)
        //    .ConfigureAwait(false);

        return requestMessage;
    }
}
