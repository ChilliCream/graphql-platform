using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Subscriptions.InMemory
{
    [Obsolete]
    public class InMemoryEventRegistryTests
    {
        [Fact]
        public async Task Subscribe_Send_MessageReceived()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);
            var eventDescription = new EventDescription("foo");
            var eventRegistry = new InMemoryEventRegistry();

            // act
            IEventStream stream = await eventRegistry.SubscribeAsync(eventDescription);
            IAsyncEnumerator<IEventMessage> enumerator = stream.GetAsyncEnumerator(cts.Token);

            // assert
            var incoming = new EventMessage("foo");
            await eventRegistry.SendAsync(incoming);
            Assert.True(await enumerator.MoveNextAsync());
            Assert.Equal(incoming, enumerator.Current);
        }

        [Fact]
        public async Task Subscribe_ObjectValueArgument_Send_MessageReceived()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);
            var eventRegistry = new InMemoryEventRegistry();

            var a = new EventDescription("event",
                new ArgumentNode("foo", new ObjectValueNode(
                new ObjectFieldNode("a", 123),
                new ObjectFieldNode("b", true),
                new ObjectFieldNode("c", "abc"))));

            var b = new EventDescription("event",
                new ArgumentNode("foo", new ObjectValueNode(
                new ObjectFieldNode("b", true),
                new ObjectFieldNode("a", 123),
                new ObjectFieldNode("c", "abc"))));

            // act
            IEventStream stream = await eventRegistry.SubscribeAsync(a);
            IAsyncEnumerator<IEventMessage> enumerator = stream.GetAsyncEnumerator(cts.Token);

            // assert
            var incoming = new EventMessage(b, "foo");
            await eventRegistry.SendAsync(incoming);
            Assert.True(await enumerator.MoveNextAsync());
            Assert.Equal(incoming, enumerator.Current);
        }
    }
}
