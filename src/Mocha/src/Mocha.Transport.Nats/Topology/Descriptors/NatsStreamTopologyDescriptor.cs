using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats;

/// <summary>
/// Default implementation of <see cref="INatsStreamTopologyDescriptor"/>.
/// </summary>
public sealed class NatsStreamTopologyDescriptor
    : MessagingDescriptorBase<NatsStreamConfiguration>
    , INatsStreamTopologyDescriptor
{
    private NatsStreamTopologyDescriptor(IMessagingConfigurationContext context, string name)
        : base(context)
    {
        Configuration = new NatsStreamConfiguration
        {
            Name = name,
            Subjects = [],

            // Declared, not convention. The transport treats a convention stream as shared and
            // yields it when something else already owns its subjects, which must never happen
            // silently to a stream the caller asked for by name.
            Origin = TopologyOrigin.Declared
        };
    }

    /// <inheritdoc />
    protected internal override NatsStreamConfiguration Configuration { get; protected set; }

    /// <summary>
    /// Creates a descriptor for a stream with the specified name.
    /// </summary>
    /// <param name="context">The configuration context.</param>
    /// <param name="name">The stream name.</param>
    /// <returns>The new descriptor.</returns>
    public static NatsStreamTopologyDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);

    /// <summary>
    /// Gets the configuration this descriptor has built.
    /// </summary>
    /// <returns>The stream configuration.</returns>
    public NatsStreamConfiguration CreateConfiguration() => Configuration;

    /// <inheritdoc />
    public INatsStreamTopologyDescriptor Subject(string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        Configuration.Subjects ??= [];

        if (!Configuration.Subjects.Contains(subject))
        {
            Configuration.Subjects.Add(subject);
        }

        return this;
    }

    /// <inheritdoc />
    public INatsStreamTopologyDescriptor Retention(StreamConfigRetention retention)
    {
        Configuration.Retention = retention;
        return this;
    }

    /// <inheritdoc />
    public INatsStreamTopologyDescriptor Storage(StreamConfigStorage storage)
    {
        Configuration.Storage = storage;
        return this;
    }

    /// <inheritdoc />
    public INatsStreamTopologyDescriptor MaxAge(TimeSpan maxAge)
    {
        Configuration.MaxAge = maxAge;
        return this;
    }

    /// <inheritdoc />
    public INatsStreamTopologyDescriptor Replicas(int replicas)
    {
        Configuration.NumReplicas = replicas;
        return this;
    }

    /// <inheritdoc />
    public INatsStreamTopologyDescriptor MaxMessages(long maxMsgs)
    {
        Configuration.MaxMsgs = maxMsgs;
        return this;
    }

    /// <inheritdoc />
    public INatsStreamTopologyDescriptor MaxBytes(long maxBytes)
    {
        Configuration.MaxBytes = maxBytes;
        return this;
    }

    /// <inheritdoc />
    public INatsStreamTopologyDescriptor DeduplicateWithin(TimeSpan window)
    {
        Configuration.DuplicateWindow = window;
        return this;
    }

    /// <inheritdoc />
    public INatsStreamTopologyDescriptor AllowMessageTtl(bool allow = true)
    {
        Configuration.AllowMsgTtl = allow;
        return this;
    }

    /// <inheritdoc />
    public INatsStreamTopologyDescriptor AllowMessageSchedules(bool allow = true)
    {
        Configuration.AllowMsgSchedules = allow;
        return this;
    }

    /// <inheritdoc />
    public INatsStreamTopologyDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }
}
