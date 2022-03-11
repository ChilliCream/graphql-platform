namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Messages;

internal static class Messages
{
    public const string ConnectionInitialize = "connection_init";
    public const string ConnectionAccept = "connection_ack";
    public const string Ping = "ping";
    public const string Pong = "pong";
    public const string Subscribe = "subscribe";
    public const string Next = "next";
    public const string Error = "error";
    public const string Complete = "complete";
}
