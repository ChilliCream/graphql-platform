using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Subscriptions.InMemory
{
    public class InMemoryEventRegistryTests
    {
        [Fact]
        public async Task Subscribe_Send_MessageReveived()
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
    }
}
