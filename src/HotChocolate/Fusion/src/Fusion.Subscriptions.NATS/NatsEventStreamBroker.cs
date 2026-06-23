using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace HotChocolate.Fusion.Subscriptions.NATS;

public sealed class NatsEventStreamBroker(NatsEventStreamOptions options)
    : IEventStreamBroker
{
    private readonly NatsConnection _connection = CreateConnection(options);
    private readonly List<SubscriptionSession> _sessions = [];
    private bool _disposed;

    public IAsyncEnumerable<EventMessage> Subscribe(
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

        return options.JetStream is null
            ? SubscribeCoreAsync(topics, cancellationToken)
            : SubscribeJetStreamAsync(topics, options.JetStream, cancellationToken);
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

    private async IAsyncEnumerable<EventMessage> SubscribeCoreAsync(
        string[] topics,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var session = CreateSession(cancellationToken);
        var channel = Channel.CreateUnbounded<EventMessage>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
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
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var session = CreateSession(cancellationToken);
        var js = new NatsJSContext(_connection);
        var consumer = await js
            .CreateOrUpdateConsumerAsync(
                jetStreamOptions.Stream,
                new ConsumerConfig(jetStreamOptions.DurableConsumer)
                {
                    FilterSubjects = topics
                },
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

                if (!writer.TryWrite(eventMessage))
                {
                    eventMessage.Dispose();
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
        var maxCursorLength = sequence == 0 ? 1 : 20;
        var owner = MemoryPool<byte>.Shared.Rent(body.Length + maxCursorLength);
        body.CopyTo(owner.Memory.Span);

        if (!Utf8Formatter.TryFormat(
            sequence,
            owner.Memory.Span[body.Length..],
            out var cursorLength))
        {
            owner.Dispose();
            throw new InvalidOperationException(
                "The NATS JetStream sequence cursor could not be formatted.");
        }

        return new EventMessage(
            owner,
            0..body.Length,
            body.Length..(body.Length + cursorLength));
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

        public SubscriptionSession(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

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
