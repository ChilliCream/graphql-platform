using System;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Squadron;
using StackExchange.Redis;
using Xunit;

namespace HotChocolate.PersistedQueries.Redis
{
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
        public void AddRedisQueryStorage_Services_Is_Null()
        {
            // arrange
            // act
            void Action() =>
                HotChocolateRedisPersistedQueriesRequestExecutorBuilderExtensions
                    .AddRedisQueryStorage(null!, _ => _database);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void AddRedisQueryStorage_MultiplexerServices_Is_Null()
        {
            // arrange
            // act
            void Action() =>
                HotChocolateRedisPersistedQueriesRequestExecutorBuilderExtensions
                    .AddRedisQueryStorage(null!, _ => _multiplexer);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void AddRedisQueryStorage_DefaultServices_Is_Null()
        {
            // arrange
            // act
            void Action() =>
                HotChocolateRedisPersistedQueriesRequestExecutorBuilderExtensions
                    .AddRedisQueryStorage(null!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void AddRedisQueryStorage_Factory_Is_Null()
        {
            // arrange
            IRequestExecutorBuilder builder = new ServiceCollection().AddGraphQL();

            // act
            void Action() =>
                HotChocolateRedisPersistedQueriesRequestExecutorBuilderExtensions
                    .AddRedisQueryStorage(builder, default(Func<IServiceProvider, IDatabase>)!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void AddRedisQueryStorage_MultiplexerFactory_Is_Null()
        {
            // arrange
            IRequestExecutorBuilder builder = new ServiceCollection().AddGraphQL();

            // act
            void Action() =>
                HotChocolateRedisPersistedQueriesRequestExecutorBuilderExtensions
                    .AddRedisQueryStorage(
                        builder,
                        default(Func<IServiceProvider, IConnectionMultiplexer>)!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void AddReadOnlyRedisQueryStorage_Services_Is_Null()
        {
            // arrange
            // act
            void Action() =>
                HotChocolateRedisPersistedQueriesRequestExecutorBuilderExtensions
                    .AddReadOnlyRedisQueryStorage(null!, _ => _database);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void AddReadOnlyRedisQueryStorage_MultiplexerServices_Is_Null()
        {
            // arrange
            // act
            void Action() =>
                HotChocolateRedisPersistedQueriesRequestExecutorBuilderExtensions
                    .AddReadOnlyRedisQueryStorage(null!, _ => _multiplexer);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void AddReadOnlyRedisQueryStorage_DefaultServices_Is_Null()
        {
            // arrange
            // act
            void Action() =>
                HotChocolateRedisPersistedQueriesRequestExecutorBuilderExtensions
                    .AddReadOnlyRedisQueryStorage(null!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void AddReadOnlyRedisQueryStorage_Factory_Is_Null()
        {
            // arrange
            IRequestExecutorBuilder builder = new ServiceCollection().AddGraphQL();

            // act
            void Action() =>
                HotChocolateRedisPersistedQueriesRequestExecutorBuilderExtensions
                    .AddReadOnlyRedisQueryStorage(
                        builder,
                        default(Func<IServiceProvider, IDatabase>)!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void AddReadOnlyRedisQueryStorage_MultiplexerFactory_Is_Null()
        {
            // arrange
            IRequestExecutorBuilder builder = new ServiceCollection().AddGraphQL();

            // act
            void Action() =>
                HotChocolateRedisPersistedQueriesRequestExecutorBuilderExtensions
                    .AddReadOnlyRedisQueryStorage(
                        builder,
                        default(Func<IServiceProvider, IConnectionMultiplexer>)!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }
    }
}
