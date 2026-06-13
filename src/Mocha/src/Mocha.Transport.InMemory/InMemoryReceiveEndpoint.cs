using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Threading;

namespace Mocha.Transport.InMemory;

/// <summary>
/// A receive endpoint that consumes messages from an <see cref="InMemoryQueue"/> and dispatches them
/// through the receive middleware pipeline.
/// </summary>
/// <remarks>
/// Message processing uses a <see cref="ChannelProcessor{T}"/> with N concurrent workers
/// (where N = MaxConcurrency) that each read directly from the queue via
/// <see cref="InMemoryQueue.ConsumeAsync"/> and invoke <see cref="ReceiveEndpoint.ExecuteAsync"/>
/// for each envelope. Faulted messages are logged but do not stop any consumer loop.
/// </remarks>
public sealed class InMemoryReceiveEndpoint(InMemoryMessagingTransport transport)
    : ReceiveEndpoint<InMemoryReceiveEndpointConfiguration>(transport)
{
    private int _maxDegreeOfParallelism = Environment.ProcessorCount;
    private ChannelProcessor<InMemoryQueueItem>? _processor;

    /// <summary>
    /// Gets the in-memory queue this endpoint is consuming from.
    /// </summary>
    public InMemoryQueue Queue { get; private set; } = null!;

    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        InMemoryReceiveEndpointConfiguration configuration)
    {
        if (configuration.QueueName is null)
        {
            throw new InvalidOperationException("Queue name is required");
        }

        _maxDegreeOfParallelism = configuration.MaxConcurrency
            ?? ReceiveEndpointConfiguration.Defaults.MaxConcurrency;
    }

    protected override void OnDiscoverTopology(
        IMessagingConfigurationContext context,
        InMemoryReceiveEndpointConfiguration configuration)
    {
        if (configuration.QueueName is null)
        {
            throw new InvalidOperationException("Queue name is required");
        }

        var topology = (InMemoryMessagingTopology)Transport.Topology;

        if (topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName) is null)
        {
            topology.AddQueue(new InMemoryQueueConfiguration { Name = configuration.QueueName });
        }

        // Materialize queue-level BindFrom intents into topic-to-queue bindings. Each intent names a
        // source topic; the topic is ensured in the topology so the binding can reference it.
        var resolver = ((InMemoryMessagingTransport)Transport).Resolver;
        foreach (var intent in configuration.QueueBindFroms)
        {
            MaterializeBindFrom(topology, resolver, configuration.QueueName, intent, messageTypeName: null);
        }

        // Materialize per-type BindFrom intents. A per-type BindFrom implies AutoBind(false) for
        // that type (handled by the topology convention); here only the explicit bindings are added.
        foreach (var typeBind in configuration.TypeBinds.Values)
        {
            foreach (var intent in typeBind.BindFroms)
            {
                MaterializeBindFrom(topology, resolver, configuration.QueueName, intent, typeBind.MessageType.Name);
            }
        }
    }

    private static void MaterializeBindFrom(
        InMemoryMessagingTopology topology,
        InMemoryDestinationResolver resolver,
        string queueName,
        BindFromIntent intent,
        string? messageTypeName)
    {
        if (intent.RoutingKey is not null)
        {
            throw ThrowHelper.BindFromWithNonNullRoutingKey(
                "in-memory",
                messageTypeName ?? intent.Source.ToString(),
                queueName);
        }

        if (!resolver.TryResolveSourceTopic(intent.Source, out var topicName))
        {
            throw new InvalidOperationException(
                $"BindFrom source '{intent.Source}' could not be resolved to an in-memory topic name.");
        }

        // Ensure the source topic exists in the topology. AddTopic merges on duplicate names, so
        // repeating the same source is a safe no-op.
        topology.AddTopic(new InMemoryTopicConfiguration { Name = topicName });

        // Add the topic-to-queue binding only if it does not already exist, so repeat intents and
        // overlap with convention bindings stay idempotent.
        if (topology.Bindings.All(b =>
                b.Source.Name != topicName
                || b is not InMemoryQueueBinding queueBinding
                || queueBinding.Destination.Name != queueName))
        {
            topology.AddBinding(
                new InMemoryBindingConfiguration
                {
                    Source = topicName,
                    Destination = queueName,
                    DestinationKind = InMemoryDestinationKind.Queue
                });
        }
    }

    protected override void OnComplete(
        IMessagingConfigurationContext context,
        InMemoryReceiveEndpointConfiguration configuration)
    {
        if (configuration.QueueName is null)
        {
            throw new InvalidOperationException("Queue name is required");
        }

        var topology = (InMemoryMessagingTopology)Transport.Topology;

        Queue =
            topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName)
            ?? throw new InvalidOperationException("Queue not found");

        Source = Queue;
    }

    protected override ValueTask OnStartAsync(IMessagingRuntimeContext context, CancellationToken cancellationToken)
    {
        var logger = context.Services.GetRequiredService<ILogger<InMemoryReceiveEndpoint>>();

        _processor = new ChannelProcessor<InMemoryQueueItem>(
            Queue.ConsumeAsync,
            (item, ct) => ProcessMessageAsync(item, logger, ct),
            _maxDegreeOfParallelism);

        return ValueTask.CompletedTask;
    }

    private async Task ProcessMessageAsync(InMemoryQueueItem item, ILogger logger, CancellationToken cancellationToken)
    {
        using var _ = item;
        try
        {
            await ExecuteAsync(
                static (context, envelope) => context.SetEnvelope(envelope),
                item.Envelope,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Error processing message");
        }
    }

    protected override async ValueTask OnStopAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.DisposeAsync();
            _processor = null;
        }
    }
}
