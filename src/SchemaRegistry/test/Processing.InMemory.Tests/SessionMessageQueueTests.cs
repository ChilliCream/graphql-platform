using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace MarshmallowPie.Processing.InMemory
{
    public class SessionMessageQueueTests
    {
        [Fact]
        public async Task Send_And_Receive_Message()
        {
            // arrange
            var test = new SessionMessageQueue<PublishSchemaEvent>();
            var message = new PublishSchemaEvent("abc", new Issue("def", IssueType.Error));

            // act
            await test.SendAsync(message);

            // assert
            await foreach (PublishSchemaEvent m in await test.SubscribeAsync(message.SessionId))
            {
                Assert.Equal(message, m);
                break;
            }
        }

        [Fact]
        public async Task Unsubscribe_On_Complete_Message()
        {
            // arrange
            var test = new SessionMessageQueue<PublishSchemaEvent>();
            var message = new PublishSchemaEvent("abc", new Issue("def", IssueType.Error));
            var completeMessage = PublishSchemaEvent.Completed("abc");
            var received = new List<PublishSchemaEvent>();

            await test.SendAsync(message);

            Task sendCompleteMessage = Task.Run(async () =>
             {
                 await Task.Delay(250);
                 await test.SendAsync(completeMessage);
             });

            // act
            await foreach (PublishSchemaEvent m in await test.SubscribeAsync(message.SessionId))
            {
                received.Add(m);

                if (received.Count == 1)
                {
                    await sendCompleteMessage;
                }
            }

            // assert
            Assert.Collection(received,
                t => Assert.Equal(message, t),
                t => Assert.Equal(completeMessage, t));
        }
    }
}
