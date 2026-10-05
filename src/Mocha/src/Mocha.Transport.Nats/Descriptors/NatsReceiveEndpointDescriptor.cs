using Mocha.Features;
using Mocha.Middlewares;
using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats;

/// <summary>
/// Default implementation of <see cref="INatsReceiveEndpointDescriptor"/>.
/// </summary>
internal sealed class NatsReceiveEndpointDescriptor
    : ReceiveEndpointDescriptor<NatsReceiveEndpointConfiguration>
    , INatsReceiveEndpointDescriptor
{
    private NatsReceiveEndpointDescriptor(IMessagingConfigurationContext context, string name)
        : base(context)
    {
        Configuration = new NatsReceiveEndpointConfiguration
        {
            Name = name,
            ConsumerName = NatsNaming.ToDurableName(name)
        };
    }

    /// <inheritdoc />
    protected internal override NatsReceiveEndpointConfiguration Configuration { get; protected set; }

    public static NatsReceiveEndpointDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);

    public NatsReceiveEndpointConfiguration CreateConfiguration() => Configuration;

    public new INatsReceiveEndpointDescriptor Handler<THandler>() where THandler : class, IHandler
    {
        base.Handler<THandler>();
        return this;
    }

    public new INatsReceiveEndpointDescriptor Handler(Type handlerType)
    {
        base.Handler(handlerType);
        return this;
    }

    public new INatsReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        base.Consumer<TConsumer>();
        return this;
    }

    public new INatsReceiveEndpointDescriptor Consumer(Type consumerType)
    {
        base.Consumer(consumerType);
        return this;
    }

    public new INatsReceiveEndpointDescriptor Receives<TMessage>()
    {
        base.Receives<TMessage>();
        return this;
    }

    public new INatsReceiveEndpointDescriptor Receives(Type messageType)
    {
        base.Receives(messageType);
        return this;
    }

    public new INatsReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency)
    {
        base.MaxConcurrency(maxConcurrency);
        return this;
    }

    public new INatsReceiveEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseReceive(configuration, before, after);
        return this;
    }

    public INatsReceiveEndpointDescriptor FromStream(string streamName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamName);

        Configuration.StreamName = streamName;
        return this;
    }

    public INatsReceiveEndpointDescriptor ConsumerName(string consumerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerName);

        Configuration.ConsumerName = NatsNaming.ToDurableName(consumerName);
        return this;
    }

    public INatsReceiveEndpointDescriptor AckWait(TimeSpan ackWait)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ackWait, TimeSpan.Zero);

        Configuration.AckWait = ackWait;
        return this;
    }

    public INatsReceiveEndpointDescriptor MaxDeliver(long maxDeliver)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDeliver, 1);

        Configuration.MaxDeliver = maxDeliver;
        return this;
    }

    public INatsReceiveEndpointDescriptor Backoff(params TimeSpan[] backoff)
    {
        ArgumentNullException.ThrowIfNull(backoff);

        if (backoff.Length == 0)
        {
            throw new ArgumentException("At least one delay is required.", nameof(backoff));
        }

        Configuration.Backoff = [.. backoff];
        return this;
    }

    public INatsReceiveEndpointDescriptor MaxAckPending(long maxAckPending)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAckPending, 1);

        Configuration.MaxAckPending = maxAckPending;
        return this;
    }

    public INatsReceiveEndpointDescriptor AckProgressEvery(TimeSpan interval)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);

        Configuration.AckProgressInterval = interval;
        return this;
    }

    public INatsReceiveEndpointDescriptor DeliverFrom(ConsumerConfigDeliverPolicy deliverPolicy)
    {
        Configuration.DeliverPolicy = deliverPolicy;
        return this;
    }

    public INatsReceiveEndpointDescriptor Subject(string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        if (!Configuration.FilterSubjects.Contains(subject))
        {
            Configuration.FilterSubjects.Add(subject);
        }

        return this;
    }

    public INatsReceiveEndpointDescriptor FaultEndpoint(Uri address)
    {
        var feature = Configuration.Features.GetOrSet<ReceiveFaultEndpointFeature>();

        feature.Address = Validate(address);
        feature.IsDisabled = false;

        return this;
    }

    public INatsReceiveEndpointDescriptor DisableFaultEndpoint()
    {
        var feature = Configuration.Features.GetOrSet<ReceiveFaultEndpointFeature>();

        feature.IsDisabled = true;
        feature.Address = null;

        return this;
    }

    public INatsReceiveEndpointDescriptor SkippedEndpoint(Uri address)
    {
        var feature = Configuration.Features.GetOrSet<ReceiveSkippedEndpointFeature>();

        feature.Address = Validate(address);
        feature.IsDisabled = false;

        return this;
    }

    public INatsReceiveEndpointDescriptor DisableSkippedEndpoint()
    {
        var feature = Configuration.Features.GetOrSet<ReceiveSkippedEndpointFeature>();

        feature.IsDisabled = true;
        feature.Address = null;

        return this;
    }

    private static Uri Validate(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (!address.IsAbsoluteUri)
        {
            throw new ArgumentException(
                "The endpoint address must be an absolute URI.",
                nameof(address));
        }

        // Rejected here rather than at start-up: an address the transport cannot turn into a subject
        // would otherwise be dropped silently and faulted messages would go nowhere.
        if (!NatsDestinations.TryResolveExplicit(NatsTransportConfiguration.DefaultSchema, address, out _))
        {
            throw new ArgumentException(
                $"'{address}' does not resolve to a NATS subject. Use 'nats:s/<subject>' or "
                + "'subject:<subject>'.",
                nameof(address));
        }

        return address;
    }
}
