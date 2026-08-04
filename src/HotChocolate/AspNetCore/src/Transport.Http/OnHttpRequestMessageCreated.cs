#if FUSION
using HotChocolate.Fusion.Execution.Clients;

namespace HotChocolate.Fusion.Transport.Http;
#else
namespace HotChocolate.Transport.Http;
#endif

/// <summary>
/// A delegate to intercept the <see cref="HttpRequestMessage"/> before it is sent.
/// </summary>
public delegate void OnHttpRequestMessageCreated(
    GraphQLHttpRequest request,
    HttpRequestMessage requestMessage,
#if FUSION
    RequestCallbackState state);
#else
    object? state);
#endif
