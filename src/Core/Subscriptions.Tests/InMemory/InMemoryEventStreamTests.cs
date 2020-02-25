using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Subscriptions.InMemory
{
    [Obsolete]
    public class InMemoryEventStreamTests
    {
        [Fact]
        public async Task Trigger_Message_IsDelivered()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);
            var sent = new EventMessage("foo", "bar");
            var eventStream = new InMemoryEventStream();
            IAsyncEnumerator<IEventMessage> enumerator = eventStream.GetAsyncEnumerator(cts.Token);

            // act
            await eventStream.TriggerAsync(sent, cts.Token);

            // assert
            Assert.True(await enumerator.MoveNextAsync());
            Assert.Equal(sent, enumerator.Current);
        }

        [Fact]
        public async Task Complete_CompletedEventIsRaised()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);
            var sent = new EventMessage("foo", "bar");
            var eventStream = new InMemoryEventStream();
            IAsyncEnumerator<IEventMessage> enumerator = eventStream.GetAsyncEnumerator(cts.Token);
            bool eventRaised = false;
            eventStream.Completed += (s, e) => eventRaised = true;

            // act
            await eventStream.CompleteAsync(cts.Token);

            // assert
            Assert.True(eventRaised);
            Assert.False(await enumerator.MoveNextAsync());
        }

        [Fact]
        public async Task Dispose_CompletedEventIsRaised()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);
            var sent = new EventMessage("foo", "bar");
            var eventStream = new InMemoryEventStream();
            IAsyncEnumerator<IEventMessage> enumerator = eventStream.GetAsyncEnumerator(cts.Token);
            bool eventRaised = false;
            eventStream.Completed += (s, e) => eventRaised = true;

            // act
            await enumerator.DisposeAsync();

            // assert
            Assert.True(eventRaised);
            Assert.False(await enumerator.MoveNextAsync());
        }
    }
}
