using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using Squadron;
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
                IEventMessage incoming = null;
                await foreach (IEventMessage item in consumer.WithCancellation(cts.Token))
                {
                    incoming = item;
                    break;
                }
                Assert.Equal(outgoing.Payload, incoming.Payload);
            });
        }

        [Fact]
        public Task SubscribeOneConsumer_Complete_StreamIsCompleted()
        {
            return TestHelper.TryTest(async () =>
            {
                // arrange
                var cts = new CancellationTokenSource(30000);
                var eventDescription = new EventDescription(
                    Guid.NewGuid().ToString());

                // act
                IEventStream consumer = await _registry.SubscribeAsync(eventDescription);
                IAsyncEnumerator<IEventMessage> enumerator = consumer.GetAsyncEnumerator(cts.Token);
                await consumer.CompleteAsync();

                // assert
                Assert.False(await enumerator.MoveNextAsync());
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
                IEventStream consumerOne =
                    await _registry.SubscribeAsync(eventDescription);
                IAsyncEnumerator<IEventMessage> enumeratorOne =
                    consumerOne.GetAsyncEnumerator(cts.Token);
                IEventStream consumerTwo =
                    await _registry.SubscribeAsync(eventDescription);
                IAsyncEnumerator<IEventMessage> enumeratorTwo =
                    consumerTwo.GetAsyncEnumerator(cts.Token);
                var outgoing = new EventMessage(eventDescription, "bar");
                await _sender.SendAsync(outgoing);

                // assert
                Assert.True(await enumeratorOne.MoveNextAsync());
                Assert.True(await enumeratorTwo.MoveNextAsync());
                Assert.Equal(outgoing.Payload, enumeratorOne.Current.Payload);
                Assert.Equal(outgoing.Payload, enumeratorTwo.Current.Payload);
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
                IEventStream consumerOne =
                   await _registry.SubscribeAsync(eventDescriptionOne);
                IAsyncEnumerator<IEventMessage> enumeratorOne =
                    consumerOne.GetAsyncEnumerator(cts.Token);
                var outgoingOne = new EventMessage(eventDescriptionOne, "foo");
                await _sender.SendAsync(outgoingOne);

                IEventStream consumerTwo =
                    await _registry.SubscribeAsync(eventDescriptionTwo);
                IAsyncEnumerator<IEventMessage> enumeratorTwo =
                    consumerTwo.GetAsyncEnumerator(cts.Token);
                var outgoingTwo = new EventMessage(eventDescriptionTwo, "bar");
                await _sender.SendAsync(outgoingTwo);

                // assert
                Assert.True(await enumeratorOne.MoveNextAsync());
                Assert.True(await enumeratorTwo.MoveNextAsync());
                Assert.Equal(outgoingOne.Payload, enumeratorOne.Current.Payload);
                Assert.Equal(outgoingTwo.Payload, enumeratorTwo.Current.Payload);
                Assert.NotEqual(enumeratorOne.Current.Event, enumeratorTwo.Current.Event);
            });
        }
    }
}
