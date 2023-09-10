using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;

namespace StrawberryShake.Transport.Http;

internal sealed class ResponseEnumerable : IAsyncEnumerable<Response<JsonDocument>>
{
    private readonly Func<HttpClient> _createClient;
    private readonly Func<HttpRequestMessage> _createRequest;

    private ResponseEnumerable(
        Func<HttpClient> createClient,
        Func<HttpRequestMessage> createRequest)
    {
        _createClient = createClient;
        _createRequest = createRequest;
    }

    public IAsyncEnumerator<Response<JsonDocument>> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
        => new ResponseEnumerator(_createClient, _createRequest, cancellationToken);

    public static ResponseEnumerable Create(
        Func<HttpClient> createClient,
        Func<HttpRequestMessage> createRequest)
    {
        if (createClient is null)
        {
            throw new ArgumentNullException(nameof(createClient));
        }

        if (createRequest is null)
        {
            throw new ArgumentNullException(nameof(createRequest));
        }

        return new ResponseEnumerable(createClient, createRequest);
    }
}
