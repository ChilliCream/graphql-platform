using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Transport.Http.Helper;
using HotChocolate.Transport.Serialization;
using HotChocolate.Utilities;

namespace HotChocolate.Transport.Http;

public class GraphQLHttpClient : IGraphQLHttpClient
{
    private const string _jsonMediaType = "application/json";
    private const string _graphqlMediaType = "application/graphql-response+json";

#if NET6_0_OR_GREATER
    private static readonly Encoding _utf8 = Encoding.UTF8;
#endif
    private static readonly OperationResult _transportError = CreateTransportError();
    private readonly HttpClient _httpClient;

    public GraphQLHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<OperationResult> GetAsync(
        OperationRequest request,
        OnHttpRequestMessageCreated? onMessageCreated = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<OperationResult> PostAsync(
        OperationRequest request,
        OnHttpRequestMessageCreated? onMessageCreated = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(request.Id) && string.IsNullOrEmpty(request.Query) && request.Extensions is null)
        {
            throw new ArgumentException("TODO: RESOURCES", nameof(request));
        }
        
        return PostInternalAsync(request, onMessageCreated, ct);
    }
    
    private async Task<OperationResult> PostInternalAsync(
        OperationRequest request,
        OnHttpRequestMessageCreated? onMessageCreated = null,
        CancellationToken ct = default)
    {
        using var requestMessage = CreateRequestMessage(HttpMethod.Post);
        requestMessage.Content = CreatePostContent(request);
        onMessageCreated?.Invoke(request, requestMessage);
        
        using var responseMessage = await _httpClient.SendAsync(requestMessage, ct).ConfigureAwait(false);
#if NET6_0_OR_GREATER
        await using var contentStream = await responseMessage.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
#else
        using var contentStream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif

        var stream = contentStream;
        var contentType = responseMessage.Content.Headers.ContentType;

#if NET6_0_OR_GREATER
        var sourceEncoding = GetEncoding(contentType?.CharSet);
        if (sourceEncoding is not null && !Equals(sourceEncoding.EncodingName, _utf8.EncodingName))
        {
            stream = GetTranscodingStream(contentStream, sourceEncoding);
        }
#endif
        // The server supports the newer graphql-response+json media type and users are free
        // to use status codes.
        if (contentType?.MediaType.EqualsOrdinal(_graphqlMediaType) ?? false)
        {
            var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return OperationResult.Parse(document);
        }

        // The server supports the older application/json media type and the status code
        // is expected to be a 2xx for a valid GraphQL response.
        if (contentType?.MediaType.EqualsOrdinal(_jsonMediaType) ?? false)
        {
            responseMessage.EnsureSuccessStatusCode();
            var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return OperationResult.Parse(document);
        }

        // if the media type is anything else we will return a transport error.
        return _transportError;
    }

    private static HttpRequestMessage CreateRequestMessage(HttpMethod method)
        => new()
        {
            Method = method,
            Headers =
            {
                Accept =
                {
                    new MediaTypeWithQualityHeaderValue(_graphqlMediaType),
                    new MediaTypeWithQualityHeaderValue(_jsonMediaType)
                }
            }
        };

    private static HttpContent CreatePostContent(OperationRequest request)
    {
        using var arrayWriter = new ArrayWriter();
        
        using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonDefaults.WriterOptions);
        request.WriteTo(jsonWriter);
        jsonWriter.Flush();

        var content = new ByteArrayContent(arrayWriter.GetInternalBuffer(), 0, arrayWriter.Length);
        content.Headers.ContentType = new MediaTypeHeaderValue(_jsonMediaType);
        return content;
    }

#if NET6_0_OR_GREATER
    private static Encoding? GetEncoding(string? charset)
    {
        Encoding? encoding = null;

        if (charset != null)
        {
            try
            {
                // Remove at most a single set of quotes.
                if (charset.Length > 2 && charset[0] == '\"' && charset[^1] == '\"')
                {
                    encoding = Encoding.GetEncoding(charset.Substring(1, charset.Length - 2));
                }
                else
                {
                    encoding = Encoding.GetEncoding(charset);
                }
            }
            catch (ArgumentException e)
            {
                throw new InvalidOperationException("Invalid Charset", e);
            }

            Debug.Assert(encoding != null);
        }

        return encoding;
    }
    
    private static Stream GetTranscodingStream(Stream contentStream, Encoding sourceEncoding)
        => Encoding.CreateTranscodingStream(
            contentStream,
            innerStreamEncoding: sourceEncoding,
            outerStreamEncoding: _utf8);
#endif

    private static OperationResult CreateTransportError()
        => new OperationResult(
            errors: JsonDocument.Parse(
                """
                [{"message": "Internal Execution Error"}]
                """).RootElement);


    public Task<OperationResult> ExecuteGetAsync(OperationRequest request, CancellationToken cancellationToken)
    {
        var parameter = GetQueryParameter(request);
        var queryString = GetQueryString(parameter);
        var requestUri = $"{_httpClient.BaseAddress}?{queryString}";
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(requestUri)
        };
        requestMessage.AddDefaultAcceptHeaders();
        return SendHttpRequestMessageAsync(requestMessage, cancellationToken);
    }

    public Task<OperationResult> ExecutePostAsync(OperationRequest request, CancellationToken cancellationToken)
    {
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = _httpClient.BaseAddress
        };
        requestMessage
            .AddDefaultAcceptHeaders()
            .AddJsonBody(request);
        return SendHttpRequestMessageAsync(requestMessage, cancellationToken);
    }

    private async Task<OperationResult> SendHttpRequestMessageAsync(
        HttpRequestMessage requestMessage,
        CancellationToken cancellationToken)
    {
        var httpResponseMessage = await _httpClient.SendAsync(requestMessage, cancellationToken);

        if (!httpResponseMessage.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Response indicates failure: {httpResponseMessage.StatusCode} {httpResponseMessage.ReasonPhrase}");

        var json = await httpResponseMessage.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(json);
        var operationResult = new OperationResult(
            jsonDocument,
            jsonDocument.RootElement.TryGetProperty("data", out var data)
                ? data
                : default,
            jsonDocument.RootElement.TryGetProperty("errors", out var errors)
                ? errors
                : default,
            jsonDocument.RootElement.TryGetProperty("extensions", out var extensions)
                ? extensions
                : default);
        return operationResult;
    }

    private static string GetQueryString(NameValueCollection valueCollection)
    {
        var sb = new StringBuilder();

        for (var i = 0; i < valueCollection.Count; i++)
        {
            sb.Append($"{valueCollection.Keys[i]}={valueCollection[i]}");

            if (i + 1 < valueCollection.Count)
            {
                sb.Append('&');
            }
        }

        return sb.ToString();
    }

    private static NameValueCollection GetQueryParameter(OperationRequest request)
    {
        var queryParameter = new NameValueCollection();

        if (request.OperationName is not null)
        {
            queryParameter["operationName"] = Uri.EscapeDataString(request.OperationName);
        }

        if (request.Query is not null)
        {
            queryParameter["query"] = Uri.EscapeDataString(request.Query);
        }

        if (request.Variables is not null)
        {
            var variablesObject = JsonSerializer.Serialize(request.Variables);
            queryParameter["variables"] = Uri.EscapeDataString(variablesObject);
        }

        if (request.Extensions is not null)
        {
            var extensionsObject = JsonSerializer.Serialize(request.Extensions);
            queryParameter["extensions"] = Uri.EscapeDataString(extensionsObject);
        }

        return queryParameter;
    }
}