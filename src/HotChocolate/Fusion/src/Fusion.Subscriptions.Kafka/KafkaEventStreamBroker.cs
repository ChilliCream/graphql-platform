using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Confluent.Kafka;

namespace HotChocolate.Fusion.Subscriptions.Kafka;

internal interface IPartitionWatermarkSource
{
    WatermarkOffsets Query(TopicPartition partition, TimeSpan timeout);
}

internal sealed class KafkaEventStreamBroker(KafkaEventStreamOptions options)
    : IEventStreamBroker
{
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

        var resumeState = string.IsNullOrEmpty(cursor)
            ? null
            : KafkaCompositeCursorFormatter.Parse(cursor, topics);
        return SubscribeCoreAsync(topics, resumeState, context.RequiresCursor, cancellationToken);
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
        KafkaResumeState? resumeState,
        bool requiresCursor,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = options.CreateMessageChannel();
        var reader = channel.Reader;
        var writer = channel.Writer;
        var consumerConfig = CreateConsumerConfig(options);
        var session = CreateSession(cancellationToken);
        var pumpTask = StartPump(topics, resumeState, requiresCursor, consumerConfig, writer, session.Token);
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

        if (config.PartitionAssignmentStrategy is PartitionAssignmentStrategy.CooperativeSticky)
        {
            throw new InvalidOperationException(
                "The Kafka event stream broker requires an eager partition assignment strategy. "
                + "A cooperative strategy (CooperativeSticky) is not supported because lossless resume "
                + "depends on a full re-assignment of every partition on each rebalance.");
        }

        return config;
    }

    private Task StartPump(
        string[] topics,
        KafkaResumeState? resumeState,
        bool requiresCursor,
        ConsumerConfig consumerConfig,
        ChannelWriter<EventMessage> writer,
        CancellationToken cancellationToken)
    {
        var state = new PumpState(
            topics,
            resumeState,
            requiresCursor,
            options.AutoOffsetReset,
            options.SeedingQueryTimeout,
            options.SeedingDeadline,
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

        // The map tracks the next offset per (topic, partition) whenever the stream emits a cursor
        // or a resume cursor was supplied. It starts as a copy of the resume cursor's offsets so
        // silent partitions keep their position and a rebalance resumes from tracked offsets. The
        // cache exists only when cursors are emitted. The map is owned by this pump thread and
        // needs no synchronization.
        var cursorMap = CreateCursorMap(state.RequiresCursor, state.ResumeState);
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
                    var watermarkSource = new ConsumerWatermarkSource(assignedConsumer);
                    var offsets = ResolveStartOffsets(
                        watermarkSource,
                        partitions,
                        state.ResumeState,
                        cursorMap,
                        state.AutoOffsetReset,
                        state.SeedingQueryTimeout,
                        state.SeedingDeadline);
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
    /// Creates the progress map that tracks the next offset to read per (topic, partition). The map
    /// is a copy of the resume cursor's offsets when a resume cursor was supplied, empty when the
    /// stream exposes a cursor without one, and null when neither applies, in which case no progress
    /// is tracked.
    /// </summary>
    internal static Dictionary<TopicPartition, long>? CreateCursorMap(
        bool requiresCursor,
        KafkaResumeState? resumeState)
    {
        if (resumeState is not null)
        {
            return new Dictionary<TopicPartition, long>(resumeState.Offsets);
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

    // Resolves the start offset for every assigned partition. Runs on the pump thread inside the
    // partitions-assigned callback. A resume validates against a partition-count shrink first, then
    // every partition is classified without a broker call: a tracked partition resumes from its
    // stored next offset, a partition absent from a dense resume is a newly grown partition and
    // starts at the beginning, and a fresh cursor-tracked subscribe with no resume is seeded from
    // the broker watermark. A non-resumable subscription leaves each partition Unset for the
    // configured AutoOffsetReset.
    internal static IEnumerable<TopicPartitionOffset> ResolveStartOffsets(
        IPartitionWatermarkSource watermarks,
        List<TopicPartition> assigned,
        KafkaResumeState? resumeState,
        Dictionary<TopicPartition, long>? cursorMap,
        AutoOffsetReset autoOffsetReset,
        TimeSpan seedingQueryTimeout,
        TimeSpan seedingDeadline)
    {
        // Phase 0: resume only. A full EAGER re-assign gives the complete live partition set, so a
        // topic that lost partitions relative to the minted cursor is detected here.
        if (resumeState is not null)
        {
            ValidateNoPartitionShrink(assigned, resumeState);
        }

        var results = new Dictionary<TopicPartition, Offset>(assigned.Count);
        var needsSeed = new List<TopicPartition>();

        // Phase 1: classify, no broker calls.
        foreach (var partition in assigned)
        {
            if (cursorMap?.ContainsKey(partition) == true)
            {
                // Tracked (from the resume copy or an earlier assignment): seed once, never re-query.
                results[partition] = new Offset(cursorMap![partition]);
            }
            else if (resumeState is not null)
            {
                // Absent from a dense resume means a genuinely new (grown) partition. It starts at
                // the beginning (offset 0 is exact for a just-created partition), independent of the
                // reset mode.
                cursorMap![partition] = 0;
                results[partition] = Offset.Beginning;
            }
            else if (cursorMap is not null)
            {
                // Fresh cursor-tracked subscribe with no resume: seed from the broker watermark.
                needsSeed.Add(partition);
            }
            else
            {
                // Non-resumable subscription: fall back to the configured AutoOffsetReset.
                results[partition] = Offset.Unset;
            }
        }

        // Phase 2: seed the fresh partitions, all-or-nothing.
        if (needsSeed.Count > 0)
        {
            SeedFreshPartitions(
                watermarks,
                needsSeed,
                cursorMap!,
                autoOffsetReset,
                seedingQueryTimeout,
                seedingDeadline,
                results);
        }

        var offsets = new List<TopicPartitionOffset>(assigned.Count);
        foreach (var partition in assigned)
        {
            offsets.Add(new TopicPartitionOffset(partition, results[partition]));
        }

        return offsets;
    }

    // Rejects a resume whose subscription lost partitions for any topic. A live assigned partition
    // count below the minted count (including zero, meaning the topic was deleted) means the cursor
    // can no longer be honored positionally.
    internal static void ValidateNoPartitionShrink(
        List<TopicPartition> assigned,
        KafkaResumeState resumeState)
    {
        foreach (var (topic, minted) in resumeState.MintedPartitionCounts)
        {
            var liveCount = 0;

            foreach (var partition in assigned)
            {
                if (string.Equals(partition.Topic, topic, StringComparison.Ordinal))
                {
                    liveCount++;
                }
            }

            if (liveCount < minted)
            {
                throw new InvalidEventMessageCursorException();
            }
        }
    }

    // Seeds the start offset for each fresh cursor-tracked partition by querying the broker
    // watermark. Latest starts at the live end (High), Earliest at the earliest retained event
    // (Low). Queries are retried until the seeding deadline is spent; a partition that still has no
    // position causes the whole seeding to fail so nothing is partially assigned.
    private static void SeedFreshPartitions(
        IPartitionWatermarkSource watermarks,
        List<TopicPartition> pending,
        Dictionary<TopicPartition, long> cursorMap,
        AutoOffsetReset autoOffsetReset,
        TimeSpan queryTimeout,
        TimeSpan deadline,
        Dictionary<TopicPartition, Offset> results)
    {
        var start = Stopwatch.GetTimestamp();
        // Work on a mutable copy so completed partitions can be removed as they succeed.
        var remaining = new List<TopicPartition>(pending);

        while (remaining.Count > 0)
        {
            var budgetLeft = deadline - Stopwatch.GetElapsedTime(start);
            if (budgetLeft <= TimeSpan.Zero)
            {
                break;
            }

            var madeProgress = false;

            for (var i = remaining.Count - 1; i >= 0; i--)
            {
                budgetLeft = deadline - Stopwatch.GetElapsedTime(start);
                if (budgetLeft <= TimeSpan.Zero)
                {
                    break;
                }

                var perQuery = budgetLeft < queryTimeout ? budgetLeft : queryTimeout;
                var partition = remaining[i];

                try
                {
                    var watermark = watermarks.Query(partition, perQuery);
                    var baseline = autoOffsetReset == AutoOffsetReset.Earliest
                        ? watermark.Low.Value
                        : watermark.High.Value;
                    cursorMap[partition] = baseline;
                    results[partition] = new Offset(baseline);
                    remaining.RemoveAt(i);
                    madeProgress = true;
                }
                catch (KafkaException)
                {
                    // Leave this partition pending and retry on a later pass.
                }
            }

            if (!madeProgress && remaining.Count > 0)
            {
                budgetLeft = deadline - Stopwatch.GetElapsedTime(start);
                if (budgetLeft <= TimeSpan.Zero)
                {
                    break;
                }

                var nap = budgetLeft < TimeSpan.FromMilliseconds(25)
                    ? budgetLeft
                    : TimeSpan.FromMilliseconds(25);
                Thread.Sleep(nap);
            }
        }

        if (remaining.Count > 0)
        {
            var names = string.Join(
                ", ",
                remaining.Select(p => $"{p.Topic}[{p.Partition.Value}]"));
            throw new EventStreamSeedingException(
                "The Kafka subscription could not establish start positions for the following "
                + $"partitions within the seeding deadline: {names}.");
        }
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
            // Fast path: the subscription observes exactly one (topic, partition 0). Its cursor
            // prefix never changes, so it is cached once per session and only the 8-byte offset is
            // written per event. The output is byte-identical to the general codec, so both paths
            // share one wire format.
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

    private sealed class ConsumerWatermarkSource(IConsumer<Ignore, byte[]> consumer)
        : IPartitionWatermarkSource
    {
        public WatermarkOffsets Query(TopicPartition partition, TimeSpan timeout)
            => consumer.QueryWatermarkOffsets(partition, timeout);
    }

    private sealed record PumpState(
        string[] Topics,
        KafkaResumeState? ResumeState,
        bool RequiresCursor,
        AutoOffsetReset AutoOffsetReset,
        TimeSpan SeedingQueryTimeout,
        TimeSpan SeedingDeadline,
        ConsumerConfig ConsumerConfig,
        ChannelWriter<EventMessage> Writer,
        CancellationToken CancellationToken,
        Action<IReadOnlyList<TopicPartition>>? OnPartitionsAssigned);
}
