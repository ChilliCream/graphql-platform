using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Utilities;
using Snapshooter.Xunit;
using Squadron;
using StackExchange.Redis;
using Xunit;

namespace HotChocolate.PersistedQueries.Redis
{
    public class ServiceCollectionExtensionsTests
        : IClassFixture<RedisResource>
    {
        private IDatabase _database;

        public ServiceCollectionExtensionsTests(RedisResource redisResource)
        {
            _database = redisResource.GetConnection().GetDatabase();
        }

        [Fact]
        public void AddRedisQueryStorage_Services_Is_Null()
        {
            // arrange
            // act
            Action action = () =>
                RedisQueryStorageServiceCollectionExtensions
                    .AddRedisQueryStorage(null, sp => _database);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddRedisQueryStorage_Factory_Is_Null()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            Action action = () =>
                RedisQueryStorageServiceCollectionExtensions
                    .AddRedisQueryStorage(services, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddRedisQueryStorage_Services()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            RedisQueryStorageServiceCollectionExtensions
                .AddRedisQueryStorage(services, sp => _database);

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
            Action action = () =>
                RedisQueryStorageServiceCollectionExtensions
                    .AddReadOnlyRedisQueryStorage(null, sp => _database);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddReadOnlyRedisQueryStorage_Factory_Is_Null()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            Action action = () =>
                RedisQueryStorageServiceCollectionExtensions
                    .AddReadOnlyRedisQueryStorage(services, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddReadOnlyRedisQueryStorage_Services()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            RedisQueryStorageServiceCollectionExtensions
                .AddReadOnlyRedisQueryStorage(services, sp => _database);

            // assert
            services.ToDictionary(
                k => k.ServiceType.GetTypeName(),
                v => v.ImplementationType?.GetTypeName())
                .OrderBy(t => t.Key)
                .MatchSnapshot();
        }
    }
}
