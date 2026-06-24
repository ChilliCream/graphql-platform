using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using Confluent.Kafka;

namespace HotChocolate.Fusion.Subscriptions.Kafka;

internal sealed class KafkaEventStreamBroker(KafkaEventStreamOptions options)
    : IEventStreamBroker
{
    private readonly List<SubscriptionSession> _sessions = [];
    private bool _disposed;

    public IAsyncEnumerable<EventMessage> SubscribeAsync(
        ISubscriptionFieldContext context,
        string[] topics,
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

        return SubscribeCoreAsync(topics, cancellationToken);
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
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var session = CreateSession(cancellationToken);
        var channel = options.CreateMessageChannel();
        var consumerConfig = CreateConsumerConfig(options);
        var pumpTask = StartPump(topics, consumerConfig, channel.Writer, session.Token);
        session.SetPumpTask(pumpTask);

        try
        {
            while (true)
            {
                EventMessage? message = null;

                try
                {
                    if (!await channel.Reader.WaitToReadAsync(session.Token).ConfigureAwait(false))
                    {
                        break;
                    }

                    if (!channel.Reader.TryRead(out message))
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
        ConsumerConfig consumerConfig,
        ChannelWriter<EventMessage> writer,
        CancellationToken cancellationToken)
    {
        var state = new PumpState(
            topics,
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

            // A unique group id per Subscribe call gives every GraphQL subscriber its own fan-out
            // stream instead of Kafka's competing-consumer load balancing.
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
        var cursorMaxLength = topicLength + 1 + 11 + 1 + 20;
        var owner = MemoryPool<byte>.Shared.Rent(body.Length + cursorMaxLength);
        body.CopyTo(owner.Memory.Span);
        var cursor = owner.Memory.Span[body.Length..];
        var written = Encoding.UTF8.GetBytes(topic.AsSpan(), cursor);
        cursor[written++] = (byte)':';

        if (!Utf8Formatter.TryFormat(partition, cursor[written..], out var partitionLength))
        {
            throw new InvalidOperationException("The Kafka partition cursor could not be formatted.");
        }

        written += partitionLength;
        cursor[written++] = (byte)':';

        if (!Utf8Formatter.TryFormat(offset, cursor[written..], out var offsetLength))
        {
            throw new InvalidOperationException("The Kafka offset cursor could not be formatted.");
        }

        written += offsetLength;

        return new EventMessage(
            owner,
            0..body.Length,
            body.Length..(body.Length + written));
    }

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
        ConsumerConfig ConsumerConfig,
        ChannelWriter<EventMessage> Writer,
        CancellationToken CancellationToken,
        Action<IReadOnlyList<TopicPartition>>? OnPartitionsAssigned);
}
