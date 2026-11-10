using System.Diagnostics;
using Mocha.Features;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Features;

/// <summary>
/// Pooled feature that tracks one publish through the RabbitMQ dispatch endpoint: the channel it rents,
/// what it is waiting for, and how often it was retried.
/// </summary>
internal sealed class RabbitMQPublishFeature : IPooledFeature
{
    /// <summary>
    /// Gets or sets the channel rented by the current attempt, or <c>null</c> when the attempt has not rented one.
    /// </summary>
    public IChannel? Channel { get; set; }

    /// <summary>
    /// Gets or sets whether the rented channel can go back to the pool when the attempt fails.
    /// </summary>
    public bool IsChannelReusable { get; set; }

    /// <summary>
    /// Gets or sets what the publish is waiting for.
    /// </summary>
    public PublishPhase Phase { get; set; }

    /// <summary>
    /// Gets or sets the number of attempts that failed because the connection was lost.
    /// </summary>
    public int Retries { get; set; }

    /// <summary>
    /// Gets the <see cref="Stopwatch"/> timestamp at which the publish started.
    /// </summary>
    public long Started { get; private set; }

    /// <summary>
    /// Starts tracking a new publish.
    /// </summary>
    public void Start()
    {
        Reset();
        Started = Stopwatch.GetTimestamp();
    }

    /// <inheritdoc />
    public void Initialize(object state) => Reset();

    /// <inheritdoc />
    public void Reset()
    {
        Channel = null;
        IsChannelReusable = false;
        Phase = PublishPhase.ReplyConsumer;
        Retries = 0;
        Started = 0;
    }
}
