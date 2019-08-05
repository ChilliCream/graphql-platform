using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;
using HotChocolate.Utilities;
using Snapshooter.Xunit;

namespace HotChocolate.PersistedQueries.Redis
{
    public class ServiceCollectionExtensionsTests
    {
        private ConnectionMultiplexer _connectionMultiplexer;
        private IDatabase _database;

        public ServiceCollectionExtensionsTests()
        {
            string endpoint =
               Environment.GetEnvironmentVariable("REDIS_ENDPOINT")
               ?? "localhost:6379";

            string password =
                Environment.GetEnvironmentVariable("REDIS_PASSWORD");

            var configuration = new ConfigurationOptions
            {
                Ssl = !string.IsNullOrEmpty(password),
                AbortOnConnectFail = false,
                Password = password
            };

            configuration.EndPoints.Add(endpoint);

            _connectionMultiplexer =
                ConnectionMultiplexer.Connect(configuration);

            _database = _connectionMultiplexer.GetDatabase();
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
                .MatchSnapshot();
        }
    }
}
