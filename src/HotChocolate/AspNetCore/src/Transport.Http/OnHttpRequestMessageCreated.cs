namespace HotChocolate.Transport.Http;

/// <summary>
/// A delegate to intercept the <see cref="HttpRequestMessage"/> before it is sent.
/// </summary>
public delegate void OnHttpRequestMessageCreated(
    GraphQLHttpRequest request,
    HttpRequestMessage requestMessage);
