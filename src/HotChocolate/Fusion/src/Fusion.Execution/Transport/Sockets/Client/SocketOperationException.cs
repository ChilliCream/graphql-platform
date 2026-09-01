namespace HotChocolate.Fusion.Transport.Sockets.Client;

/// <summary>
/// Represents a failure isolated to one operation on a shared WebSocket connection.
/// </summary>
public sealed class SocketOperationException : Exception
{
    internal SocketOperationException(string message)
        : base(message)
    {
    }
}
