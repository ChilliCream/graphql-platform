using System;
using HotChocolate.Subscriptions.Redis;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace HotChocolate.Subscriptions
{
    public class RedisSubscriptionServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddRedisSubscriptionProviderWithOptions_WhenNullServiceCollection_Throws()
        {
            // arrange
            IServiceCollection serviceCollection = null;

            // act, assert
            Assert.Throws<ArgumentNullException>(() => serviceCollection
                .AddRedisSubscriptionProvider(sp => new ConfigurationOptions()));
        }

        [Fact]
        public void AddRedisSubscriptionProviderWithConnection_WhenNullServiceCollection_Throws()
        {
            // arrange
            IServiceCollection serviceCollection = null;

            // act, assert
            Assert.Throws<ArgumentNullException>(() => serviceCollection
                .AddRedisSubscriptionProvider(sp =>
                    ConnectionMultiplexer.Connect(new ConfigurationOptions())));
        }

        [Fact]
        public void AddRedisSubscriptionProvider_HasCustomSerializer()
        {
            // arrange
            IServiceCollection serviceCollection = new ServiceCollection();

            // act
            IServiceProvider serviceProvider = serviceCollection
                .AddRedisSubscriptionProvider<CustomSerializer>(sp => new ConfigurationOptions())
                .BuildServiceProvider();

            // assert
            IPayloadSerializer payloadSerializer = serviceProvider
                .GetService<IPayloadSerializer>();
            Assert.IsType<CustomSerializer>(payloadSerializer);
        }

        [Fact]
        public void AddRedisSubscriptionProvider_HasRedisEventRegistry()
        {
            // arrange
            IServiceCollection serviceCollection = new ServiceCollection();
            IConnectionMultiplexer connectionMultiplexer = Mock.Of<IConnectionMultiplexer>();

            // act
            IServiceProvider serviceProvider = serviceCollection
                .AddRedisSubscriptionProvider(sp => connectionMultiplexer)
                .BuildServiceProvider();

            // assert
            IEventRegistry eventRegistry = serviceProvider
                .GetService<IEventRegistry>();
            Assert.IsType<RedisEventRegistry>(eventRegistry);
        }

        [Fact]
        public void AddRedisSubscriptionProvider_HasRedisEventSender()
        {
            // arrange
            IServiceCollection serviceCollection = new ServiceCollection();
            IConnectionMultiplexer connectionMultiplexer = Mock.Of<IConnectionMultiplexer>();

            // act
            IServiceProvider serviceProvider = serviceCollection
                .AddRedisSubscriptionProvider(sp => connectionMultiplexer)
                .BuildServiceProvider();

            // assert
            IEventSender eventSender = serviceProvider
                .GetService<IEventSender>();
            Assert.IsType<RedisEventRegistry>(eventSender);
        }

        private class CustomSerializer : IPayloadSerializer
        {
            public byte[] Serialize(object value)
            {
                return new byte[0];
            }

            public object Deserialize(byte[] content)
            {
                return new object();
            }
        }
    }
}
