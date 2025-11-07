#if FUSION
namespace HotChocolate.Fusion.Transport.Http;
#else
namespace HotChocolate.Transport.Http;
#endif

/// <summary>
/// This class provides the default HTTP methods for GraphQL requests.
/// </summary>
public sealed class GraphQLHttpMethod
{
    private readonly HttpMethod _method;

    private GraphQLHttpMethod(HttpMethod method)
    {
        _method = method;
    }

    /// <summary>
    /// Gets the HTTP GET method.
    /// </summary>
    public static GraphQLHttpMethod Get { get; } = new(HttpMethod.Get);

    /// <summary>
    /// Gets the HTTP POST method.
    /// </summary>
    public static GraphQLHttpMethod Post { get; } = new(HttpMethod.Post);

    public static implicit operator HttpMethod(GraphQLHttpMethod method) => method._method;
}
