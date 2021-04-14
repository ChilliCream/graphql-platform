using System.Text.Json;

namespace StrawberryShake.Transport.Http
{
    public interface IHttpConnection : IConnection<JsonDocument>
    {
    }
}
