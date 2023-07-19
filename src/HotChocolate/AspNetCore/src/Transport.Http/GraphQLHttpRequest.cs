using System;
using System.Net.Http;

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

        Operation = new OperationRequest(query);
        Uri = requestUri;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="GraphQLHttpRequest"/>.
    /// </summary>
    /// <param name="operation">
    /// The GraphQL request operation.
    /// </param>
    /// <param name="requestUri">
    /// The GraphQL request URI.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="operation"/> has no <see cref="OperationRequest.Id"/>, <see cref="OperationRequest.Query"/>,
    /// <see cref="OperationRequest.Extensions"/> or <see cref="OperationRequest.ExtensionsNode"/>.
    /// </exception>
    public GraphQLHttpRequest(OperationRequest operation, Uri? requestUri = null)
    {
        if (string.IsNullOrEmpty(operation.Id) &&
            string.IsNullOrEmpty(operation.Query) &&
            operation.Extensions is null &&
            operation.ExtensionsNode is null)
        {
            throw new ArgumentException(
                HttpResources.GraphQLHttpRequest_QueryIdAndExtensionsNullOrEmpty, 
                nameof(operation));
        }

        Operation = operation;
        Uri = requestUri;
    }

    /// <summary>
    /// Gets the GraphQL operation.
    /// </summary>
    public OperationRequest Operation { get; }

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

    public static implicit operator GraphQLHttpRequest(OperationRequest method) => new(method);
}