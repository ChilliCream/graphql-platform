using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;
using Xunit;

namespace MarshmallowPie.Processing.InMemory
{
    public class SessionMessageQueueTests
    {
        [Fact]
        public async Task Send_And_Receive_Message()
        {
            // arrange
            var sessionManager = new SessionManager();
            var test = new SessionMessageQueue<PublishDocumentEvent>(sessionManager);

            string sessionId = await sessionManager.CreateSessionAsync();

            var message = new PublishDocumentEvent(
                sessionId,
                new Issue("def", "foo", new Location(0, 0, 0, 0),
                IssueType.Error));

            // act
            await test.SendAsync(message);

            // assert
            await foreach (PublishDocumentEvent m in await test.SubscribeAsync(message.SessionId))
            {
                Assert.Equal(message, m);
                break;
            }
        }

        [Fact]
        public async Task Unsubscribe_On_Complete_Message()
        {
            // arrange
            var sessionManager = new SessionManager();
            var test = new SessionMessageQueue<PublishDocumentEvent>(sessionManager);

            string sessionId = await sessionManager.CreateSessionAsync();

            var message = new PublishDocumentEvent(
                sessionId,
                new Issue("def", "foo", new Location(0, 0, 0, 0),
                IssueType.Error));
            var completeMessage = PublishDocumentEvent.Completed(sessionId);
            var received = new List<PublishDocumentEvent>();

            await test.SendAsync(message);

            Task sendCompleteMessage = Task.Run(async () =>
             {
                 await Task.Delay(250);
                 await test.SendAsync(completeMessage);
             });

            // act
            await foreach (PublishDocumentEvent m in await test.SubscribeAsync(message.SessionId))
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
