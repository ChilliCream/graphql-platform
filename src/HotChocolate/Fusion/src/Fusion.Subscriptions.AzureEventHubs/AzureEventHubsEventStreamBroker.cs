using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;

namespace HotChocolate.Fusion.Subscriptions.AzureEventHubs;

internal sealed class AzureEventHubsEventStreamBroker(AzureEventHubsEventStreamOptions options)
    : IEventStreamBroker
{
    private static readonly TimeSpan s_metadataRetryDelay = TimeSpan.FromMilliseconds(25);

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

        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            var properties = EventHubsConnectionStringProperties.Parse(options.ConnectionString);

            if (!string.IsNullOrEmpty(properties.EventHubName) && topics.Length > 1)
            {
                throw new InvalidOperationException(
                    "The entity-scoped Event Hubs connection string cannot be used with multiple topics.");
            }
        }

        var resumeState = string.IsNullOrEmpty(cursor)
            ? null
            : AzureEventHubsCompositeCursorFormatter.Parse(cursor, topics);

        if (!context.RequiresCursor && resumeState is null)
        {
            return topics.Length == 1
                ? SubscribeSingleHubPassthroughAsync(topics[0], cancellationToken)
                : SubscribeMultipleHubsPassthroughAsync(topics, cancellationToken);
        }

        return SubscribeTrackedAsync(topics, resumeState, context.RequiresCursor, cancellationToken);
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

    private async IAsyncEnumerable<EventMessage> SubscribeSingleHubPassthroughAsync(
        string hub,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var session = CreateSession(cancellationToken);
        EventHubConsumerClient? client = null;

        try
        {
            client = CreateConsumerClient(hub);
            var receiverReady = false;

            await foreach (var partitionEvent in client
                .ReadEventsAsync(options.StartFromEarliest, CreateReadOptions(), session.Token)
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

                yield return CreateMessage(partitionEvent.Data.EventBody.ToMemory().Span);
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

    private async IAsyncEnumerable<EventMessage> SubscribeMultipleHubsPassthroughAsync(
        string[] hubs,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = options.CreateMessageChannel();
        var session = CreateSession(cancellationToken);
        var clients = new EventHubConsumerClient?[hubs.Length];

        try
        {
            for (var i = 0; i < hubs.Length; i++)
            {
                clients[i] = CreateConsumerClient(hubs[i]);
                var pumpTask = PumpFanInAsync(
                    clients[i]!,
                    channel.Writer,
                    options.StartFromEarliest,
                    session.Token);
                session.RegisterPumpTask(pumpTask);
            }

            await foreach (var message in ReadMessagesAsync(channel.Reader, session.Token)
                .ConfigureAwait(false))
            {
                yield return message;
            }
        }
        finally
        {
            session.Cancel();
            await session.WaitForPumpsAsync().ConfigureAwait(false);
            await DisposeClientsAsync(clients).ConfigureAwait(false);
            RemoveSession(session);
            channel.Writer.TryComplete();
            DisposeQueuedMessages(channel);
            session.Dispose();
        }
    }

    private async IAsyncEnumerable<EventMessage> SubscribeTrackedAsync(
        string[] hubs,
        AzureEventHubsResumeState? resumeState,
        bool emitCursor,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var outputChannel = options.CreateMessageChannel();
        var internalChannel = Channel.CreateBounded<AggregatorItem>(
            new BoundedChannelOptions(8)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });
        var session = CreateSession(cancellationToken);
        var clients = new Dictionary<string, EventHubConsumerClient>(StringComparer.Ordinal);

        try
        {
            for (var i = 0; i < hubs.Length; i++)
            {
                clients.Add(hubs[i], CreateConsumerClient(hubs[i]));
            }

            var partitionSource = new EventHubsPartitionSource(clients);
            var preflightTask = RunPreflightAsync(
                hubs,
                resumeState,
                emitCursor,
                clients,
                partitionSource,
                partitionSource,
                outputChannel.Writer,
                internalChannel.Reader,
                internalChannel.Writer,
                session);
            session.SetPreflightTask(preflightTask);

            await foreach (var message in ReadMessagesAsync(outputChannel.Reader, session.Token)
                .ConfigureAwait(false))
            {
                yield return message;
            }
        }
        finally
        {
            session.Cancel();
            await session.WaitForPreflightAsync().ConfigureAwait(false);
            await session.WaitForAggregatorAsync().ConfigureAwait(false);
            await session.WaitForDiscoveryAsync().ConfigureAwait(false);
            await session.WaitForPumpsAsync().ConfigureAwait(false);
            await DisposeClientsAsync(clients).ConfigureAwait(false);
            outputChannel.Writer.TryComplete();
            DisposeQueuedMessages(outputChannel);
            internalChannel.Writer.TryComplete();
            DisposeQueuedAggregatorItems(internalChannel);
            RemoveSession(session);
            session.Dispose();
        }
    }

    private Task RunPreflightAsync(
        string[] hubs,
        AzureEventHubsResumeState? resumeState,
        bool emitCursor,
        IReadOnlyDictionary<string, EventHubConsumerClient> clients,
        IPartitionIdsSource idsSource,
        IPartitionPropertiesSource propertiesSource,
        ChannelWriter<EventMessage> outputWriter,
        ChannelReader<AggregatorItem> internalReader,
        ChannelWriter<AggregatorItem> internalWriter,
        SubscriptionSession session)
        => Task.Run(async () =>
        {
            try
            {
                var startState = resumeState is null
                    ? await SeedFreshPartitionsAsync(
                        idsSource,
                        propertiesSource,
                        options.StartFromEarliest,
                        options.SeedingQueryTimeout,
                        options.SeedingDeadline,
                        hubs,
                        session.Token)
                        .ConfigureAwait(false)
                    : await ResolveResumeAsync(
                        idsSource,
                        propertiesSource,
                        resumeState,
                        options.SeedingQueryTimeout,
                        options.SeedingDeadline,
                        hubs,
                        session.Token)
                        .ConfigureAwait(false);

                options.OnPartitionsSeeded?.Invoke(
                    CreateSeededPartitionsList(startState.StartPositions));

                var aggregatorTask = RunAggregatorAsync(
                    startState.CursorMap,
                    emitCursor,
                    clients,
                    outputWriter,
                    internalReader,
                    internalWriter,
                    session);
                session.SetAggregatorTask(aggregatorTask);

                // One reader per (hub, partition) pair;
                // see AzureEventHubsEventStreamOptions.ConsumerGroup
                // for the per-group reader budget.
                foreach (var (partition, startPosition) in startState.StartPositions)
                {
                    var pumpTask = StartPartitionPump(
                        partition.Hub,
                        partition.PartitionId,
                        startPosition,
                        clients,
                        internalWriter,
                        outputWriter,
                        session);
                    session.RegisterPumpTask(pumpTask);
                }

                var discoveryTask = RunDiscoveryAsync(
                    hubs,
                    idsSource,
                    propertiesSource,
                    startState.PerHubKnownIds,
                    internalWriter,
                    outputWriter,
                    session);
                session.SetDiscoveryTask(discoveryTask);
            }
            catch (OperationCanceledException) when (session.Token.IsCancellationRequested)
            {
                // The session was cancelled during startup; teardown owns cleanup, nothing to report.
            }
            catch (Exception ex)
            {
                outputWriter.TryComplete(ex);
            }
        }, CancellationToken.None);

    private async Task RunAggregatorAsync(
        Dictionary<HubPartition, long> cursorMap,
        bool emitCursor,
        IReadOnlyDictionary<string, EventHubConsumerClient> clients,
        ChannelWriter<EventMessage> outputWriter,
        ChannelReader<AggregatorItem> internalReader,
        ChannelWriter<AggregatorItem> internalWriter,
        SubscriptionSession session)
    {
        var startedPartitions = new HashSet<HubPartition>(cursorMap.Keys);

        try
        {
            while (await internalReader.WaitToReadAsync(session.Token).ConfigureAwait(false))
            {
                while (internalReader.TryRead(out var item))
                {
                    if (session.Token.IsCancellationRequested)
                    {
                        DisposeAggregatorItem(item);
                        return;
                    }

                    if (item is PartitionDiscoveredItem discovered)
                    {
                        if (TryFoldDiscoveredPartition(
                                cursorMap,
                                startedPartitions,
                                discovered.Hub,
                                discovered.PartitionId,
                                discovered.Baseline))
                        {
                            var pumpTask = StartPartitionPump(
                                discovered.Hub,
                                discovered.PartitionId,
                                ResolveStartPosition(discovered.IsEmpty, discovered.Baseline),
                                clients,
                                internalWriter,
                                outputWriter,
                                session);
                            session.RegisterPumpTask(pumpTask);
                        }

                        continue;
                    }

                    var eventItem = (PartitionEventItem)item;

                    try
                    {
                        var outcome = await EmitAsync(
                            outputWriter,
                            cursorMap,
                            emitCursor,
                            eventItem.Hub,
                            eventItem.PartitionId,
                            eventItem.SequenceNumber,
                            eventItem.BodyOwner.Memory[..eventItem.BodyLength],
                            session.Token)
                            .ConfigureAwait(false);

                        if (outcome is WriteOutcome.Closed)
                        {
                            session.Cancel();
                            return;
                        }
                    }
                    finally
                    {
                        eventItem.BodyOwner.Dispose();
                    }
                }
            }
        }
        catch (OperationCanceledException) when (session.Token.IsCancellationRequested)
        {
            // The session was cancelled (unsubscribe or broker teardown). This is an
            // orderly shutdown, not a failure, so the aggregator exits quietly.
        }
        catch (ChannelClosedException)
        {
            // A channel was completed by another task (for example, a pump or the
            // discovery loop faulted and completed the output writer). That task has
            // already propagated the fault to the consumer, so there is nothing left
            // to report here.
        }
        catch (Exception ex)
        {
            outputWriter.TryComplete(ex);
            session.Cancel();
        }
    }

    internal static bool TryFoldDiscoveredPartition(
        Dictionary<HubPartition, long> cursorMap,
        HashSet<HubPartition> startedPartitions,
        string hub,
        string partitionId,
        long baseline)
    {
        var key = new HubPartition(hub, partitionId);

        if (!startedPartitions.Add(key))
        {
            return false;
        }

        cursorMap[key] = baseline;
        return true;
    }

    private Task RunDiscoveryAsync(
        string[] hubs,
        IPartitionIdsSource idsSource,
        IPartitionPropertiesSource propertiesSource,
        Dictionary<string, HashSet<string>> knownIds,
        ChannelWriter<AggregatorItem> internalWriter,
        ChannelWriter<EventMessage> outputWriter,
        SubscriptionSession session)
        => Task.Run(async () =>
        {
            try
            {
                while (!session.Token.IsCancellationRequested)
                {
                    await Task.Delay(options.PartitionDiscoveryInterval, session.Token)
                        .ConfigureAwait(false);

                    var fault = await RunDiscoveryTickAsync(
                        hubs,
                        idsSource,
                        propertiesSource,
                        knownIds,
                        internalWriter,
                        session.Token)
                        .ConfigureAwait(false);

                    if (fault is not null)
                    {
                        outputWriter.TryComplete(fault);
                        session.Cancel();
                        return;
                    }
                }
            }
            catch (OperationCanceledException) when (session.Token.IsCancellationRequested)
            {
                // The session was cancelled; discovery stops as part of orderly shutdown.
            }
            catch (ChannelClosedException)
            {
                // A channel was completed by another task which already reported the reason.
            }
            catch (Exception ex)
            {
                outputWriter.TryComplete(ex);
                session.Cancel();
            }
        }, CancellationToken.None);

    private Task StartPartitionPump(
        string hub,
        string partitionId,
        EventPosition startPosition,
        IReadOnlyDictionary<string, EventHubConsumerClient> clients,
        ChannelWriter<AggregatorItem> internalWriter,
        ChannelWriter<EventMessage> outputWriter,
        SubscriptionSession session)
        => Task.Run(
            () => PumpPartitionAsync(
                hub,
                partitionId,
                startPosition,
                clients,
                internalWriter,
                outputWriter,
                session),
            CancellationToken.None);

    internal static async Task<InvalidOperationException?> RunDiscoveryTickAsync(
        string[] hubs,
        IPartitionIdsSource idsSource,
        IPartitionPropertiesSource propertiesSource,
        Dictionary<string, HashSet<string>> knownIds,
        ChannelWriter<AggregatorItem> internalWriter,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < hubs.Length; i++)
        {
            var hub = hubs[i];
            string[] liveIds;

            try
            {
                liveIds = await idsSource.GetPartitionIdsAsync(hub, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (IsDiscoveryTransientException(ex))
            {
                continue;
            }

            var known = knownIds[hub];
            var liveSet = new HashSet<string>(liveIds, StringComparer.Ordinal);

            foreach (var knownId in known)
            {
                if (!liveSet.Contains(knownId))
                {
                    return new InvalidOperationException(
                        "Event Hub re-created mid-subscription; lossless resume can no longer be guaranteed.");
                }
            }

            for (var partitionIndex = 0; partitionIndex < liveIds.Length; partitionIndex++)
            {
                var partitionId = liveIds[partitionIndex];

                if (known.Contains(partitionId))
                {
                    continue;
                }

                PartitionProperties properties;

                try
                {
                    properties = await propertiesSource.GetPartitionPropertiesAsync(
                        hub,
                        partitionId,
                        cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex) when (IsDiscoveryTransientException(ex))
                {
                    continue;
                }

                var baseline = Math.Max(properties.BeginningSequenceNumber, 0);
                known.Add(partitionId);
                await internalWriter.WriteAsync(
                    new PartitionDiscoveredItem(hub, partitionId, baseline, properties.IsEmpty),
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return null;
    }

    private async Task PumpPartitionAsync(
        string hub,
        string partitionId,
        EventPosition startPosition,
        IReadOnlyDictionary<string, EventHubConsumerClient> clients,
        ChannelWriter<AggregatorItem> internalWriter,
        ChannelWriter<EventMessage> outputWriter,
        SubscriptionSession session)
    {
        try
        {
            await foreach (var partitionEvent in clients[hub]
                .ReadEventsFromPartitionAsync(
                    partitionId,
                    startPosition,
                    CreateReadOptions(),
                    session.Token)
                .ConfigureAwait(false))
            {
                var data = partitionEvent.Data;

                if (data is null)
                {
                    continue;
                }

                var body = data.EventBody.ToMemory();
                var bodyOwner = MemoryPool<byte>.Shared.Rent(body.Length);
                body.Span.CopyTo(bodyOwner.Memory.Span);

                try
                {
                    await internalWriter.WriteAsync(
                        new PartitionEventItem(
                            hub,
                            partitionId,
                            data.SequenceNumber,
                            bodyOwner,
                            body.Length),
                        session.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (session.Token.IsCancellationRequested)
                {
                    bodyOwner.Dispose();
                    break;
                }
                catch (ChannelClosedException)
                {
                    bodyOwner.Dispose();
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (session.Token.IsCancellationRequested)
        {
            // The session was cancelled; the pump stops as part of orderly shutdown.
        }
        catch (Exception ex) when (session.Token.IsCancellationRequested
            && ex is ObjectDisposedException or EventHubsException)
        {
            // Teardown may dispose the consumer client while a read is in flight; cancellation is already underway.
        }
        catch (Exception ex)
        {
            outputWriter.TryComplete(ex);
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

    internal static async Task<WriteOutcome> EmitAsync(
        ChannelWriter<EventMessage> writer,
        Dictionary<HubPartition, long> cursorMap,
        bool emitCursor,
        string hub,
        string partitionId,
        long sequenceNumber,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken)
    {
        var eventMessage = emitCursor
            ? CreateMessage(body.Span, hub, partitionId, sequenceNumber, cursorMap)
            : CreateMessage(body.Span);
        var outcome = await WriteMessageAsync(writer, eventMessage, cancellationToken)
            .ConfigureAwait(false);

        if (outcome is WriteOutcome.Delivered)
        {
            cursorMap[new HubPartition(hub, partitionId)] = sequenceNumber + 1;
        }

        return outcome;
    }

    internal static async Task<(
        Dictionary<HubPartition, long> CursorMap,
        Dictionary<HubPartition, EventPosition> StartPositions,
        Dictionary<string, HashSet<string>> PerHubKnownIds)> SeedFreshPartitionsAsync(
            IPartitionIdsSource idsSource,
            IPartitionPropertiesSource propertiesSource,
            bool startFromEarliest,
            TimeSpan seedingQueryTimeout,
            TimeSpan seedingDeadline,
            string[] topics,
            CancellationToken cancellationToken)
    {
        var start = Stopwatch.GetTimestamp();
        var idsByHub = await GetPartitionIdsByHubAsync(
            idsSource,
            topics,
            seedingQueryTimeout,
            seedingDeadline,
            start,
            cancellationToken)
            .ConfigureAwait(false);
        var pending = new List<HubPartition>();
        var perHubKnownIds = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        for (var i = 0; i < topics.Length; i++)
        {
            var hub = topics[i];
            var ids = idsByHub[hub];
            var knownIds = new HashSet<string>(ids, StringComparer.Ordinal);
            perHubKnownIds[hub] = knownIds;

            for (var partitionIndex = 0; partitionIndex < ids.Length; partitionIndex++)
            {
                pending.Add(new HubPartition(hub, ids[partitionIndex]));
            }
        }

        var propertiesByPartition = await GetPartitionPropertiesByPartitionAsync(
            propertiesSource,
            pending,
            seedingQueryTimeout,
            seedingDeadline,
            start,
            cancellationToken)
            .ConfigureAwait(false);
        var cursorMap = new Dictionary<HubPartition, long>(pending.Count);
        var startPositions = new Dictionary<HubPartition, EventPosition>(pending.Count);

        for (var i = 0; i < pending.Count; i++)
        {
            var partition = pending[i];
            var properties = propertiesByPartition[partition];
            var baseline = startFromEarliest
                ? Math.Max(properties.BeginningSequenceNumber, 0)
                : properties.LastEnqueuedSequenceNumber + 1;
            cursorMap[partition] = baseline;
            startPositions[partition] = ResolveStartPosition(properties.IsEmpty, baseline);
        }

        return (cursorMap, startPositions, perHubKnownIds);
    }

    /// <summary>
    /// Resolves the reader start position for a partition from its emptiness and folded baseline.
    /// A non-empty partition starts at <paramref name="sequenceNumber"/> inclusive. An empty
    /// partition starts at <see cref="EventPosition.Earliest"/> because a sequence position with
    /// no backing event cannot be addressed, and for an empty partition Earliest resolves to the
    /// same folded baseline by construction, so the reader start still equals the folded baseline.
    /// </summary>
    internal static EventPosition ResolveStartPosition(bool isEmpty, long sequenceNumber)
        => isEmpty
            ? EventPosition.Earliest
            : EventPosition.FromSequenceNumber(sequenceNumber, isInclusive: true);

    internal static async Task<(
        Dictionary<HubPartition, long> CursorMap,
        Dictionary<HubPartition, EventPosition> StartPositions,
        Dictionary<string, HashSet<string>> PerHubKnownIds)> ResolveResumeAsync(
        IPartitionIdsSource idsSource,
        IPartitionPropertiesSource propertiesSource,
        AzureEventHubsResumeState resumeState,
        TimeSpan seedingQueryTimeout,
        TimeSpan seedingDeadline,
        string[] topics,
        CancellationToken cancellationToken)
    {
        var start = Stopwatch.GetTimestamp();
        var idsByHub = await GetPartitionIdsByHubAsync(
            idsSource,
            topics,
            seedingQueryTimeout,
            seedingDeadline,
            start,
            cancellationToken)
            .ConfigureAwait(false);
        var partitionsToLoad = new List<HubPartition>();
        var perHubKnownIds = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        for (var i = 0; i < topics.Length; i++)
        {
            var hub = topics[i];
            var liveIds = new HashSet<string>(idsByHub[hub], StringComparer.Ordinal);
            perHubKnownIds[hub] = liveIds;

            if (!resumeState.MintedPartitionIds.TryGetValue(hub, out var mintedIds))
            {
                throw new InvalidEventMessageCursorException();
            }

            foreach (var partitionId in mintedIds)
            {
                if (!liveIds.Contains(partitionId))
                {
                    throw new InvalidEventMessageCursorException();
                }

                partitionsToLoad.Add(new HubPartition(hub, partitionId));
            }

            foreach (var partitionId in liveIds)
            {
                if (!mintedIds.Contains(partitionId))
                {
                    partitionsToLoad.Add(new HubPartition(hub, partitionId));
                }
            }
        }

        var propertiesByPartition = await GetPartitionPropertiesByPartitionAsync(
            propertiesSource,
            partitionsToLoad,
            seedingQueryTimeout,
            seedingDeadline,
            start,
            cancellationToken)
            .ConfigureAwait(false);
        var cursorMap = new Dictionary<HubPartition, long>(partitionsToLoad.Count);
        var startPositions = new Dictionary<HubPartition, EventPosition>(partitionsToLoad.Count);

        for (var i = 0; i < topics.Length; i++)
        {
            var hub = topics[i];
            var liveIds = perHubKnownIds[hub];
            var mintedIds = resumeState.MintedPartitionIds[hub];

            foreach (var partitionId in mintedIds)
            {
                var partition = new HubPartition(hub, partitionId);

                if (!resumeState.NextSequenceNumbers.TryGetValue(partition, out var next))
                {
                    throw new InvalidEventMessageCursorException();
                }

                var properties = propertiesByPartition[partition];

                if (next < properties.BeginningSequenceNumber)
                {
                    throw new InvalidEventMessageCursorException();
                }

                if (next > properties.LastEnqueuedSequenceNumber + 1)
                {
                    properties = await GetPartitionPropertiesWithRetryAsync(
                        propertiesSource,
                        partition,
                        seedingQueryTimeout,
                        seedingDeadline,
                        start,
                        cancellationToken)
                        .ConfigureAwait(false);

                    if (next > properties.LastEnqueuedSequenceNumber + 1)
                    {
                        throw new InvalidEventMessageCursorException();
                    }
                }

                cursorMap[partition] = next;
                startPositions[partition] = ResolveStartPosition(properties.IsEmpty, next);
            }

            foreach (var partitionId in liveIds)
            {
                if (mintedIds.Contains(partitionId))
                {
                    continue;
                }

                var partition = new HubPartition(hub, partitionId);
                var properties = propertiesByPartition[partition];
                var baseline = Math.Max(properties.BeginningSequenceNumber, 0);
                cursorMap[partition] = baseline;
                startPositions[partition] = ResolveStartPosition(properties.IsEmpty, baseline);
            }
        }

        return (cursorMap, startPositions, perHubKnownIds);
    }

    private static async Task<Dictionary<string, string[]>> GetPartitionIdsByHubAsync(
        IPartitionIdsSource idsSource,
        string[] hubs,
        TimeSpan queryTimeout,
        TimeSpan deadline,
        long start,
        CancellationToken cancellationToken)
    {
        var results = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var remaining = new List<string>(hubs);

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

                var hub = remaining[i];

                try
                {
                    results[hub] = await QueryPartitionIdsAsync(
                        idsSource,
                        hub,
                        queryTimeout,
                        budgetLeft,
                        cancellationToken)
                        .ConfigureAwait(false);
                    remaining.RemoveAt(i);
                    madeProgress = true;
                }
                catch (Exception ex) when (IsMetadataRetryException(ex, cancellationToken))
                {
                    // Transient metadata failure; the hub stays in the remaining list and is retried on the next sweep.
                }
            }

            if (!madeProgress && remaining.Count > 0)
            {
                budgetLeft = deadline - Stopwatch.GetElapsedTime(start);
                await DelayBeforeRetryAsync(budgetLeft, cancellationToken).ConfigureAwait(false);
            }
        }

        if (remaining.Count > 0)
        {
            var names = string.Join(", ", remaining);
            throw new EventStreamSeedingException(
                "The Azure Event Hubs subscription could not establish partition ids for the following "
                + $"hubs within the seeding deadline: {names}.");
        }

        return results;
    }

    private static async Task<Dictionary<HubPartition, PartitionProperties>>
        GetPartitionPropertiesByPartitionAsync(
            IPartitionPropertiesSource propertiesSource,
            List<HubPartition> partitions,
            TimeSpan queryTimeout,
            TimeSpan deadline,
            long start,
            CancellationToken cancellationToken)
    {
        var results = new Dictionary<HubPartition, PartitionProperties>(partitions.Count);
        var remaining = new List<HubPartition>(partitions);

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

                var partition = remaining[i];

                try
                {
                    results[partition] = await QueryPartitionPropertiesAsync(
                        propertiesSource,
                        partition,
                        queryTimeout,
                        budgetLeft,
                        cancellationToken)
                        .ConfigureAwait(false);
                    remaining.RemoveAt(i);
                    madeProgress = true;
                }
                catch (Exception ex) when (IsMetadataRetryException(ex, cancellationToken))
                {
                    // Transient metadata failure; the partition stays in the remaining list and is retried on the next sweep.
                }
            }

            if (!madeProgress && remaining.Count > 0)
            {
                budgetLeft = deadline - Stopwatch.GetElapsedTime(start);
                await DelayBeforeRetryAsync(budgetLeft, cancellationToken).ConfigureAwait(false);
            }
        }

        if (remaining.Count > 0)
        {
            var names = string.Join(", ", remaining.Select(p => $"{p.Hub}[{p.PartitionId}]"));
            throw new EventStreamSeedingException(
                "The Azure Event Hubs subscription could not establish start positions for the following "
                + $"partitions within the seeding deadline: {names}.");
        }

        return results;
    }

    private static async Task<PartitionProperties> GetPartitionPropertiesWithRetryAsync(
        IPartitionPropertiesSource propertiesSource,
        HubPartition partition,
        TimeSpan queryTimeout,
        TimeSpan deadline,
        long start,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var budgetLeft = deadline - Stopwatch.GetElapsedTime(start);

            if (budgetLeft <= TimeSpan.Zero)
            {
                break;
            }

            try
            {
                return await QueryPartitionPropertiesAsync(
                    propertiesSource,
                    partition,
                    queryTimeout,
                    budgetLeft,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (IsMetadataRetryException(ex, cancellationToken))
            {
                budgetLeft = deadline - Stopwatch.GetElapsedTime(start);
                await DelayBeforeRetryAsync(budgetLeft, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new EventStreamSeedingException(
            "The Azure Event Hubs subscription could not establish start positions for the following "
            + $"partitions within the seeding deadline: {partition.Hub}[{partition.PartitionId}].");
    }

    private static async Task<string[]> QueryPartitionIdsAsync(
        IPartitionIdsSource idsSource,
        string hub,
        TimeSpan queryTimeout,
        TimeSpan budgetLeft,
        CancellationToken cancellationToken)
    {
        var perQuery = budgetLeft < queryTimeout ? budgetLeft : queryTimeout;
        using var queryCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        queryCts.CancelAfter(perQuery);
        return await idsSource.GetPartitionIdsAsync(hub, queryCts.Token).ConfigureAwait(false);
    }

    private static async Task<PartitionProperties> QueryPartitionPropertiesAsync(
        IPartitionPropertiesSource propertiesSource,
        HubPartition partition,
        TimeSpan queryTimeout,
        TimeSpan budgetLeft,
        CancellationToken cancellationToken)
    {
        var perQuery = budgetLeft < queryTimeout ? budgetLeft : queryTimeout;
        using var queryCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        queryCts.CancelAfter(perQuery);
        return await propertiesSource.GetPartitionPropertiesAsync(
            partition.Hub,
            partition.PartitionId,
            queryCts.Token)
            .ConfigureAwait(false);
    }

    private static async Task DelayBeforeRetryAsync(
        TimeSpan budgetLeft,
        CancellationToken cancellationToken)
    {
        if (budgetLeft <= TimeSpan.Zero)
        {
            return;
        }

        var delay = budgetLeft < s_metadataRetryDelay ? budgetLeft : s_metadataRetryDelay;
        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsMetadataRetryException(
        Exception exception,
        CancellationToken cancellationToken)
        => exception is EventHubsException
            or TimeoutException
            || exception is OperationCanceledException && !cancellationToken.IsCancellationRequested;

    private static bool IsDiscoveryTransientException(Exception exception)
        => exception is EventHubsException
            or TimeoutException
            or OperationCanceledException;

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

                if (await WriteMessageAsync(writer, eventMessage, cancellationToken)
                    .ConfigureAwait(false) is WriteOutcome.Closed)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The subscription was cancelled; the pump stops as part of orderly shutdown.
        }
        catch (ChannelClosedException)
        {
            // The output channel was completed by another task which already reported the reason.
        }
        catch (Exception ex) when (cancellationToken.IsCancellationRequested
            && ex is ObjectDisposedException or EventHubsException)
        {
            // Teardown may dispose the consumer client while a read is in flight; cancellation is already underway.
        }
        catch (Exception ex)
        {
            writer.TryComplete(ex);
        }
    }

    private static async ValueTask<WriteOutcome> WriteMessageAsync(
        ChannelWriter<EventMessage> writer,
        EventMessage eventMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            if (writer.TryWrite(eventMessage))
            {
                return WriteOutcome.Delivered;
            }

            await writer.WriteAsync(eventMessage, cancellationToken).ConfigureAwait(false);
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
                if (reader.Completion.IsCompleted)
                {
                    await reader.Completion.ConfigureAwait(false);
                }

                break;
            }

            yield return message;
        }
    }

    private static async Task DisposeSessionsAsync(SubscriptionSession[] sessions)
    {
        for (var i = 0; i < sessions.Length; i++)
        {
            await sessions[i].WaitForAllAsync().ConfigureAwait(false);
        }
    }

    private static async ValueTask DisposeClientsAsync(EventHubConsumerClient?[] clients)
    {
        for (var i = 0; i < clients.Length; i++)
        {
            if (clients[i] is { } client)
            {
                await DisposeClientAsync(client).ConfigureAwait(false);
            }
        }
    }

    private static async ValueTask DisposeClientsAsync(
        IReadOnlyDictionary<string, EventHubConsumerClient> clients)
    {
        foreach (var (_, client) in clients)
        {
            await DisposeClientAsync(client).ConfigureAwait(false);
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
            // The client was already disposed elsewhere; disposing twice is a no-op.
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
        string hub,
        string partitionId,
        long sequenceNumber,
        Dictionary<HubPartition, long> cursorMap)
    {
        var key = new HubPartition(hub, partitionId);
        var hadPrevious = cursorMap.TryGetValue(key, out var previous);
        var nextSequenceNumber = sequenceNumber + 1;
        cursorMap[key] = nextSequenceNumber;

        try
        {
            var rawCursorLength = AzureEventHubsCompositeCursorFormatter.GetFormattedLength(cursorMap);

            byte[]? rented = null;
            var rawCursor = rawCursorLength <= 256
                ? stackalloc byte[rawCursorLength]
                : rented = ArrayPool<byte>.Shared.Rent(rawCursorLength);

            try
            {
                AzureEventHubsCompositeCursorFormatter.Format(
                    cursorMap,
                    rawCursor[..rawCursorLength]);
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
                cursorMap[key] = previous;
            }
            else
            {
                cursorMap.Remove(key);
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
            throw new InvalidOperationException("The Azure Event Hubs cursor could not be encoded.");
        }

        return new EventMessage(
            owner,
            0..body.Length,
            body.Length..(body.Length + bytesWritten));
    }

    private static int GetBase64EncodedLength(int length)
        => (length + 2) / 3 * 4;

    private static List<HubPartition> CreateSeededPartitionsList(
        Dictionary<HubPartition, EventPosition> startPositions)
    {
        var partitions = new List<HubPartition>(startPositions.Keys);
        partitions.Sort(static (left, right) =>
        {
            var hubComparison = StringComparer.Ordinal.Compare(left.Hub, right.Hub);

            if (hubComparison != 0)
            {
                return hubComparison;
            }

            return StringComparer.Ordinal.Compare(left.PartitionId, right.PartitionId);
        });
        return partitions;
    }

    private static void DisposeQueuedMessages(Channel<EventMessage> channel)
    {
        while (channel.Reader.TryRead(out var message))
        {
            message.Dispose();
        }
    }

    private static void DisposeQueuedAggregatorItems(Channel<AggregatorItem> channel)
    {
        while (channel.Reader.TryRead(out var item))
        {
            DisposeAggregatorItem(item);
        }
    }

    private static void DisposeAggregatorItem(AggregatorItem item)
    {
        if (item is PartitionEventItem eventItem)
        {
            eventItem.BodyOwner.Dispose();
        }
    }

    internal abstract class AggregatorItem;

    internal sealed class PartitionEventItem(
        string hub,
        string partitionId,
        long sequenceNumber,
        IMemoryOwner<byte> bodyOwner,
        int bodyLength) : AggregatorItem
    {
        public string Hub { get; } = hub;

        public string PartitionId { get; } = partitionId;

        public long SequenceNumber { get; } = sequenceNumber;

        public IMemoryOwner<byte> BodyOwner { get; } = bodyOwner;

        public int BodyLength { get; } = bodyLength;
    }

    internal sealed class PartitionDiscoveredItem(
        string hub,
        string partitionId,
        long baseline,
        bool isEmpty) : AggregatorItem
    {
        public string Hub { get; } = hub;

        public string PartitionId { get; } = partitionId;

        public long Baseline { get; } = baseline;

        public bool IsEmpty { get; } = isEmpty;
    }

    private sealed class SubscriptionSession : IDisposable
    {
        private readonly object _sync = new();
        private readonly CancellationTokenSource _cts;
        private readonly List<Task> _pumpTasks = [];
        private Task? _preflightTask;
        private Task? _aggregatorTask;
        private Task? _discoveryTask;

        public SubscriptionSession(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        public CancellationToken Token => _cts.Token;

        public void SetPreflightTask(Task preflightTask)
        {
            lock (_sync)
            {
                _preflightTask = preflightTask;
            }
        }

        public void SetAggregatorTask(Task aggregatorTask)
        {
            lock (_sync)
            {
                _aggregatorTask = aggregatorTask;
            }
        }

        public void SetDiscoveryTask(Task discoveryTask)
        {
            lock (_sync)
            {
                _discoveryTask = discoveryTask;
            }
        }

        public void RegisterPumpTask(Task pumpTask)
        {
            lock (_sync)
            {
                _pumpTasks.Add(pumpTask);
            }
        }

        public void Cancel()
        {
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }

        public async Task WaitForAllAsync()
        {
            await WaitForPreflightAsync().ConfigureAwait(false);
            await WaitForAggregatorAsync().ConfigureAwait(false);
            await WaitForDiscoveryAsync().ConfigureAwait(false);
            await WaitForPumpsAsync().ConfigureAwait(false);
        }

        public Task WaitForPreflightAsync()
            => WaitForSessionTaskAsync(GetPreflightTask());

        public Task WaitForAggregatorAsync()
            => WaitForSessionTaskAsync(GetAggregatorTask());

        public Task WaitForDiscoveryAsync()
            => WaitForSessionTaskAsync(GetDiscoveryTask());

        public async Task WaitForPumpsAsync()
        {
            Task[] pumpTasks;

            lock (_sync)
            {
                pumpTasks = [.. _pumpTasks];
            }

            for (var i = 0; i < pumpTasks.Length; i++)
            {
                await WaitForSessionTaskAsync(pumpTasks[i]).ConfigureAwait(false);
            }
        }

        private Task? GetPreflightTask()
        {
            lock (_sync)
            {
                return _preflightTask;
            }
        }

        private Task? GetAggregatorTask()
        {
            lock (_sync)
            {
                return _aggregatorTask;
            }
        }

        private Task? GetDiscoveryTask()
        {
            lock (_sync)
            {
                return _discoveryTask;
            }
        }

        private static async Task WaitForSessionTaskAsync(Task? task)
        {
            if (task is null)
            {
                return;
            }

            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // The task was cancelled as part of session shutdown; that is the expected outcome here.
            }
            catch (Exception ex) when (ex is ObjectDisposedException or EventHubsException)
            {
                // The task observed the client teardown; the fault was already surfaced via the output channel.
            }
        }

        public void Dispose()
        {
            _cts.Dispose();
        }
    }

    internal enum WriteOutcome
    {
        Delivered,
        Closed
    }
}
