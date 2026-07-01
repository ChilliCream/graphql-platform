using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;

namespace HotChocolate.Fusion.Subscriptions.AzureEventHubs;

internal sealed class AzureEventHubsEventStreamBroker(AzureEventHubsEventStreamOptions options)
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

        if (topics.Length == 1)
        {
            var topic = topics[0];
            ArgumentException.ThrowIfNullOrEmpty(topic, nameof(topics));
            return SubscribeSingleHubAsync(topic, cursor: ParseCursor(cursor), cancellationToken);
        }
        else
        {
            for (var i = 0; i < topics.Length; i++)
            {
                ArgumentException.ThrowIfNullOrEmpty(topics[i]);
            }

            if (!string.IsNullOrEmpty(cursor))
            {
                throw new InvalidEventMessageCursorException();
            }

            return SubscribeMultipleHubsAsync(topics, cancellationToken);
        }
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
    }

    private async IAsyncEnumerable<EventMessage> SubscribeSingleHubAsync(
        string hub,
        EventHubsCursor? cursor,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var session = CreateSession(cancellationToken);
        EventHubConsumerClient? client = null;

        try
        {
            client = CreateConsumerClient(hub);

            var cursorPartitionId = cursor?.PartitionId;
            IAsyncEnumerable<PartitionEvent> eventStream;
            if (cursor is { } resumeCursor)
            {
                await ValidatePartitionAsync(client, resumeCursor.PartitionId, session.Token)
                    .ConfigureAwait(false);

                eventStream = client.ReadEventsFromPartitionAsync(
                    resumeCursor.PartitionId,
                    EventPosition.FromSequenceNumber(
                        resumeCursor.SequenceNumber,
                        isInclusive: false),
                    CreateReadOptions(),
                    session.Token);
            }
            else
            {
                eventStream = client.ReadEventsAsync(
                    options.StartFromEarliest,
                    CreateReadOptions(),
                    session.Token);
            }

            var receiverReady = false;
            await using var events = eventStream.GetAsyncEnumerator(session.Token);

            while (true)
            {
                PartitionEvent partitionEvent;

                try
                {
                    if (!await events.MoveNextAsync().ConfigureAwait(false))
                    {
                        break;
                    }

                    partitionEvent = events.Current;
                }
                catch (OperationCanceledException) when (session.Token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex) when (session.Token.IsCancellationRequested
                    && ex is ObjectDisposedException or EventHubsException)
                {
                    break;
                }

                if (!receiverReady)
                {
                    receiverReady = true;
                    options.OnReceiverReady?.Invoke();
                }

                if (partitionEvent.Data is null)
                {
                    continue;
                }

                if (cursorPartitionId is not null)
                {
                    yield return CreateMessage(
                        partitionEvent.Data.EventBody.ToMemory().Span,
                        cursorPartitionId,
                        partitionEvent.Data.SequenceNumber);
                }
                else
                {
                    yield return CreateMessage(partitionEvent.Data.EventBody.ToMemory().Span);
                }
            }
        }
        finally
        {
            session.Cancel();

            if (client is not null)
            {
                await DisposeClientAsync(client).ConfigureAwait(false);
            }

            RemoveSession(session);
            session.Dispose();
        }
    }

    private async IAsyncEnumerable<EventMessage> SubscribeMultipleHubsAsync(
        string[] hubs,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = options.CreateMessageChannel();
        var session = CreateSession(cancellationToken);
        var clients = new EventHubConsumerClient[hubs.Length];
        var pumpTasks = new Task[hubs.Length];

        try
        {
            for (var i = 0; i < hubs.Length; i++)
            {
                clients[i] = CreateConsumerClient(hubs[i]);
                pumpTasks[i] = PumpFanInAsync(
                    clients[i],
                    channel.Writer,
                    options.StartFromEarliest,
                    session.Token);
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
            await DisposeClientsAsync(clients).ConfigureAwait(false);
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

    private EventHubConsumerClient CreateConsumerClient(string hub)
    {
        var clientOptions = CreateClientOptions();

        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            var connectionString = options.ConnectionString;
            var properties = EventHubsConnectionStringProperties.Parse(connectionString);

            if (!string.IsNullOrEmpty(properties.EventHubName))
            {
                if (!properties.EventHubName.Equals(hub, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "The entity-scoped Event Hubs connection string does not match the topic.");
                }

                return new EventHubConsumerClient(
                    options.ConsumerGroup,
                    connectionString,
                    clientOptions);
            }

            return new EventHubConsumerClient(
                options.ConsumerGroup,
                connectionString,
                hub,
                clientOptions);
        }

        return new EventHubConsumerClient(
            options.ConsumerGroup,
            options.FullyQualifiedNamespace,
            hub,
            options.Credential,
            clientOptions);
    }

    private EventHubConsumerClientOptions CreateClientOptions()
    {
        var clientOptions = new EventHubConsumerClientOptions();

        if (options.ConfigureClientOptions is { } configure)
        {
            clientOptions = configure(clientOptions);
        }

        return clientOptions;
    }

    private ReadEventOptions CreateReadOptions()
        => new()
        {
            MaximumWaitTime = options.MaximumWaitTime
        };

    private static async Task ValidatePartitionAsync(
        EventHubConsumerClient client,
        string partitionId,
        CancellationToken cancellationToken)
    {
        string[] partitionIds;

        try
        {
            partitionIds = await client.GetPartitionIdsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not InvalidEventMessageCursorException)
        {
            throw;
        }

        for (var i = 0; i < partitionIds.Length; i++)
        {
            if (partitionIds[i].Equals(partitionId, StringComparison.Ordinal))
            {
                return;
            }
        }

        throw new InvalidEventMessageCursorException();
    }

    private async Task PumpFanInAsync(
        EventHubConsumerClient client,
        ChannelWriter<EventMessage> writer,
        bool startReadingAtEarliestEvent,
        CancellationToken cancellationToken)
    {
        try
        {
            var receiverReady = false;

            await foreach (var partitionEvent in client
                .ReadEventsAsync(
                    startReadingAtEarliestEvent,
                    CreateReadOptions(),
                    cancellationToken)
                .ConfigureAwait(false))
            {
                if (!receiverReady)
                {
                    receiverReady = true;
                    options.OnReceiverReady?.Invoke();
                }

                if (partitionEvent.Data is null)
                {
                    continue;
                }

                var eventMessage = CreateMessage(partitionEvent.Data.EventBody.ToMemory().Span);

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
        catch (Exception ex) when (cancellationToken.IsCancellationRequested
            && ex is ObjectDisposedException or EventHubsException)
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
            catch (Exception ex) when (ex is ObjectDisposedException or EventHubsException)
            {
            }
        }
    }

    private static async ValueTask DisposeClientsAsync(EventHubConsumerClient[] clients)
    {
        for (var i = 0; i < clients.Length; i++)
        {
            if (clients[i] is { } client)
            {
                await DisposeClientAsync(client).ConfigureAwait(false);
            }
        }
    }

    private static async ValueTask DisposeClientAsync(EventHubConsumerClient client)
    {
        try
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static EventMessage CreateMessage(ReadOnlySpan<byte> body)
    {
        var owner = MemoryPool<byte>.Shared.Rent(body.Length);
        body.CopyTo(owner.Memory.Span);

        return new EventMessage(owner, 0..body.Length, 0..0);
    }

    private static EventMessage CreateMessage(
        ReadOnlySpan<byte> body,
        string partitionId,
        long sequenceNumber)
    {
        var partitionIdLength = Encoding.UTF8.GetByteCount(partitionId);
        var rawCursorMaxLength = partitionIdLength + 1 + 20;

        byte[]? rented = null;
        var rawCursor = rawCursorMaxLength <= 256
            ? stackalloc byte[rawCursorMaxLength]
            : rented = ArrayPool<byte>.Shared.Rent(rawCursorMaxLength);

        try
        {
            var written = Encoding.UTF8.GetBytes(partitionId.AsSpan(), rawCursor);
            rawCursor[written++] = (byte)':';

            if (!Utf8Formatter.TryFormat(sequenceNumber, rawCursor[written..], out var sequenceLength))
            {
                throw new InvalidOperationException(
                    "The Azure Event Hubs sequence cursor could not be formatted.");
            }

            written += sequenceLength;

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
                throw new InvalidOperationException(
                    "The Azure Event Hubs cursor could not be encoded.");
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

    private static EventHubsCursor? ParseCursor(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor))
        {
            return null;
        }

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

            return ParseDecodedCursor(buffer[..bytesWritten]);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static EventHubsCursor ParseDecodedCursor(ReadOnlySpan<byte> cursor)
    {
        var separator = cursor.LastIndexOf((byte)':');

        if (separator <= 0 || separator == cursor.Length - 1)
        {
            throw new InvalidEventMessageCursorException();
        }

        var partitionId = DecodePartitionId(cursor[..separator]);
        var sequenceSpan = cursor[(separator + 1)..];

        if (!Utf8Parser.TryParse(sequenceSpan, out long sequenceNumber, out var bytesConsumed)
            || bytesConsumed != sequenceSpan.Length
            || sequenceNumber < 0
            || sequenceNumber == long.MaxValue)
        {
            throw new InvalidEventMessageCursorException();
        }

        return new EventHubsCursor(partitionId, sequenceNumber);
    }

    private static string DecodePartitionId(ReadOnlySpan<byte> partitionId)
    {
        int charCount;

        try
        {
            charCount = s_strictUtf8.GetCharCount(partitionId);
        }
        catch (DecoderFallbackException ex)
        {
            throw new InvalidEventMessageCursorException(ex);
        }

        char[]? rented = null;
        var buffer = charCount <= 256
            ? stackalloc char[charCount]
            : rented = ArrayPool<char>.Shared.Rent(charCount);

        try
        {
            var written = s_strictUtf8.GetChars(partitionId, buffer);
            return new string(buffer[..written]);
        }
        catch (DecoderFallbackException ex)
        {
            throw new InvalidEventMessageCursorException(ex);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<char>.Shared.Return(rented);
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
        private Task[] _pumpTasks = [];

        public SubscriptionSession(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        public CancellationToken Token => _cts.Token;

        public void SetPumpTasks(Task[] pumpTasks)
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
            => AzureEventHubsEventStreamBroker.WaitForPumpsAsync(_pumpTasks);

        public void Dispose()
        {
            _cts.Dispose();
        }
    }

    private readonly record struct EventHubsCursor(
        string PartitionId,
        long SequenceNumber);
}
