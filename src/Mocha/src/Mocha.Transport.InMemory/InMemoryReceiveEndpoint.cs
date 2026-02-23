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

        _maxDegreeOfParallelism = configuration.MaxConcurrency;
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
            item => ProcessMessageAsync(item, logger),
            _maxDegreeOfParallelism);

        return ValueTask.CompletedTask;
    }

    private async Task ProcessMessageAsync(InMemoryQueueItem item, ILogger logger)
    {
        using var _ = item;
        try
        {
            await ExecuteAsync(
                static (context, envelope) => context.SetEnvelope(envelope),
                item.Envelope,
                CancellationToken.None);
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
