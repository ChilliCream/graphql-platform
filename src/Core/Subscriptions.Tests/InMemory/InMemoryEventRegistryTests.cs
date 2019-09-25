using System.Threading.Tasks;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Subscriptions.InMemory
{
    public class InMemoryEventRegistryTests
    {
        [Fact]
        public async Task Subscribe_Send_MessageReceived()
        {
            // arrange
            var eventDescription = new EventDescription("foo");
            var eventRegistry = new InMemoryEventRegistry();

            // act
            IEventStream stream =
                await eventRegistry.SubscribeAsync(eventDescription);

            // assert
            var incoming = new EventMessage("foo");
            await eventRegistry.SendAsync(incoming);
            IEventMessage outgoing = await stream.ReadAsync();
            Assert.Equal(incoming, outgoing);
        }

        [Fact]
        public async Task Subscribe_ObjectValueArgument_Send_MessageReceived()
        {
            // arrange
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
            IEventStream stream =
                await eventRegistry.SubscribeAsync(a);

            // assert
            var incoming = new EventMessage(b, "foo");
            await eventRegistry.SendAsync(incoming);
            IEventMessage outgoing = await stream.ReadAsync();
            Assert.Equal(incoming, outgoing);
        }
    }
}
