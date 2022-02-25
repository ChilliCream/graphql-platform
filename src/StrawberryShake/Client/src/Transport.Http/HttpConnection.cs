using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using StrawberryShake.Internal;
using StrawberryShake.Json;
using static StrawberryShake.Transport.Http.ResponseEnumerable;

namespace StrawberryShake.Transport.Http;

public sealed class HttpConnection : IHttpConnection
{
    private readonly Func<HttpClient> _createClient;
    private readonly JsonOperationRequestSerializer _serializer = new();

    public HttpConnection(Func<HttpClient> createClient)
    {
        _createClient = createClient ?? throw new ArgumentNullException(nameof(createClient));
    }

    public IAsyncEnumerable<Response<JsonDocument>> ExecuteAsync(OperationRequest request)
        => Create(_createClient, () => CreateRequestMessage(request));

    private HttpRequestMessage CreateRequestMessage(OperationRequest request)
    {
        var content = new ByteArrayContent(CreateRequestMessageBody(request));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        return new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            Content = content
        };
    }

    private byte[] CreateRequestMessageBody(
        OperationRequest request)
    {
        using var arrayWriter = new ArrayWriter();
        _serializer.Serialize(request, arrayWriter);
        var buffer = new byte[arrayWriter.Length];
        arrayWriter.Body.Span.CopyTo(buffer);
        return buffer;
    }
}

