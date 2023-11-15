using StrawberryShake.Transport.Http;

namespace StrawberryShake.Tools;

public class DefaultHttpConnectionFactory : IHttpConnectionFactory
{
    private readonly System.Net.Http.IHttpClientFactory _clientFactory;

    public DefaultHttpConnectionFactory(System.Net.Http.IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public IHttpConnection CreateHttpConnection(string name)
    {
        return new HttpConnection(() => _clientFactory.CreateClient(name));
    }
}
