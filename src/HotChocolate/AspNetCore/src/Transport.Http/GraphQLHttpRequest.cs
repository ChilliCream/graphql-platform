namespace HotChocolate.Transport.Http;

/// <summary>
/// Represents a GraphQL over HTTP request.
/// </summary>
public sealed class GraphQLHttpRequest
{
    /// <summary>
    /// Initializes a new instance of <see cref="GraphQLHttpRequest"/>.
    /// </summary>
    /// <param name="query">
    /// The GraphQL query string.
    /// </param>
    /// <param name="requestUri">
    /// The GraphQL request URI.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="query"/> is <see langword="null"/> or whitespace.
    /// </exception>
    public GraphQLHttpRequest(string query, Uri? requestUri = null)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException(
                HttpResources.GraphQLHttpRequest_QueryNullOrEmpty,
                nameof(query));
        }

        Body = new OperationRequest(query);
        Uri = requestUri;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="GraphQLHttpRequest"/>.
    /// </summary>
    /// <param name="body">
    /// The GraphQL request operation.
    /// </param>
    /// <param name="requestUri">
    /// The GraphQL request URI.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="body"/> has no <see cref="OperationRequest.Id"/>, <see cref="OperationRequest.Query"/>,
    /// <see cref="OperationRequest.Extensions"/> or <see cref="OperationRequest.ExtensionsNode"/>.
    /// </exception>
    public GraphQLHttpRequest(OperationRequest body, Uri? requestUri = null)
    {
        if (string.IsNullOrEmpty(body.Id) &&
            string.IsNullOrEmpty(body.Query) &&
            body.Extensions is null &&
            body.ExtensionsNode is null)
        {
            throw new ArgumentException(
                HttpResources.GraphQLHttpRequest_QueryIdAndExtensionsNullOrEmpty,
                nameof(body));
        }

        Body = body;
        Uri = requestUri;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="GraphQLHttpRequest"/>.
    /// </summary>
    /// <param name="body">
    /// The GraphQL request operation.
    /// </param>
    /// <param name="requestUri">
    /// The GraphQL request URI.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="body"/> has no <see cref="VariableBatchRequest.Id"/>, <see cref="VariableBatchRequest.Query"/>,
    /// <see cref="VariableBatchRequest.Extensions"/> or <see cref="VariableBatchRequest.ExtensionsNode"/>.
    /// </exception>
    public GraphQLHttpRequest(VariableBatchRequest body, Uri? requestUri = null)
    {
        if (string.IsNullOrEmpty(body.Id) &&
            string.IsNullOrEmpty(body.Query) &&
            body.Extensions is null &&
            body.ExtensionsNode is null)
        {
            throw new ArgumentException(
                HttpResources.GraphQLHttpRequest_QueryIdAndExtensionsNullOrEmpty,
                nameof(body));
        }

        Body = body;
        Uri = requestUri;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="GraphQLHttpRequest"/>.
    /// </summary>
    /// <param name="body">
    /// The GraphQL request operation.
    /// </param>
    /// <param name="requestUri">
    /// The GraphQL request URI.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="body"/> has no <see cref="OperationBatchRequest.Requests"/>.
    /// </exception>
    public GraphQLHttpRequest(OperationBatchRequest body, Uri? requestUri = null)
    {
        if (body.Requests is { Count: 0, })
        {
            throw new ArgumentException(
                HttpResources.GraphQLHttpRequest_RequiresOneOrMoreRequests,
                nameof(body));
        }

        foreach (var request in body.Requests)
        {
            if (string.IsNullOrEmpty(request.Id) &&
                string.IsNullOrEmpty(request.Query) &&
                request.Extensions is null &&
                request.ExtensionsNode is null)
            {
                throw new ArgumentException(
                    HttpResources.GraphQLHttpRequest_QueryIdAndExtensionsNullOrEmpty,
                    nameof(body));
            }
        }

        Body = body.Requests.Count > 1 ? body : body.Requests[0];
        Uri = requestUri;
    }

    /// <summary>
    /// Gets the request body.
    /// </summary>
    public IRequestBody Body { get; }

    /// <summary>
    /// Gets or sets the HTTP method.
    /// </summary>
    public GraphQLHttpMethod Method { get; set; } = GraphQLHttpMethod.Post;

    /// <summary>
    /// Gets or sets the GraphQL request <see cref="Uri"/>.
    /// </summary>
    public Uri? Uri { get; set; }

    /// <summary>
    /// Gets or sets a hook that can alter the <see cref="HttpRequestMessage"/> before it is sent.
    /// </summary>
    public OnHttpRequestMessageCreated? OnMessageCreated { get; set; }

    /// <summary>
    /// Specifies if files shall be uploaded using the multipart request spec.
    /// </summary>
    public bool EnableFileUploads { get; set; }

    /// <summary>
    /// Specifies that the request URI represents a persisted document URI.
    /// </summary>
    public bool PersistedDocumentUri { get; set; }

    public static implicit operator GraphQLHttpRequest(OperationRequest body) => new(body);

    public static implicit operator GraphQLHttpRequest(VariableBatchRequest body) => new(body);

    public static implicit operator GraphQLHttpRequest(OperationBatchRequest body) => new(body);
}
