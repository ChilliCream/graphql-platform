using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Transport.InMemory;

namespace Mocha.Sagas.Tests;

/// <summary>
/// Tests that a reply reaching the shared reply endpoint without a pending request is reported when
/// nothing else claims it, and stays quiet when a saga route owns it.
/// </summary>
public class ReplyDiagnosticsTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task ReplyConsumer_Should_WarnAboutDiscardedReply_When_NoRouteClaimsIt()
    {
        // A saga that handles only fault replies leaves a successful reply unclaimed: it fails the
        // fault route's message type term and matches no request promise, so it is dropped.

        // arrange
        var logs = new CapturingLoggerProvider();
        var handlerRan = new TaskCompletionSource();
        await using var provider = await CreateBusAsync(logs, b =>
        {
            b.Services.AddSingleton(handlerRan);
            b.AddRequestHandler<DiagnosticsRequestHandler>();
            b.AddSaga<FaultOnlySaga>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new StartDiagnosticsEvent(), CancellationToken.None);
        await handlerRan.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        var entry = await logs.WaitForAsync(LogLevel.Warning, s_timeout);
        Assert.Contains("Discarded a reply", entry);
    }

    [Fact]
    public async Task ReplyConsumer_Should_StaySilent_When_SagaRouteClaimsTheReply()
    {
        // The happy path for every saga reply, so it must not warn.

        // arrange
        var logs = new CapturingLoggerProvider();
        var handlerRan = new TaskCompletionSource();
        await using var provider = await CreateBusAsync(logs, b =>
        {
            b.Services.AddSingleton(handlerRan);
            b.AddRequestHandler<DiagnosticsRequestHandler>();
            b.AddSaga<AnyReplySaga>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();

        // act
        await bus.PublishAsync(new StartDiagnosticsEvent(), CancellationToken.None);
        await handlerRan.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // wait for the saga to consume the reply and finalize, so the reply has been fully processed
        var deadline = DateTime.UtcNow + s_timeout;
        while (storage.Count != 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        // assert - the saga owned the reply, so the reply path reported nothing
        Assert.Equal(0, storage.Count);
        Assert.Equal(
            [],
            logs.Entries.Where(e => e.Level >= LogLevel.Warning).Select(e => e.Message).ToArray());
    }

    private static async Task<ServiceProvider> CreateBusAsync(
        CapturingLoggerProvider logs,
        Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(logs));
        services.AddInMemorySagas();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    public sealed class DiagnosticsState : SagaStateBase;

    public sealed class StartDiagnosticsEvent;

    public sealed record DiagnosticsRequest : IEventRequest<DiagnosticsResponse>;

    public sealed record DiagnosticsResponse;

    public sealed class DiagnosticsRequestHandler(TaskCompletionSource handlerRan)
        : IEventRequestHandler<DiagnosticsRequest, DiagnosticsResponse>
    {
        public ValueTask<DiagnosticsResponse> HandleAsync(
            DiagnosticsRequest request,
            CancellationToken cancellationToken)
        {
            handlerRan.TrySetResult();
            return new(new DiagnosticsResponse());
        }
    }

    /// <summary>
    /// Handles only fault replies, so a successful reply matches no route on the reply endpoint.
    /// </summary>
    public sealed class FaultOnlySaga : Saga<DiagnosticsState>
    {
        protected override void Configure(ISagaDescriptor<DiagnosticsState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<StartDiagnosticsEvent>()
                .StateFactory(_ => new DiagnosticsState())
                .Send((_, _) => new DiagnosticsRequest())
                .TransitionTo("Awaiting");

            descriptor.During("Awaiting").OnFault().TransitionTo("Failed");

            descriptor.Finally("Failed");
        }
    }

    public sealed class AnyReplySaga : Saga<DiagnosticsState>
    {
        protected override void Configure(ISagaDescriptor<DiagnosticsState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<StartDiagnosticsEvent>()
                .StateFactory(_ => new DiagnosticsState())
                .Send((_, _) => new DiagnosticsRequest())
                .TransitionTo("Awaiting");

            descriptor.During("Awaiting").OnAnyReply().TransitionTo("Done");
            descriptor.During("Awaiting").OnFault().TransitionTo("Done");

            descriptor.Finally("Done");
        }
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<(LogLevel Level, string Message)> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Entries);

        public void Dispose() { }

        public async Task<string> WaitForAsync(LogLevel level, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (Entries.FirstOrDefault(e => e.Level == level) is { Message: { } message })
                {
                    return message;
                }

                await Task.Delay(50, TestContext.Current.CancellationToken);
            }

            throw new TimeoutException($"No {level} log entry was written within {timeout}.");
        }

        private sealed class CapturingLogger(ConcurrentQueue<(LogLevel, string)> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
                => entries.Enqueue((logLevel, formatter(state, exception)));
        }
    }
}
