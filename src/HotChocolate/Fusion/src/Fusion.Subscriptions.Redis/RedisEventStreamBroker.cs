using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using StackExchange.Redis;

namespace HotChocolate.Fusion.Subscriptions.Redis;

internal sealed class RedisEventStreamBroker(RedisEventStreamOptions options)
    : IEventStreamBroker
{
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private readonly List<SubscriptionSession> _sessions = [];
    private IConnectionMultiplexer? _multiplexer;
    private bool _ownsMultiplexer;
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

        if (!string.IsNullOrEmpty(cursor))
        {
            throw new InvalidEventMessageCursorException();
        }

        return SubscribeChannelsAsync(topics, cancellationToken);
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

        for (var i = 0; i < sessions.Length; i++)
        {
            await sessions[i].WaitForPumpsAsync().ConfigureAwait(false);
        }

        if (_ownsMultiplexer && _multiplexer is not null)
        {
            if (_multiplexer is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else if (_multiplexer is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private async IAsyncEnumerable<EventMessage> SubscribeChannelsAsync(
        string[] topics,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = options.CreateMessageChannel();
        var session = CreateSession(cancellationToken);
        var queues = new ChannelMessageQueue?[topics.Length];
        var pumpTasks = new Task?[topics.Length];

        try
        {
            var multiplexer = await GetMultiplexerAsync(session.Token).ConfigureAwait(false);
            var subscriber = multiplexer.GetSubscriber();

            for (var i = 0; i < topics.Length; i++)
            {
                queues[i] = await subscriber
                    .SubscribeAsync(CreateRedisChannel(topics[i]))
                    .ConfigureAwait(false);
                options.OnReceiverReady?.Invoke();
                pumpTasks[i] = PumpChannelAsync(queues[i]!, channel.Writer, session.Token);
            }

            session.SetPumpTasks(pumpTasks);

            await foreach (var message in ReadMessagesAsync(channel.Reader, session.Token)
                .ConfigureAwait(false))
            {
                yield return message;
            }
        }
        finally
        {
            session.Cancel();
            await WaitForPumpsAsync(pumpTasks).ConfigureAwait(false);
            await UnsubscribeAllAsync(queues).ConfigureAwait(false);
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

    private async Task<IConnectionMultiplexer> GetMultiplexerAsync(CancellationToken cancellationToken)
    {
        if (options.ConnectionMultiplexer is { } suppliedMultiplexer)
        {
            _multiplexer = suppliedMultiplexer;
            _ownsMultiplexer = false;
            return suppliedMultiplexer;
        }

        if (_multiplexer is { } existingMultiplexer)
        {
            return existingMultiplexer;
        }

        await _connectLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_multiplexer is { } connectedMultiplexer)
            {
                return connectedMultiplexer;
            }

            ObjectDisposedException.ThrowIf(_disposed, this);

            var multiplexer = options.ConfigurationOptions is { } configurationOptions
                ? await ConnectionMultiplexer
                    .ConnectAsync(configurationOptions)
                    .ConfigureAwait(false)
                : await ConnectionMultiplexer
                    .ConnectAsync(options.Configuration!)
                    .ConfigureAwait(false);

            if (_disposed)
            {
                await multiplexer.DisposeAsync().ConfigureAwait(false);
                throw new ObjectDisposedException(GetType().FullName);
            }

            _multiplexer = multiplexer;
            _ownsMultiplexer = true;

            return _multiplexer;
        }
        finally
        {
            _connectLock.Release();
        }
    }

    private static async Task PumpChannelAsync(
        ChannelMessageQueue queue,
        ChannelWriter<EventMessage> writer,
        CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                var channelMessage = await queue.ReadAsync(cancellationToken).ConfigureAwait(false);
                var payload = channelMessage.Message;

                if (payload.IsNull)
                {
                    continue;
                }

                byte[]? body = payload;
                if (body is null)
                {
                    continue;
                }

                var eventMessage = CreateMessage(body);

                if (!await WriteMessageAsync(writer, eventMessage, cancellationToken)
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
        catch (Exception ex) when (cancellationToken.IsCancellationRequested
            && ex is ObjectDisposedException or RedisException)
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

    private static async IAsyncEnumerable<EventMessage> ReadMessagesAsync(
        ChannelReader<EventMessage> reader,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (true)
        {
            EventMessage? message;
            try
            {
                if (!await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    break;
                }

                if (!reader.TryRead(out message))
                {
                    continue;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            yield return message;
        }
    }

    private static async Task WaitForPumpsAsync(Task?[] pumpTasks)
    {
        for (var i = 0; i < pumpTasks.Length; i++)
        {
            if (pumpTasks[i] is not { } pumpTask)
            {
                continue;
            }

            try
            {
                await pumpTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex) when (ex is ObjectDisposedException or RedisException)
            {
            }
        }
    }

    private static async ValueTask UnsubscribeAllAsync(ChannelMessageQueue?[] queues)
    {
        for (var i = 0; i < queues.Length; i++)
        {
            if (queues[i] is not { } queue)
            {
                continue;
            }

            try
            {
                await queue.UnsubscribeAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (RedisException)
            {
            }
        }
    }

    private static RedisChannel CreateRedisChannel(string topic)
        => new(topic, RedisChannel.PatternMode.Literal);

    private static EventMessage CreateMessage(ReadOnlySpan<byte> body)
    {
        var owner = MemoryPool<byte>.Shared.Rent(body.Length);
        body.CopyTo(owner.Memory.Span);

        return new EventMessage(owner, 0..body.Length, 0..0);
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
        private Task?[] _pumpTasks = [];

        public SubscriptionSession(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        public CancellationToken Token => _cts.Token;

        public void SetPumpTasks(Task?[] pumpTasks)
        {
            _pumpTasks = pumpTasks;
        }

        public void Cancel()
        {
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }

        public Task WaitForPumpsAsync()
            => RedisEventStreamBroker.WaitForPumpsAsync(_pumpTasks);

        public void Dispose()
        {
            _cts.Dispose();
        }
    }
}
