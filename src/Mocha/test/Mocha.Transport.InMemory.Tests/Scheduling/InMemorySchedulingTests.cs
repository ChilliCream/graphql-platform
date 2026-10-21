using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using Mocha.Middlewares;

namespace Mocha.Transport.InMemory.Tests.Scheduling;

public class InMemorySchedulingTests
{
    private static readonly TimeSpan s_deliveryTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan s_notDeliveredWindow = TimeSpan.FromMilliseconds(500);
    private static readonly DateTimeOffset s_start = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ScheduledMessage_Should_NotBeDelivered_When_BeforeDueTime()
    {
        // arrange
        var recorder = new MessageRecorder();
        var time = new FakeTimeProvider(s_start);
        await using var env = await CreateBusAsync(recorder, time);

        // act
        await PublishScheduledAsync(env.Provider, s_start.AddMinutes(10));

        // assert
        Assert.False(
            await recorder.WaitAsync(s_notDeliveredWindow),
            "Message must not be delivered before its due time");
    }

    [Fact]
    public async Task ScheduledMessage_Should_BeDelivered_When_DueTimeReached()
    {
        // arrange
        var recorder = new MessageRecorder();
        var time = new FakeTimeProvider(s_start);
        await using var env = await CreateBusAsync(recorder, time);
        await PublishScheduledAsync(env.Provider, s_start.AddMinutes(10));

        // act
        time.Advance(TimeSpan.FromMinutes(11));

        // assert
        Assert.True(
            await recorder.WaitAsync(s_deliveryTimeout),
            "Message must be delivered once its due time is reached");
        var received = Assert.Single(recorder.Messages.OfType<ScheduledPing>());
        Assert.Equal("ping", received.Text);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ScheduledMessage_Should_RunDispatchMiddlewareOnce_When_PublishedOrSent(bool send)
    {
        // arrange
        var recorder = new MessageRecorder();
        var time = new FakeTimeProvider(s_start);
        var beforeSerialization = 0;
        var afterScheduling = 0;
        var received = new TaskCompletionSource<MessageEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var env = await CreateBusAsync(recorder, time, builder =>
        {
            builder.AddRequestHandler<ScheduledPingRequestHandler>();
            builder.ConfigureMessageBus(bus =>
            {
                bus.UseDispatch(new DispatchMiddlewareConfiguration(
                    (_, next) => context =>
                    {
                        beforeSerialization++;
                        context.Message = new ScheduledPing { Text = "prepared" };
                        context.Headers.Set("prepared-header", "stored");
                        return next(context);
                    },
                    "PrepareScheduledMessage"), before: "Serialization");
                bus.UseDispatch(new DispatchMiddlewareConfiguration(
                    (_, next) => context =>
                    {
                        afterScheduling++;
                        return next(context);
                    },
                    "ObserveScheduledMessage"), after: "Scheduling");
                bus.UseReceive(new ReceiveMiddlewareConfiguration(
                    (_, next) => async context =>
                    {
                        var envelope = MessageEnvelope.CopyWithOwnedBody(context.Envelope!);
                        await next(context);
                        received.TrySetResult(envelope);
                    },
                    "CaptureScheduledMessage"));
            });
        });
        using var scope = env.Provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        if (send)
        {
            await bus.ScheduleSendAsync(
                new ScheduledPing { Text = "original" },
                s_start.AddMinutes(10),
                TestContext.Current.CancellationToken);
        }
        else
        {
            await bus.SchedulePublishAsync(
                new ScheduledPing { Text = "original" },
                s_start.AddMinutes(10),
                TestContext.Current.CancellationToken);
        }

        var atInvocation = new { BeforeSerialization = beforeSerialization, AfterScheduling = afterScheduling };
        time.Advance(TimeSpan.FromMinutes(11));
        var delivered = await received.Task.WaitAsync(s_deliveryTimeout, TestContext.Current.CancellationToken);

        // assert
        new
        {
            AtInvocation = atInvocation,
            AtDelivery = new { BeforeSerialization = beforeSerialization, AfterScheduling = afterScheduling },
            Body = System.Text.Encoding.UTF8.GetString(delivered.Body.Span),
            Header = delivered.Headers?.GetValue("prepared-header"),
            SentAt = delivered.SentAt?.ToString("O"),
            ScheduledTime = delivered.ScheduledTime?.ToString("O")
        }.MatchInlineSnapshot(
            """
            {
              "AtInvocation": {
                "BeforeSerialization": 1,
                "AfterScheduling": 1
              },
              "AtDelivery": {
                "BeforeSerialization": 1,
                "AfterScheduling": 1
              },
              "Body": "{\"Text\":\"prepared\"}",
              "Header": "stored",
              "SentAt": "2026-06-01T12:11:00.0000000+00:00",
              "ScheduledTime": "2026-06-01T12:10:00.0000000+00:00"
            }
            """);
    }

    [Fact]
    public async Task CancelScheduledMessage_Should_PreventDelivery_When_BeforeDueTime()
    {
        // arrange
        var recorder = new MessageRecorder();
        var time = new FakeTimeProvider(s_start);
        await using var env = await CreateBusAsync(recorder, time);
        var token = await ScheduleCancellableAsync(env.Provider, s_start.AddMinutes(10));

        // act
        using (var scope = env.Provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            var cancelled = await bus.CancelScheduledMessageAsync(
                token,
                TestContext.Current.CancellationToken);
            Assert.True(cancelled);
        }

        time.Advance(TimeSpan.FromMinutes(11));

        // assert
        Assert.False(
            await recorder.WaitAsync(s_notDeliveredWindow),
            "Cancelled message must never be delivered");
    }

    [Fact]
    public async Task ScheduledMessage_Should_PreserveBody_When_PooledContextReusedBeforeDue()
    {
        // arrange
        // schedule one message, then dispatch another immediately so the pooled dispatch context is
        // reused; the scheduled body must survive because the store deep-copied it.
        var recorder = new MessageRecorder();
        var time = new FakeTimeProvider(s_start);
        await using var env = await CreateBusAsync(recorder, time);

        using (var scope = env.Provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(
                new ScheduledPing { Text = "scheduled" },
                new PublishOptions { ScheduledTime = s_start.AddMinutes(10) },
                TestContext.Current.CancellationToken);
            await bus.PublishAsync(
                new ScheduledPing { Text = "immediate" },
                TestContext.Current.CancellationToken);
        }

        Assert.True(
            await recorder.WaitAsync(s_deliveryTimeout),
            "The immediate message should be delivered");

        // act
        time.Advance(TimeSpan.FromMinutes(11));

        // assert
        Assert.True(
            await recorder.WaitAsync(s_deliveryTimeout),
            "The scheduled message should be delivered after its due time");
        var texts = recorder.Messages
            .OfType<ScheduledPing>()
            .Select(p => p.Text)
            .Order()
            .ToList();
        Assert.Equal(["immediate", "scheduled"], texts);
    }

    [Fact]
    public async Task ScheduledMessage_Should_BeDroppedAndWorkerContinue_When_DispatchThrows()
    {
        // arrange
        var recorder = new MessageRecorder();
        var time = new FakeTimeProvider(s_start);
        await using var env = await CreateBusAsync(
            recorder,
            time,
            AddInvalidReplyDestination);

        await PublishScheduledAsync(env.Provider, s_start.AddMinutes(5), "dropped");
        time.Advance(TimeSpan.FromMinutes(6));

        await PublishScheduledAsync(env.Provider, s_start.AddMinutes(10), "delivered");

        // act
        time.Advance(TimeSpan.FromMinutes(6));

        // assert
        Assert.True(
            await recorder.WaitAsync(s_deliveryTimeout),
            "The worker should survive a dispatch failure and deliver later messages");
        var received = Assert.Single(recorder.Messages.OfType<ScheduledPing>());
        Assert.Equal("delivered", received.Text);
    }

    private static async Task PublishScheduledAsync(
        IServiceProvider provider,
        DateTimeOffset when,
        string text = "ping")
    {
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(
            new ScheduledPing { Text = text },
            new PublishOptions { ScheduledTime = when },
            TestContext.Current.CancellationToken);
    }

    private static async Task<string> ScheduleCancellableAsync(IServiceProvider provider, DateTimeOffset when)
    {
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var result = await bus.SchedulePublishAsync(
            new ScheduledPing { Text = "ping" },
            when,
            TestContext.Current.CancellationToken);

        return result.Token!;
    }

    private static async Task<TestEnvironment> CreateBusAsync(
        MessageRecorder recorder,
        FakeTimeProvider time,
        Action<IMessageBusHostBuilder>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        services.AddLogging();
        services.AddSingleton<TimeProvider>(time);

        var builder = services.AddMessageBus();
        builder.AddEventHandler<ScheduledPingHandler>();
        builder.AddInMemory();
        configure?.Invoke(builder);

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestContext.Current.CancellationToken);

        var hostedServices = provider.GetServices<IHostedService>().ToList();
        foreach (var svc in hostedServices)
        {
            await svc.StartAsync(TestContext.Current.CancellationToken);
        }

        return new TestEnvironment(provider, hostedServices);
    }

