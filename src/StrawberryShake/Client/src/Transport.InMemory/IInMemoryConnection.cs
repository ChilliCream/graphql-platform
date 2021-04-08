using System.Text.Json;

namespace StrawberryShake.Transport.InMemory
{
    public interface IInMemoryConnection : IConnection<JsonDocument>
    {
    }
}
