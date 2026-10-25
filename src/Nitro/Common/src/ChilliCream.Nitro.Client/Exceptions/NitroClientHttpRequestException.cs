using System.Net;

namespace ChilliCream.Nitro.Client;

public sealed class NitroClientHttpRequestException(HttpStatusCode? statusCode) : NitroClientException("")
{
    public HttpStatusCode? StatusCode { get; } = statusCode;
}
