using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using Squadron;
using StackExchange.Redis;
using Xunit;

namespace HotChocolate.Subscriptions.Redis
{
    public class RedisTests
        : IClassFixture<RedisResource>
    {
        private readonly IEventRegistry _registry;
        private readonly IEventSender _sender;

        public RedisTests(RedisResource redisResource)
        {
            var redisEventRegistry = new RedisEventRegistry(
                redisResource.GetConnection(),
                new JsonPayloadSerializer());

            _sender = redisEventRegistry;
            _registry = redisEventRegistry;
        }

        [Fact]
        public Task SubscribeOneConsumer_SendMessage_ConsumerReceivesMessage()
        {
            return TestHelper.TryTest(async () =>
            {
                // arrange
                var cts = new CancellationTokenSource(30000);
                var eventDescription = new EventDescription(
                    Guid.NewGuid().ToString());

                // act
                IEventStream consumer = await _registry
                    .SubscribeAsync(eventDescription);
                var outgoing = new EventMessage(eventDescription, "bar");
                await _sender.SendAsync(outgoing);

                // assert
                IEventMessage incoming = await consumer.ReadAsync(cts.Token);
                Assert.Equal(outgoing.Payload, incoming.Payload);
            });
        }

        [Fact]
        public Task SubscribeOneConsumer_Complete_StreamIsCompleted()
        {
            return TestHelper.TryTest(async () =>
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
            });
        }

        [Fact]
        public Task SubscribeTwoConsumer_SendOneMessage_BothConsumerReceivesMessage()
        {
            return TestHelper.TryTest(async () =>
            {
                // arrange
                var cts = new CancellationTokenSource(30000);
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
                IEventMessage incomingOne =
                    await consumerOne.ReadAsync(cts.Token);
                IEventMessage incomingTwo =
                    await consumerTwo.ReadAsync(cts.Token);
                Assert.Equal(outgoing.Payload, incomingOne.Payload);
                Assert.Equal(outgoing.Payload, incomingTwo.Payload);
            });
        }

        [Fact]
        public Task SubscribeTwoConsumer_SendTwoMessage_BothConsumerReceivesIndependentMessage()
        {
            return TestHelper.TryTest(async () =>
            {
                // arrange
                var cts = new CancellationTokenSource(30000);
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
                IEventMessage incomingOne =
                    await consumerOne.ReadAsync(cts.Token);
                IEventMessage incomingTwo =
                    await consumerTwo.ReadAsync(cts.Token);
                Assert.Equal(outgoingOne.Payload, incomingOne.Payload);
                Assert.Equal(outgoingTwo.Payload, incomingTwo.Payload);
                Assert.NotEqual(incomingOne.Event, incomingTwo.Event);
            });
        }
    }
}
