using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Confluent.Kafka;

namespace HotChocolate.Fusion.Subscriptions.Kafka;

internal sealed class KafkaEventStreamBroker(KafkaEventStreamOptions options)
    : IEventStreamBroker
{
    // Seeding runs inside the partitions-assigned callback, so a slow broker must not stall the
    // assignment. Each watermark query is bounded by this per-query timeout, and the whole seeding
    // loop shares an overall budget so that many partitions cannot compound into a long stall.
    private static readonly TimeSpan s_watermarkQueryTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan s_watermarkSeedingBudget = TimeSpan.FromSeconds(5);

    private readonly List<SubscriptionSession> _sessions = [];
    private bool _disposed;

    public IAsyncEnumerable<EventMessage> SubscribeAsync(
        ISubscriptionFieldContext context,
        string[] topics,
        string? cursor,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(topics);
        ArgumentOutOfRangeException.ThrowIfZero(topics.Length);

        for (var i = 0; i < topics.Length; i++)
        {
            ArgumentException.ThrowIfNullOrEmpty(topics[i]);
        }

        var resumeMap = string.IsNullOrEmpty(cursor)
            ? null
            : KafkaCompositeCursorFormatter.Parse(cursor, topics);
        return SubscribeCoreAsync(topics, resumeMap, context.RequiresCursor, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        SubscriptionSession[] sessions;

        lock (_sessions)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            sessions = [.. _sessions];
            _sessions.Clear();
        }

        for (var i = 0; i < sessions.Length; i++)
        {
            sessions[i].Cancel();
        }

        await DisposeSessionsAsync(sessions).ConfigureAwait(false);
    }

    private async IAsyncEnumerable<EventMessage> SubscribeCoreAsync(
        string[] topics,
        Dictionary<TopicPartition, long>? resumeMap,
        bool requiresCursor,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = options.CreateMessageChannel();
        var reader = channel.Reader;
        var writer = channel.Writer;
        var consumerConfig = CreateConsumerConfig(options);
        var session = CreateSession(cancellationToken);
        var pumpTask = StartPump(topics, resumeMap, requiresCursor, consumerConfig, writer, session.Token);
        session.SetPumpTask(pumpTask);

        try
        {
            while (true)
            {
                EventMessage? message = null;

                try
                {
                    if (!await reader.WaitToReadAsync(session.Token).ConfigureAwait(false))
                    {
                        break;
                    }

                    if (!reader.TryRead(out message))
                    {
                        continue;
                    }
                }
                catch (OperationCanceledException) when (session.Token.IsCancellationRequested)
                {
                    break;
                }

                yield return message;
            }
        }
        finally
        {
            session.Cancel();
            await WaitForPumpAsync(pumpTask).ConfigureAwait(false);
            RemoveSession(session);
            channel.Writer.TryComplete();
            DisposeQueuedMessages(channel);
            session.Dispose();
        }
    }

    private SubscriptionSession CreateSession(CancellationToken cancellationToken)
    {
        var session = new SubscriptionSession(cancellationToken);

        lock (_sessions)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _sessions.Add(session);
        }

        return session;
    }

    private void RemoveSession(SubscriptionSession session)
    {
        lock (_sessions)
        {
            _sessions.Remove(session);
        }
    }

    private static async Task DisposeSessionsAsync(SubscriptionSession[] sessions)
    {
        for (var i = 0; i < sessions.Length; i++)
        {
            await sessions[i].WaitForPumpAsync().ConfigureAwait(false);
        }
    }

    private static ConsumerConfig CreateConsumerConfig(KafkaEventStreamOptions options)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            GroupId = options.GroupIdPrefix + Guid.NewGuid().ToString("N"),
            AutoOffsetReset = options.AutoOffsetReset,
            EnableAutoCommit = false,
            EnablePartitionEof = false,
            QueuedMinMessages = options.ConsumerQueuedMinMessages,
            QueuedMaxMessagesKbytes = options.ConsumerQueuedMaxMessagesKbytes,
            FetchQueueBackoffMs = options.ConsumerFetchQueueBackoffMs
        };

        if (options.SecurityProtocol is { } securityProtocol)
        {
            config.SecurityProtocol = securityProtocol;
        }

        if (options.SaslMechanism is { } saslMechanism)
        {
            config.SaslMechanism = saslMechanism;
        }

        if (!string.IsNullOrWhiteSpace(options.SaslUsername))
        {
            config.SaslUsername = options.SaslUsername;
        }

        if (!string.IsNullOrWhiteSpace(options.SaslPassword))
        {
            config.SaslPassword = options.SaslPassword;
        }

        if (!string.IsNullOrWhiteSpace(options.SslCaLocation))
        {
            config.SslCaLocation = options.SslCaLocation;
        }

        if (!string.IsNullOrWhiteSpace(options.SslCertificateLocation))
        {
            config.SslCertificateLocation = options.SslCertificateLocation;
        }

        if (!string.IsNullOrWhiteSpace(options.SslKeyLocation))
        {
            config.SslKeyLocation = options.SslKeyLocation;
        }

        if (options.EnableSslCertificateVerification is { } enableSslCertificateVerification)
        {
            config.EnableSslCertificateVerification = enableSslCertificateVerification;
        }

        if (options.ConfigureConsumer is { } configure)
        {
            config = configure(config);
        }

        return config;
    }

    private Task StartPump(
        string[] topics,
        Dictionary<TopicPartition, long>? resumeMap,
        bool requiresCursor,
        ConsumerConfig consumerConfig,
        ChannelWriter<EventMessage> writer,
        CancellationToken cancellationToken)
    {
        var state = new PumpState(
            topics,
            resumeMap,
            requiresCursor,
            options.AutoOffsetReset,
            consumerConfig,
            writer,
            cancellationToken,
            options.OnPartitionsAssigned);

        return Task.Factory.StartNew(
            static state => RunPumpAsync((PumpState)state!),
            state,
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default).Unwrap();
    }

    private static async Task RunPumpAsync(PumpState state)
    {
        IConsumer<Ignore, byte[]>? consumer = null;

        // The map tracks the next offset per (topic, partition) whenever the stream exposes a
        // cursor or an inbound cursor was supplied. It starts as a copy of the inbound cursor so
        // silent partitions keep their position and a rebalance resumes from tracked offsets
        // rather than the inbound cursor. The cache exists only when cursors are emitted; an
        // inbound-only resume seeks but emits none. The map is owned by this pump thread and
        // needs no synchronization.
        var cursorMap = CreateCursorMap(state.RequiresCursor, state.ResumeMap);
        var cursorCache = state.RequiresCursor ? new SingleEntryCursorCache() : null;

        try
        {
            consumer = new ConsumerBuilder<Ignore, byte[]>(state.ConsumerConfig)
                .SetPartitionsAssignedHandler((assignedConsumer, partitions) =>
                {
                    // Compute the start offsets first, then signal readiness. The handler runs on
                    // every rebalance, so the barrier a test waits on must mean "assigned and
                    // seeded" rather than merely "assigned", which would let a produced event race
                    // ahead of seeding.
                    var offsets = ResolveStartOffsets(
                        assignedConsumer,
                        partitions,
                        state.ResumeMap,
                        cursorMap,
                        state.AutoOffsetReset);
                    state.OnPartitionsAssigned?.Invoke(partitions);
                    return offsets;
                })
                .Build();

            // A unique group id per Subscribe call gives every GraphQL subscriber its own fan-out
            // stream instead of Kafka's competing-consumer load balancing. Starting positions for
            // every assigned partition are chosen in the partitions-assigned handler, which avoids a
            // single-partition Assign and lets multi-topic and multi-partition subscriptions resume.
            consumer.Subscribe(state.Topics);

            while (!state.CancellationToken.IsCancellationRequested)
            {
                ConsumeResult<Ignore, byte[]>? result;

                try
                {
                    result = consumer.Consume(state.CancellationToken);
                }
                catch (OperationCanceledException) when (state.CancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ConsumeException ex) when (!ex.Error.IsFatal)
                {
                    continue;
                }
                catch (ConsumeException)
                {
                    break;
                }

                if (result is null || result.IsPartitionEOF)
                {
                    continue;
                }

                var outcome = await EmitMessageAsync(
                    state.Writer,
                    cursorMap,
                    cursorCache,
                    state.RequiresCursor,
                    result.TopicPartition,
                    result.Offset.Value,
                    result.Message.Value ?? [],
                    state.CancellationToken)
                    .ConfigureAwait(false);

                if (outcome is WriteOutcome.Closed)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (state.CancellationToken.IsCancellationRequested)
        {
            // Graceful cancellation requested, exit the pump loop.
        }
        catch (Exception ex)
        {
            state.Writer.TryComplete(ex);
        }
        finally
        {
            if (consumer is not null)
            {
                try
                {
                    consumer.Close();
                }
                catch (Exception) when (state.CancellationToken.IsCancellationRequested)
                {
                }
                catch (KafkaException)
                {
                }
                finally
                {
                    consumer.Dispose();
                }
            }

            state.Writer.TryComplete();
        }
    }

    /// <summary>
    /// Creates the progress map that tracks the next offset to read per (topic, partition).
    /// The map is seeded from <paramref name="resumeMap"/> when a resume cursor was supplied,
    /// empty when the stream exposes a cursor without one, and null when neither applies,
    /// in which case no progress is tracked.
    /// </summary>
    internal static Dictionary<TopicPartition, long>? CreateCursorMap(
        bool requiresCursor,
        IReadOnlyDictionary<TopicPartition, long>? resumeMap)
    {
        if (resumeMap is not null)
        {
            return [with(resumeMap)];
        }

        return requiresCursor ? [] : null;
    }

    internal static async ValueTask<WriteOutcome> EmitMessageAsync(
        ChannelWriter<EventMessage> writer,
        Dictionary<TopicPartition, long>? cursorMap,
        SingleEntryCursorCache? cursorCache,
        bool emitCursor,
        TopicPartition topicPartition,
        long offset,
        byte[] body,
        CancellationToken cancellationToken)
    {
        // When the stream exposes no cursor, suppress emission by formatting the body only, even
        // though the progress map may still advance to honor an inbound resume across a rebalance.
        // The explicit flag, not the cache, is the emission gate: the multi-partition codec emits
        // cursors without consulting the cache, so only this flag suppresses emission on all paths.
        var eventMessage = emitCursor
            ? CreateMessage(body, topicPartition, offset, cursorMap, cursorCache)
            : CreateMessage(body, topicPartition, offset, cursorMap: null, cursorCache: null);
        var outcome = await WriteMessageAsync(writer, eventMessage, cancellationToken)
            .ConfigureAwait(false);

        if (outcome is WriteOutcome.Delivered && cursorMap is not null)
        {
            // A successful channel write is the sole signal that advances the committed cursor. It is
            // committed here on the pump thread that exclusively owns the cursor map, and no shared,
            // reader-mutated message state is inspected, so the classification cannot race the
            // consumer draining the channel.
            cursorMap[topicPartition] = offset + 1;
        }

        return outcome;
    }

    private static async ValueTask<WriteOutcome> WriteMessageAsync(
        ChannelWriter<EventMessage> writer,
        EventMessage eventMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            // A successful write is delivered: TryWrite returning true or WriteAsync completing both
            // mean the channel accepted the message. Only a completed or closed channel is not
            // delivered. A bounded Drop-mode channel may still discard the message after accepting it,
            // which is the caller's opted-in at-most-once behavior, not a failure to deliver.
            if (writer.TryWrite(eventMessage))
            {
                return WriteOutcome.Delivered;
            }

            await writer.WriteAsync(eventMessage, cancellationToken);
            return WriteOutcome.Delivered;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            eventMessage.Dispose();
            return WriteOutcome.Closed;
        }
        catch (ChannelClosedException)
        {
            eventMessage.Dispose();
            return WriteOutcome.Closed;
        }
    }

    private static async Task WaitForPumpAsync(Task pumpTask)
    {
        try
        {
            await pumpTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex) when (ex is ObjectDisposedException or KafkaException)
        {
        }
    }

    private static IEnumerable<TopicPartitionOffset> ResolveStartOffsets(
        IConsumer<Ignore, byte[]> consumer,
        List<TopicPartition> partitions,
        Dictionary<TopicPartition, long>? resumeMap,
        Dictionary<TopicPartition, long>? cursorMap,
        AutoOffsetReset autoOffsetReset)
    {
        var offsets = new List<TopicPartitionOffset>(partitions.Count);
        var seedingStart = Stopwatch.GetTimestamp();

        foreach (var partition in partitions)
        {
            offsets.Add(
                new TopicPartitionOffset(
                    partition,
                    ResolveStartOffset(
                        consumer,
                        partition,
                        resumeMap,
                        cursorMap,
                        autoOffsetReset,
                        GetRemainingQueryTimeout(seedingStart))));
        }

        return offsets;
    }

    // Bounds a single watermark query by both the per-query timeout and the seeding loop's overall
    // budget. Once the budget is spent the remaining partitions get a zero timeout, which skips the
    // watermark query and falls back so that a slow broker cannot stall the assignment indefinitely.
    private static TimeSpan GetRemainingQueryTimeout(long seedingStart)
    {
        var remaining = s_watermarkSeedingBudget - Stopwatch.GetElapsedTime(seedingStart);

        if (remaining <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return remaining < s_watermarkQueryTimeout ? remaining : s_watermarkQueryTimeout;
    }

    private static Offset ResolveStartOffset(
        IConsumer<Ignore, byte[]> consumer,
        TopicPartition partition,
        Dictionary<TopicPartition, long>? resumeMap,
        Dictionary<TopicPartition, long>? cursorMap,
        AutoOffsetReset autoOffsetReset,
        TimeSpan queryTimeout)
    {
        // A partition's start offset is resolved only the first time it is assigned. Once the cursor
        // map tracks it (from the inbound cursor, an earlier assignment, or a delivered message), a
        // later re-assignment during a rebalance resumes from the tracked next offset instead of
        // re-querying the watermark and seeking forward, which would silently skip the events in
        // between. That seed-once decision is TryResolveTrackedOffset, which takes no consumer, so a
        // tracked or inbound-resume partition never touches the watermark.
        if (TryResolveTrackedOffset(partition, resumeMap, cursorMap, autoOffsetReset, out var resolved))
        {
            return resolved;
        }

        if (cursorMap is not null)
        {
            // Fresh resumable subscription: seed the partition's baseline the first time it is
            // assigned (not lazily on first message) so that a partition which then stays silent
            // still resumes from its subscribe-time position and does not lose gap events.
            return SeedFreshPartition(consumer, partition, autoOffsetReset, cursorMap, queryTimeout);
        }

        // Fresh subscription without cursor tracking: fall back to the configured AutoOffsetReset
        // behavior.
        return Offset.Unset;
    }

    /// <summary>
    /// Resolves the start offset for a partition whose position is already known, either tracked
    /// in the cursor map or carried by the inbound resume cursor. Returns <c>false</c> when the
    /// partition is untracked and must be seeded fresh. The resolution never queries the broker,
    /// so a tracked partition cannot be re-seeded past unread events on a rebalance.
    /// </summary>
    internal static bool TryResolveTrackedOffset(
        TopicPartition partition,
        IReadOnlyDictionary<TopicPartition, long>? resumeMap,
        IReadOnlyDictionary<TopicPartition, long>? cursorMap,
        AutoOffsetReset autoOffsetReset,
        out Offset offset)
    {
        if (cursorMap is not null && cursorMap.TryGetValue(partition, out var tracked))
        {
            offset = new Offset(tracked);
            return true;
        }

        if (resumeMap is not null)
        {
            // Inbound resume: honor the stored position independently of whether the output cursor is
            // tracked. The map stores the next offset to read, so we seek to it directly. A
            // (topic, partition) absent from the resume cursor was either never baselined (a seed
            // failure left no numeric position) or is genuinely new relative to the cursor. In both
            // cases the configured reset mode decides: Earliest replays from the beginning, Latest
            // starts at the live end and may skip events, consistent with SeedFreshPartition's own
            // Latest-to-High asymmetry. There is no numeric baseline to record, so the partition is
            // left absent from the map, which is self-stable.
            offset = resumeMap.TryGetValue(partition, out var next)
                ? new Offset(next)
                : autoOffsetReset == AutoOffsetReset.Earliest
                    ? Offset.Beginning
                    : Offset.End;
            return true;
        }

        offset = Offset.Unset;
        return false;
    }

    private static Offset SeedFreshPartition(
        IConsumer<Ignore, byte[]> consumer,
        TopicPartition partition,
        AutoOffsetReset autoOffsetReset,
        Dictionary<TopicPartition, long> cursorMap,
        TimeSpan queryTimeout)
    {
        if (queryTimeout > TimeSpan.Zero)
        {
            try
            {
                var watermarks = consumer.QueryWatermarkOffsets(partition, queryTimeout);

                // Latest starts at the live end (High is the offset the next produced record
                // receives) so only future events are delivered. Earliest starts at the earliest
                // retained event (Low) so a fresh subscribe replays history. Either way the seeded
                // baseline is the offset the first delivered message will carry, so the emitted cursor
                // resumes from the true start position.
                var baseline = autoOffsetReset == AutoOffsetReset.Earliest
                    ? watermarks.Low.Value
                    : watermarks.High.Value;
                cursorMap[partition] = baseline;
                return new Offset(baseline);
            }
            catch (KafkaException)
            {
                // The watermark could not be queried within the budget. Fall through to the reset
                // fallback below.
            }
        }

        // The watermark is unavailable (the query failed or the seeding budget is spent). For
        // Earliest, record a zero baseline and seek to the beginning so that a later rebalance
        // resolves this partition through the tracked-offset branch and replays from the start
        // instead of re-querying a watermark whose Low may have advanced past retained events: it
        // over-delivers but never loses. Offset zero is a valid non-negative next-offset, so a silent
        // partition folds cleanly into a composite cursor. For Latest there is no known numeric
        // position to record, so the partition seeds itself on its first delivered message; a Latest
        // partition that stays silent across a rebalance is re-seeded to the live end and may skip
        // events produced during the gap, which matches Latest's future-only, best-effort semantics.
        if (autoOffsetReset == AutoOffsetReset.Earliest)
        {
            cursorMap[partition] = 0;
            return Offset.Beginning;
        }

        return Offset.End;
    }

    private static EventMessage CreateMessage(
        ReadOnlySpan<byte> body,
        TopicPartition topicPartition,
        long offset,
        Dictionary<TopicPartition, long>? cursorMap,
        SingleEntryCursorCache? cursorCache)
    {
        if (cursorMap is null)
        {
            // The operation does not expose the resume cursor, so deliver the body without cursor
            // data, mirroring a non-resumable path.
            var bodyOnlyOwner = MemoryPool<byte>.Shared.Rent(body.Length);
            body.CopyTo(bodyOnlyOwner.Memory.Span);
            return new EventMessage(bodyOnlyOwner, 0..body.Length, 0..0);
        }

        // Emit a snapshot of the full map with this partition advanced past the current message, so
        // the client can resume every observed (topic, partition) pair from just after it. The
        // advance is applied to the shared map only temporarily here and committed for real only
        // after a successful channel write (see EmitMessageAsync), so a message the channel could not
        // accept never folds its offset into the shared cursor.
        var hadPrevious = cursorMap.TryGetValue(topicPartition, out var previous);
        var nextOffset = offset + 1;
        cursorMap[topicPartition] = nextOffset;

        try
        {
            // Fast path: the subscription observes exactly one (topic, partition). Its cursor prefix
            // (version, count, topic, partition) never changes, so it is cached once per session and
            // only the 8-byte offset is written per event. The output is byte-identical to the
            // general codec, so both paths share one wire format.
            if (cursorCache is not null && cursorMap.Count == 1)
            {
                var singleLength = cursorCache.GetFormattedLength(topicPartition);

                byte[]? singleRented = null;
                var singleCursor = singleLength <= 256
                    ? stackalloc byte[singleLength]
                    : singleRented = ArrayPool<byte>.Shared.Rent(singleLength);

                try
                {
                    cursorCache.Format(topicPartition, nextOffset, singleCursor[..singleLength]);
                    return EncodeMessage(body, singleCursor[..singleLength]);
                }
                finally
                {
                    if (singleRented is not null)
                    {
                        ArrayPool<byte>.Shared.Return(singleRented);
                    }
                }
            }

            var rawCursorLength = KafkaCompositeCursorFormatter.GetFormattedLength(cursorMap);

            byte[]? rented = null;
            var rawCursor = rawCursorLength <= 256
                ? stackalloc byte[rawCursorLength]
                : rented = ArrayPool<byte>.Shared.Rent(rawCursorLength);

            try
            {
                KafkaCompositeCursorFormatter.Format(cursorMap, rawCursor[..rawCursorLength]);
                return EncodeMessage(body, rawCursor[..rawCursorLength]);
            }
            finally
            {
                if (rented is not null)
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }
        finally
        {
            if (hadPrevious)
            {
                cursorMap[topicPartition] = previous;
            }
            else
            {
                cursorMap.Remove(topicPartition);
            }
        }
    }

    private static EventMessage EncodeMessage(ReadOnlySpan<byte> body, ReadOnlySpan<byte> rawCursor)
    {
        var cursorLength = GetBase64EncodedLength(rawCursor.Length);
        var owner = MemoryPool<byte>.Shared.Rent(body.Length + cursorLength);
        body.CopyTo(owner.Memory.Span);

        if (Base64.EncodeToUtf8(
                rawCursor,
                owner.Memory.Span[body.Length..],
                out _,
                out var bytesWritten) is not OperationStatus.Done)
        {
            owner.Dispose();
            throw new InvalidOperationException("The Kafka cursor could not be encoded.");
        }

        return new EventMessage(
            owner,
            0..body.Length,
            body.Length..(body.Length + bytesWritten));
    }

    private static int GetBase64EncodedLength(int length)
        => (length + 2) / 3 * 4;

    private static void DisposeQueuedMessages(Channel<EventMessage> channel)
    {
        while (channel.Reader.TryRead(out var message))
        {
            message.Dispose();
        }
    }

    private sealed class SubscriptionSession : IDisposable
    {
        private readonly CancellationTokenSource _cts;
        private Task? _pumpTask;

        public SubscriptionSession(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        public CancellationToken Token => _cts.Token;

        public void SetPumpTask(Task pumpTask)
        {
            _pumpTask = pumpTask;
        }

        public void Cancel()
        {
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }

        public Task WaitForPumpAsync()
            => _pumpTask is null ? Task.CompletedTask : KafkaEventStreamBroker.WaitForPumpAsync(_pumpTask);

        public void Dispose()
        {
            _cts.Dispose();
        }
    }

    internal enum WriteOutcome
    {
        // The channel accepted the message, so its offset advances the committed cursor on the pump
        // thread. A bounded Drop-mode channel may still discard the message after accepting it, which
        // yields at-most-once delivery where a resume skips the discarded offset.
        Delivered,

        // The channel was closed or the subscription was cancelled, so the pump must stop and the
        // committed cursor must not advance.
        Closed
    }

    private sealed record PumpState(
        string[] Topics,
        Dictionary<TopicPartition, long>? ResumeMap,
        bool RequiresCursor,
        AutoOffsetReset AutoOffsetReset,
        ConsumerConfig ConsumerConfig,
        ChannelWriter<EventMessage> Writer,
        CancellationToken CancellationToken,
        Action<IReadOnlyList<TopicPartition>>? OnPartitionsAssigned);
}
