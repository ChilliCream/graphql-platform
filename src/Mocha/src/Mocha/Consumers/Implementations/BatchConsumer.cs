using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Features;
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
/// </remarks>
internal sealed class BatchConsumer<THandler, TEvent>(
    Action<IConsumerDescriptor>? configure = null)
    : Consumer where THandler : IBatchEventHandler<TEvent>
{
    private BatchCollector<TEvent> _collector = null!;
    private Channel<MessageBatch<TEvent>> _channel = null!;
    private ChannelProcessor<MessageBatch<TEvent>> _processor = null!;
    private IServiceProvider _applicationServices = null!;
    private ILogger _logger = null!;
    private MessageType? _itemMessageType;

    protected override void Configure(IConsumerDescriptor descriptor)
    {
        descriptor
            .Name(typeof(THandler).Name)
            .AddRoute(r => r.MessageType(typeof(TEvent)).Kind(InboundRouteKind.Subscribe));

        configure?.Invoke(descriptor);
    }

    protected override void OnAfterInitialize(IMessagingSetupContext context)
    {
        base.OnAfterInitialize(context);
        SetIdentity(typeof(THandler));

        var options = Configuration!.Features.Get<BatchOptions>() ?? new BatchOptions();
        options.Validate();

        _applicationServices = context.Services.GetRequiredService<IRootServiceProviderAccessor>().ServiceProvider;
        _logger = context.Services.GetRequiredService<ILogger<BatchConsumer<THandler, TEvent>>>();

        var timeProvider = context.Services.GetRequiredService<TimeProvider>();

        _channel =
            Channel.CreateBounded<MessageBatch<TEvent>>(
                new BoundedChannelOptions(options.MaxConcurrentBatches)
                {
                    SingleReader = options.MaxConcurrentBatches == 1
                });

        _processor = new ChannelProcessor<MessageBatch<TEvent>>(
            _channel.Reader.ReadAllAsync,
            ProcessBatchAsync,
            options.MaxConcurrentBatches);

        _collector = new BatchCollector<TEvent>(options, batch => _channel.Writer.WriteAsync(batch), timeProvider);
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

        var entry = await _collector.Add(batchContext);
        await entry.Task;
    }

    private async Task ProcessBatchAsync(MessageBatch<TEvent> batch, CancellationToken cancellationToken)
    {
        try
        {
            _logger.DispatchingBatch(batch.Count, batch.CompletionMode);

            await using var scope = _applicationServices.CreateAsyncScope();

            var batchContext = new BatchConsumeContext<TEvent>(
                batch,
                scope.ServiceProvider,
                batch.GetContext(0),
                Guid.NewGuid().ToString(),
                _itemMessageType,
                cancellationToken);

            var consumerFeature = batchContext.Features.GetOrSet<ReceiveConsumerFeature>();
            consumerFeature.CurrentConsumer = this;

            var accessor = scope.ServiceProvider.GetRequiredService<ConsumeContextAccessor>();
            var previousContext = accessor.Context;
            accessor.Context = batchContext;

            try
            {
                await Pipeline(batchContext);
            }
            finally
            {
                accessor.Context = previousContext;
            }

            foreach (var entry in batch.Entries)
            {
                entry.Complete();
            }
        }
        catch (OperationCanceledException)
        {
            // Handler observed cancellation - cancel all entries so per-message pipelines
            // unblock for NACK/redelivery
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

    public override ConsumerDescription Describe()
    {
        return new ConsumerDescription(Name, DescriptionHelpers.GetTypeName(Identity), Identity.FullName, null, true);
    }

    public override async ValueTask DisposeAsync()
    {
        await _collector.DisposeAsync();

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

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Debug, "Dispatching batch of {BatchSize} messages (mode: {CompletionMode}).")]
    public static partial void DispatchingBatch(this ILogger logger, int batchSize, BatchCompletionMode completionMode);

    [LoggerMessage(LogLevel.Error, "Batch handler failed for batch of {BatchSize} messages.")]
    public static partial void BatchHandlerFailed(this ILogger logger, Exception exception, int batchSize);

    [LoggerMessage(LogLevel.Error, "Failed to fault entry for message.")]
    public static partial void FaultingEntryFailed(this ILogger logger, Exception exception);
}
