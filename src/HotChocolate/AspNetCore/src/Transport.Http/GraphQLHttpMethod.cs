using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Language;
using HotChocolate.Transport;
#if FUSION
using HotChocolate.Transport.Http;
#endif
using HotChocolate.Transport.Serialization;
using static System.Net.Http.HttpCompletionOption;

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
