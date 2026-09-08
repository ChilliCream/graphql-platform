using StackExchange.Redis;
using Testcontainers.Redis;

namespace CookieCrumble.Resources;

public class RedisResource : ContainerResource<RedisContainer>
{
    private readonly Lazy<ConnectionMultiplexer> _connection;

    public RedisResource()
    {
        _connection = new(() => ConnectionMultiplexer.Connect(ConnectionString));
    }

    public string ConnectionString => Container.GetConnectionString();

    /// <summary>
    /// Gets the connection shared by every caller of this resource. The resource owns it
    /// and disposes it together with the container.
    /// </summary>
    public ConnectionMultiplexer GetConnection() => _connection.Value;

    protected override RedisContainer Build() => Configure(new RedisBuilder("redis:7.0")).Build();

    protected virtual RedisBuilder Configure(RedisBuilder builder) => builder;

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_connection.IsValueCreated)
        {
            await _connection.Value.DisposeAsync();
        }
    }
}
