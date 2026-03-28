namespace Mocha.Transport.NATS;

/// <summary>
/// Manages the NATS JetStream topology model (streams, subjects, consumers) for a transport instance,
/// providing thread-safe mutation and lookup of topology resources.
/// </summary>
public sealed class NatsMessagingTopology(
    NatsMessagingTransport transport,
    Uri baseAddress,
    NatsBusDefaults defaults,
    bool autoProvision)
    : MessagingTopology<NatsMessagingTransport>(transport, baseAddress)
{
    private readonly object _lock = new();
    private readonly List<NatsStream> _streams = [];
    private readonly List<NatsSubject> _subjects = [];
    private readonly List<NatsConsumer> _consumers = [];

    /// <summary>
    /// Gets a value indicating whether topology resources should be auto-provisioned by default.
    /// Individual resources may override this setting via their own <c>AutoProvision</c> property.
    /// </summary>
    public bool AutoProvision => autoProvision;

    /// <summary>
    /// Gets a snapshot of the streams registered in this topology.
    /// </summary>
    public IReadOnlyList<NatsStream> Streams
    {
        get { lock (_lock) { return _streams.ToArray(); } }
    }

    /// <summary>
    /// Gets a snapshot of the subjects registered in this topology.
    /// </summary>
    public IReadOnlyList<NatsSubject> Subjects
    {
        get { lock (_lock) { return _subjects.ToArray(); } }
    }

    /// <summary>
    /// Gets a snapshot of the consumers registered in this topology.
    /// </summary>
    public IReadOnlyList<NatsConsumer> Consumers
    {
        get { lock (_lock) { return _consumers.ToArray(); } }
    }

    /// <summary>
    /// Gets the bus-level defaults applied to all auto-provisioned streams and consumers.
    /// </summary>
    public NatsBusDefaults Defaults => defaults;

    /// <summary>
    /// Adds a new stream to the topology, initializing it from the given configuration.
    /// </summary>
    /// <param name="configuration">The stream configuration specifying name, subjects, and limits.</param>
    /// <returns>The created and initialized stream resource.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a stream with the same name already exists.</exception>
    public NatsStream AddStream(NatsStreamConfiguration configuration)
    {
        lock (_lock)
        {
            var stream = _streams.FirstOrDefault(s => s.Name == configuration.Name);
            if (stream is not null)
            {
                throw new InvalidOperationException($"Stream '{configuration.Name}' already exists");
            }

            stream = new NatsStream();

            configuration.Topology = this;
            defaults.Stream.ApplyTo(configuration);
            stream.Initialize(configuration);

            _streams.Add(stream);

            stream.Complete();

            return stream;
        }
    }

    /// <summary>
    /// Adds a new subject to the topology, initializing it from the given configuration.
    /// </summary>
    /// <param name="configuration">The subject configuration specifying name and stream binding.</param>
    /// <returns>The created and initialized subject resource.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a subject with the same name already exists.</exception>
    public NatsSubject AddSubject(NatsSubjectConfiguration configuration)
    {
        lock (_lock)
        {
            var subject = _subjects.FirstOrDefault(s => s.Name == configuration.Name);
            if (subject is not null)
            {
                throw new InvalidOperationException($"Subject '{configuration.Name}' already exists");
            }

            subject = new NatsSubject();

            configuration.Topology = this;
            subject.Initialize(configuration);

            _subjects.Add(subject);

            subject.Complete();

            return subject;
        }
    }

    /// <summary>
    /// Adds a new consumer to the topology, initializing it from the given configuration.
    /// </summary>
    /// <param name="configuration">The consumer configuration specifying name, stream, and settings.</param>
    /// <returns>The created and initialized consumer resource.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a consumer with the same name already exists.</exception>
    public NatsConsumer AddConsumer(NatsConsumerConfiguration configuration)
    {
        lock (_lock)
        {
            var consumer = _consumers.FirstOrDefault(c => c.Name == configuration.Name);
            if (consumer is not null)
            {
                throw new InvalidOperationException($"Consumer '{configuration.Name}' already exists");
            }

            consumer = new NatsConsumer();

            configuration.Topology = this;
            defaults.Consumer.ApplyTo(configuration);
            consumer.Initialize(configuration);

            _consumers.Add(consumer);

            consumer.Complete();

            return consumer;
        }
    }

    /// <summary>
    /// Finds the stream that captures the specified subject name.
    /// </summary>
    /// <param name="subjectName">The subject name to look up.</param>
    /// <returns>The stream that captures the subject, or <c>null</c> if not found.</returns>
    public NatsStream? GetStreamForSubject(string subjectName)
    {
        lock (_lock)
        {
            foreach (var stream in _streams)
            {
                foreach (var subject in stream.Subjects)
                {
                    if (subject == subjectName || MatchesWildcard(subject, subjectName))
                    {
                        return stream;
                    }
                }
            }

            return null;
        }
    }

    private static bool MatchesWildcard(string pattern, string subject)
    {
        // NATS wildcard matching: ">" matches any number of tokens, "*" matches a single token
        if (pattern == ">")
        {
            return true;
        }

        if (pattern.EndsWith(".>"))
        {
            var prefix = pattern[..^2];
            return subject.StartsWith(prefix + ".", StringComparison.Ordinal) || subject == prefix;
        }

        if (!pattern.Contains('*'))
        {
            return false;
        }

        var patternParts = pattern.Split('.');
        var subjectParts = subject.Split('.');

        if (patternParts.Length != subjectParts.Length)
        {
            return false;
        }

        for (var i = 0; i < patternParts.Length; i++)
        {
            if (patternParts[i] != "*" && patternParts[i] != subjectParts[i])
            {
                return false;
            }
        }

        return true;
    }
}
