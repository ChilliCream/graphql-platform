using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.Server;

namespace HotChocolate.AspNetCore.Subscriptions.Interceptors
{
    public interface IConnectMessageInterceptor
    {
        Task<ConnectionStatus> OnReceiveAsync(
            ISocketConnection connection,
            InitializeConnectionMessage message,
            CancellationToken cancellationToken);
    }

}
