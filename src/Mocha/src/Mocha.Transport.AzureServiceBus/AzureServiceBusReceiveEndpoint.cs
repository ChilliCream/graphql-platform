using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private volatile bool _isStopping;

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

        _maxConcurrentCalls = configuration.MaxConcurrency ?? ReceiveEndpointConfiguration.Defaults.MaxConcurrency;
        _maxConcurrentCalls = Math.Clamp(_maxConcurrentCalls, 1, 1000);

        // PrefetchCount 0 disables local buffering and fetches messages on demand.
        // Handler concurrency is governed independently by MaxConcurrentCalls.
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

        if (Queue.ForwardTo is not null)
        {
            throw new InvalidOperationException(
                $"Receive endpoint '{Name}' cannot target queue '{Queue.Name}' configured with auto-forwarding "
                    + "(ForwardTo). The broker rejects receivers on an auto-forwarding source. Declare forwarding-only "
                    + "queues with DeclareQueue instead of Queue or Endpoint.");
        }

        if (Queue.RequiresSession != true)
        {
            var sessionOnly = new List<string>();
            if (configuration.MaxConcurrentSessions is not null)
            {
                sessionOnly.Add(nameof(IAzureServiceBusReceiveEndpointDescriptor.MaxConcurrentSessions));
            }
            if (configuration.MaxConcurrentCallsPerSession is not null)
            {
                sessionOnly.Add(nameof(IAzureServiceBusReceiveEndpointDescriptor.MaxConcurrentCallsPerSession));
            }
            if (configuration.SessionIdleTimeout is not null)
            {
                sessionOnly.Add(nameof(IAzureServiceBusReceiveEndpointDescriptor.SessionIdleTimeout));
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

    /// <summary>
    /// Starts when the messaging runtime activates this endpoint. The endpoint selects and starts
    /// the appropriate session or non-session processor, and starts a heartbeat for queues that
    /// have an idle deletion window configured. If startup fails, it disposes any resources created
    /// during the attempt before propagating the failure.
    /// </summary>
    protected override async ValueTask OnStartAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        _logger = context.Services.GetRequiredService<ILogger<AzureServiceBusReceiveEndpoint>>();

        try
        {
            if (Queue.RequiresSession == true)
            {
                _processor = MessageProcessor.ForSessionProcessor(CreateSessionProcessor(transport));
            }
            else
            {
                _processor = MessageProcessor.ForProcessor(CreateProcessor(transport));
            }

            await _processor.StartProcessingAsync(cancellationToken);

            if (Queue.AutoDeleteOnIdle is { } autoDeleteOnIdle)
            {
                var receiver = transport.ClientManager.CreateReceiver(Queue.Name);
                _heartbeat = new QueueHeartbeat(receiver, autoDeleteOnIdle, _logger, Queue.Name);
            }
        }
        catch
        {
            await _heartbeat.DisposeAsyncSafe();
            await _processor.DisposeAsyncSafe();

            _heartbeat = null;
            _processor = null;

            throw;
        }
    }

    private ServiceBusProcessor CreateProcessor(AzureServiceBusMessagingTransport asbTransport)
    {
        var processingToken = ProcessingToken;
        var maxAutoLockRenewal = Configuration.MaxAutoLockRenewalDuration ?? TimeSpan.FromMinutes(5);

        var options = new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = _maxConcurrentCalls,
            AutoCompleteMessages = false, // We handle ack/nack in middleware
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            PrefetchCount = _prefetchCount,
            MaxAutoLockRenewalDuration = maxAutoLockRenewal
        };

        _logger.ReceiveEndpointStarted(Queue.Name, _prefetchCount, _maxConcurrentCalls);

        var processor = asbTransport.ClientManager.CreateProcessor(Queue.Name, options);

        // The processor cancels args.CancellationToken only when it stops, and stopping lets handlers
        // drain, so the handler observes the processing token alone.
        processor.ProcessMessageAsync += async args =>
        {
            try
            {
                await ExecuteAsync(
                    static (ctx, state) => ctx.Features.GetOrSet<AzureServiceBusReceiveFeature>().SetNonSession(state),
                    args,
                    processingToken);
            }
            catch (Exception) when (processingToken.IsCancellationRequested)
            {
                // The message was not completed, so the broker redelivers it.
            }
        };
        processor.ProcessErrorAsync += OnProcessorError;
        return processor;
    }

    private ServiceBusSessionProcessor CreateSessionProcessor(AzureServiceBusMessagingTransport asbTransport)
    {
        var processingToken = ProcessingToken;
        var maxAutoLockRenewal = Configuration.MaxAutoLockRenewalDuration ?? TimeSpan.FromMinutes(5);
        var maxSessions = Configuration.MaxConcurrentSessions ?? _maxConcurrentCalls;
        var maxCallsPerSession = Configuration.MaxConcurrentCallsPerSession ?? 1;
        var prefetchCount = Configuration.PrefetchCount ?? maxSessions * maxCallsPerSession * 2;

        var options = new ServiceBusSessionProcessorOptions
        {
            MaxConcurrentSessions = maxSessions,
            MaxConcurrentCallsPerSession = maxCallsPerSession,
            AutoCompleteMessages = false,
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            PrefetchCount = prefetchCount,
            MaxAutoLockRenewalDuration = maxAutoLockRenewal
        };

        if (Configuration.SessionIdleTimeout is { } idle)
        {
            options.SessionIdleTimeout = idle;
        }

        _logger.SessionReceiveEndpointStarted(Queue.Name, prefetchCount, maxSessions, maxCallsPerSession);

        var sessionProcessor = asbTransport.ClientManager.CreateSessionProcessor(Queue.Name, options);
        sessionProcessor.ProcessMessageAsync += async args =>
        {
            using var cancellation = new SessionHandlerCancellation(this, processingToken, args.CancellationToken);

            try
            {
                await ExecuteAsync(
                    static (ctx, state) => ctx.Features.GetOrSet<AzureServiceBusReceiveFeature>().SetSession(state),
                    args,
                    cancellation.Token);
            }
            catch (Exception) when (cancellation.Token.IsCancellationRequested)
            {
                // The message was not completed, so the broker redelivers it.
            }
        };
        sessionProcessor.ProcessErrorAsync += OnProcessorError;
        return sessionProcessor;
    }

    /// <summary>
    /// Handles an error reported by either Azure Service Bus processor. Transient errors are logged
    /// as warnings and other errors as failures. Processor retry and connection recovery remain the
    /// responsibility of the Azure Service Bus SDK.
    /// </summary>
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

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the processor and the heartbeat. A temporary endpoint also removes the forwarding
    /// subscriptions that Mocha provisioned for its queue, even when the stop is cancelled.
    /// </summary>
    protected override async ValueTask OnStopAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        _isStopping = true;

        try
        {
            if (_processor is { } processor)
            {
                var stopping = processor.StopProcessingAsync(CancellationToken.None);

                if (!await WaitForProcessingAsync(stopping, cancellationToken))
                {
                    // The processor is disposed once its handlers return.
                    _processor = null;
                    _ = DisposeProcessorWhenStoppedAsync(processor, stopping);
                }
            }

            if (Configuration.IsTemporary)
            {
                await CleanupTemporaryResourcesAsync();
            }
        }
        finally
        {
            if (_heartbeat is not null)
            {
                try
                {
                    await _heartbeat.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _logger.HeartbeatDisposeFailed(ex, Queue.Name);
                }

                _heartbeat = null;
            }

            if (_processor is not null)
            {
                await DisposeProcessorAsync(_processor);
                _processor = null;
            }
        }
    }

    private async Task DisposeProcessorWhenStoppedAsync(MessageProcessor processor, Task stopping)
    {
        try
        {
            await stopping;
        }
        catch
        {
            // Disposing the processor stops it as well.
        }

        await DisposeProcessorAsync(processor);
    }

    private async ValueTask DisposeProcessorAsync(MessageProcessor processor)
    {
        try
        {
            await processor.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.ProcessorDisposeFailed(ex, Queue.Name);
        }
    }

    /// <summary>
    /// Removes convention forwarding subscriptions for this temporary endpoint, each within the cleanup
    /// timeout. The queue is removed when no provisioned declared subscriptions remain and all convention
    /// cleanup succeeds.
    /// </summary>
    private async Task CleanupTemporaryResourcesAsync()
    {
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;
        var cleanupTimeout = Transport.Options.Shutdown.CleanupTimeout;
        var canDeleteQueue = true;

        foreach (var subscription in Queue.Subscriptions)
        {
            if (!(subscription.AutoProvision ?? topology.AutoProvision))
            {
                continue;
            }

            if (subscription.Origin != TopologyOrigin.Convention)
            {
                canDeleteQueue = false;
                continue;
            }

            try
            {
                using var cleanupCts = new CancellationTokenSource(cleanupTimeout);
                await subscription.DeprovisionAsync(transport.ClientManager, cleanupCts.Token);
            }
            catch (Exception ex)
            {
                canDeleteQueue = false;
                _logger.ForwardingSubscriptionCleanupFailed(ex, subscription.Source.Name, subscription.Name);
            }
        }

        if (!canDeleteQueue || !(Queue.AutoProvision ?? topology.AutoProvision))
        {
            return;
        }

        try
        {
            using var cleanupCts = new CancellationTokenSource(cleanupTimeout);
            await Queue.DeprovisionAsync(transport.ClientManager, cleanupCts.Token);
        }
        catch (Exception ex)
        {
            _logger.TemporaryQueueCleanupFailed(ex, Queue.Name);
        }
    }

    /// <summary>
    /// The cancellation of a session message handler. It is cancelled when the endpoint cancels
    /// in-flight messages, or when the session processor cancels the handler, for example because the
    /// session lock was lost, while the endpoint is not stopping.
    /// </summary>
    private sealed class SessionHandlerCancellation : IDisposable
    {
        private readonly AzureServiceBusReceiveEndpoint _endpoint;
        private readonly CancellationTokenSource _source;
        private readonly CancellationTokenRegistration _sessionRegistration;

        public SessionHandlerCancellation(
            AzureServiceBusReceiveEndpoint endpoint,
            CancellationToken processingToken,
            CancellationToken sessionToken)
        {
            _endpoint = endpoint;
            _source = CancellationTokenSource.CreateLinkedTokenSource(processingToken);
            _sessionRegistration = sessionToken.UnsafeRegister(
                static state => ((SessionHandlerCancellation)state!).OnSessionCancelled(),
                this);
        }

        public CancellationToken Token => _source.Token;

        private void OnSessionCancelled()
        {
            // Stopping the processor cancels its handlers, which drain until the endpoint cancels them.
            if (!_endpoint._isStopping)
            {
                _source.Cancel();
            }
        }

        public void Dispose()
        {
            _sessionRegistration.Dispose();
            _source.Dispose();
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
        LogLevel.Information,
        "Azure Service Bus receive endpoint '{EntityPath}' started (PrefetchCount: {PrefetchCount}, MaxConcurrentCalls: {MaxConcurrentCalls})")]
    public static partial void ReceiveEndpointStarted(
        this ILogger logger,
        string entityPath,
        int prefetchCount,
        int maxConcurrentCalls);

    [LoggerMessage(
        LogLevel.Information,
        "Azure Service Bus session receive endpoint '{EntityPath}' started (PrefetchCount: {PrefetchCount}, MaxConcurrentSessions: {MaxConcurrentSessions}, MaxConcurrentCallsPerSession: {MaxConcurrentCallsPerSession})")]
    public static partial void SessionReceiveEndpointStarted(
        this ILogger logger,
        string entityPath,
        int prefetchCount,
        int maxConcurrentSessions,
        int maxConcurrentCallsPerSession);

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

    [LoggerMessage(
        LogLevel.Warning,
        "Failed to delete Azure Service Bus forwarding subscription '{SubscriptionName}' on topic '{TopicName}'")]
    public static partial void ForwardingSubscriptionCleanupFailed(
        this ILogger logger,
        Exception exception,
        string topicName,
        string subscriptionName);

    [LoggerMessage(LogLevel.Warning, "Failed to delete temporary Azure Service Bus queue '{QueueName}'")]
    public static partial void TemporaryQueueCleanupFailed(
        this ILogger logger,
        Exception exception,
        string queueName);

    [LoggerMessage(
        LogLevel.Warning,
        "Failed to dispose the keep-alive heartbeat for Azure Service Bus queue '{QueueName}'")]
    public static partial void HeartbeatDisposeFailed(
        this ILogger logger,
        Exception exception,
        string queueName);

    [LoggerMessage(
        LogLevel.Warning,
        "Failed to dispose the Azure Service Bus processor for queue '{QueueName}'")]
    public static partial void ProcessorDisposeFailed(
        this ILogger logger,
        Exception exception,
        string queueName);
}
