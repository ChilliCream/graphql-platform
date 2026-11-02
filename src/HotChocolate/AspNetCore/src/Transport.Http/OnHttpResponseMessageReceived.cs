#if FUSION
using HotChocolate.Fusion.Execution.Clients;

namespace HotChocolate.Fusion.Transport.Http;
#else
namespace HotChocolate.Transport.Http;
#endif

/// <summary>
/// A delegate to intercept the <see cref="HttpResponseMessage"/> after it is received.
/// </summary>
public delegate void OnHttpResponseMessageReceived(
    GraphQLHttpRequest request,
    HttpResponseMessage responseMessage,
#if FUSION
    RequestCallbackState state);
#else
    object? state);
#endif
