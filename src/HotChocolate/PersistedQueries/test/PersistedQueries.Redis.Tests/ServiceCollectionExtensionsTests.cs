using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Utilities;
using Snapshooter.Xunit;
using Squadron;
using StackExchange.Redis;
using Xunit;

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
                .AddRedisQueryStorage(null!, _ => _database);

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
            => HotChocolateRedisPersistedQueriesServiceCollectionExtensions
                .AddRedisQueryStorage(services, null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void AddRedisQueryStorage_Services()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        HotChocolateRedisPersistedQueriesServiceCollectionExtensions
            .AddRedisQueryStorage(services, _ => _database);

        // assert
        services.ToDictionary(
                k => k.ServiceType.GetTypeName(),
                v => v.ImplementationType?.GetTypeName())
            .OrderBy(t => t.Key)
            .MatchSnapshot();
    }

    [Fact]
    public void AddReadOnlyRedisQueryStorage_Services_Is_Null()
    {
        // arrange
        // act
        void Action()
            => HotChocolateRedisPersistedQueriesServiceCollectionExtensions
                .AddReadOnlyRedisQueryStorage(null!, _ => _database);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void AddReadOnlyRedisQueryStorage_Factory_Is_Null()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        void Action()
            => HotChocolateRedisPersistedQueriesServiceCollectionExtensions
                .AddReadOnlyRedisQueryStorage(services, null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void AddReadOnlyRedisQueryStorage_Services()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        HotChocolateRedisPersistedQueriesServiceCollectionExtensions
            .AddReadOnlyRedisQueryStorage(services, _ => _database);

        // assert
        services.ToDictionary(
                k => k.ServiceType.GetTypeName(),
                v => v.ImplementationType?.GetTypeName())
            .OrderBy(t => t.Key)
            .MatchSnapshot();
    }
}
