using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
        var operation = CreateRequestMessageBody(request);

        var content = request.Files.Count == 0
            ? CreateRequestContent(operation)
            : CreateMultipartContent(request, operation);

        return new HttpRequestMessage { Method = HttpMethod.Post, Content = content };
    }

    private byte[] CreateRequestMessageBody(OperationRequest request)
    {
        using var arrayWriter = new ArrayWriter();
        _serializer.Serialize(request, arrayWriter);
        var buffer = new byte[arrayWriter.Length];
        arrayWriter.Body.Span.CopyTo(buffer);
        return buffer;
    }

    private static HttpContent CreateRequestContent(byte[] operation)
    {
        var content = new ByteArrayContent(operation);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
    }

    private static HttpContent CreateMultipartContent(OperationRequest request, byte[] operation)
    {
        var fileMap = new Dictionary<string, string[]>();
        var form = new MultipartFormDataContent
        {
            { new ByteArrayContent(operation), "operations" },
            { JsonContent.Create(fileMap), "map" }
        };

        foreach (var file in request.Files)
        {
            if (file.Value is { } fileContent)
            {
                var identifier = (fileMap.Count + 1).ToString();
                fileMap.Add(identifier, new[] { file.Key });
                form.Add(new StreamContent(fileContent.Content), identifier, fileContent.FileName);
            }
        }

        return form;
    }
}
