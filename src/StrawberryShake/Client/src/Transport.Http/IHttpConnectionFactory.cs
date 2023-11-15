namespace StrawberryShake.Transport.Http;

public interface IHttpConnectionFactory
{
    public IHttpConnection CreateHttpConnection(string name);
}
