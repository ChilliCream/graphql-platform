using System;
using System.Threading.Tasks;
using HotChocolate.Language;
using StackExchange.Redis;
using Xunit;

namespace HotChocolate.Subscriptions.Redis
{
    public class RedisTests
    {
        private readonly IEventRegistry _registry;
        private readonly IEventSender _sender;

        public RedisTests()
        {
            string endpoint =
                Environment.GetEnvironmentVariable("REDIS_ENDPOINT")
                ?? "localhost:6379";

            string password =
                Environment.GetEnvironmentVariable("REDIS_PASSWORD");

            var configuration = new ConfigurationOptions
            {
                Ssl = true,
                AbortOnConnectFail = false,
                Password = password
            };

            configuration.EndPoints.Add(endpoint);

            var redisEventRegistry = new RedisEventRegistry(
                ConnectionMultiplexer.Connect(configuration),
                new JsonPayloadSerializer());

            _sender = redisEventRegistry;
            _registry = redisEventRegistry;
        }

        [Fact]
        public async Task SubscribeOneConsumer_SendMessage_ConsumerReceivesMessage()
        {
            // arrange
            var eventDescription = new EventDescription(
                Guid.NewGuid().ToString());

            // act
            IEventStream consumer = await _registry
                .SubscribeAsync(eventDescription);
            var outgoing = new EventMessage(eventDescription, "bar");
            await _sender.SendAsync(outgoing);

            // assert
            IEventMessage incoming = await consumer.ReadAsync();
            Assert.Equal(outgoing.Payload, incoming.Payload);
        }

        [Fact]
        public async Task SubscribeOneConsumer_Complete_StreamIsCompleted()
        {
            // arrange
            var eventDescription = new EventDescription(
                Guid.NewGuid().ToString());

            // act
            IEventStream consumer = await _registry
                .SubscribeAsync(eventDescription);
            await consumer.CompleteAsync();

            // assert
            Assert.True(consumer.IsCompleted);
        }

        [Fact]
        public async Task SubscribeTwoConsumer_SendOneMessage_BothConsumerReceivesMessage()
        {
            // arrange
            var eventDescription = new EventDescription(
                Guid.NewGuid().ToString());

            // act
            IEventStream consumerOne = await _registry
                .SubscribeAsync(eventDescription);
            IEventStream consumerTwo = await _registry
                .SubscribeAsync(eventDescription);
            var outgoing = new EventMessage(eventDescription, "bar");
            await _sender.SendAsync(outgoing);

            // assert
            IEventMessage incomingOne = await consumerOne.ReadAsync();
            IEventMessage incomingTwo = await consumerTwo.ReadAsync();
            Assert.Equal(outgoing.Payload, incomingOne.Payload);
            Assert.Equal(outgoing.Payload, incomingTwo.Payload);
        }

        [Fact]
        public async Task SubscribeTwoConsumer_SendTwoMessage_BothConsumerReceivesIndependentMessage()
        {
            // arrange
            string name = Guid.NewGuid().ToString();
            var eventDescriptionOne = new EventDescription(
                name, new ArgumentNode("b", "x"));
            var eventDescriptionTwo = new EventDescription(
                name, new ArgumentNode("b", "y"));

            // act
            IEventStream consumerOne = await _registry
                .SubscribeAsync(eventDescriptionOne);
            var outgoingOne = new EventMessage(eventDescriptionOne, "foo");
            await _sender.SendAsync(outgoingOne);

            IEventStream consumerTwo = await _registry
                .SubscribeAsync(eventDescriptionTwo);
            var outgoingTwo = new EventMessage(eventDescriptionTwo, "bar");
            await _sender.SendAsync(outgoingTwo);

            // assert
            IEventMessage incomingOne = await consumerOne.ReadAsync();
            IEventMessage incomingTwo = await consumerTwo.ReadAsync();
            Assert.Equal(outgoingOne.Payload, incomingOne.Payload);
            Assert.Equal(outgoingTwo.Payload, incomingTwo.Payload);
            Assert.NotEqual(incomingOne.Event, incomingTwo.Event);
        }
    }
}
