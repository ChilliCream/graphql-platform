using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Transport.Serialization;
using HotChocolate.Utilities;

namespace HotChocolate.Transport.Http;

public sealed class DefaultGraphQLHttpClient : IGraphQLHttpClient
{
    private readonly HttpClient _httpClient;

    public DefaultGraphQLHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<GraphQLHttpResponse> ExecuteAsync(
        GraphQLHttpRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.Uri is null && _httpClient.BaseAddress is null)
        {
            // TODO: resources
            throw new ArgumentException(
                "The request URI is not set and the underlying HTTP client has no base address.",
                nameof(request));
        }

        var requestUri = request.Uri ?? _httpClient.BaseAddress!;
        return ExecuteInternalAsync(request, requestUri, cancellationToken);
    }

    private async Task<GraphQLHttpResponse> ExecuteInternalAsync(
        GraphQLHttpRequest request,
        Uri requestUri,
        CancellationToken ct)
    {
        using var requestMessage = CreateRequestMessage(request, requestUri);
        var responseMessage = await _httpClient.SendAsync(requestMessage, ct).ConfigureAwait(false);
        return new GraphQLHttpResponse(responseMessage);
    }

    private static HttpRequestMessage CreateRequestMessage(
        GraphQLHttpRequest request,
        Uri requestUri)
    {
        var method = request.Method;
        
        var message = new HttpRequestMessage
        {
            Method = method,
            Headers =
            {
                Accept =
                {
                    new MediaTypeWithQualityHeaderValue(ContentType.GraphQL),
                    new MediaTypeWithQualityHeaderValue(ContentType.Json)
                }
            }
        };

        if(method == GraphQLHttpMethod.Post)
        {
            message.Content = CreatePostContent(request.Body);
        }
        else if (method == GraphQLHttpMethod.Get)
        {
            message.RequestUri = CreateGetRequestUri(requestUri, request.Body);
        }
        else
        {
            throw new NotSupportedException($"The HTTP method `{method}` is not supported.");
        }
        
        request.OnMessageCreated?.Invoke(request.Body, message);

        return message;
    }

    private static HttpContent CreatePostContent(OperationRequest request)
    {
        using var arrayWriter = new ArrayWriter();

        using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonDefaults.WriterOptions);
        request.WriteTo(jsonWriter);
        jsonWriter.Flush();

        var content = new ByteArrayContent(arrayWriter.GetInternalBuffer(), 0, arrayWriter.Length);
        content.Headers.ContentType = new MediaTypeHeaderValue(ContentType.Json, "utf-8");
        return content;
    }

    private static Uri CreateGetRequestUri(Uri baseAddress, OperationRequest request)
    {
        var sb = new StringBuilder();
        var appendAmpersand = false;

        sb.Append(baseAddress);
        sb.Append('?');

        if (!string.IsNullOrWhiteSpace(request.Id))
        {
            AppendAmpersand(sb, ref appendAmpersand);
            sb.Append("id=");
            sb.Append(Uri.EscapeDataString(request.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            AppendAmpersand(sb, ref appendAmpersand);
            sb.Append("query=");
            sb.Append(Uri.EscapeDataString(request.Query));
        }

        if (!string.IsNullOrWhiteSpace(request.OperationName))
        {
            AppendAmpersand(sb, ref appendAmpersand);
            sb.Append("operationName=");
            sb.Append(Uri.EscapeDataString(request.OperationName));
        }

        if (request.VariablesNode is not null)
        {
            AppendAmpersand(sb, ref appendAmpersand);
            sb.Append("variables=");
            // TODO : implement
            throw new NotImplementedException();
        }
        else if (request.Variables is not null)
        {
            AppendAmpersand(sb, ref appendAmpersand);
            sb.Append("variables=");
            sb.Append(Uri.EscapeDataString(JsonSerializer.Serialize(request.Variables)));
        }

        if (request.ExtensionsNode is not null)
        {
            // TODO : implement
            throw new NotImplementedException();
        }
        else if (request.Extensions is not null)
        {
            AppendAmpersand(sb, ref appendAmpersand);
            sb.Append("extensions=");
            sb.Append(Uri.EscapeDataString(JsonSerializer.Serialize(request.Extensions)));
        }

        return new Uri(sb.ToString());

        static void AppendAmpersand(StringBuilder sb, ref bool appendAmpersand)
        {
            if (appendAmpersand)
            {
                sb.Append('&');
            }

            appendAmpersand = true;
        }
    }
}