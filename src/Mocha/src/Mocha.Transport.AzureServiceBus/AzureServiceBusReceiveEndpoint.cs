using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Features;
using Mocha.Transport.AzureServiceBus.Features;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// A receive endpoint that consumes messages from an Azure Service Bus queue using a
/// <see cref="ServiceBusProcessor"/> and processes each message through the receive middleware pipeline.
/// </summary>
/// <param name="transport">The owning Azure Service Bus transport instance.</param>
public sealed class AzureServiceBusReceiveEndpoint(AzureServiceBusMessagingTransport transport)
    : ReceiveEndpoint<AzureServiceBusReceiveEndpointConfiguration>(transport)
{
    private ServiceBusProcessor? _processor;
    private int _maxConcurrentCalls = 1;
    private int _prefetchCount;

    /// <summary>
    /// Gets the Azure Service Bus queue this endpoint is consuming from.
    /// </summary>
    public AzureServiceBusQueue Queue { get; private set; } = null!;

    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        AzureServiceBusReceiveEndpointConfiguration configuration)
    {
        if (configuration.QueueName is null)
        {
            throw new InvalidOperationException("Queue name is required");
        }

        _maxConcurrentCalls = Math.Clamp(
            configuration.MaxConcurrency ?? ReceiveEndpointConfiguration.Defaults.MaxConcurrency,
            1, 1000);

        // Compute a sensible PrefetchCount default when not explicitly set.
        // Without this, ServiceBusProcessor falls back to synchronous one-at-a-time pull (PrefetchCount=0).
        _prefetchCount = configuration.PrefetchCount ?? _maxConcurrentCalls * 2;
    }

    protected override void OnComplete(
        IMessagingConfigurationContext context,
        AzureServiceBusReceiveEndpointConfiguration configuration)
    {
        if (configuration.QueueName is null)
        {
            throw new InvalidOperationException("Queue name is required");
        }

        var topology = (AzureServiceBusMessagingTopology)Transport.Topology;

        Queue =
            topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName)
            ?? throw new InvalidOperationException("Queue not found");

        Source = Queue;
    }

    protected override async ValueTask OnStartAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        if (Transport is not AzureServiceBusMessagingTransport asbTransport)
        {
            throw new InvalidOperationException("Transport is not an AzureServiceBusMessagingTransport");
        }

        var logger = context.Services.GetRequiredService<ILogger<AzureServiceBusReceiveEndpoint>>();

        var options = new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = _maxConcurrentCalls,
            AutoCompleteMessages = false,  // We handle ack/nack in middleware
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            PrefetchCount = _prefetchCount,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5)
        };

        _processor = asbTransport.ClientManager.CreateProcessor(Queue.Name, options);

        _processor.ProcessMessageAsync += async args =>
        {
            await ExecuteAsync(
                static (ctx, state) =>
                {
                    var feature = ctx.Features.GetOrSet<AzureServiceBusReceiveFeature>();
                    feature.ProcessMessageEventArgs = state;

                    // Expose the pooled feature under its public power-user contract so handlers
                    // can resolve it via ctx.AzureServiceBus(). The same instance backs both keys —
                    // no extra allocation.
                    ctx.Features.Set<IAzureServiceBusMessageContext>(feature);
                },
                args,
                args.CancellationToken);
        };

        _processor.ProcessErrorAsync += args =>
        {
            // Transient/recoverable conditions are surfaced as warnings; only unknown faults escalate to error.
            if (IsTransientProcessorError(args.Exception))
            {
                logger.LogWarning(args.Exception,
                    "Azure Service Bus processor transient error on entity {EntityPath} (Source: {ErrorSource})",
                    args.EntityPath, args.ErrorSource);
            }
            else
            {
                logger.LogError(args.Exception,
                    "Azure Service Bus processor error on entity {EntityPath} (Source: {ErrorSource})",
                    args.EntityPath, args.ErrorSource);
            }

            return Task.CompletedTask;
        };

        await _processor.StartProcessingAsync(cancellationToken);
    }

    protected override async ValueTask OnStopAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
            _processor = null;
        }
    }

    private static bool IsTransientProcessorError(Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            return true;
        }

        if (exception is ServiceBusException sbEx)
        {
            return sbEx.Reason
                is ServiceBusFailureReason.ServiceCommunicationProblem
                or ServiceBusFailureReason.ServiceBusy
                or ServiceBusFailureReason.ServiceTimeout
                or ServiceBusFailureReason.MessageLockLost
                or ServiceBusFailureReason.SessionLockLost;
        }

        return false;
    }
}
