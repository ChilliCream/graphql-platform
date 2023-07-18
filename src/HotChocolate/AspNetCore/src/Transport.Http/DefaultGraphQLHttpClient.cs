using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Transport.Serialization;
using HotChocolate.Utilities;

namespace HotChocolate.Transport.Http;

/// <summary>
/// A default implementation of <see cref="IGraphQLHttpClient"/> that supports the GraphQL over HTTP spec draft.
/// </summary>
public sealed class DefaultGraphQLHttpClient : IGraphQLHttpClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultGraphQLHttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The underlying HTTP client that is used to send the GraphQL request.
    /// </param>
    public DefaultGraphQLHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Sends the GraphQL request to the specified GraphQL request <see cref="Uri"/>.
    /// </summary>
    /// <param name="request">
    /// The GraphQL over HTTP request.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the HTTP request.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="request"/> has no <see cref="GraphQLHttpRequest.Uri"/> and the underlying
    /// HTTP client has no <see cref="HttpClient.BaseAddress"/>.
    /// </exception>
    public Task<GraphQLHttpResponse> SendAsync(
        GraphQLHttpRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.Uri is null && _httpClient.BaseAddress is null)
        {
            throw new ArgumentException(
                HttpResources.DefaultGraphQLHttpClient_SendAsync_RequestUriIsNull,
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

        if (method == GraphQLHttpMethod.Post)
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

        request.OnMessageCreated?.Invoke(request, message);

        return message;
    }

    private static HttpContent CreatePostContent(OperationRequest request)
    {
        using var arrayWriter = new ArrayWriter();

        using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonOptionDefaults.WriterOptions);
        request.WriteTo(jsonWriter);
        jsonWriter.Flush();

        var content = new ByteArrayContent(arrayWriter.GetInternalBuffer(), 0, arrayWriter.Length);
#if NET7_0_OR_GREATER
        content.Headers.ContentType = new MediaTypeHeaderValue(ContentType.Json, "utf-8");
#else
        content.Headers.ContentType = new MediaTypeHeaderValue(ContentType.Json) { CharSet = "utf-8" };
#endif
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
            sb.Append(Uri.EscapeDataString(request.Id!));
        }

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            AppendAmpersand(sb, ref appendAmpersand);
            sb.Append("query=");
            sb.Append(Uri.EscapeDataString(request.Query!));
        }

        if (!string.IsNullOrWhiteSpace(request.OperationName))
        {
            AppendAmpersand(sb, ref appendAmpersand);
            sb.Append("operationName=");
            sb.Append(Uri.EscapeDataString(request.OperationName!));
        }

        if (request.VariablesNode is not null)
        {
            AppendAmpersand(sb, ref appendAmpersand);
            sb.Append("variables=");
            sb.Append(Uri.EscapeDataString(FormatDocumentAsJson(request.VariablesNode)));
        }
        else if (request.Variables is not null)
        {
            AppendAmpersand(sb, ref appendAmpersand);
            sb.Append("variables=");
            sb.Append(Uri.EscapeDataString(JsonSerializer.Serialize(request.Variables)));
        }

        if (request.ExtensionsNode is not null)
        {
            AppendAmpersand(sb, ref appendAmpersand);
            sb.Append("extensions=");
            sb.Append(Uri.EscapeDataString(FormatDocumentAsJson(request.ExtensionsNode)));
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

    private static string FormatDocumentAsJson(ObjectValueNode obj)
    {
        using var arrayWriter = new ArrayWriter();

        using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonOptionDefaults.WriterOptions);
        Utf8JsonWriterHelper.WriteFieldValue(jsonWriter, obj);
        jsonWriter.Flush();

#if NET6_0_OR_GREATER
        return Encoding.UTF8.GetString(arrayWriter.GetWrittenSpan());
#else
        return Encoding.UTF8.GetString(arrayWriter.GetInternalBuffer(), 0, arrayWriter.Length);
#endif
    }

    public void Dispose() => _httpClient.Dispose();
}