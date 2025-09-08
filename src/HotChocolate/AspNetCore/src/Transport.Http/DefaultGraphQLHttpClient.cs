// ReSharper disable IntroduceOptionalParameters.Global

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Language;
using HotChocolate.Transport.Serialization;
using static System.Net.Http.HttpCompletionOption;

namespace HotChocolate.Transport.Http;

/// <summary>
/// A default implementation of <see cref="GraphQLHttpClient"/> that supports the GraphQL over HTTP spec draft.
/// </summary>
public sealed class DefaultGraphQLHttpClient : GraphQLHttpClient
{
    private readonly HttpClient _http;
    private readonly bool _disposeInnerClient;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultGraphQLHttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The underlying HTTP client that is used to send the GraphQL request.
    /// </param>
    /// <param name="disposeInnerClient">
    /// Specifies if <paramref name="httpClient"/> shall be disposed when this instance is disposed.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public DefaultGraphQLHttpClient(HttpClient httpClient, bool disposeInnerClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _http = httpClient;
        _disposeInnerClient = disposeInnerClient;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultGraphQLHttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The underlying HTTP client that is used to send the GraphQL request.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public DefaultGraphQLHttpClient(HttpClient httpClient)
        : this(httpClient, disposeInnerClient: true)
    {
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
    public override Task<GraphQLHttpResponse> SendAsync(
        GraphQLHttpRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Uri is null && _http.BaseAddress is null)
        {
            throw new ArgumentException(
                HttpResources.DefaultGraphQLHttpClient_SendAsync_RequestUriIsNull,
                nameof(request));
        }

        var requestUri = request.Uri ?? _http.BaseAddress!;
        return ExecuteInternalAsync(request, requestUri, cancellationToken);
    }

    private async Task<GraphQLHttpResponse> ExecuteInternalAsync(
        GraphQLHttpRequest request,
        Uri requestUri,
        CancellationToken ct)
    {
        // The array writer is needed for formatting the request.
        // We keep it up here so that the associated memory is being
        // kept until the request is done.
        // DO NOT move the writer out of this method.
        using var arrayWriter = new PooledArrayWriter();
        using var requestMessage = CreateRequestMessage(arrayWriter, request, requestUri);

        request.OnMessageCreated?.Invoke(request, requestMessage, request.State);

        requestMessage.Version = _http.DefaultRequestVersion;
        requestMessage.VersionPolicy = _http.DefaultVersionPolicy;

        var responseMessage = await _http
            .SendAsync(requestMessage, ResponseHeadersRead, ct)
            .ConfigureAwait(false);

        request.OnMessageReceived?.Invoke(request, responseMessage, request.State);

        return new GraphQLHttpResponse(responseMessage);
    }

    private static HttpRequestMessage CreateRequestMessage(
        PooledArrayWriter arrayWriter,
        GraphQLHttpRequest request,
        Uri requestUri)
    {
        var method = request.Method;

        if (method == GraphQLHttpMethod.Get)
        {
            if (request.Body is not OperationRequest)
            {
                throw new InvalidOperationException(
                    HttpResources.DefaultGraphQLHttpClient_BatchNotAllowed);
            }

            if (request.EnableFileUploads)
            {
                throw new NotSupportedException(
                    HttpResources.DefaultGraphQLHttpClient_FileUploadNotAllowed);
            }
        }

        var message = new HttpRequestMessage
        {
            Method = method
        };

        message.Headers.Accept.Clear();
        foreach (var contentType in request.Accept)
        {
            message.Headers.Accept.Add(contentType);
        }

        if (method == GraphQLHttpMethod.Post)
        {
            if (request.EnableFileUploads)
            {
                message.Content = CreateMultipartContent(arrayWriter, request);
                message.Headers.AddGraphQLPreflight();
            }
            else
            {
                message.Content = CreatePostContent(arrayWriter, request);
            }

            message.RequestUri = requestUri;
        }
        else if (method == GraphQLHttpMethod.Get)
        {
            message.RequestUri = CreateGetRequestUri(arrayWriter, requestUri, request.Body);
        }
        else
        {
            throw new NotSupportedException($"The HTTP method `{method}` is not supported.");
        }

        return message;
    }

    private static ByteArrayContent CreatePostContent(
        PooledArrayWriter arrayWriter,
        GraphQLHttpRequest request)
    {
        using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonOptionDefaults.WriterOptions);
        request.Body.WriteTo(jsonWriter);
        jsonWriter.Flush();

