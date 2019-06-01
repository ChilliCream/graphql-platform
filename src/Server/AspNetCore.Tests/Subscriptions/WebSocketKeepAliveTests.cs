using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class WebSocketKeepAliveTests
    {
        [Fact]
        public async Task AfterTimeout_ReceiveKeepAlive()
        {
            // arrange
            var timeout = TimeSpan.FromMilliseconds(100);
            var context = new InMemoryWebSocketContext();
            var keepAlive = new WebSocketKeepAlive(context, timeout, new CancellationTokenSource());

            // act
            keepAlive.Start();
            await Task.Delay(timeout.Add(TimeSpan.FromMilliseconds(100)));

            // assert
            Assert.Collection(context.Outgoing,
                t =>
                {
                    Assert.Equal(MessageTypes.Connection.KeepAlive, t.Type);
                });
        }
    }
}