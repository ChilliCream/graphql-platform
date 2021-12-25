using System;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace HotChocolate.Subscriptions.Redis
{
    public class RedisSubscriptionsServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddRedisSubscriptions_1_Services_Is_Null()
        {
            void Fail() =>
                default(IServiceCollection).AddRedisSubscriptions(
                    sp => sp.GetRequiredService<IConnectionMultiplexer>());

            Assert.Throws<ArgumentNullException>(Fail);
        }

        [Fact]
        public void AddRedisSubscriptions_1_Connection_Is_Null()
        {
            void Fail() =>
                new ServiceCollection().AddRedisSubscriptions(
                    default(Func<IServiceProvider, IConnectionMultiplexer>));

            Assert.Throws<ArgumentNullException>(Fail);
        }

        [Fact]
        public void AddRedisSubscriptions_2_Services_Is_Null()
        {
            void Fail() =>
                default(IRequestExecutorBuilder).AddRedisSubscriptions(
                    sp => sp.GetRequiredService<IConnectionMultiplexer>());

            Assert.Throws<ArgumentNullException>(Fail);
        }

        [Fact]
        public void AddRedisSubscriptions_2_Connection_Is_Null()
        {
            var builder = new Mock<IRequestExecutorBuilder>();

            void Fail() =>
                builder.Object.AddRedisSubscriptions(
                    default(Func<IServiceProvider, IConnectionMultiplexer>));

            Assert.Throws<ArgumentNullException>(Fail);
        }

        [Fact]
        public void AddRedisSubscriptions_3_Services_Is_Null()
        {
            void Fail() =>
                default(IRequestExecutorBuilder).AddRedisSubscriptions();

            Assert.Throws<ArgumentNullException>(Fail);
        }
    }
}
