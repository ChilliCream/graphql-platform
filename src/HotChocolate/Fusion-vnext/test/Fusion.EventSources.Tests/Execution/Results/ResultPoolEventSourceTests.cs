using System.Collections.Concurrent;
using System.Diagnostics.Tracing;

namespace HotChocolate.Fusion.Execution.Results;

public class ResultPoolEventSourceTests : IDisposable
{
    private readonly TestEventListener _listener = new();

    [Fact]
    public void SessionCreated_LogsEvent()
    {
        // Act
        ResultPoolEventSource.Log.SessionCreated();

        // Assert
        var events = _listener.GetEvents();
        Assert.Single(events);

        var sessionEvent = events[0];
        Assert.Equal(1, sessionEvent.EventId);
        Assert.Equal("SessionCreated", sessionEvent.EventName);
        Assert.Equal(EventLevel.Informational, sessionEvent.Level);
    }

    [Fact]
    public void SessionDisposed_LogsEventWithMetrics()
    {
        // Arrange
        const int totalRents = 42;
        const int batchesUsed = 3;

        // Act
        ResultPoolEventSource.Log.SessionDisposed(totalRents, batchesUsed);

        // Assert
        var events = _listener.GetEvents();
        Assert.Single(events);

        var sessionEvent = events[0];
        Assert.Equal(2, sessionEvent.EventId);
        Assert.Equal("SessionDisposed", sessionEvent.EventName);
        Assert.Equal(totalRents, sessionEvent.Payload![0]);
        Assert.Equal(batchesUsed, sessionEvent.Payload[1]);
    }

    [Fact]
    public void BatchExhausted_LogsEventWithPoolType()
    {
        // Arrange
        const string poolType = "ObjectResult";
        const int batchIndex = 2;

        // Act
        ResultPoolEventSource.Log.BatchExhausted(poolType, batchIndex);

        // Assert
        var events = _listener.GetEvents();
        Assert.Single(events);

        var batchEvent = events[0];
        Assert.Equal(10, batchEvent.EventId);
        Assert.Equal("BatchExhausted", batchEvent.EventName);
        Assert.Equal(EventLevel.Verbose, batchEvent.Level);
        Assert.Equal(poolType, batchEvent.Payload![0]);
        Assert.Equal(batchIndex, batchEvent.Payload[1]);
    }

    [Fact]
    public void BatchAllocated_LogsEventWithPoolType()
    {
        // Arrange
        const string poolType = "LeafFieldResult";
        const int batchIndex = 1;

        // Act
        ResultPoolEventSource.Log.BatchAllocated(poolType, batchIndex);

        // Assert
        var events = _listener.GetEvents();
        Assert.Single(events);

        var batchEvent = events[0];
        Assert.Equal(11, batchEvent.EventId);
        Assert.Equal("BatchAllocated", batchEvent.EventName);
        Assert.Equal(poolType, batchEvent.Payload![0]);
        Assert.Equal(batchIndex, batchEvent.Payload[1]);
    }

    [Fact]
    public void ObjectRented_LogsEventAtLogAlwaysLevel()
    {
        // Arrange
        const string poolType = "ObjectResult";
        const int objectIndex = 5;

        // Act
        ResultPoolEventSource.Log.ObjectRented(poolType, objectIndex);

        // Assert
        var events = _listener.GetEvents();
        Assert.Single(events);

        var rentalEvent = events[0];
        Assert.Equal(20, rentalEvent.EventId);
        Assert.Equal("ObjectRented", rentalEvent.EventName);
        Assert.Equal(EventLevel.LogAlways, rentalEvent.Level);
        Assert.Equal(poolType, rentalEvent.Payload![0]);
        Assert.Equal(objectIndex, rentalEvent.Payload[1]);
    }

    [Fact]
    public void ObjectRecreated_LogsEventWithReason()
    {
        // Arrange
        const string poolType = "ObjectResult";
        const ResultPoolEventSource.ObjectRecreationReason reason =
            ResultPoolEventSource.ObjectRecreationReason.ResetFailed;

        // Act
        ResultPoolEventSource.Log.ObjectCreated(poolType, reason);

        // Assert
        var events = _listener.GetEvents();
        Assert.Single(events);

        var recreationEvent = events[0];
        Assert.Equal(21, recreationEvent.EventId);
        Assert.Equal("ObjectCreated", recreationEvent.EventName);
        Assert.Equal(EventLevel.Warning, recreationEvent.Level);
        Assert.Equal(poolType, recreationEvent.Payload![0]);
        Assert.Equal((int)reason, recreationEvent.Payload[1]);
    }

