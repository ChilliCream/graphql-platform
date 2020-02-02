using System.Threading.Tasks;
using Xunit;

namespace MarshmallowPie.Processing.InMemory
{
    public class MessageQueueTests
    {
        [Fact]
        public async Task Send_And_Receive_Message()
        {
            // arrange
            var test = new MessageQueue<PublishSchemaEvent>();
            var message = new PublishSchemaEvent("abc", new Issue("def", IssueType.Error));

            // act
            await test.SendAsync(message);

            // assert
            await foreach (PublishSchemaEvent m in await test.SubscribeAsync())
            {
                Assert.Equal(message, m);
                break;
            }
        }
    }
}
