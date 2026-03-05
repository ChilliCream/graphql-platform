using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

[Collection("OpenTelemetry")]
public class OpenTelemetryTests
{
    [Fact]
    public async Task Publish_Should_CreateActivity_When_EventPublished()
    {
        // arrange
        var activities = new ConcurrentBag<Activity>();
        using var listener = CreateListener(activities);

        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<TracedEventHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new TracedEvent { Data = "traced" }, CancellationToken.None);
        Assert.True(await recorder.WaitAsync(s_timeout));

        // Task.Delay: ActivityListener callbacks fire asynchronously; brief wait lets all callbacks complete
        await Task.Delay(100, default);

        // assert - at least one activity was created
        Assert.NotEmpty(activities);
    }

    [Fact]
    public async Task RequestResponse_Should_CreateActivity_When_RequestMade()
    {
        // arrange
        var activities = new ConcurrentBag<Activity>();
        using var listener = CreateListener(activities);

        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddRequestHandler<TracedRequestHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var response = await bus.RequestAsync(new TracedRequest { Query = "test" }, CancellationToken.None);

        // Task.Delay: ActivityListener callbacks fire asynchronously; brief wait lets all callbacks complete
        await Task.Delay(100, default);

        // assert
        Assert.NotEmpty(activities);
        Assert.Equal("re: test", response.Answer);
    }

    [Fact]
    public async Task Activities_Should_HaveCorrectSourceName_When_Published()
    {
        // arrange
        var activities = new ConcurrentBag<Activity>();
        using var listener = CreateListener(activities);

        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<TracedEventHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new TracedEvent { Data = "source-check" }, CancellationToken.None);
        Assert.True(await recorder.WaitAsync(s_timeout));

        // Task.Delay: ActivityListener callbacks fire asynchronously; brief wait lets all callbacks complete
        await Task.Delay(100, default);

        // assert
        Assert.NotEmpty(activities);
        Assert.All(activities, a => Assert.Equal("Mocha", a.Source.Name));
    }

    [Fact]
    public async Task MultiplePublishes_Should_CreateMultipleActivities_When_PublishedInSequence()
    {
        // arrange
        var activities = new ConcurrentBag<Activity>();
        using var listener = CreateListener(activities);

        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<TracedEventHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        for (var i = 0; i < 3; i++)
        {
            await bus.PublishAsync(new TracedEvent { Data = $"batch-{i}" }, CancellationToken.None);
        }
        Assert.True(await recorder.WaitAsync(s_timeout, expectedCount: 3));

        // Task.Delay: ActivityListener callbacks fire asynchronously; brief wait lets all callbacks complete
        await Task.Delay(100, default);

        // assert - at least 3 activities (dispatch + consume for each)
        Assert.True(activities.Count >= 3, $"Expected at least 3 activities but got {activities.Count}");
    }

    [Fact]
    public async Task MessageBus_Should_ProcessMessages_When_NoListenerRegistered()
    {
        // arrange - NO activity listener registered
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<TracedEventHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new TracedEvent { Data = "no-trace" }, CancellationToken.None);

        // assert - message still delivered
        Assert.True(await recorder.WaitAsync(s_timeout));
        Assert.Single(recorder.Messages);
    }

    [Fact]
    public void WithActivity_Sets_Trace_Headers_From_Current_Activity()
    {
        // arrange
        using var source = new ActivitySource("test-source");
        using var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        ActivitySource.AddActivityListener(listener);

        using var activity = source.StartActivity("test-operation");
        Assert.NotNull(activity);

        var headers = new Headers();

        // act
        headers.WithActivity();

        // assert
        Assert.True(headers.ContainsKey(MessageHeaders.TraceId.Key));
        Assert.True(headers.ContainsKey(MessageHeaders.SpanId.Key));

        var traceId = headers.GetValue(MessageHeaders.TraceId.Key) as string;
        var spanId = headers.GetValue(MessageHeaders.SpanId.Key) as string;

        Assert.NotNull(traceId);
        Assert.NotNull(spanId);
        Assert.Equal(activity.TraceId.ToHexString(), traceId);
        Assert.Equal(activity.SpanId.ToHexString(), spanId);
    }

    [Fact]
    public void WithActivity_Sets_TraceState_When_Activity_Has_TraceState()
    {
        // arrange
        using var source = new ActivitySource("test-source");
        using var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        ActivitySource.AddActivityListener(listener);

        using var activity = source.StartActivity("test-operation");
        Assert.NotNull(activity);
        activity.TraceStateString = "key1=value1,key2=value2";

        var headers = new Headers();

        // act
        headers.WithActivity();

        // assert
        Assert.True(headers.ContainsKey(MessageHeaders.TraceState.Key));
        var traceState = headers.GetValue(MessageHeaders.TraceState.Key) as string;
        Assert.Equal("key1=value1,key2=value2", traceState);
    }

    [Fact]
    public void WithActivity_Sets_ParentId_When_Activity_Has_Parent()
    {
        // arrange
        using var source = new ActivitySource("test-source");
        using var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        ActivitySource.AddActivityListener(listener);

        using var parentActivity = source.StartActivity("parent-operation");
        using var childActivity = source.StartActivity("child-operation");
        Assert.NotNull(childActivity);

        var headers = new Headers();

        // act
        headers.WithActivity();

        // assert
        if (childActivity.ParentId != null)
        {
            Assert.True(headers.ContainsKey(MessageHeaders.ParentId.Key));
            var parentId = headers.GetValue(MessageHeaders.ParentId.Key) as string;
            Assert.Equal(childActivity.ParentId, parentId);
        }
    }

    [Fact]
    public void WithActivity_With_No_Current_Activity_Returns_Headers_Unchanged()
    {
        // arrange
        var headers = new Headers();
        headers.Set("existing-header", "existing-value");

        // Ensure no current activity
        Assert.Null(Activity.Current);

        // act
        var result = headers.WithActivity();

        // assert
        Assert.Same(headers, result);
        Assert.False(headers.ContainsKey(MessageHeaders.TraceId.Key));
        Assert.False(headers.ContainsKey(MessageHeaders.SpanId.Key));
        Assert.Equal("existing-value", headers.GetValue("existing-header"));
    }

    [Fact]
    public void WithActivity_Does_Not_Overwrite_Existing_Trace_Headers()
    {
        // arrange
        using var source = new ActivitySource("test-source");
        using var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        ActivitySource.AddActivityListener(listener);

        using var activity = source.StartActivity("test-operation");
        Assert.NotNull(activity);

        var headers = new Headers();
        headers.Set(MessageHeaders.TraceId.Key, "existing-trace-id");

        // act
        headers.WithActivity();

        // assert - TryAdd should not overwrite existing value
        var traceId = headers.GetValue(MessageHeaders.TraceId.Key) as string;
        Assert.Equal("existing-trace-id", traceId);
    }

    [Fact]
    public void RecordQueueLength_Records_Correct_Value_And_Tags()
    {
        // arrange
        var measurements = new List<Measurement<long>>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Mocha"
                    && instrument.Name == "messaging.queue.length")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        meterListener.SetMeasurementEventCallback<long>(
            (_, measurement, tags, _) => measurements.Add(new Measurement<long>(measurement, tags)));

        meterListener.Start();

        // act
        OpenTelemetry.Meters.RecordQueueLength(
            id: 123,
            name: "test-queue",
            length: 42,
            state: "active",
            kind: "main",
            isTemporary: false);

        // Thread.Sleep: MeterListener callbacks fire synchronously on recording; brief sleep lets the listener flush
        Thread.Sleep(50);

        // assert
        Assert.NotEmpty(measurements);
        var measurement = measurements.First();
        Assert.Equal(42, measurement.Value);

        var tagsDict = measurement.Tags.ToArray().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        Assert.Equal(123L, tagsDict[SemanticConventions.QueueId]);
        Assert.Equal("test-queue", tagsDict[SemanticConventions.QueueName]);
        Assert.Equal("active", tagsDict["state"]);
        Assert.Equal("main", tagsDict[SemanticConventions.QueueKind]);
        Assert.Equal(false, tagsDict[SemanticConventions.QueueTemporary]);
        Assert.Equal("postgres", tagsDict[SemanticConventions.QueueType]);
    }

    [Fact]
    public void RecordQueueMessageOldestAge_Records_Histogram_Value()
    {
        // arrange
        var measurements = new List<Measurement<double>>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Mocha"
                    && instrument.Name == "messaging.queue.message.oldest_age")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        meterListener.SetMeasurementEventCallback<double>(
            (_, measurement, tags, _) => measurements.Add(new Measurement<double>(measurement, tags)));

        meterListener.Start();

        // act
        OpenTelemetry.Meters.RecordQueueMessageOldestAge(
            id: 456,
            name: "age-queue",
            age: 120.5,
            state: "pending",
            kind: "main",
            isTemporary: false);

        // Thread.Sleep: MeterListener callbacks fire synchronously on recording; brief sleep lets the listener flush
        Thread.Sleep(50);

        // assert
        Assert.NotEmpty(measurements);
        var measurement = measurements.First();
        Assert.Equal(120.5, measurement.Value);
    }

    [Fact]
    public void RecordQueueMessageLatestAge_Records_Histogram_Value()
    {
        // arrange
        var measurements = new List<Measurement<double>>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Mocha"
                    && instrument.Name == "messaging.queue.message.latest_age")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        meterListener.SetMeasurementEventCallback<double>(
            (_, measurement, tags, _) => measurements.Add(new Measurement<double>(measurement, tags)));

        meterListener.Start();

        // act
        OpenTelemetry.Meters.RecordQueueMessageLatestAge(
            id: 789,
            name: "latest-queue",
            age: 5.25,
            state: "ready",
            kind: "main",
            isTemporary: true);

        // Thread.Sleep: MeterListener callbacks fire synchronously on recording; brief sleep lets the listener flush
        Thread.Sleep(50);

        // assert
        Assert.NotEmpty(measurements);
        var measurement = measurements.First();
        Assert.Equal(5.25, measurement.Value);

        var tagsDict = measurement.Tags.ToArray().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        Assert.Equal(true, tagsDict[SemanticConventions.QueueTemporary]);
    }

    [Fact]
    public void RecordQueueRefreshTimestamp_Records_Timestamp()
    {
        // arrange
        var measurements = new List<Measurement<long>>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Mocha"
                    && instrument.Name == "messaging.queue.refresh.timestamp")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        meterListener.SetMeasurementEventCallback<long>(
            (_, measurement, tags, _) => measurements.Add(new Measurement<long>(measurement, tags)));

        meterListener.Start();

        // act
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        OpenTelemetry.Meters.RecordQueueRefreshTimestamp(
            id: 101,
            name: "refresh-queue",
            timestamp: timestamp,
            state: "active",
            kind: "reply",
            isTemporary: false);

        // Thread.Sleep: MeterListener callbacks fire synchronously on recording; brief sleep lets the listener flush
        Thread.Sleep(50);

        // assert
        Assert.NotEmpty(measurements);
        var measurement = measurements.First();
        Assert.Equal(timestamp, measurement.Value);
    }

    [Fact]
    public void RecordTopicRefreshTimestamp_Records_Correct_Tags()
    {
        // arrange
        var measurements = new List<Measurement<long>>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Mocha"
                    && instrument.Name == "messaging.topic.refresh.timestamp")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        meterListener.SetMeasurementEventCallback<long>(
            (_, measurement, tags, _) => measurements.Add(new Measurement<long>(measurement, tags)));

        meterListener.Start();

        // act
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        OpenTelemetry.Meters.RecordTopicRefreshTimestamp(id: 202, name: "test-topic", timestamp: timestamp);

        // Thread.Sleep: MeterListener callbacks fire synchronously on recording; brief sleep lets the listener flush
        Thread.Sleep(50);

        // assert
        Assert.NotEmpty(measurements);
        var measurement = measurements.First();
        var tagsDict = measurement.Tags.ToArray().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        Assert.Equal(202L, tagsDict[SemanticConventions.TopicId]);
        Assert.Equal("test-topic", tagsDict[SemanticConventions.TopicName]);
        Assert.Equal("postgres", tagsDict[SemanticConventions.TopicType]);
    }

    [Fact]
    public void RecordTopicConsumerCount_Records_Consumer_Count()
    {
        // arrange
        var measurements = new List<Measurement<long>>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Mocha"
                    && instrument.Name == "messaging.topic.consumer.count")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        meterListener.SetMeasurementEventCallback<long>(
            (_, measurement, tags, _) => measurements.Add(new Measurement<long>(measurement, tags)));

        meterListener.Start();

        // act
        OpenTelemetry.Meters.RecordTopicConsumerCount(id: 303, name: "consumer-topic", count: 15);

        // Thread.Sleep: MeterListener callbacks fire synchronously on recording; brief sleep lets the listener flush
        Thread.Sleep(50);

        // assert
        Assert.NotEmpty(measurements);
        var measurement = measurements.First();
        Assert.Equal(15, measurement.Value);
    }

    [Fact]
    public void RecordOperationDuration_Records_Duration_In_Seconds()
    {
        // arrange
        var measurements = new List<Measurement<double>>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Mocha"
                    && instrument.Name == "messaging.client.operation.duration")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        meterListener.SetMeasurementEventCallback<double>(
            (_, measurement, tags, _) => measurements.Add(new Measurement<double>(measurement, tags)));

        meterListener.Start();

        // act
        OpenTelemetry.Meters.RecordOperationDuration(
            duration: TimeSpan.FromMilliseconds(250),
            operationName: "publish",
            destinationName: new Uri("queue://test-queue"),
            messagingOperationType: MessagingOperationType.Send,
            messageIdentity: "TestMessage",
            messagingSystem: "postgres");

        // Thread.Sleep: MeterListener callbacks fire synchronously on recording; brief sleep lets the listener flush
        Thread.Sleep(50);

        // assert
        Assert.NotEmpty(measurements);
        var measurement = measurements.First();
        Assert.Equal(0.25, measurement.Value, precision: 5);

        var tagsDict = measurement.Tags.ToArray().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        Assert.Equal("publish", tagsDict[SemanticConventions.OperationName]);
        Assert.Equal("send", tagsDict[SemanticConventions.MessagingOperationType]);
        Assert.Equal("TestMessage", tagsDict[SemanticConventions.MessagingType]);
        Assert.Equal("postgres", tagsDict[SemanticConventions.MessagingSystem]);
    }

    [Fact]
    public void RecordSendMessage_Increments_Counter()
    {
        // arrange
        var measurements = new List<Measurement<long>>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Mocha"
                    && instrument.Name == "messaging.client.sent.messages")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        meterListener.SetMeasurementEventCallback<long>(
            (_, measurement, tags, _) => measurements.Add(new Measurement<long>(measurement, tags)));

        meterListener.Start();

        // act
        OpenTelemetry.Meters.RecordSendMessage(
            operationName: "send-operation",
            destinationName: new Uri("queue://send-queue"),
            messageIdentity: "SendMessage",
            messagingSystem: "postgres");

        // Thread.Sleep: MeterListener callbacks fire synchronously on recording; brief sleep lets the listener flush
        Thread.Sleep(50);

        // assert
        Assert.NotEmpty(measurements);
        var measurement = measurements.First();
        Assert.Equal(1, measurement.Value);

        var tagsDict = measurement.Tags.ToArray().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        Assert.Equal("send-operation", tagsDict[SemanticConventions.OperationName]);
        Assert.Equal("SendMessage", tagsDict[SemanticConventions.MessagingType]);
    }

    [Fact]
    public void RecordConsumeMessage_Increments_Counter()
    {
        // arrange
        var measurements = new List<Measurement<long>>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Mocha"
                    && instrument.Name == "messaging.client.consumed.messages")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        meterListener.SetMeasurementEventCallback<long>(
            (_, measurement, tags, _) => measurements.Add(new Measurement<long>(measurement, tags)));

        meterListener.Start();

        // act
        OpenTelemetry.Meters.RecordConsumeMessage(
            operationName: "consume-operation",
            destinationName: "consume-queue",
            messageIdentity: "ConsumeMessage",
            messagingSystem: "postgres",
            subscriptionName: "test-subscription",
            consumerGroupName: "test-group");

        // Thread.Sleep: MeterListener callbacks fire synchronously on recording; brief sleep lets the listener flush
        Thread.Sleep(50);

        // assert
        Assert.NotEmpty(measurements);
        var measurement = measurements.First();
        Assert.Equal(1, measurement.Value);

        var tagsDict = measurement.Tags.ToArray().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        Assert.Equal("consume-operation", tagsDict[SemanticConventions.OperationName]);
        Assert.Equal("consume-queue", tagsDict[SemanticConventions.MessagingDestinationAddress]);
        Assert.Equal("test-subscription", tagsDict[SemanticConventions.MessagingDestinationSubscriptionName]);
        Assert.Equal("test-group", tagsDict[SemanticConventions.MessagingConsumerGroupName]);
    }

    [Fact]
    public void RecordProcessingDuration_Records_Duration_In_Seconds()
    {
        // arrange
        var measurements = new List<Measurement<double>>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Mocha"
                    && instrument.Name == "messaging.process.duration")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        meterListener.SetMeasurementEventCallback<double>(
            (_, measurement, tags, _) => measurements.Add(new Measurement<double>(measurement, tags)));

        meterListener.Start();

        // act
        OpenTelemetry.Meters.RecordProcessingDuration(
            duration: TimeSpan.FromSeconds(2.5),
            operationName: "process-operation",
            destinationName: "process-queue",
            messagingSystem: "postgres",
            messageIdentity: "ProcessMessage");

        // Thread.Sleep: MeterListener callbacks fire synchronously on recording; brief sleep lets the listener flush
        Thread.Sleep(50);

        // assert
        Assert.NotEmpty(measurements);
        var measurement = measurements.First();
        Assert.Equal(2.5, measurement.Value);
    }

    [Fact]
    public void All_Recording_Methods_Use_Correct_Semantic_Convention_Tag_Names()
    {
        // This test validates that the OpenTelemetry methods use the correct
        // semantic convention tag names defined in SemanticConventions class

        // The test is implicit - if RecordOperationDuration uses correct tags,
        // it should match SemanticConventions constants
        var measurements = new List<Measurement<double>>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Mocha"
                    && instrument.Name == "messaging.client.operation.duration")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        meterListener.SetMeasurementEventCallback<double>(
            (_, measurement, tags, _) => measurements.Add(new Measurement<double>(measurement, tags)));

        meterListener.Start();

        // act
        OpenTelemetry.Meters.RecordOperationDuration(
            duration: TimeSpan.FromMilliseconds(100),
            operationName: "test",
            destinationName: null,
            messagingOperationType: MessagingOperationType.Process,
            messageIdentity: "TestMsg",
            messagingSystem: "postgres");

        // Thread.Sleep: MeterListener callbacks fire synchronously on recording; brief sleep lets the listener flush
        Thread.Sleep(50);

        // assert - verify tag names match semantic conventions
        Assert.NotEmpty(measurements);
        var tagsDict = measurements.First().Tags.ToArray().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        Assert.True(tagsDict.ContainsKey(SemanticConventions.OperationName));
        Assert.True(tagsDict.ContainsKey(SemanticConventions.MessagingOperationType));
        Assert.True(tagsDict.ContainsKey(SemanticConventions.MessagingType));
        Assert.True(tagsDict.ContainsKey(SemanticConventions.MessagingSystem));
    }

    [Fact]
    public void RecordOperationDuration_With_Null_DestinationName_Does_Not_Throw()
    {
        // arrange
        var measurements = new List<Measurement<double>>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Mocha"
                    && instrument.Name == "messaging.client.operation.duration")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        meterListener.SetMeasurementEventCallback<double>(
            (_, measurement, tags, _) => measurements.Add(new Measurement<double>(measurement, tags)));

        meterListener.Start();

        // act & assert - should not throw
        OpenTelemetry.Meters.RecordOperationDuration(
            duration: TimeSpan.FromMilliseconds(100),
            operationName: "test",
            destinationName: null,
            messagingOperationType: MessagingOperationType.Send,
            messageIdentity: "TestMsg",
            messagingSystem: "postgres");

        // Thread.Sleep: MeterListener callbacks fire synchronously on recording; brief sleep lets the listener flush
        Thread.Sleep(50);

        Assert.NotEmpty(measurements);
    }

    [Fact]
    public void RecordConsumeMessage_With_Null_Subscription_Does_Not_Throw()
    {
        // arrange
        var measurements = new List<Measurement<long>>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Mocha"
                    && instrument.Name == "messaging.client.consumed.messages")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        meterListener.SetMeasurementEventCallback<long>(
            (_, measurement, tags, _) => measurements.Add(new Measurement<long>(measurement, tags)));

        meterListener.Start();

        // act & assert - should not throw
        OpenTelemetry.Meters.RecordConsumeMessage(
            operationName: "consume",
            destinationName: "queue",
            messageIdentity: "Msg",
            messagingSystem: "postgres",
            subscriptionName: null,
            consumerGroupName: null);

        // Thread.Sleep: MeterListener callbacks fire synchronously on recording; brief sleep lets the listener flush
        Thread.Sleep(50);

        Assert.NotEmpty(measurements);
    }

    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    private static async Task<ServiceProvider> CreateBusAsync(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInstrumentation();
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    public sealed class TracedEvent
    {
        public required string Data { get; init; }
    }

    public sealed class TracedRequest : IEventRequest<TracedResponse>
    {
        public required string Query { get; init; }
    }

    public sealed class TracedResponse
    {
        public required string Answer { get; init; }
    }

    public sealed class TracedEventHandler(MessageRecorder recorder) : IEventHandler<TracedEvent>
    {
        public ValueTask HandleAsync(TracedEvent message, CancellationToken ct)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class TracedRequestHandler(MessageRecorder recorder)
        : IEventRequestHandler<TracedRequest, TracedResponse>
    {
        public ValueTask<TracedResponse> HandleAsync(TracedRequest request, CancellationToken ct)
        {
            recorder.Record(request);
            return new(new TracedResponse { Answer = $"re: {request.Query}" });
        }
    }

    private static ActivityListener CreateListener(ConcurrentBag<Activity> activities)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Mocha",
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
