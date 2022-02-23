using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

namespace HotChocolate.AspNetCore.Subscriptions;

internal static class SocketConnectionExtensions
{
    public static Task SendAsync(
        this ISocketConnection connection,
        OperationMessage message,
        CancellationToken cancellationToken) =>
        connection.SendAsync(message.Serialize(), cancellationToken);
}
