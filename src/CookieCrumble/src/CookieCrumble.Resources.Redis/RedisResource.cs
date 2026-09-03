using StackExchange.Redis;
using Testcontainers.Redis;

namespace CookieCrumble.Resources;

public class RedisResource : ContainerResource<RedisContainer>
{
    public string ConnectionString => Container.GetConnectionString();

    public ConnectionMultiplexer GetConnection() => ConnectionMultiplexer.Connect(ConnectionString);

    protected override RedisContainer Build() => Configure(new RedisBuilder("redis:7.0")).Build();

    protected virtual RedisBuilder Configure(RedisBuilder builder) => builder;
}
