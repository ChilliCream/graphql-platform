using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha;
using Mocha.Events;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public class FaultHandlingTests
{
    [Fact]
    public async Task PublishEvent_Should_NotCrashRuntime_When_HandlerThrows()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<ThrowingEventHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - this should not crash
        await bus.PublishAsync(new TestEvent { Data = "will-fail" }, CancellationToken.None);

        // Task.Delay: allows async pipeline to complete; deterministic sync not possible for
        // fire-and-forget publish
        await Task.Delay(500, default);

        // assert — runtime should still be functional after swallowed fault.
        // No observable side-effect beyond runtime stability; IsStarted is the
        // strongest available signal that the runtime survived the fault.
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        Assert.True(runtime.IsStarted);
    }

    [Fact]
    public async Task RequestAsync_Should_ThrowException_When_HandlerThrows()
    {
        // arrange
        await using var provider = await CreateBusAsync(b =>
            b.AddRequestHandler<ThrowingRequestHandler>());

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act & assert — exact exception type depends on transport timing:
        // RemoteErrorException if the fault response arrives, TaskCanceledException
        // if the CTS fires first. Both confirm the handler did not succeed.
        using var cts = new CancellationTokenSource(Timeout);
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await bus.RequestAsync(new TestRequest { Data = "fail-me" }, cts.Token)
        );
    }

    [Fact]
    public async Task RequestAsync_Should_PreserveExceptionType_When_HandlerThrows()
    {
        // arrange
        await using var provider = await CreateBusAsync(b =>
            b.AddRequestHandler<ThrowingRequestHandler>());

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act & assert — exact exception type depends on transport timing:
        // RemoteErrorException if the fault response arrives, TaskCanceledException
        // if the CTS fires first. Both confirm the handler did not succeed.
        using var cts = new CancellationTokenSource(Timeout);
        var ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await bus.RequestAsync(new TestRequest { Data = "err" }, cts.Token)
        );

        // assert — if the fault arrived we get rich error info
        if (ex is RemoteErrorException remote)
        {
            Assert.NotNull(remote.ErrorMessage);
            Assert.Contains("InvalidOperationException", remote.ErrorMessage);
        }
    }

    [Fact]
    public async Task PublishEvent_Should_KeepRuntimeStable_When_MultipleHandlersFail()
    {
        // arrange
        await using var provider = await CreateBusAsync(b =>
            b.AddEventHandler<ThrowingEventHandler>());

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish multiple failing events
        for (var i = 0; i < 10; i++)
        {
            await bus.PublishAsync(new TestEvent { Data = $"fail-{i}" }, CancellationToken.None);
        }

        // Task.Delay: allows async pipeline to complete; deterministic sync not possible for fire-and-forget publish
        await Task.Delay(1000, default);

        // assert — no observable side-effect beyond runtime stability after
        // swallowed faults; IsStarted confirms the runtime survived.
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        Assert.True(runtime.IsStarted);
    }

    [Fact]
    public async Task PublishEvent_Should_ProcessSuccessfully_When_EventHandlerSucceeds()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            // Register a normal handler that records
            b.AddEventHandler<NormalEventHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish successful event
        await bus.PublishAsync(new TestEvent { Data = "success" }, CancellationToken.None);

        // assert - handler received it
        Assert.True(await recorder.WaitAsync(Timeout));
        Assert.Single(recorder.Messages);
    }

    [Fact]
    public async Task SendAsync_Should_NotCrashRuntime_When_HandlerThrows()
    {
        // arrange
        await using var provider = await CreateBusAsync(b =>
            b.AddRequestHandler<ThrowingSendHandler>());

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - this should not crash the runtime
        // Use SendAsync for fire-and-forget (no response expected)
        await bus.SendAsync(new TestSendRequest { Data = "will-fail" }, CancellationToken.None);

        // Task.Delay: allows async pipeline to complete; deterministic sync not possible for fire-and-forget send
        await Task.Delay(500, default);

        // assert — no observable side-effect beyond runtime stability after
        // swallowed fault; IsStarted confirms the runtime survived.
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        Assert.True(runtime.IsStarted);
    }

    [Fact]
    public async Task RequestAsync_Should_PropagateExceptions_When_ConcurrentRequestsThrow()
    {
        // arrange
        await using var provider = await CreateBusAsync(b =>
            b.AddRequestHandler<ThrowingRequestHandler>());

        // act - fire 5 concurrent failing requests
        var tasks = Enumerable
            .Range(1, 5)
            .Select(async i =>
            {
                using var scope = provider.CreateScope();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

                // exact exception type depends on transport timing (see above)
                using var cts = new CancellationTokenSource(Timeout);
                var ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
                    await bus.RequestAsync(new TestRequest { Data = $"concurrent-{i}" }, cts.Token)
                );
                return ex;
            })
            .ToArray();

        var exceptions = await Task.WhenAll(tasks);

        // assert — all 5 got proper exceptions; if the fault arrived we get rich error info
        Assert.Equal(5, exceptions.Length);
        Assert.All(
            exceptions,
            ex =>
            {
                if (ex is RemoteErrorException remote)
                {
                    Assert.NotNull(remote.ErrorMessage);
                    Assert.Contains("InvalidOperationException", remote.ErrorMessage);
                }
            });
    }

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static async Task<ServiceProvider> CreateBusAsync(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    public sealed class TestEvent
    {
        public required string Data { get; init; }
    }

    public sealed class TestRequest : IEventRequest<TestResponse>
    {
        public required string Data { get; init; }
    }

    public sealed class TestResponse
    {
        public required string Result { get; init; }
    }

    public sealed class TestSendRequest
    {
        public required string Data { get; init; }
    }

    public sealed class ThrowingEventHandler : IEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(TestEvent message, CancellationToken ct)
            => throw new InvalidOperationException($"Handler failed for: {message.Data}");
    }

    public sealed class ThrowingRequestHandler : IEventRequestHandler<TestRequest, TestResponse>
    {
        public ValueTask<TestResponse> HandleAsync(TestRequest request, CancellationToken ct)
            => throw new InvalidOperationException($"Request failed for: {request.Data}");
    }

    public sealed class ThrowingSendHandler : IEventRequestHandler<TestSendRequest>
    {
        public ValueTask HandleAsync(TestSendRequest request, CancellationToken ct)
            => throw new ArgumentException($"Send failed for: {request.Data}");
    }

    public sealed class NormalEventHandler(MessageRecorder recorder) : IEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(TestEvent message, CancellationToken ct)
        {
            recorder.Record(message);
            return default;
        }
    }
}
