using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Utilities;
using Snapshooter.Xunit;
using Squadron;
using StackExchange.Redis;

namespace HotChocolate.PersistedQueries.Redis;

public class ServiceCollectionExtensionsTests
    : IClassFixture<RedisResource>
{
    private readonly IDatabase _database;

    public ServiceCollectionExtensionsTests(RedisResource redisResource)
    {
        _database = redisResource.GetConnection().GetDatabase();
    }

    [Fact]
    public void AddRedisQueryStorage_Services_Is_Null()
    {
        // arrange
        // act
        void Action()
            => HotChocolateRedisPersistedQueriesServiceCollectionExtensions
                .AddRedisOperationDocumentStorage(null!, _ => _database);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void AddRedisQueryStorage_Factory_Is_Null()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        void Action()
            => services.AddRedisOperationDocumentStorage(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void AddRedisQueryStorage_Services()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddRedisOperationDocumentStorage(_ => _database);

        // assert
        services.ToDictionary(
                k => k.ServiceType.GetTypeName(),
                v => v.ImplementationType?.GetTypeName())
            .OrderBy(t => t.Key)
            .MatchSnapshot();
    }
}
