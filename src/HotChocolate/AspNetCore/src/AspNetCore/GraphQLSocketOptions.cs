namespace HotChocolate.AspNetCore;

/// <summary>
/// Options relevant to GraphQL over Websocket.
/// </summary>
public sealed class GraphQLSocketOptions
{
    private const int DefaultMaxAllowedMessageSize = 20 * 1000 * 1024;
    private int _maxAllowedMessageSize = DefaultMaxAllowedMessageSize;

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

    /// <summary>
    /// <para>
    /// Defines the maximum size in bytes of a single incoming WebSocket message.
    /// If a message exceeds this size the server closes the connection with
    /// the close status <c>1009 MessageTooBig</c>.
    /// </para>
    /// <para>Default: <c>20 * 1000 * 1024</c> (20,480,000 bytes)</para>
    /// </summary>
    public int MaxAllowedMessageSize
    {
        get => _maxAllowedMessageSize;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            _maxAllowedMessageSize = value;
        }
    }
}
