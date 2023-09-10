namespace HotChocolate.AspNetCore;

/// <summary>
/// Options relevant to GraphQL over Websocket.
/// </summary>
public sealed class GraphQLSocketOptions
{
    /// <summary>
    /// Defines the time in which the client must send a connection initialization
    /// message before the server closes the connection.
    ///
    /// Default: <c>TimeSpan.FromSeconds(10)</c>
    /// </summary>
    public TimeSpan ConnectionInitializationTimeout { get; set; } =
        TimeSpan.FromSeconds(10);

    /// <summary>
    /// Defines an interval in which the server will send keep alive messages to the client
    /// in order to keep the connection open.
    ///
    /// If the interval is set to null the server will send no keep alive messages.
    ///
    /// Default: <c>TimeSpan.FromSeconds(5)</c>
    /// </summary>
    public TimeSpan? KeepAliveInterval { get; set; } =
        TimeSpan.FromSeconds(5);
}
