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
    /// <exception cref="ArgumentException">
    /// <paramref name="query"/> is <see langword="null"/> or whitespace.
    /// </exception>
    public GraphQLHttpRequest(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException(
                HttpResources.GraphQLHttpRequest_QueryNullOrEmpty, 
                nameof(query));
        }

        Body = new OperationRequest(query);
    }
    
    /// <summary>
    /// Initializes a new instance of <see cref="GraphQLHttpRequest"/>.
    /// </summary>
    /// <param name="body">
    /// The GraphQL request body.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="body"/> has no <see cref="OperationRequest.Id"/>, <see cref="OperationRequest.Query"/>,
    /// <see cref="OperationRequest.Extensions"/> or <see cref="OperationRequest.ExtensionsNode"/>.
    /// </exception>
    public GraphQLHttpRequest(OperationRequest body)
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
    }

    /// <summary>
    /// Gets the GraphQL request body.
    /// </summary>
    public OperationRequest Body { get; }

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
    
    public static implicit operator GraphQLHttpRequest(OperationRequest method) => new(method);
}