using Azure.Messaging.EventHubs;
using Mocha.Features;
using Mocha.Transport.AzureEventHub.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Event Hub receive endpoint that consumes messages from a specific hub using a custom event processor.
/// </summary>
/// <param name="transport">The owning Event Hub transport instance.</param>
public sealed class EventHubReceiveEndpoint(EventHubMessagingTransport transport)
    : ReceiveEndpoint<EventHubReceiveEndpointConfiguration>(transport)
{
    private string _consumerGroup = "$Default";

    /// <summary>
    /// Gets the Event Hub topic that this endpoint consumes from.
    /// </summary>
    public EventHubTopic Topic { get; private set; } = null!;

    /// <summary>
    /// Gets the Event Hub subscription (consumer group) associated with this endpoint, if any.
    /// </summary>
    public EventHubSubscription? Subscription { get; private set; }

    private int _checkpointInterval = 100;
    private MochaEventProcessor? _processor;

    /// <summary>
    /// Gets a value indicating whether the underlying event processor is currently running.
    /// </summary>
    public bool IsProcessorRunning => _processor?.IsRunning ?? false;

    /// <inheritdoc />
    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        EventHubReceiveEndpointConfiguration configuration)
    {
        if (configuration.HubName is null)
        {
            throw new InvalidOperationException("Hub name is required");
        }

        _consumerGroup = configuration.ConsumerGroup ?? "$Default";
        _checkpointInterval = configuration.CheckpointInterval;
    }

    /// <inheritdoc />
    protected override void OnComplete(
        IMessagingConfigurationContext context,
        EventHubReceiveEndpointConfiguration configuration)
    {
        var topology = (EventHubMessagingTopology)Transport.Topology;

        Topic = topology.Topics.FirstOrDefault(t => t.Name == configuration.HubName)
            ?? throw new InvalidOperationException($"Topic '{configuration.HubName}' not found");

        Source = Topic;
        Subscription = topology.Subscriptions
            .FirstOrDefault(s => s.TopicName == configuration.HubName && s.ConsumerGroup == _consumerGroup);
    }

    /// <inheritdoc />
    protected override async ValueTask OnStartAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        if (Transport is not EventHubMessagingTransport ehTransport)
        {
            throw new InvalidOperationException("Transport is not EventHubMessagingTransport");
        }

        var connectionProvider = ehTransport.ConnectionManager.ConnectionProvider;
        var transportConfig = ehTransport.TransportConfiguration;
        var checkpointStore = transportConfig.CheckpointStoreFactory is not null
            ? transportConfig.CheckpointStoreFactory(context.Services)
            : new InMemoryCheckpointStore();
        var ownershipStore = transportConfig.OwnershipStoreFactory?.Invoke(context.Services);

        Func<EventData, string, CancellationToken, ValueTask> messageHandler =
            (eventData, partitionId, ct) =>
                ExecuteAsync(
                    static (context, state) =>
                    {
                        var feature = context.Features.GetOrSet<EventHubReceiveFeature>();
                        feature.EventData = state.eventData;
                        feature.PartitionId = state.partitionId;
                        feature.SequenceNumber = state.eventData.SequenceNumber;
                        feature.EnqueuedTime = state.eventData.EnqueuedTime;
                    },
                    (eventData, partitionId),
                    ct);

        var logger = context.Services.GetRequiredService<ILogger<MochaEventProcessor>>();

        if (connectionProvider.ConnectionString is not null)
        {
            _processor = new MochaEventProcessor(
                logger,
                _consumerGroup,
                connectionProvider.ConnectionString,
                Topic.Name,
                messageHandler,
                checkpointStore,
                ownershipStore,
                checkpointInterval: _checkpointInterval);
        }
        else if (connectionProvider.Credential is not null)
        {
            _processor = new MochaEventProcessor(
                logger,
                _consumerGroup,
                connectionProvider.FullyQualifiedNamespace,
                Topic.Name,
                connectionProvider.Credential,
                messageHandler,
                checkpointStore,
                ownershipStore,
                checkpointInterval: _checkpointInterval);
        }
        else
        {
            throw new InvalidOperationException(
                "Connection provider must supply either a connection string or token credential");
        }

        if (ownershipStore is null && checkpointStore is not InMemoryCheckpointStore)
        {
            logger.LogWarning(
                "Event Hub processor for '{HubName}' consumer group '{ConsumerGroup}' has "
                + "persistent checkpoints but no OwnershipStore. Multiple instances "
                + "will duplicate event processing.",
                Topic!.Name, _consumerGroup);
        }

        // StartProcessingAsync creates its own internal CancellationTokenSource.
        // The cancellationToken here is only used for the startup operation itself.
        await _processor.StartProcessingAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override async ValueTask OnStopAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            // StopProcessingAsync gracefully cancels the internal processing loop
            // and waits for all in-flight event processing to complete.
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.FlushCheckpointsAsync(cancellationToken);
            _processor = null;
        }
    }
}
