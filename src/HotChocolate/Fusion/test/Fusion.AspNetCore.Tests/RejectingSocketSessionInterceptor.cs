using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;

namespace HotChocolate.Fusion;

/// <summary>
/// Refuses a connection without identity as unauthorized and an identity without the read scope
/// as rejected.
/// </summary>
public sealed class RejectingSocketSessionInterceptor : DefaultSocketSessionInterceptor
{
    public override ValueTask<ConnectionStatus> OnConnectAsync(
        ISocketSession session,
        IOperationMessagePayload connectionInitMessage,
        CancellationToken cancellationToken = default)
    {
        var user = session.Connection.HttpContext.User;

        if (user.Identity?.IsAuthenticated is not true)
        {
            return new(ConnectionStatus.Unauthorized("Unauthenticated"));
        }

        return user.HasClaim("scope", "read")
            ? new(ConnectionStatus.Accept())
            : new(ConnectionStatus.Reject("Forbidden"));
    }
}
