using System;

namespace HotChocolate.Transport.Http;

public sealed class GraphQLHttpRequest
{
    public GraphQLHttpRequest(OperationRequest body)
    {
        if (string.IsNullOrEmpty(body.Id) &&
            string.IsNullOrEmpty(body.Query) &&
            body.Extensions is null &&
            body.ExtensionsNode is null)
        {
            throw new ArgumentException("TODO: RESOURCES", nameof(body));
        }

        Body = body;
    }

    public OperationRequest Body { get; }

    public GraphQLHttpMethod Method { get; set; } = GraphQLHttpMethod.Post;
    
    public Uri? Uri { get; set; }

    public OnHttpRequestMessageCreated? OnMessageCreated { get; set; }

    public static implicit operator GraphQLHttpRequest(OperationRequest method) => new(method);
}