using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace CookieCrumble.Resources;

public class MongoResource : ContainerResource<MongoDbContainer>
{
    private readonly Lazy<MongoClient> _client;

    public MongoResource()
    {
        _client = new(() => new MongoClient(ConnectionString));
    }

    public IMongoClient Client => _client.Value;

    public string ConnectionString => Container.GetConnectionString();

    // Every call without an explicit database creates a new, uniquely named
    // database, so callers sharing one container stay isolated.
    public IMongoDatabase CreateDatabase(string? name = null)
        => Client.GetDatabase(name ?? $"db_{Guid.NewGuid():N}");

    public IMongoCollection<T> CreateCollection<T>(string? name = null)
    {
        var database = CreateDatabase();
        name ??= $"col_{Guid.NewGuid():N}";
        database.CreateCollection(name);

        return database.GetCollection<T>(name);
    }

    protected override MongoDbContainer Build()
        => Configure(new MongoDbBuilder("mongo:6.0")).Build();

    protected virtual MongoDbBuilder Configure(MongoDbBuilder builder) => builder;

    protected override ValueTask DisposeAsyncCore()
    {
        if (_client.IsValueCreated)
        {
            _client.Value.Dispose();
        }

        return ValueTask.CompletedTask;
    }
}
