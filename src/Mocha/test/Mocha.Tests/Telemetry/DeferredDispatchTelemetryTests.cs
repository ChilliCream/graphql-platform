using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Telemetry;

/// <summary>
/// Tests that a message dispatched from a consumer is reported as created when it is captured and
/// as sent when the consumer attempt releases it.
/// </summary>
[Collection("OpenTelemetry")]
public class DeferredDispatchTelemetryTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Dispatch_Should_ReportCreateUnderConsumerAndSendUnderRelease_When_ConsumerPublishes()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var activities = new ConcurrentBag<Activity>();
        using var listener = CreateListener(activities);
        await using var provider = await CreateBusAsync(b =>
        {
            b.AddRequestHandler<PublishingHandler>();
            b.AddEventHandler<HeldEventHandler>();
        });

        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var topic = runtime.GetPublishEndpoint(runtime.GetMessageType(typeof(HeldEvent))).Address;

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        // the root activity gives every span of this flow one trace id to filter on, and the
        // acknowledgement is released after the event, so its arrival means the event's send was reported
        using var root = new Activity("test").Start();
        await bus.RequestAsync(new StartPublish(), ct);
        var trace = activities.Where(a => a.TraceId == root.TraceId).ToList();

        // assert
        var create = Assert.Single(trace, a => a.DisplayName == $"{topic} create");
        var send = Assert.Single(
            trace,
            a => a.DisplayName == $"{topic} publish" && a.Parent?.DisplayName == "PublishingHandler dispatch");
        Assert.Equal("PublishingHandler consume", create.Parent?.DisplayName);
        Assert.Equal(
            ["create", "send"],
            new[] { create, send }.Select(a => (string?)a.GetTagItem(SemanticConventions.MessagingOperationType)));
        Assert.Equal(
            create.GetTagItem(SemanticConventions.MessagingMessageId),
            send.GetTagItem(SemanticConventions.MessagingMessageId));
    }

    [Fact]
    public async Task Dispatch_Should_CountSentMessageOnce_When_ConsumerPublishes()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var provider = await CreateBusAsync(b =>
        {
            b.AddRequestHandler<PublishingHandler>();
            b.AddEventHandler<HeldEventHandler>();
        });
        var identity = provider.GetRequiredService<IMessagingRuntime>().GetMessageType(typeof(HeldEvent)).Identity;
        var sent = new ConcurrentBag<long>();
        using var meterListener = CreateSentMessagesListener(identity, sent);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        // the acknowledgement is released after the event, so its arrival means the event's send was counted
        await bus.RequestAsync(new StartPublish(), ct);

        // assert
        Assert.Equal(1, Assert.Single(sent));
    }

    [Fact]
    public async Task Dispatch_Should_ReportError_When_HeldMessageFailsToLeave()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var errors = new DispatchErrorListener();
        var probe = new DeadAddressProbe();
        await using var provider = await CreateBusAsync(
            b =>
            {
                b.Services.AddSingleton(probe);
                b.AddDiagnosticEventListener(errors);
                b.AddEventHandler<ReplyingHandler>();
            },
            instrumentation: false);
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var markerIdentity = runtime.GetMessageType(typeof(Marker)).Identity;
        // the in-memory transport accepts the reply at capture time and rejects the address when it sends
        probe.DeadAddress = new Uri($"{runtime.Transports.Single().Schema}://localhost/nowhere");

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new StartReply(), ct);
        var (envelope, exception) = await errors.Error.Task.WaitAsync(s_timeout, ct);

        // assert
        Assert.Equal(markerIdentity, envelope?.MessageType);
        Assert.IsType<InvalidOperationException>(exception);
    }

    private static async Task<ServiceProvider> CreateBusAsync(
        Action<IMessageBusHostBuilder> configure,
        bool instrumentation = true)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);

        if (instrumentation)
        {
            builder.AddInstrumentation();
        }

        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
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

    private static MeterListener CreateSentMessagesListener(string messageIdentity, ConcurrentBag<long> sent)
    {
        var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == "Mocha" && instrument.Name == "messaging.client.sent.messages")
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };

        listener.SetMeasurementEventCallback<long>(
            (_, measurement, tags, _) =>
            {
                foreach (var tag in tags)
                {
                    if (tag.Key == SemanticConventions.MessagingType && Equals(tag.Value, messageIdentity))
                    {
                        sent.Add(measurement);
                    }
                }
            });

        listener.Start();
        return listener;
    }

    public sealed record StartPublish;

    public sealed record HeldEvent;

    public sealed record StartReply;

    public sealed record Marker;

    public sealed class PublishingHandler(IMessageBus bus) : IEventRequestHandler<StartPublish>
    {
        public async ValueTask HandleAsync(StartPublish request, CancellationToken cancellationToken)
            => await bus.PublishAsync(new HeldEvent(), cancellationToken);
    }

    public sealed class HeldEventHandler : IEventHandler<HeldEvent>
    {
        public ValueTask HandleAsync(HeldEvent message, CancellationToken cancellationToken) => default;
    }

    public sealed class DeadAddressProbe
    {
        public Uri DeadAddress { get; set; } = null!;
    }

    public sealed class ReplyingHandler(IMessageBus bus, DeadAddressProbe probe) : IEventHandler<StartReply>
    {
        public async ValueTask HandleAsync(StartReply message, CancellationToken cancellationToken)
        {
            var options = new ReplyOptions
            {
                ReplyAddress = probe.DeadAddress,
                CorrelationId = Guid.NewGuid().ToString()
            };

            await bus.ReplyAsync(new Marker(), options, cancellationToken);
        }
    }

    private sealed class DispatchErrorListener : MessagingDiagnosticEventListener
    {
        public TaskCompletionSource<(MessageEnvelope? Envelope, Exception Exception)> Error { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override void DispatchError(IDispatchContext context, Exception exception)
        {
            Error.TrySetResult((context.Envelope, exception));
        }
    }
}
