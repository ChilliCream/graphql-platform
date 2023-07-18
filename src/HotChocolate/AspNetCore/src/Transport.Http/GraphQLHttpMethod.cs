using System.Net.Http;

namespace HotChocolate.Transport.Http;

public sealed class GraphQLHttpMethod
{
    private readonly HttpMethod _method;
    
    private GraphQLHttpMethod(HttpMethod method)
    {
        _method = method;
    }
    
    public static GraphQLHttpMethod Get { get; } = new(HttpMethod.Get);
    
    public static GraphQLHttpMethod Post { get; } = new(HttpMethod.Post);
    
    public static implicit operator HttpMethod(GraphQLHttpMethod method) => method._method;
}