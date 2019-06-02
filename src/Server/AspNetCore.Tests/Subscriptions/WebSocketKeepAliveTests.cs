using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Execution;
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
            (WebSocketContext context, WebSocketMock socket) =
                WebSocketContextHelper.Create();
            var keepAlive = new WebSocketKeepAlive(context, timeout, new CancellationTokenSource());

            // act
            keepAlive.Start();
            await Task.Delay(timeout.Add(TimeSpan.FromMilliseconds(100)));

            // assert
            Assert.Collection(socket.Outgoing,
                t =>
                {
                    Assert.Equal(MessageTypes.Connection.KeepAlive, t.Type);
                });
        }
    }
}
