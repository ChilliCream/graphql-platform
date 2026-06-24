using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using Confluent.Kafka;

namespace HotChocolate.Fusion.Subscriptions.Kafka;

internal sealed class KafkaEventStreamBroker(KafkaEventStreamOptions options)
    : IEventStreamBroker
{
    private static readonly Encoding s_strictUtf8 =
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

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

        KafkaCursor? kafkaCursor = string.IsNullOrEmpty(cursor) ? null : ParseCursor(cursor, topics);
        return SubscribeCoreAsync(topics, kafkaCursor, cancellationToken);
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
        KafkaCursor? cursor,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = options.CreateMessageChannel();
        var reader = channel.Reader;
        var writer = channel.Writer;
        var consumerConfig = CreateConsumerConfig(options);
        var session = CreateSession(cancellationToken);
        var pumpTask = StartPump(topics, cursor, consumerConfig, writer, session.Token);
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

                yield return message!;
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
        KafkaCursor? cursor,
        ConsumerConfig consumerConfig,
        ChannelWriter<EventMessage> writer,
        CancellationToken cancellationToken)
    {
        var state = new PumpState(
            topics,
            cursor,
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

        try
        {
            consumer = new ConsumerBuilder<Ignore, byte[]>(state.ConsumerConfig)
                .SetPartitionsAssignedHandler((_, partitions) =>
                    state.OnPartitionsAssigned?.Invoke(partitions))
                .Build();

            if (state.Cursor is { } cursor)
            {
                consumer.Assign(
                    new TopicPartitionOffset(
                        cursor.Topic,
                        new Partition(cursor.Partition),
                        new Offset(cursor.Offset + 1)));
            }
            else
            {
                // A unique group id per Subscribe call gives every GraphQL subscriber its own fan-out
                // stream instead of Kafka's competing-consumer load balancing.
                consumer.Subscribe(state.Topics);
            }

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

                var eventMessage = CreateMessage(
                    result.Message.Value ?? [],
                    result.Topic,
                    result.Partition.Value,
                    result.Offset.Value);

                if (!await WriteMessageAsync(
                    state.Writer,
                    eventMessage,
                    state.CancellationToken)
                    .ConfigureAwait(false))
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

    private static async ValueTask<bool> WriteMessageAsync(
        ChannelWriter<EventMessage> writer,
        EventMessage eventMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            if (writer.TryWrite(eventMessage))
            {
                return true;
            }

            await writer.WriteAsync(eventMessage, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            eventMessage.Dispose();
            return false;
        }
        catch (ChannelClosedException)
        {
            eventMessage.Dispose();
            return false;
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

    private static EventMessage CreateMessage(
        ReadOnlySpan<byte> body,
        string topic,
        int partition,
        long offset)
    {
        // Kafka cursors are per partition, so the at-most-once path still emits the full
        // coordinate as informational cursor data.
        var topicLength = Encoding.UTF8.GetByteCount(topic);
        var rawCursorMaxLength = topicLength + 1 + 11 + 1 + 20;

        byte[]? rented = null;
        var rawCursor = rawCursorMaxLength <= 256
            ? stackalloc byte[rawCursorMaxLength]
            : rented = ArrayPool<byte>.Shared.Rent(rawCursorMaxLength);

        try
        {
            var written = Encoding.UTF8.GetBytes(topic.AsSpan(), rawCursor);
            rawCursor[written++] = (byte)':';

            if (!Utf8Formatter.TryFormat(partition, rawCursor[written..], out var partitionLength))
            {
                throw new InvalidOperationException("The Kafka partition cursor could not be formatted.");
            }

            written += partitionLength;
            rawCursor[written++] = (byte)':';

            if (!Utf8Formatter.TryFormat(offset, rawCursor[written..], out var offsetLength))
            {
                throw new InvalidOperationException("The Kafka offset cursor could not be formatted.");
            }

            written += offsetLength;

            var cursorLength = GetBase64EncodedLength(written);
            var owner = MemoryPool<byte>.Shared.Rent(body.Length + cursorLength);
            body.CopyTo(owner.Memory.Span);

            if (Base64.EncodeToUtf8(
                    rawCursor[..written],
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
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static KafkaCursor ParseCursor(string cursor, string[] topics)
    {
        var maxDecodedLength = GetMaxBase64DecodedLength(cursor.Length);
        byte[]? rented = null;
        var buffer = maxDecodedLength <= 256
            ? stackalloc byte[maxDecodedLength]
            : rented = ArrayPool<byte>.Shared.Rent(maxDecodedLength);

        try
        {
            if (!Convert.TryFromBase64Chars(cursor.AsSpan(), buffer, out var bytesWritten))
            {
                throw new InvalidEventMessageCursorException();
            }

            return ParseDecodedCursor(buffer[..bytesWritten], topics);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static KafkaCursor ParseDecodedCursor(
        ReadOnlySpan<byte> cursor,
        string[] topics)
    {
        var offsetSeparator = cursor.LastIndexOf((byte)':');
        var partitionSeparator = offsetSeparator > 0
            ? cursor[..offsetSeparator].LastIndexOf((byte)':')
            : -1;

        if (partitionSeparator <= 0
            || offsetSeparator <= partitionSeparator + 1
            || offsetSeparator == cursor.Length - 1)
        {
            throw new InvalidEventMessageCursorException();
        }

        if (!TryGetTopic(cursor[..partitionSeparator], topics, out var topic))
        {
            throw new InvalidEventMessageCursorException();
        }

        var partitionSpan = cursor[(partitionSeparator + 1)..offsetSeparator];
        if (!Utf8Parser.TryParse(partitionSpan, out int partition, out var partitionBytesConsumed)
            || partitionBytesConsumed != partitionSpan.Length
            || partition < 0)
        {
            throw new InvalidEventMessageCursorException();
        }

        var offsetSpan = cursor[(offsetSeparator + 1)..];
        if (!Utf8Parser.TryParse(offsetSpan, out long offset, out var offsetBytesConsumed)
            || offsetBytesConsumed != offsetSpan.Length
            || offset < 0
            || offset == long.MaxValue)
        {
            throw new InvalidEventMessageCursorException();
        }

        return new KafkaCursor(topic, partition, offset);
    }

    private static bool TryGetTopic(
        ReadOnlySpan<byte> cursorTopic,
        string[] topics,
        [NotNullWhen(true)] out string? topic)
    {
        if (topics.Length == 1)
        {
            topic = topics[0];
            if (TopicEquals(cursorTopic, topic))
            {
                return true;
            }

            topic = null;
            return false;
        }

        int charCount;

        try
        {
            charCount = s_strictUtf8.GetCharCount(cursorTopic);
        }
        catch (DecoderFallbackException)
        {
            topic = null;
            return false;
        }

        char[]? rented = null;
        var buffer = charCount <= 256
            ? stackalloc char[charCount]
            : rented = ArrayPool<char>.Shared.Rent(charCount);

        try
        {
            var written = s_strictUtf8.GetChars(cursorTopic, buffer);
            var cursorTopicText = buffer[..written];

            for (var i = 0; i < topics.Length; i++)
            {
                var candidate = topics[i];

                if (cursorTopicText.SequenceEqual(candidate.AsSpan()))
                {
                    topic = candidate;
                    return true;
                }
            }

            topic = null;
            return false;
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }
    }

    private static bool TopicEquals(ReadOnlySpan<byte> cursorTopic, string topic)
    {
        if (cursorTopic.Length == topic.Length)
        {
            for (var i = 0; i < cursorTopic.Length; i++)
            {
                var cursorByte = cursorTopic[i];
                var topicChar = topic[i];

                if (cursorByte > 0x7f || topicChar > 0x7f)
                {
                    break;
                }

                if (cursorByte != topicChar)
                {
                    return false;
                }

                if (i == cursorTopic.Length - 1)
                {
                    return true;
                }
            }
        }

        var byteCount = Encoding.UTF8.GetByteCount(topic);
        if (byteCount != cursorTopic.Length)
        {
            return false;
        }

        byte[]? rented = null;
        var buffer = byteCount <= 256
            ? stackalloc byte[byteCount]
            : rented = ArrayPool<byte>.Shared.Rent(byteCount);

        try
        {
            var written = Encoding.UTF8.GetBytes(topic.AsSpan(), buffer);
            return cursorTopic.SequenceEqual(buffer[..written]);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static int GetBase64EncodedLength(int length)
        => (length + 2) / 3 * 4;

    private static int GetMaxBase64DecodedLength(int length)
        => (length + 3) / 4 * 3;

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

    private sealed record PumpState(
        string[] Topics,
        KafkaCursor? Cursor,
        ConsumerConfig ConsumerConfig,
        ChannelWriter<EventMessage> Writer,
        CancellationToken CancellationToken,
        Action<IReadOnlyList<TopicPartition>>? OnPartitionsAssigned);

    private readonly record struct KafkaCursor(
        string Topic,
        int Partition,
        long Offset);
}
