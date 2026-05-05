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
    private MessageProcessor? _processor;
    private QueueHeartbeat? _heartbeat;
    private ILogger<AzureServiceBusReceiveEndpoint> _logger = null!;
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

        if (Queue.RequiresSession == true)
        {
            _processor = MessageProcessor.ForSessionProcessor(CreateSessionProcessor(asbTransport));
        }
        else
        {
            _processor = MessageProcessor.ForProcessor(CreateProcessor(asbTransport));
        }

        await _processor.StartProcessingAsync(cancellationToken);

        if (Configuration.Kind == ReceiveEndpointKind.Reply)
        {
            var receiver = asbTransport.ClientManager.CreateReceiver(Queue.Name);
            _heartbeat = new QueueHeartbeat(receiver, _logger, Queue.Name);
        }
    }

    private ServiceBusProcessor CreateProcessor(AzureServiceBusMessagingTransport asbTransport)
    {
        var maxAutoLockRenewal = Configuration.MaxAutoLockRenewalDuration ?? TimeSpan.FromMinutes(5);

        var options = new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = _maxConcurrentCalls,
            AutoCompleteMessages = false, // We handle ack/nack in middleware
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            PrefetchCount = _prefetchCount,
            MaxAutoLockRenewalDuration = maxAutoLockRenewal
        };

        var processor = asbTransport.ClientManager.CreateProcessor(Queue.Name, options);
        processor.ProcessMessageAsync += OnNonSessionMessage;
        processor.ProcessErrorAsync += OnProcessorError;
        return processor;
    }

    private ServiceBusSessionProcessor CreateSessionProcessor(AzureServiceBusMessagingTransport asbTransport)
    {
        var maxAutoLockRenewal = Configuration.MaxAutoLockRenewalDuration ?? TimeSpan.FromMinutes(5);
        var maxSessions = Configuration.MaxConcurrentSessions ?? _maxConcurrentCalls;
        var maxCallsPerSession = Configuration.MaxConcurrentCallsPerSession ?? 1;

        var options = new ServiceBusSessionProcessorOptions
        {
            MaxConcurrentSessions = maxSessions,
            MaxConcurrentCallsPerSession = maxCallsPerSession,
            AutoCompleteMessages = false,
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            PrefetchCount = Configuration.PrefetchCount ?? maxSessions * maxCallsPerSession * 2,
            MaxAutoLockRenewalDuration = maxAutoLockRenewal
        };

        if (Configuration.SessionIdleTimeout is { } idle)
        {
            options.SessionIdleTimeout = idle;
        }

        var sessionProcessor = asbTransport.ClientManager.CreateSessionProcessor(Queue.Name, options);
        sessionProcessor.ProcessMessageAsync += OnSessionMessage;
        sessionProcessor.ProcessErrorAsync += OnProcessorError;
        return sessionProcessor;
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
        // Transient/recoverable conditions are surfaced as warnings; only unknown faults escalate to error.
        if (IsTransientProcessorError(args.Exception))
        {
            _logger.ProcessorTransientError(args.Exception, args.EntityPath, args.ErrorSource);
        }
        else
        {
            _logger.ProcessorError(args.Exception, args.EntityPath, args.ErrorSource);
        }

        if (args.Exception is ServiceBusException { Reason: ServiceBusFailureReason.MessagingEntityNotFound })
        {
            _processor?.StopProcessingAsync();
        }

        return Task.CompletedTask;
    }

    protected override async ValueTask OnStopAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        if (_heartbeat is not null)
        {
            await _heartbeat.DisposeAsync();
            _heartbeat = null;
        }

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

internal static partial class Logs
{
    [LoggerMessage(
        LogLevel.Warning,
        "Azure Service Bus processor transient error on entity {EntityPath} (Source: {ErrorSource})")]
    public static partial void ProcessorTransientError(
        this ILogger logger,
        Exception exception,
        string entityPath,
        ServiceBusErrorSource errorSource);

    [LoggerMessage(LogLevel.Error, "Azure Service Bus processor error on entity {EntityPath} (Source: {ErrorSource})")]
    public static partial void ProcessorError(
        this ILogger logger,
        Exception exception,
        string entityPath,
        ServiceBusErrorSource errorSource);

    [LoggerMessage(LogLevel.Warning, "Reply queue keep-alive peek failed for {EntityPath}")]
    public static partial void ReplyQueueKeepAliveFailed(this ILogger logger, Exception exception, string entityPath);
}
