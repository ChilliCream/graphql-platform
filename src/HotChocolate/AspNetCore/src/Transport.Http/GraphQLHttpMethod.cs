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

    /// <summary>
    /// Gets the HTTP QUERY method.
    /// </summary>
    public static GraphQLHttpMethod Query { get; } =
#if NET10_0_OR_GREATER
        new(HttpMethod.Query);
#else
        new(new HttpMethod("QUERY"));
#endif

    public static implicit operator HttpMethod(GraphQLHttpMethod method) => method._method;
}
