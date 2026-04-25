using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Features;
using Mocha.Transport.AzureServiceBus.Features;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// A receive endpoint that consumes messages from an Azure Service Bus queue. When the queue
/// requires sessions the endpoint runs a <see cref="ServiceBusSessionProcessor"/>; otherwise it
/// runs a <see cref="ServiceBusProcessor"/>. Both paths flow through the same receive middleware
/// pipeline.
/// </summary>
/// <param name="transport">The owning Azure Service Bus transport instance.</param>
public sealed class AzureServiceBusReceiveEndpoint(AzureServiceBusMessagingTransport transport)
    : ReceiveEndpoint<AzureServiceBusReceiveEndpointConfiguration>(transport)
{
    private ServiceBusProcessor? _processor;
    private ServiceBusSessionProcessor? _sessionProcessor;
    private ILogger<AzureServiceBusReceiveEndpoint>? _logger;
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
            1,
            1000);

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

        if (Queue.RequiresSession != true)
        {
            var sessionOnly = new List<string>();
            if (configuration.MaxConcurrentSessions is not null)
            {
                sessionOnly.Add(nameof(IAzureServiceBusReceiveEndpointDescriptor.WithMaxConcurrentSessions));
            }
            if (configuration.MaxConcurrentCallsPerSession is not null)
            {
                sessionOnly.Add(nameof(IAzureServiceBusReceiveEndpointDescriptor.WithMaxConcurrentCallsPerSession));
            }
            if (configuration.SessionIdleTimeout is not null)
            {
                sessionOnly.Add(nameof(IAzureServiceBusReceiveEndpointDescriptor.WithSessionIdleTimeout));
            }

            if (sessionOnly.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Receive endpoint '{configuration.Name}' targets queue '{Queue.Name}', "
                        + $"which does not require sessions ({nameof(AzureServiceBusQueue.RequiresSession)}=false). "
                        + "The following session-only options are set and would have no effect: "
                        + $"{string.Join(", ", sessionOnly)}. Either remove these options, or call "
                        + "WithRequiresSession() on the queue declaration.");
            }
        }
    }

    protected override async ValueTask OnStartAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        if (Transport is not AzureServiceBusMessagingTransport asbTransport)
        {
            throw new InvalidOperationException("Transport is not an AzureServiceBusMessagingTransport");
        }

        _logger = context.Services.GetRequiredService<ILogger<AzureServiceBusReceiveEndpoint>>();

        var configuration = Configuration;
        var maxAutoLockRenewal = configuration.MaxAutoLockRenewalDuration ?? TimeSpan.FromMinutes(5);

        if (Queue.RequiresSession == true)
        {
            var maxSessions = configuration.MaxConcurrentSessions ?? _maxConcurrentCalls;
            var maxCallsPerSession = configuration.MaxConcurrentCallsPerSession ?? 1;

            var options = new ServiceBusSessionProcessorOptions
            {
                MaxConcurrentSessions = maxSessions,
                MaxConcurrentCallsPerSession = maxCallsPerSession,
                AutoCompleteMessages = false,
                ReceiveMode = ServiceBusReceiveMode.PeekLock,
                PrefetchCount = configuration.PrefetchCount ?? maxSessions * maxCallsPerSession * 2,
                MaxAutoLockRenewalDuration = maxAutoLockRenewal
            };

            if (configuration.SessionIdleTimeout is { } idle)
            {
                options.SessionIdleTimeout = idle;
            }

            _sessionProcessor = asbTransport.ClientManager.CreateSessionProcessor(Queue.Name, options);
            _sessionProcessor.ProcessMessageAsync += OnSessionMessage;
            _sessionProcessor.ProcessErrorAsync += OnProcessorError;
            await _sessionProcessor.StartProcessingAsync(cancellationToken);
        }
        else
        {
            var options = new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = _maxConcurrentCalls,
                AutoCompleteMessages = false, // We handle ack/nack in middleware
                ReceiveMode = ServiceBusReceiveMode.PeekLock,
                PrefetchCount = _prefetchCount,
                MaxAutoLockRenewalDuration = maxAutoLockRenewal
            };

            _processor = asbTransport.ClientManager.CreateProcessor(Queue.Name, options);
            _processor.ProcessMessageAsync += OnNonSessionMessage;
            _processor.ProcessErrorAsync += OnProcessorError;
            await _processor.StartProcessingAsync(cancellationToken);
        }
    }

    private async Task OnNonSessionMessage(ProcessMessageEventArgs args)
    {
        await ExecuteAsync(
            static (ctx, state) => ctx.Features.GetOrSet<AzureServiceBusReceiveFeature>().SetNonSession(state),
            args,
            args.CancellationToken);
    }

    private async Task OnSessionMessage(ProcessSessionMessageEventArgs args)
    {
        await ExecuteAsync(
            static (ctx, state) => ctx.Features.GetOrSet<AzureServiceBusReceiveFeature>().SetSession(state),
            args,
            args.CancellationToken);
    }

    private Task OnProcessorError(ProcessErrorEventArgs args)
    {
        var logger = _logger;
        if (logger is null)
        {
            return Task.CompletedTask;
        }

        // Transient/recoverable conditions are surfaced as warnings; only unknown faults escalate to error.
        if (IsTransientProcessorError(args.Exception))
        {
            logger.LogWarning(
                args.Exception,
                "Azure Service Bus processor transient error on entity {EntityPath} (Source: {ErrorSource})",
                args.EntityPath,
                args.ErrorSource);
        }
        else
        {
            logger.LogError(
                args.Exception,
                "Azure Service Bus processor error on entity {EntityPath} (Source: {ErrorSource})",
                args.EntityPath,
                args.ErrorSource);
        }

        return Task.CompletedTask;
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

        if (_sessionProcessor is not null)
        {
            await _sessionProcessor.StopProcessingAsync(cancellationToken);
            await _sessionProcessor.DisposeAsync();
            _sessionProcessor = null;
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
