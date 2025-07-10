namespace HotChocolate.Transport.Http;

/// <summary>
/// A delegate to intercept the <see cref="HttpResponseMessage"/> after it is received.
/// </summary>
public delegate void OnHttpResponseMessageReceived(
    GraphQLHttpRequest request,
    HttpResponseMessage responseMessage,
    object? state);
