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
    private static readonly TimeSpan s_maximumRecoveryDelay = TimeSpan.FromSeconds(30);
    private readonly object _processorRecoverySync = new();
    private MessageProcessor? _processor;
    private CancellationTokenSource? _processorLifetime;
    private Task _processorRecoveryTask = Task.CompletedTask;
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
            throw ThrowHelper.ReceiveEndpointQueueNameRequired();
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
            throw ThrowHelper.ReceiveEndpointQueueNameRequired();
        }

        var topology = (AzureServiceBusMessagingTopology)Transport.Topology;

        Queue =
            topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName)
            ?? throw ThrowHelper.ReceiveEndpointQueueNotFound();

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
                throw ThrowHelper.ReceiveEndpointHasSessionOnlyOptionsOnNonSessionQueue(
                    Name,
                    Queue.Name,
                    string.Join(", ", sessionOnly));
            }
        }
    }

    protected override async ValueTask OnStartAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        if (Transport is not AzureServiceBusMessagingTransport asbTransport)
        {
            throw ThrowHelper.TransportIsNotAzureServiceBus();
        }

        _logger = context.Services.GetRequiredService<ILogger<AzureServiceBusReceiveEndpoint>>();
        _processorLifetime = new CancellationTokenSource();

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
            BeginProcessorRecovery();
        }

        return Task.CompletedTask;
    }

    protected override async ValueTask OnStopAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        var processorLifetime = _processorLifetime;
        processorLifetime?.Cancel();

        Task processorRecoveryTask;
        lock (_processorRecoverySync)
        {
            processorRecoveryTask = _processorRecoveryTask;
        }

        try
        {
            await processorRecoveryTask;
        }
        catch (OperationCanceledException) when (processorLifetime?.IsCancellationRequested == true)
        {
            // Expected when endpoint shutdown interrupts an in-flight recovery.
        }

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

        processorLifetime?.Dispose();
        _processorLifetime = null;
    }

    private void BeginProcessorRecovery()
    {
        lock (_processorRecoverySync)
        {
            if (_processor is not { } processor
                || _processorLifetime is not { IsCancellationRequested: false } processorLifetime
                || !_processorRecoveryTask.IsCompleted)
            {
                return;
            }

            Task stopTask;
            try
            {
                // Do not await this from ProcessErrorAsync. The SDK waits for the error callback
                // while stopping, so recovery continues only after this callback has returned.
                stopTask = processor.StopProcessingAsync(processorLifetime.Token);
            }
            catch (Exception exception)
            {
                stopTask = Task.FromException(exception);
            }

            _processorRecoveryTask = RecoverProcessorAsync(
                processor,
                stopTask,
                processorLifetime.Token);
        }
    }

    private async Task RecoverProcessorAsync(
        MessageProcessor processor,
        Task stopTask,
        CancellationToken cancellationToken)
    {
        try
        {
            await stopTask;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            _logger.ProcessorRecoveryFailed(exception, Queue.Name, TimeSpan.Zero);
        }

        var retryDelay = TimeSpan.FromSeconds(1);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var topology = (AzureServiceBusMessagingTopology)transport.Topology;
                if (Queue.AutoProvision ?? topology.AutoProvision)
                {
                    await Queue.ProvisionAsync(transport.ClientManager, cancellationToken);
                }

                if (!ReferenceEquals(_processor, processor))
                {
                    return;
                }

                await processor.StartProcessingAsync(cancellationToken);
                _logger.ProcessorRecovered(Queue.Name);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.ProcessorRecoveryFailed(exception, Queue.Name, retryDelay);

                try
                {
                    await processor.StopProcessingAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception stopException)
                {
                    _logger.ProcessorRecoveryFailed(stopException, Queue.Name, retryDelay);
                }

                await Task.Delay(retryDelay, cancellationToken);
                retryDelay = TimeSpan.FromSeconds(
                    Math.Min(retryDelay.TotalSeconds * 2, s_maximumRecoveryDelay.TotalSeconds));
            }
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

    [LoggerMessage(LogLevel.Information, "Azure Service Bus processor recovered on entity {EntityPath}")]
    public static partial void ProcessorRecovered(this ILogger logger, string entityPath);

    [LoggerMessage(
        LogLevel.Warning,
        "Azure Service Bus processor recovery failed on entity {EntityPath}; retrying after {RetryDelay}")]
    public static partial void ProcessorRecoveryFailed(
        this ILogger logger,
        Exception exception,
        string entityPath,
        TimeSpan retryDelay);

    [LoggerMessage(LogLevel.Warning, "Reply queue keep-alive peek failed for {EntityPath}")]
    public static partial void ReplyQueueKeepAliveFailed(this ILogger logger, Exception exception, string entityPath);
}