        var internalBuffer = PooledArrayWriterMarshal.GetUnderlyingBuffer(arrayWriter);
        var content = new ByteArrayContent(internalBuffer, 0, arrayWriter.Length);
        content.Headers.ContentType = new MediaTypeHeaderValue(ContentType.Json, "utf-8");
        return content;
    }

    private static HttpContent CreateMultipartContent(
        PooledArrayWriter arrayWriter,
        GraphQLHttpRequest request)
    {
        var fileInfos = WriteFileMapJson(arrayWriter, request);

        if (fileInfos.Count == 0)
        {
            arrayWriter.Reset();
            return CreatePostContent(arrayWriter, request);
        }

        var start = arrayWriter.Length;
        WriteOperationJson(arrayWriter, request);
        var buffer = PooledArrayWriterMarshal.GetUnderlyingBuffer(arrayWriter);

        var form = new MultipartFormDataContent();

        var operation = new ByteArrayContent(buffer, start, arrayWriter.Length - start);
        operation.Headers.ContentType = new MediaTypeHeaderValue(ContentType.Json, "utf-8");
        form.Add(operation, "operations");

        var fileMap = new ByteArrayContent(buffer, 0, start);
        fileMap.Headers.ContentType = new MediaTypeHeaderValue(ContentType.Json, "utf-8");
        form.Add(fileMap, "map");

        foreach (var fileInfo in fileInfos)
        {
            var fileContent = new StreamContent(fileInfo.File.OpenRead());
            if (!string.IsNullOrEmpty(fileInfo.File.ContentType))
            {
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(fileInfo.File.ContentType);
            }

            form.Add(fileContent, fileInfo.Name, fileInfo.File.FileName);
        }

        return form;
    }

    private static void WriteOperationJson(PooledArrayWriter arrayWriter, GraphQLHttpRequest request)
    {
        using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonOptionDefaults.WriterOptions);
        request.Body.WriteTo(jsonWriter);
    }

    private static IReadOnlyList<FileReferenceInfo> WriteFileMapJson(
        PooledArrayWriter arrayWriter,
        GraphQLHttpRequest request)
    {
        using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonOptionDefaults.WriterOptions);
        return Utf8JsonWriterHelper.WriteFilesMap(jsonWriter, request.Body);
    }

    private static Uri CreateGetRequestUri(
        PooledArrayWriter arrayWriter,
        Uri baseAddress,
        IRequestBody body)
    {
        if (body is not OperationRequest or)
        {
            throw new InvalidOperationException(
                HttpResources.DefaultGraphQLHttpClient_BatchNotAllowed);
        }

        var sb = new StringBuilder();
        var appendAmpersand = false;

        sb.Append(baseAddress);
        sb.Append('?');

        if (!string.IsNullOrWhiteSpace(or.Id))
        {
            AppendAmpersand(sb, ref appendAmpersand);
            sb.Append("id=");
            sb.Append(Uri.EscapeDataString(or.Id!));
        }

        if (!string.IsNullOrWhiteSpace(or.Query))
        {
            AppendAmpersand(sb, ref appendAmpersand);
            sb.Append("query=");
            sb.Append(Uri.EscapeDataString(or.Query!));
        }

        if (!string.IsNullOrWhiteSpace(or.OperationName))
        {
            AppendAmpersand(sb, ref appendAmpersand);
            sb.Append("operationName=");
            sb.Append(Uri.EscapeDataString(or.OperationName!));
        }

        if (or.OnError is {} errorHandlingMode)
        {
            AppendAmpersand(sb, ref appendAmpersand);
            sb.Append("onError=");
            sb.Append(GetErrorHandlingModeAsString(errorHandlingMode));
        }

        if (or.VariablesNode is not null)
        {
            AppendAmpersand(sb, ref appendAmpersand);
            sb.Append("variables=");
            sb.Append(Uri.EscapeDataString(FormatDocumentAsJson(arrayWriter, or.VariablesNode)));
        }
        else if (or.Variables is not null)
        {
            AppendAmpersand(sb, ref appendAmpersand);
            sb.Append("variables=");
            sb.Append(Uri.EscapeDataString(FormatDocumentAsJson(arrayWriter, or.Variables)));
        }

        if (or.ExtensionsNode is not null)
        {
            AppendAmpersand(sb, ref appendAmpersand);
            sb.Append("extensions=");
            sb.Append(Uri.EscapeDataString(FormatDocumentAsJson(arrayWriter, or.ExtensionsNode)));
        }
        else if (or.Extensions is not null)
        {
            AppendAmpersand(sb, ref appendAmpersand);
            sb.Append("extensions=");
            sb.Append(Uri.EscapeDataString(FormatDocumentAsJson(arrayWriter, or.Extensions)));
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

    private static string GetErrorHandlingModeAsString(ErrorHandlingMode mode)
    {
        return mode switch
        {
            ErrorHandlingMode.Propagate => "PROPAGATE",
            ErrorHandlingMode.Null => "NULL",
            ErrorHandlingMode.Halt => "HALT",
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    private static string FormatDocumentAsJson(PooledArrayWriter arrayWriter, object? obj)
    {
        arrayWriter.Reset();

        using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonOptionDefaults.WriterOptions);
        Utf8JsonWriterHelper.WriteFieldValue(jsonWriter, obj);
        jsonWriter.Flush();

        return Encoding.UTF8.GetString(arrayWriter.WrittenSpan);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _disposeInnerClient)
        {
            _http.Dispose();
        }
    }
}
