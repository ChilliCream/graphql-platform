namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Thrown when the broker did not answer a request to open a channel because it blocked the connection.
/// The client keeps the channel number of the request until the connection is replaced.
/// </summary>
internal sealed class ChannelOpenBlockedException(string message, Exception innerException)
    : TimeoutException(message, innerException);
