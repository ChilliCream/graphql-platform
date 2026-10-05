using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Features;
using Mocha.Transport.Nats.Features;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace Mocha.Transport.Nats;

/// <summary>
/// NATS receive endpoint that consumes messages from a durable JetStream pull consumer.
/// </summary>
/// <param name="transport">The owning NATS transport instance.</param>
public sealed class NatsReceiveEndpoint(NatsMessagingTransport transport)
    : ReceiveEndpoint<NatsReceiveEndpointConfiguration>(transport)
{
    private static readonly INatsDeserialize<ReadOnlyMemory<byte>> s_deserializer =
        NatsRawSerializer<ReadOnlyMemory<byte>>.Default;

    private static readonly TimeSpan s_abortGrace = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan s_restartDelay = TimeSpan.FromSeconds(5);

    private CancellationTokenSource? _stopping;
    private CancellationTokenSource? _aborting;
    private Task? _consumeLoop;
    private ILogger _logger = null!;
    private string? _replySubject;
    private int _maxConcurrency = 1;

    /// <summary>
    /// Gets the durable consumer this endpoint reads from, or <see langword="null"/> for reply
    /// endpoints, which subscribe over core NATS rather than JetStream.
    /// </summary>
    public NatsConsumer? Consumer { get; private set; }

    /// <inheritdoc />
    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        NatsReceiveEndpointConfiguration configuration)
    {
        if (configuration.ConsumerName is null)
        {
            throw new InvalidOperationException("Consumer name is required.");
        }

        _maxConcurrency = Math.Clamp(
            configuration.MaxConcurrency ?? ReceiveEndpointConfiguration.Defaults.MaxConcurrency,
            1,
            int.MaxValue);
    }

    /// <inheritdoc />
    protected override void OnComplete(
        IMessagingConfigurationContext context,
        NatsReceiveEndpointConfiguration configuration)
    {
        if (Kind is ReceiveEndpointKind.Reply)
        {
            _replySubject = configuration.FilterSubjects.FirstOrDefault()
                ?? throw new InvalidOperationException("The reply endpoint has no subject.");

            Address = new Uri($"{Transport.Schema}:{NatsAddress.SubjectSegment}/{_replySubject}");

            Source = ((NatsMessagingTopology)Transport.Topology)
                .Subjects.FirstOrDefault(s => s.Subject == _replySubject)
                ?? throw new InvalidOperationException($"Reply subject '{_replySubject}' not found.");

            return;
        }

        var topology = (NatsMessagingTopology)Transport.Topology;

        Consumer =
            topology.Consumers.FirstOrDefault(c => c.Name == configuration.ConsumerName)
            ?? throw new InvalidOperationException($"Consumer '{configuration.ConsumerName}' not found.");

        Source = Consumer;
    }

    /// <inheritdoc />
    protected override async ValueTask OnStartAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        // Two tokens, because stopping and aborting are different things. Cancelling _stopping stops
        // pulling and lets the buffer drain; _aborting is only cancelled once shutdown has run out
        // of patience, and is what the handlers themselves observe.
        _logger = context.Services.GetRequiredService<ILogger<NatsReceiveEndpoint>>();

        _stopping = new CancellationTokenSource();
        _aborting = new CancellationTokenSource();

        if (Kind is ReceiveEndpointKind.Reply)
        {
            _consumeLoop = RunAsync(
                token => SubscribeRepliesAsync(_replySubject!, token, _aborting.Token),
                _stopping.Token);

            return;
        }

        if (Consumer is not { StreamName: { } streamName } consumer)
        {
            // Nothing to consume from. Provisioning fails start-up before this point, so reaching
            // here means the endpoint is inert and would otherwise be silently deaf.
            _logger.EndpointHasNoStream(Name, Consumer?.Name);

            return;
        }

        var jsConsumer = await transport.JetStream.GetConsumerAsync(
            streamName,
            consumer.Name,
            cancellationToken);

        _consumeLoop = RunAsync(
            token => ConsumeAsync(jsConsumer, token, _aborting.Token),
            _stopping.Token);
    }

    /// <summary>
    /// Keeps a consume loop running until the endpoint is asked to stop, restarting it after a
    /// failure.
    /// </summary>
    /// <param name="consume">Starts a consume loop bound to the supplied token.</param>
    /// <param name="stopping">Signals that the endpoint is stopping.</param>
    // Without this a terminal error leaves the endpoint alive but deaf, with nothing logged until
    // shutdown, because the loop is only awaited when the endpoint stops. NATS.Net recovers the
    // connection on its own, but not a consumer deleted or altered on the server.
    private async Task RunAsync(Func<CancellationToken, Task> consume, CancellationToken stopping)
    {
        while (!stopping.IsCancellationRequested)
        {
            try
            {
                await consume(stopping);

                return;
            }
            catch (OperationCanceledException) when (stopping.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.ConsumeLoopFailed(exception, Name, s_restartDelay.TotalSeconds);

                try
                {
                    await Task.Delay(s_restartDelay, stopping);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    /// <inheritdoc />
    protected override async ValueTask OnStopAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        if (_stopping is null)
        {
            return;
        }

        await _stopping.CancelAsync();

        if (_consumeLoop is not null)
        {
            try
            {
                // Bounded by the host's shutdown token: draining should finish in-flight work, not
                // hold shutdown open indefinitely behind a slow handler.
                await _consumeLoop.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await AbortAsync();
            }
        }

        _stopping.Dispose();
        _stopping = null;
        _aborting?.Dispose();
        _aborting = null;
        _consumeLoop = null;
    }

    /// <summary>
    /// Cancels the handlers still running once shutdown has run out of time, so they stop promptly
    /// instead of being left behind. Their messages are released for redelivery.
    /// </summary>
    private async Task AbortAsync()
    {
        await _aborting!.CancelAsync();

        try
        {
            await _consumeLoop!.WaitAsync(s_abortGrace, CancellationToken.None);
        }
        catch (TimeoutException)
        {
            // Nothing left to do: a handler is ignoring cancellation, and its message will be
            // redelivered once the acknowledgement deadline expires.
        }
        catch (OperationCanceledException)
        {
            // Expected: the loop unwound through the abort token.
        }
    }

    private async Task SubscribeRepliesAsync(
        string subject,
        CancellationToken stopping,
        CancellationToken processing)
    {
        // Replies are correlated over a core subscription, so the connection-level DropNewest
        // default would silently discard responses under load. Wait applies back pressure instead.
        var options = new NatsSubOpts
        {
            ChannelOpts = new NatsSubChannelOpts { FullMode = BoundedChannelFullMode.Wait }
        };

        var messages = transport.Connection.Connection.SubscribeAsync(
            subject,
            queueGroup: null,
            s_deserializer,
            options,
            stopping);

        await ForEachAsync(
            messages,
            stopping,
            message => ExecuteAsync(
                static (context, state) =>
                {
                    var feature = context.Features.GetOrSet<NatsReceiveFeature>();
                    feature.Headers = state.Headers;
                    feature.Body = state.Data;
                },
                message,
                processing));
    }

    private static int DeliveryCountOf(INatsJSMsg<ReadOnlyMemory<byte>> message)
    {
        var delivered = message.Metadata?.NumDelivered ?? 0;

        return delivered > int.MaxValue ? int.MaxValue : (int)delivered;
    }

    private async Task ConsumeAsync(
        INatsJSConsumer consumer,
        CancellationToken stopping,
        CancellationToken processing)
    {
        // DrainOnCancel turns stopping into a drain: no new messages are pulled, but everything
        // already buffered is still handled and acknowledged instead of being abandoned mid-flight.
        var options = new NatsJSConsumeOpts
        {
            MaxMsgs = PrefetchCount,
            DrainOnCancel = true
        };

        var ackProgressInterval = Consumer!.AckProgressInterval;

        await ForEachAsync(
            consumer.ConsumeAsync(s_deserializer, options, stopping),
            stopping,
            message => ExecuteAsync(
                static (context, state) =>
                {
                    var feature = context.Features.GetOrSet<NatsReceiveFeature>();
                    feature.Message = state.Message;
                    feature.Headers = state.Message.Headers;
                    feature.Body = state.Message.Data;
                    feature.DeliveryCount = DeliveryCountOf(state.Message);
                    feature.AckProgressInterval = state.AckProgressInterval;
                },
                (Message: message, AckProgressInterval: ackProgressInterval),
                processing));
    }

    /// <summary>
    /// Gets how many messages are buffered locally, which is what bounds parallel handling.
    /// </summary>
    /// <remarks>
    /// Bounded by concurrency rather than by <c>MaxAckPending</c>, since a buffered message is
    /// already counting against its acknowledgement deadline. Never exceeds the server ceiling.
    /// </remarks>
    private int PrefetchCount
        => (int)Math.Clamp(Math.Min(_maxConcurrency, Consumer!.MaxAckPending), 1, int.MaxValue);

    /// <summary>
    /// Runs <paramref name="handle"/> over the source, up to <see cref="_maxConcurrency"/> messages
    /// at a time, and awaits the in-flight handlers before returning.
    /// </summary>
    private async Task ForEachAsync<T>(
        IAsyncEnumerable<T> source,
        CancellationToken stopping,
        Func<T, ValueTask> handle)
    {
        try
        {
            // The parallel loop itself is deliberately not cancellable: cancelling the source is
            // what ends the enumeration, and the loop then has to run the drained messages to
            // completion rather than abandoning them.
            await Parallel.ForEachAsync(
                source,
                new ParallelOptions { MaxDegreeOfParallelism = _maxConcurrency },
                async (message, _) => await handle(message));
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested)
        {
            // Expected: the subscription unwound through cancellation rather than draining.
        }
    }
}
