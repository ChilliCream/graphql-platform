using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Middlewares;
using Mocha.Threading;

namespace Mocha;

/// <summary>
/// Consumer adapter for batch event handlers (<see cref="IBatchEventHandler{TEvent}"/>).
/// </summary>
/// <remarks>
/// Uses a TCS-based pattern to hold each per-message pipeline open until the batch handler
/// completes. This preserves existing middleware semantics (ACK, fault, circuit breaker)
/// without any modifications to the middleware chain.
/// Each receive endpoint collects and processes its own batches.
/// </remarks>
internal sealed class BatchConsumer<THandler, TEvent> : Consumer
    where THandler : class, IBatchEventHandler<TEvent>
{
    public BatchConsumer() : base(typeof(THandler)) { }

#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private readonly ConcurrentDictionary<ReceiveEndpoint, EndpointBatches> _endpoints = new();
    private BatchOptions _options = null!;
    private TimeProvider _timeProvider = null!;
    private IServiceProvider _applicationServices = null!;
    private ILogger _logger = null!;
    private MessageType? _itemMessageType;
    private bool _disposed;

    protected override void Configure(IConsumerDescriptor descriptor)
    {
        descriptor
            .Name(typeof(THandler).Name)
            .AddRoute(r => r.MessageType(typeof(TEvent)).Kind(InboundRouteKind.Subscribe));
    }

    protected override void OnAfterInitialize(IMessagingSetupContext context)
    {
        base.OnAfterInitialize(context);

        var options = Configuration!.Features.Get<BatchOptions>() ?? new BatchOptions();
        options.Validate();
        _options = options;

        _applicationServices = context.Services.GetRequiredService<IRootServiceProviderAccessor>().ServiceProvider;
        _logger = context.Services.GetRequiredService<ILogger<BatchConsumer<THandler, TEvent>>>();
        _timeProvider = context.Services.GetRequiredService<TimeProvider>();
        _itemMessageType = context.Messages.GetMessageType(typeof(TEvent));
    }

    protected override async ValueTask ConsumeAsync(IConsumeContext context)
    {
        var batchContext = (IBatchConsumeContext<TEvent>)context;
        var handler = context.Services.GetRequiredService<THandler>();
        await handler.HandleAsync(batchContext.Message, context.CancellationToken);
    }

    public override async ValueTask ProcessAsync(IReceiveContext context)
    {
        if (context is not IConsumeContext consumeContext)
        {
            throw ThrowHelper.InvalidHandlerContext();
        }

        // we dispose the consume context so the reference is free as soon as we leave consume
        // as then the context will be returned to the pool
        using var batchContext = new ConsumeContext<TEvent>(consumeContext);

        // force deserialization to keep the work outside of the batch and also verify that
        // the message can be deserialized before adding to the batch
        _ = batchContext.Message;

        var cancellationToken = context.CancellationToken;
        var collector = GetEndpointBatches(context.Endpoint).Collector;
        var entry = await collector.Add(batchContext);

        try
        {
            await entry.Task.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (collector.TryRemove(entry))
            {
                throw;
            }

            // A dispatched batch observes the same cancellation and uses the context until it completes.
            await entry.Task;
        }
    }

    private EndpointBatches GetEndpointBatches(ReceiveEndpoint endpoint)
    {
        if (_endpoints.TryGetValue(endpoint, out var batches))
        {
            return batches;
        }

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_endpoints.TryGetValue(endpoint, out batches))
            {
                batches = new EndpointBatches(_options, _timeProvider, ProcessBatchAsync);
                _endpoints[endpoint] = batches;
            }

            return batches;
        }
    }

    private async Task ProcessBatchAsync(MessageBatch<TEvent> batch, CancellationToken cancellationToken)
    {
        using var cancellation = LinkCancellation(batch, cancellationToken);
        var batchToken = cancellation.Token;

        try
        {
            batchToken.ThrowIfCancellationRequested();

            _logger.DispatchingBatch(batch.Count, batch.CompletionMode);

            // The batch has no receive scope of its own, so it gets one here, mirroring the scope
            // ReceiveEndpoint creates per receive context. Each consumer attempt then runs in a child scope.
            await using var scope = _applicationServices.CreateAsyncScope();

            var batchContext = new BatchConsumeContext<TEvent>(
                batch,
                scope.ServiceProvider,
                batch.GetContext(0),
                Guid.NewGuid().ToString(),
                _itemMessageType,
                batchToken);

            await Pipeline(batchContext);

            foreach (var entry in batch.Entries)
            {
                entry.Complete();
            }
        }
        catch (Exception) when (batchToken.IsCancellationRequested)
        {
            foreach (var entry in batch.Entries)
            {
                entry.Cancel();
            }
        }
        catch (Exception ex)
        {
            _logger.BatchHandlerFailed(ex, batch.Count);

            // Each entry gets its own wrapped exception to avoid shared mutation
            foreach (var entry in batch.Entries)
            {
                try
                {
                    entry.Fault(new BatchProcessingException("Batch handler failed.", ex));
                }
                catch (Exception faultEx)
                {
                    _logger.FaultingEntryFailed(faultEx);
                }
            }
        }
    }

    private static CancellationTokenSource LinkCancellation(
        MessageBatch<TEvent> batch,
        CancellationToken processorToken)
    {
        var first = batch.Entries[0].Context.CancellationToken;
        List<CancellationToken>? others = null;

        for (var i = 1; i < batch.Count; i++)
        {
            var token = batch.Entries[i].Context.CancellationToken;
            if (token != first && others?.Contains(token) != true)
            {
                (others ??= []).Add(token);
            }
        }

        if (others is null)
        {
            return CancellationTokenSource.CreateLinkedTokenSource(processorToken, first);
        }

        others.Add(processorToken);
        others.Add(first);
        return CancellationTokenSource.CreateLinkedTokenSource([.. others]);
    }

    public override ConsumerDescription Describe()
    {
        return new ConsumerDescription(
            Urn,
            Name,
            DescriptionHelpers.GetTypeName(Identity),
            Identity.FullName,
            null,
            true,
            Configuration?.Source);
    }

    public override async ValueTask DisposeAsync()
    {
        EndpointBatches[] endpoints;

        lock (_sync)
        {
            _disposed = true;
            endpoints = [.. _endpoints.Values];
        }

        foreach (var batches in endpoints)
        {
            await batches.DisposeAsync();
        }
    }

    /// <summary>
    /// The batch collector, channel and processor of one receive endpoint.
    /// </summary>
    private sealed class EndpointBatches : IAsyncDisposable
    {
        private readonly Channel<MessageBatch<TEvent>> _channel;
        private readonly ChannelProcessor<MessageBatch<TEvent>> _processor;

        public EndpointBatches(
            BatchOptions options,
            TimeProvider timeProvider,
            Func<MessageBatch<TEvent>, CancellationToken, Task> processBatch)
        {
            _channel = Channel.CreateBounded<MessageBatch<TEvent>>(
                new BoundedChannelOptions(options.MaxConcurrentBatches)
                {
                    SingleReader = options.MaxConcurrentBatches == 1
                });
            _processor = new ChannelProcessor<MessageBatch<TEvent>>(
                _channel.Reader.ReadAllAsync,
                processBatch,
                options.MaxConcurrentBatches);
            Collector = new BatchCollector<TEvent>(options, batch => _channel.Writer.WriteAsync(batch), timeProvider);
        }

        public BatchCollector<TEvent> Collector { get; }

        public async ValueTask DisposeAsync()
        {
            await Collector.DisposeAsync();

            _channel.Writer.Complete();
            await _processor.DisposeAsync();

            while (_channel.Reader.TryRead(out var batch))
            {
                foreach (var entry in batch.Entries)
                {
                    try
                    {
                        entry.Cancel();
                    }
                    catch
                    {
                        // Best-effort cancellation
                    }
                }
            }
        }
    }
}

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Debug, "Dispatching batch of {BatchSize} messages (mode: {CompletionMode}).")]
    public static partial void DispatchingBatch(this ILogger logger, int batchSize, BatchCompletionMode completionMode);

    [LoggerMessage(LogLevel.Error, "Batch handler failed for batch of {BatchSize} messages.")]
    public static partial void BatchHandlerFailed(this ILogger logger, Exception exception, int batchSize);

    [LoggerMessage(LogLevel.Error, "Failed to fault entry for message.")]
    public static partial void FaultingEntryFailed(this ILogger logger, Exception exception);
}
