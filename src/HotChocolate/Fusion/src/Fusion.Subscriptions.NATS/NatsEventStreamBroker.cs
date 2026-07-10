using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace HotChocolate.Fusion.Subscriptions.NATS;

internal sealed class NatsEventStreamBroker(NatsEventStreamOptions options)
    : IEventStreamBroker
{
    private readonly NatsConnection _connection = CreateConnection(options);
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

        if (options.JetStream is null)
        {
            if (!string.IsNullOrEmpty(cursor))
            {
                throw new InvalidEventMessageCursorException();
            }

            return SubscribeCoreAsync(topics, cancellationToken);
        }

        var startSequence = string.IsNullOrEmpty(cursor)
            ? default(ulong?)
            : ParseJetStreamSequence(cursor);

        return SubscribeJetStreamAsync(topics, options.JetStream, startSequence, cancellationToken);
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

        await _connection.DisposeAsync().ConfigureAwait(false);
    }

    private IAsyncEnumerable<EventMessage> SubscribeCoreAsync(
        string[] topics,
        CancellationToken cancellationToken)
        => topics.Length == 1
            ? SubscribeCoreSingleTopicAsync(topics[0], cancellationToken)
            : SubscribeCoreMultipleTopicsAsync(topics, cancellationToken);

    private async IAsyncEnumerable<EventMessage> SubscribeCoreSingleTopicAsync(
        string topic,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var session = CreateSession(cancellationToken);
        INatsSub<byte[]>? subscription = null;

        try
        {
            subscription = await _connection
                .SubscribeCoreAsync<byte[]>(
                    topic,
                    queueGroup: null,
                    serializer: null,
                    opts: null,
                    cancellationToken: session.Token)
                .ConfigureAwait(false);

            await _connection.PingAsync(session.Token).ConfigureAwait(false);

            await foreach (var message in subscription.Msgs
                .ReadAllAsync(session.Token)
                .ConfigureAwait(false))
            {
                message.EnsureSuccess();
                yield return CreateMessage(message.Data ?? []);
            }
        }
        finally
        {
            session.Cancel();

            if (subscription is not null)
            {
                try
                {
                    await subscription.UnsubscribeAsync().ConfigureAwait(false);
                }
                catch (Exception) when (session.Token.IsCancellationRequested)
                {
                }
            }

            RemoveSession(session);
            session.Dispose();
        }
    }

    private async IAsyncEnumerable<EventMessage> SubscribeCoreMultipleTopicsAsync(
        string[] topics,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = options.CreateMessageChannel();
        var session = CreateSession(cancellationToken);
        var subscriptions = new INatsSub<byte[]>[topics.Length];
        var pumpTasks = new Task[topics.Length];

        try
        {
            for (var i = 0; i < topics.Length; i++)
            {
                subscriptions[i] = await _connection
                    .SubscribeCoreAsync<byte[]>(
                        topics[i],
                        queueGroup: null,
                        serializer: null,
                        opts: null,
                        cancellationToken: session.Token)
                    .ConfigureAwait(false);
                pumpTasks[i] = PumpCoreMessagesAsync(subscriptions[i], channel.Writer, session.Token);
            }

            await _connection.PingAsync(session.Token).ConfigureAwait(false);

            await foreach (var message in channel.Reader
                .ReadAllAsync(session.Token)
                .ConfigureAwait(false))
            {
                yield return message;
            }
        }
        finally
        {
            session.Cancel();

            for (var i = 0; i < subscriptions.Length; i++)
            {
                if (subscriptions[i] is { } subscription)
                {
                    try
                    {
                        await subscription.UnsubscribeAsync().ConfigureAwait(false);
                    }
                    catch (Exception) when (session.Token.IsCancellationRequested)
                    {
                    }
                }
            }

            await WaitForPumpsAsync(pumpTasks).ConfigureAwait(false);

            RemoveSession(session);
            channel.Writer.TryComplete();
            DisposeQueuedMessages(channel);
            session.Dispose();
        }
    }

    private async IAsyncEnumerable<EventMessage> SubscribeJetStreamAsync(
        string[] topics,
        NatsJetStreamOptions jetStreamOptions,
        ulong? startSequence,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var session = CreateSession(cancellationToken);
        var js = new NatsJSContext(_connection);
        var consumer = await js
            .CreateOrUpdateConsumerAsync(
                jetStreamOptions.Stream,
                CreateConsumerConfig(topics, startSequence),
                session.Token)
            .ConfigureAwait(false);

        try
        {
            await foreach (var message in consumer
                .ConsumeAsync<byte[]>(
                    serializer: null,
                    opts: null,
                    cancellationToken: session.Token)
                .ConfigureAwait(false))
            {
                message.EnsureSuccess();

                var eventMessage = CreateMessage(
                    message.Data ?? [],
                    message.Metadata?.Sequence.Stream ?? 0);

                try
                {
                    yield return eventMessage;
                }
                finally
                {
                    await message.AckAsync(cancellationToken: session.Token).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            session.Cancel();
            RemoveSession(session);
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

    private static NatsConnection CreateConnection(NatsEventStreamOptions options)
    {
        var opts = new NatsOpts();

        if (!string.IsNullOrWhiteSpace(options.Url))
        {
            opts = opts with { Url = options.Url };
        }

        if (options.ConfigureConnection is { } configure)
        {
            opts = configure(opts);
        }

        return new NatsConnection(opts);
    }

    private static ConsumerConfig CreateConsumerConfig(
        string[] topics,
        ulong? startSequence)
    {
        if (startSequence is null)
        {
            return new ConsumerConfig
            {
                AckPolicy = ConsumerConfigAckPolicy.Explicit,
                DeliverPolicy = ConsumerConfigDeliverPolicy.New,
                FilterSubjects = topics
            };
        }

        return new ConsumerConfig
        {
            AckPolicy = ConsumerConfigAckPolicy.Explicit,
            DeliverPolicy = ConsumerConfigDeliverPolicy.ByStartSequence,
            OptStartSeq = startSequence.GetValueOrDefault() + 1,
            FilterSubjects = topics
        };
    }

    private static ulong ParseJetStreamSequence(string cursor)
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

            var decodedCursor = buffer[..bytesWritten];

            if (!Utf8Parser.TryParse(decodedCursor, out ulong sequence, out var bytesConsumed)
                || bytesConsumed != decodedCursor.Length)
            {
                throw new InvalidEventMessageCursorException();
            }

            if (sequence == ulong.MaxValue)
            {
                throw new InvalidEventMessageCursorException();
            }

            return sequence;
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static async Task PumpCoreMessagesAsync(
        INatsSub<byte[]> subscription,
        ChannelWriter<EventMessage> writer,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in subscription.Msgs
                .ReadAllAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                message.EnsureSuccess();

                var eventMessage = CreateMessage(message.Data ?? []);

                if (!await WriteMessageAsync(
                    writer,
                    eventMessage,
                    cancellationToken)
                    .ConfigureAwait(false))
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (ChannelClosedException)
        {
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            writer.TryComplete(ex);
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

            await writer.WriteAsync(eventMessage, cancellationToken).ConfigureAwait(false);
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

    private static async Task WaitForPumpsAsync(Task[] pumpTasks)
    {
        for (var i = 0; i < pumpTasks.Length; i++)
        {
            if (pumpTasks[i] is null)
            {
                continue;
            }

            try
            {
                await pumpTasks[i].ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex) when (ex is ObjectDisposedException or NatsException)
            {
            }
        }
    }

    private static EventMessage CreateMessage(ReadOnlySpan<byte> body)
    {
        var owner = MemoryPool<byte>.Shared.Rent(body.Length);
        body.CopyTo(owner.Memory.Span);

        return new EventMessage(owner, 0..body.Length, 0..0);
    }

    private static EventMessage CreateMessage(ReadOnlySpan<byte> body, ulong sequence)
    {
        Span<byte> rawCursor = stackalloc byte[20];

        if (!Utf8Formatter.TryFormat(
            sequence,
            rawCursor,
            out var rawCursorLength))
        {
            throw new InvalidOperationException(
                "The NATS JetStream sequence cursor could not be formatted.");
        }

        var cursorLength = GetBase64EncodedLength(rawCursorLength);
        var owner = MemoryPool<byte>.Shared.Rent(body.Length + cursorLength);
        body.CopyTo(owner.Memory.Span);

        if (Base64.EncodeToUtf8(
                rawCursor[..rawCursorLength],
                owner.Memory.Span[body.Length..],
                out _,
                out var bytesWritten) is not OperationStatus.Done)
        {
            owner.Dispose();
            throw new InvalidOperationException(
                "The NATS JetStream sequence cursor could not be encoded.");
        }

        return new EventMessage(
            owner,
            0..body.Length,
            body.Length..(body.Length + bytesWritten));
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

    private sealed class SubscriptionSession(CancellationToken cancellationToken) : IDisposable
    {
        private readonly CancellationTokenSource _cts =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        public CancellationToken Token => _cts.Token;

        public void Cancel()
        {
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }

        public void Dispose()
        {
            _cts.Dispose();
        }
    }
}
