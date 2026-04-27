using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mocha.Transport.AzureEventHub.Tests.Behaviors;

public class DeadLetterCheckpointTests
{
    [Fact]
    public async Task Checkpoint_Should_NotAdvancePastFailedEvent_When_HandlerThrows()
    {
        // arrange
        var checkpointStore = new RecordingCheckpointStore();
        var processor = CreateProcessor(
            checkpointStore,
            messageHandler: static (e, _, _) =>
            {
                if (e.SequenceNumber == 7)
                {
                    throw new InvalidOperationException("handler failed");
                }

                return default;
            });
        var partition = new TestPartition("0");
        var failingEvent = CreateEvent(sequenceNumber: 7, body: "fail");
        var laterEvent = CreateEvent(sequenceNumber: 8, body: "ok");

        // act
        await processor.RunBatchAsync(
            [failingEvent, laterEvent],
            partition,
            CancellationToken.None);

        // assert
        Assert.Empty(checkpointStore.Checkpoints);
    }

    [Fact]
    public async Task Checkpoint_Should_AdvancePastSuccessfulEvent_When_HandlerSucceeds()
    {
        // arrange
        var checkpointStore = new RecordingCheckpointStore();
        var processor = CreateProcessor(
            checkpointStore,
            messageHandler: static (_, _, _) => default);
        var partition = new TestPartition("0");
        var ok = CreateEvent(sequenceNumber: 11, body: "ok");

        // act
        await processor.RunBatchAsync([ok], partition, CancellationToken.None);

        // assert
        var checkpoint = Assert.Single(checkpointStore.Checkpoints);
        Assert.Equal(11L, checkpoint.SequenceNumber);
    }

    private static TestableProcessor CreateProcessor(
        ICheckpointStore checkpointStore,
        Func<EventData, string, CancellationToken, ValueTask> messageHandler)
    {
        // Use a fake connection string — the processor never actually connects in this unit test
        // because we invoke OnProcessingEventBatchAsync directly via the testable subclass.
        const string fakeConnection =
            "Endpoint=sb://fake.example.com;SharedAccessKeyName=key;SharedAccessKey=value";

        return new TestableProcessor(
            NullLogger.Instance,
            "$Default",
            fakeConnection,
            "test-hub",
            messageHandler,
            checkpointStore,
            ownershipStore: null);
    }

    private static EventData CreateEvent(long sequenceNumber, string body)
        => EventHubsModelFactory.EventData(
            eventBody: new BinaryData(body),
            properties: new Dictionary<string, object>(),
            systemProperties: null,
            partitionKey: null,
            sequenceNumber: sequenceNumber,
            offsetString: sequenceNumber.ToString(),
            enqueuedTime: DateTimeOffset.UtcNow);

    private sealed class TestPartition : EventProcessorPartition
    {
        public TestPartition(string id) { PartitionId = id; }
    }

    private sealed class TestableProcessor : MochaEventProcessor
    {
        public TestableProcessor(
            ILogger logger,
            string consumerGroup,
            string connectionString,
            string eventHubName,
            Func<EventData, string, CancellationToken, ValueTask> messageHandler,
            ICheckpointStore checkpointStore,
            IPartitionOwnershipStore? ownershipStore = null)
            : base(
                logger,
                consumerGroup,
                connectionString,
                eventHubName,
                messageHandler,
                checkpointStore,
                ownershipStore)
        {
        }

        public Task RunBatchAsync(
            IEnumerable<EventData> events,
            EventProcessorPartition partition,
            CancellationToken cancellationToken)
            => OnProcessingEventBatchAsync(events, partition, cancellationToken);
    }

    private sealed class RecordingCheckpointStore : ICheckpointStore
    {
        public List<(string Partition, long SequenceNumber)> Checkpoints { get; } = [];

        public ValueTask<long?> GetCheckpointAsync(
            string fullyQualifiedNamespace,
            string eventHubName,
            string consumerGroup,
            string partitionId,
            CancellationToken cancellationToken)
            => new((long?)null);

        public ValueTask SetCheckpointAsync(
            string fullyQualifiedNamespace,
            string eventHubName,
            string consumerGroup,
            string partitionId,
            long sequenceNumber,
            CancellationToken cancellationToken)
        {
            Checkpoints.Add((partitionId, sequenceNumber));
            return default;
        }
    }
}
