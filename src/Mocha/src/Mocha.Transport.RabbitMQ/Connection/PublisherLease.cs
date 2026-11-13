using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// A publisher channel that is leased to one publish.
/// </summary>
/// <param name="Pool">The pool the channel belongs to.</param>
/// <param name="Channel">The leased channel, which has publisher confirms enabled.</param>
internal readonly record struct PublisherLease(PublisherChannelPool Pool, IChannel Channel)
{
    /// <summary>
    /// Gets the connection the channel belongs to.
    /// </summary>
    public IConnection Connection => Pool.Connection;
}
