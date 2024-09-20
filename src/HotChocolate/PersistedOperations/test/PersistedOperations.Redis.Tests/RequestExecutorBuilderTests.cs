using Microsoft.Extensions.DependencyInjection;
using Squadron;
using StackExchange.Redis;

namespace HotChocolate.PersistedOperations.Redis;

public class RequestExecutorBuilderTests : IClassFixture<RedisResource>
{
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly IDatabase _database;

    public RequestExecutorBuilderTests(RedisResource redisResource)
    {
        _multiplexer = redisResource.GetConnection();
        _database = _multiplexer.GetDatabase();
    }

    [Fact]
    public void AddRedisOperationDocumentStorage_Services_Is_Null()
    {
        // arrange
        // act
        void Action() =>
            HotChocolateRedisPersistedOperationsRequestExecutorBuilderExtensions
                .AddRedisOperationDocumentStorage(null!, _ => _database);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void AddRedisOperationDocumentStorage_MultiplexerServices_Is_Null()
    {
        // arrange
        // act
        void Action() =>
            HotChocolateRedisPersistedOperationsRequestExecutorBuilderExtensions
                .AddRedisOperationDocumentStorage(null!, _ => _multiplexer);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void AddRedisOperationDocumentStorage_DefaultServices_Is_Null()
    {
        // arrange
        // act
        void Action() =>
            HotChocolateRedisPersistedOperationsRequestExecutorBuilderExtensions
                .AddRedisOperationDocumentStorage(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void AddRedisOperationDocumentStorage_Factory_Is_Null()
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL();

        // act
        void Action() =>
            builder.AddRedisOperationDocumentStorage(default(Func<IServiceProvider, IDatabase>)!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void AddRedisOperationDocumentStorage_MultiplexerFactory_Is_Null()
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL();

        // act
        void Action() =>
            builder.AddRedisOperationDocumentStorage(default(Func<IServiceProvider, IConnectionMultiplexer>)!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }
}
