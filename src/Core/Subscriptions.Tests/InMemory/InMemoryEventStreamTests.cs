using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Subscriptions.InMemory
{
    public class InMemoryEventStreamTests
    {
        [Fact]
        public async Task Trigger_Message_IsDelivered()
        {
            // arrange
            var sent = new EventMessage("foo", "bar");
            var eventStream = new InMemoryEventStream();

            // act
            eventStream.Trigger(sent);

            // assert
            IEventMessage received = await eventStream.ReadAsync();
            Assert.Equal(sent, received);
        }

        [Fact]
        public async Task Complete_CompletedEventIsRaised()
        {
            // arrange
            var sent = new EventMessage("foo", "bar");
            var eventStream = new InMemoryEventStream();
            bool eventRaised = false;
            eventStream.Completed += (s, e) => eventRaised = true;

            // act
            await eventStream.CompleteAsync();

            // assert
            Assert.True(eventRaised);
            Assert.True(eventStream.IsCompleted);
        }

        [Fact]
        public void Dispose_CompletedEventIsRaised()
        {
            // arrange
            var sent = new EventMessage("foo", "bar");
            var eventStream = new InMemoryEventStream();
            bool eventRaised = false;
            eventStream.Completed += (s, e) => eventRaised = true;

            // act
            eventStream.Dispose();

            // assert
            Assert.True(eventRaised);
            Assert.True(eventStream.IsCompleted);
        }
    }
}
