using System.Threading.Tasks;
using StackExchange.Redis;
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
                CreateConnectionMultiplexer(),
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

        private static IConnectionMultiplexer CreateConnectionMultiplexer()
        {
            var configurationOptions = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                ConnectRetry = 3,
                EndPoints = { "localhost:6379" }
            };

            return ConnectionMultiplexer.Connect(
                configurationOptions);
        }
    }
}
