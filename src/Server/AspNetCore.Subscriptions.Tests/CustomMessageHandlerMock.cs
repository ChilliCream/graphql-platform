using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.Server;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class CustomMessageHandlerMock
        : MessageHandler<InitializeConnectionMessage>
    {
        public bool HasBeenCalled { get; set; } = false;

        protected override Task HandleAsync(
            ISocketConnection connection,
            InitializeConnectionMessage message,
            CancellationToken cancellationToken)
        {
            HasBeenCalled = true;
            return Task.CompletedTask;
        }
    }
}
