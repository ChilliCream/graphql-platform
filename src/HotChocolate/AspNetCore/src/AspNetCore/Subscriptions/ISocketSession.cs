using HotChocolate.AspNetCore.Subscriptions.Protocols;

namespace HotChocolate.AspNetCore.Subscriptions;

public interface ISocketSession : IDisposable
{
    ISocketConnection Connection { get; }

    IProtocolHandler Protocol { get; }

    /// <summary>
    /// Gets access to the subscription manager of this connection.
    /// </summary>
    IOperationManager Operations { get; }
}
