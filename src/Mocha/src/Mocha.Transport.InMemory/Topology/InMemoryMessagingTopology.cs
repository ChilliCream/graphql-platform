namespace Mocha.Transport.InMemory;

/// <summary>
/// Manages the in-memory topology of topics, queues, and bindings for a single transport instance.
/// </summary>
/// <remarks>
/// All mutations (adding topics, queues, bindings) are serialized under a lock to prevent
/// duplicate or inconsistent topology state. Resources are resolved by name and must be
/// pre-declared before bindings that reference them.
/// </remarks>
public sealed class InMemoryMessagingTopology(InMemoryMessagingTransport transport, Uri baseAddress)
    : MessagingTopology<InMemoryMessagingTransport>(transport, baseAddress)
{
    private readonly object _lock = new();
    private readonly List<InMemoryTopic> _topics = [];
    private readonly List<InMemoryQueue> _queues = [];
    private readonly List<InMemoryBinding> _bindings = [];

    /// <summary>
    /// Gets all topics currently registered in this topology.
    /// </summary>
    public IReadOnlyList<InMemoryTopic> Topics => _topics;

    /// <summary>
    /// Gets all queues currently registered in this topology.
    /// </summary>
    public IReadOnlyList<InMemoryQueue> Queues => _queues;

    /// <summary>
    /// Gets all bindings currently registered in this topology.
    /// </summary>
    public IReadOnlyList<InMemoryBinding> Bindings => _bindings;

    /// <summary>
    /// Registers a new topic in the topology.
    /// </summary>
    /// <param name="configuration">The topic configuration specifying the topic name.</param>
    /// <returns>The newly created <see cref="InMemoryTopic"/>.</returns>
    /// <exception cref="InvalidOperationException">A topic with the same name already exists.</exception>
    public InMemoryTopic AddTopic(InMemoryTopicConfiguration configuration)
    {
        lock (_lock)
        {
            var topic = _topics.FirstOrDefault(e => e.Name == configuration.Name);
            if (topic is not null)
            {
                throw new InvalidOperationException($"Topic '{configuration.Name}' already exists");
            }

            topic = new InMemoryTopic();

            configuration.Topology = this;
            topic.Initialize(configuration);

            _topics.Add(topic);

            topic.Complete();

            return topic;
        }
    }

    /// <summary>
    /// Registers a new queue in the topology.
    /// </summary>
    /// <param name="configuration">The queue configuration specifying the queue name.</param>
    /// <returns>The newly created <see cref="InMemoryQueue"/>.</returns>
    /// <exception cref="InvalidOperationException">A queue with the same name already exists.</exception>
    public InMemoryQueue AddQueue(InMemoryQueueConfiguration configuration)
    {
        lock (_lock)
        {
            configuration.Topology ??= this;

            var queue = _queues.FirstOrDefault(q => q.Name == configuration.Name);
            if (queue is not null)
            {
                throw new InvalidOperationException($"Queue '{configuration.Name}' already exists");
            }

            configuration.Topology = this;
            queue = new InMemoryQueue();
            queue.Initialize(configuration);

            _queues.Add(queue);

            queue.Complete();

            return queue;
        }
    }

    /// <summary>
    /// Creates a binding that routes messages from a source topic to a destination queue or topic.
    /// </summary>
    /// <param name="configuration">The binding configuration specifying source, destination, and destination kind.</param>
    /// <returns>The newly created <see cref="InMemoryBinding"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// The source topic or destination resource does not exist, or the destination kind is unknown.
    /// </exception>
    public InMemoryBinding AddBinding(InMemoryBindingConfiguration configuration)
    {
        lock (_lock)
        {
            var source = _topics.FirstOrDefault(e => e.Name == configuration.Source);
            if (source is null)
            {
                throw new InvalidOperationException($"Source topic '{configuration.Source}' not found");
            }

            InMemoryBinding binding;

            if (configuration.DestinationKind == InMemoryDestinationKind.Queue)
            {
                var destination = _queues.FirstOrDefault(q => q.Name == configuration.Destination);
                if (destination is null)
                {
                    throw new InvalidOperationException($"Destination queue '{configuration.Destination}' not found");
                }

                var queueBinding = new InMemoryQueueBinding();
                configuration.Topology = this;
                queueBinding.Initialize(configuration);
                queueBinding.SetDestination(destination);
                destination.AddBinding(queueBinding);

                binding = queueBinding;
            }
            else if (configuration.DestinationKind == InMemoryDestinationKind.Topic)
            {
                var destination = _topics.FirstOrDefault(e => e.Name == configuration.Destination);
                if (destination is null)
                {
                    throw new InvalidOperationException($"Destination topic '{configuration.Destination}' not found");
                }

                var topicBinding = new InMemoryTopicBinding();
                configuration.Topology = this;
                topicBinding.Initialize(configuration);
                topicBinding.SetDestination(destination);
                destination.AddBinding(topicBinding);

                binding = topicBinding;
            }
            else
            {
                throw new InvalidOperationException($"Unknown destination kind: {configuration.DestinationKind}");
            }

            binding.SetSource(source);
            source.AddBinding(binding);

            _bindings.Add(binding);

            binding.Complete();

            return binding;
        }
    }

    /// <summary>
    /// Looks up a topic by name.
    /// </summary>
    /// <param name="name">The name of the topic to find.</param>
    /// <returns>The matching <see cref="InMemoryTopic"/>, or <c>null</c> if no topic with that name exists.</returns>
    public InMemoryTopic? GetTopic(ReadOnlySpan<char> name)
    {
        foreach (var topic in _topics)
        {
            if (topic.Name.SequenceEqual(name))
            {
                return topic;
            }
        }

        return null;
    }

    /// <summary>
    /// Looks up a queue by name.
    /// </summary>
    /// <param name="name">The name of the queue to find.</param>
    /// <returns>The matching <see cref="InMemoryQueue"/>, or <c>null</c> if no queue with that name exists.</returns>
    public InMemoryQueue? GetQueue(ReadOnlySpan<char> name)
    {
        foreach (var queue in _queues)
        {
            if (queue.Name.SequenceEqual(name))
            {
                return queue;
            }
        }

        return null;
    }
}
