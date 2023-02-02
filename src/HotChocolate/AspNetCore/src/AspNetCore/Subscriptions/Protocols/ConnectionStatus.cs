using static HotChocolate.AspNetCore.Properties.AspNetCoreResources;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols;

/// <summary>
/// The socket connection initialization status.
/// </summary>
public sealed class ConnectionStatus
{
    private static readonly ConnectionStatus _defaultAccepted =
        new(true, "You connection was accepted.", null);
    private static readonly ConnectionStatus _defaultRejected =
        new(false, "Your connection was rejected.", null);

    private ConnectionStatus(
        bool accepted,
        string message,
        IReadOnlyDictionary<string, object?>? extensions)
    {
        Accepted = accepted;
        Message = message;
        Extensions = extensions;
    }

    /// <summary>
    /// Specifies if the connection is accepted.
    /// </summary>
    public bool Accepted { get; }

    /// <summary>
    /// The connection status message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Custom properties.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Extensions { get; }

    /// <summary>
    /// Accepts the socket connection.
    /// </summary>
    /// <returns>
    /// The connection accept status.
    /// </returns>
    public static ConnectionStatus Accept()
        => _defaultAccepted;

    /// <summary>
    /// Reject the socket connection with a custom message.
    /// </summary>
    /// <param name="message">
    /// The message.
    /// </param>
    /// <param name="extensions">
    /// The custom properties that shall be passed on with the rejection.
    /// </param>
    /// <returns>
    /// The connection reject status.
    /// </returns>
    public static ConnectionStatus Reject(
        string message,
        IReadOnlyDictionary<string, object?>? extensions)
    {
        if (string.IsNullOrEmpty(message))
        {
            throw new ArgumentException(
                ConnectionStatus_Reject_MessageCannotBeNullOrEmpty,
                nameof(message));
        }

        return new ConnectionStatus(false, message, extensions);
    }

    /// <summary>
    /// Reject the socket connection with a custom message.
    /// </summary>
    /// <param name="message">
    /// The message.
    /// </param>
    /// <returns>
    /// The connection reject status.
    /// </returns>
    public static ConnectionStatus Reject(string message)
        => Reject(message, null);

    /// <summary>
    /// Reject the socket connection with a custom message.
    /// </summary>
    /// <returns>
    /// The connection reject status.
    /// </returns>
    public static ConnectionStatus Reject()
        => _defaultRejected;

    /// <summary>
    /// Reject the socket connection with a custom message.
    /// </summary>
    /// <param name="extensions">
    /// The custom properties that shall be passed on with the rejection.
    /// </param>
    /// <returns>
    /// The connection reject status.
    /// </returns>
    public static ConnectionStatus Reject(IReadOnlyDictionary<string, object?> extensions)
        => Reject("Your connection was rejected.", extensions);
}