    private static void AddInvalidReplyDestination(IMessageBusHostBuilder builder)
    {
        builder.ConfigureMessageBus(bus =>
            bus.UseDispatch(new DispatchMiddlewareConfiguration(
                (_, next) => context =>
                {
                    if (context.Message is ScheduledPing { Text: "dropped" })
                    {
                        context.Headers.Set("message-kind", "reply");
                        context.DestinationAddress = new Uri("memory://localhost/q/missing");
                    }

                    return next(context);
                },
                "InvalidReplyDestination"), before: "Serialization"));
    }

    public sealed class ScheduledPing
    {
        public required string Text { get; init; }
    }

    public sealed class ScheduledPingHandler(MessageRecorder recorder) : IEventHandler<ScheduledPing>
    {
        public ValueTask HandleAsync(ScheduledPing message, CancellationToken cancellationToken)
        {
            recorder.Record(message);

            return default;
        }
    }

    public sealed class ScheduledPingRequestHandler(MessageRecorder recorder) : IEventRequestHandler<ScheduledPing>
    {
        public ValueTask HandleAsync(ScheduledPing message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    private sealed class TestEnvironment(ServiceProvider provider, List<IHostedService> hostedServices)
        : IAsyncDisposable
    {
        public ServiceProvider Provider => provider;

        public async ValueTask DisposeAsync()
        {
            // Stop hosted services in reverse registration order, matching IHost shutdown, so the
            // scheduled message worker stops before the messaging runtime it dispatches through.
            for (var i = hostedServices.Count - 1; i >= 0; i--)
            {
                await hostedServices[i].StopAsync(default);
            }

            await provider.DisposeAsync();
        }
    }
}