    [Fact]
    public void CapacityExceeded_LogsEventWithCapacityInfo()
    {
        // Arrange
        const string poolType = "ObjectResult";
        const int currentCapacity = 1024;
        const int maxAllowed = 512;

        // Act
        ResultPoolEventSource.Log.CapacityExceeded(poolType, currentCapacity, maxAllowed);

        // Assert
        var events = _listener.GetEvents();
        Assert.Single(events);

        var capacityEvent = events[0];
        Assert.Equal(41, capacityEvent.EventId);
        Assert.Equal("CapacityExceeded", capacityEvent.EventName);
        Assert.Equal(EventLevel.Warning, capacityEvent.Level);
        Assert.Equal(poolType, capacityEvent.Payload![0]);
        Assert.Equal(currentCapacity, capacityEvent.Payload[1]);
        Assert.Equal(maxAllowed, capacityEvent.Payload[2]);
    }

    [Fact]
    public void PoolCorruption_LogsCriticalEvent()
    {
        // Arrange
        const string poolType = "LeafFieldResult";

        // Act
        ResultPoolEventSource.Log.PoolCorruption(poolType);

        // Assert
        var events = _listener.GetEvents();
        Assert.Single(events);

        var corruptionEvent = events[0];
        Assert.Equal(40, corruptionEvent.EventId);
        Assert.Equal("PoolCorruption", corruptionEvent.EventName);
        Assert.Equal(EventLevel.Critical, corruptionEvent.Level);
        Assert.Equal(poolType, corruptionEvent.Payload![0]);
    }

    [Fact]
    public void BatchUtilization_LogsPerformanceMetrics()
    {
        // Arrange
        const string poolType = "ObjectResult";
        const int batchSize = 64;
        const int utilizationPercentage = 75;

        // Act
        ResultPoolEventSource.Log.BatchUtilization(poolType, batchSize, utilizationPercentage);

        // Assert
        var events = _listener.GetEvents();
        Assert.Single(events);

        var utilizationEvent = events[0];
        Assert.Equal(30, utilizationEvent.EventId);
        Assert.Equal("BatchUtilization", utilizationEvent.EventName);
        Assert.Equal(EventLevel.Informational, utilizationEvent.Level);
        Assert.Equal(poolType, utilizationEvent.Payload![0]);
        Assert.Equal(batchSize, utilizationEvent.Payload[1]);
        Assert.Equal(utilizationPercentage, utilizationEvent.Payload[2]);
    }

    [Fact]
    public void EventSource_HasCorrectName()
    {
        // Assert
        Assert.Equal("HotChocolate-Fusion-Result-Pools", ResultPoolEventSource.Log.Name);
    }

    [Fact]
    public void Keywords_HaveCorrectValues()
    {
        // Assert
        Assert.Equal((EventKeywords)0x1, ResultPoolEventSource.Keywords.Session);
        Assert.Equal((EventKeywords)0x2, ResultPoolEventSource.Keywords.Batch);
        Assert.Equal((EventKeywords)0x4, ResultPoolEventSource.Keywords.Rental);
        Assert.Equal((EventKeywords)0x8, ResultPoolEventSource.Keywords.Performance);
        Assert.Equal((EventKeywords)0x10, ResultPoolEventSource.Keywords.Error);
    }

    [Fact]
    public void MultipleEvents_AreAllCaptured()
    {
        // Act
        ResultPoolEventSource.Log.SessionCreated();
        ResultPoolEventSource.Log.BatchAllocated("ObjectResult", 0);
        ResultPoolEventSource.Log.ObjectRented("ObjectResult", 1);
        ResultPoolEventSource.Log.SessionDisposed(5, 1);

        // Assert
        var events = _listener.GetEvents();
        Assert.Equal(4, events.Count);

        Assert.Equal(1, events[0].EventId); // SessionCreated
        Assert.Equal(11, events[1].EventId); // BatchAllocated
        Assert.Equal(20, events[2].EventId); // ObjectRented
        Assert.Equal(2, events[3].EventId); // SessionDisposed
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    internal class TestEventListener : EventListener
    {
        private readonly ConcurrentQueue<EventWrittenEventArgs> _events = new();

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "HotChocolate-Fusion-Result-Pools")
            {
                // Enable all events for testing
                EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            _events.Enqueue(eventData);
        }

        public List<EventWrittenEventArgs> GetEvents()
        {
            var events = new List<EventWrittenEventArgs>();
            while (_events.TryDequeue(out var eventData))
            {
                events.Add(eventData);
            }

            return events;
        }
    }
}
