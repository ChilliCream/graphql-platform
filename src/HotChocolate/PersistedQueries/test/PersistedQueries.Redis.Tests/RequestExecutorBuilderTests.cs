using System;
using System.Linq;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Utilities;
using Snapshooter.Xunit;
using Squadron;
using StackExchange.Redis;
using Xunit;

namespace HotChocolate.PersistedQueries.Redis
{
    public class RequestExecutorBuilderTests
        : IClassFixture<RedisResource>
    {
        private IDatabase _database;

        public RequestExecutorBuilderTests(RedisResource redisResource)
        {
            _database = redisResource.GetConnection().GetDatabase();
        }

        [Fact]
        public void AddRedisQueryStorage_Services_Is_Null()
        {
            // arrange
            // act
            void Action() =>
                HotChocolateRedisPersistedQueriesRequestExecutorBuilderExtensions
                    .AddRedisQueryStorage(null!, sp => _database);

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
                    .AddRedisQueryStorage(builder, null!);

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
                    .AddReadOnlyRedisQueryStorage(null!, sp => _database);

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
                    .AddReadOnlyRedisQueryStorage(builder, null!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }
    }
}
