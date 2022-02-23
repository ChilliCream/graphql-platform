namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal static class Messages
{
    public const string ConnectionInitialize = "connection_init";
    public const string ConnectionAccept = "connection_ack";
    public const string ConnectionError = "connection_error";
    public const string ConnectionTerminate = "connection_terminate";
    public const string Start = "start";
    public const string Data = "data";
    public const string Complete = "complete";
    public const string Stop = "stop";
    public const string KeepAlive = "ka";
}
