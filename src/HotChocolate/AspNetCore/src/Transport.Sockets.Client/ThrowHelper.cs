using HotChocolate.Transport.Sockets.Client.Properties;

#if FUSION
namespace HotChocolate.Fusion.Transport.Sockets.Client;
#else
namespace HotChocolate.Transport.Sockets.Client;
#endif

internal static class ThrowHelper
{
#if FUSION
    public static ArgumentOutOfRangeException MaxOperationQueueBytesOutOfRange(int value)
        => new(
            "value",
            value,
            "The maximum operation queue size must be greater than zero.");

    public static InvalidOperationException MessageHasNoPayload()
        => new("The WebSocket message has no payload.");

    public static InvalidOperationException SourceResultDocumentCapacityExceeded()
        => new("The source result document has exceeded its maximum data capacity.");

    public static ObjectDisposedException PayloadBufferDisposed()
        => new("PooledSocketPayload");

    public static InvalidOperationException PayloadNotParsed()
        => new("The WebSocket payload has not been parsed.");

    public static SocketOperationException OperationQueueCapacityExceeded(
        string operationId,
        int capacity)
        => new(
            $"The WebSocket operation `{operationId}` exceeded its queued payload limit "
            + $"of {capacity} bytes.");
#endif

    public static Exception MessageHasNoId() =>
        new InvalidOperationException(SocketClientResources.GraphQLOverWebsockets_MessageHasNoId);
}
