using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// A publisher channel that is leased to one publish. Disposing the lease gives the channel back.
/// </summary>
/// <param name="dispatcher">The dispatcher that the channel goes back to.</param>
/// <param name="connection">The connection the channel belongs to.</param>
/// <param name="channel">The leased channel, which has publisher confirms enabled.</param>
internal sealed class ChannelLease(RabbitMQDispatcher dispatcher, IConnection connection, IChannel channel)
    : IDisposable
{
    /// <summary>
    /// Gets the connection the channel belongs to.
    /// </summary>
    public IConnection Connection => connection;

    /// <summary>
    /// Gets the leased channel.
    /// </summary>
    public IChannel Channel => channel;

    /// <summary>
    /// Gets or sets whether the channel can be leased again, which requires that the broker answered the
    /// last publish on it. Otherwise the channel is closed when the lease is disposed.
    /// </summary>
    public bool IsReusable { get; set; }

    /// <summary>
    /// Gives the channel back to the dispatcher.
    /// </summary>
    public void Dispose() => dispatcher.Return(this);
}
