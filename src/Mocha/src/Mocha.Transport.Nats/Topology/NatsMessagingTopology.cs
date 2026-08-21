namespace Mocha.Transport.Nats;

/// <summary>
/// Manages the JetStream topology model (streams, consumers and subjects) for a transport instance,
/// providing thread-safe mutation and lookup of topology resources.
/// </summary>
/// <param name="transport">The owning NATS transport instance.</param>
/// <param name="baseAddress">The base address derived from the NATS server URL.</param>
/// <param name="defaults">The bus-level defaults applied to streams and consumers.</param>
/// <param name="autoProvision">Whether resources are provisioned during start-up by default.</param>
public sealed class NatsMessagingTopology(
    NatsMessagingTransport transport,
    Uri baseAddress,
    NatsBusDefaults defaults,
    bool autoProvision)
    : MessagingTopology<NatsMessagingTransport>(transport, baseAddress)
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly List<NatsStream> _streams = [];
    private readonly List<NatsConsumer> _consumers = [];
    private readonly List<NatsSubject> _subjects = [];

    /// <summary>
    /// Gets a value indicating whether resources are provisioned during start-up by default.
    /// Individual resources may override this through their own <c>AutoProvision</c> property.
    /// </summary>
    public bool AutoProvision => autoProvision;

    /// <summary>
    /// Gets the bus-level defaults applied to the streams and consumers this topology creates.
    /// </summary>
    public NatsBusDefaults Defaults => defaults;

    /// <summary>
    /// Gets the streams registered in this topology.
    /// </summary>
    public IReadOnlyList<NatsStream> Streams => _streams;

    /// <summary>
    /// Gets the durable consumers registered in this topology.
    /// </summary>
    public IReadOnlyList<NatsConsumer> Consumers => _consumers;

    /// <summary>
    /// Gets the subjects registered in this topology.
    /// </summary>
    public IReadOnlyList<NatsSubject> Subjects => _subjects;

    /// <summary>
    /// Adds a stream to the topology, folding the configuration into the existing stream when one
    /// with the same name is already present.
    /// </summary>
    /// <param name="configuration">The stream configuration.</param>
    /// <returns>The new or existing stream.</returns>
    public NatsStream AddStream(NatsStreamConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        lock (_lock)
        {
            var existing = _streams.FirstOrDefault(s => s.Name == configuration.Name);

            if (existing is not null)
            {
                existing.Merge(configuration);

                return existing;
            }

            var stream = new NatsStream();
            configuration.Topology = this;
            defaults.Stream.ApplyTo(configuration);
            stream.Topology = this;
            stream.Initialize(configuration);
            _streams.Add(stream);
            stream.Complete();

            return stream;
        }
    }

    /// <summary>
    /// Adds a durable consumer to the topology, folding the configuration into the existing consumer
    /// when one with the same name is already present.
    /// </summary>
    /// <param name="configuration">The consumer configuration.</param>
    /// <returns>The new or existing consumer.</returns>
    public NatsConsumer AddConsumer(NatsConsumerConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        lock (_lock)
        {
            var existing = _consumers.FirstOrDefault(c => c.Name == configuration.Name);

            if (existing is not null)
            {
                existing.Merge(configuration);

                return existing;
            }

            var consumer = new NatsConsumer();
            configuration.Topology = this;
            defaults.Consumer.ApplyTo(configuration);
            consumer.Topology = this;
            consumer.Initialize(configuration);
            _consumers.Add(consumer);
            consumer.Complete();

            return consumer;
        }
    }

    /// <summary>
    /// Adds a subject to the topology, or returns the existing subject with the same name.
    /// </summary>
    /// <param name="configuration">The subject configuration.</param>
    /// <returns>The new or existing subject.</returns>
    public NatsSubject AddSubject(NatsSubjectConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        lock (_lock)
        {
            var existing = _subjects.FirstOrDefault(s => s.Subject == configuration.Subject);

            if (existing is not null)
            {
                return existing;
            }

            var subject = new NatsSubject();
            configuration.Topology = this;
            subject.Topology = this;
            subject.Initialize(configuration);
            _subjects.Add(subject);
            subject.Complete();

            return subject;
        }
    }

    /// <summary>
    /// Removes a stream from the topology.
    /// </summary>
    /// <param name="stream">The stream to remove.</param>
    /// <returns><see langword="true"/> when the stream was present.</returns>
    /// <remarks>
    /// Used when a convention stream turns out not to be this service's to create, so that the
    /// subjects it would have claimed resolve to the stream that already owns them.
    /// </remarks>
    public bool RemoveStream(NatsStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        lock (_lock)
        {
            return _streams.Remove(stream);
        }
    }

    /// <summary>
    /// Finds the stream declared in this topology that captures the specified subject.
    /// </summary>
    /// <param name="subject">The subject to match.</param>
    /// <returns>The capturing stream, or <see langword="null"/> when no local stream matches.</returns>
    /// <remarks>
    /// Only streams this bus declares are considered. Subjects published by another service are
    /// resolved against the server at start-up instead.
    /// </remarks>
    public NatsStream? FindStreamForSubject(string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        lock (_lock)
        {
            foreach (var stream in _streams)
            {
                foreach (var filter in stream.Subjects)
                {
                    if (SubjectMatcher.Matches(filter, subject))
                    {
                        return stream;
                    }
                }
            }

            return null;
        }
    }
}
