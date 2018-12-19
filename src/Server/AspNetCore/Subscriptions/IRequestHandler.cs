#if !ASPNETCLASSIC

using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal interface IRequestHandler
    {
        Task HandleAsync(
            IWebSocketContext context,
            GenericOperationMessage message,
            CancellationToken cancellationToken);

        bool CanHandle(GenericOperationMessage message);
    }
}

#endif
