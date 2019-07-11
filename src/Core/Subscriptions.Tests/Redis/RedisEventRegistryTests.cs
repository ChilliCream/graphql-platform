using System.Threading.Tasks;
using Subscriptions.Redis;
using Xunit;

namespace HotChocolate.Subscriptions.Redis
{
    public class RedisEventRegistryTests
    {
        [Fact(Skip = "Add underlining service")]
        public async Task Subscribe_Send_MessageReceived()
        {
            // arrange
            var eventDescription = new EventDescription("foo");
            var eventRegistry = new RedisEventRegistry(
                RedisConnection.Create("localhost:6379"),
                new JsonPayloadSerializer());

            // act
            IEventStream stream =
                await eventRegistry.SubscribeAsync(eventDescription);

            // assert
            var incoming = new EventMessage("foo", "bar");
            await eventRegistry.SendAsync(incoming);
            IEventMessage outgoing = await stream.ReadAsync();
            Assert.Equal(incoming.Payload, outgoing.Payload);
        }
    }
}
