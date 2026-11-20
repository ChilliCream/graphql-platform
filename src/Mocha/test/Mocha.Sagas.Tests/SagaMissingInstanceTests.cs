using System.Collections.Concurrent;
using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Transport.InMemory;

namespace Mocha.Sagas.Tests;

/// <summary>
/// Tests how a saga handles a correlated event for which no saga instance exists.
/// </summary>
public class SagaMissingInstanceTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);
    private static readonly Guid s_orderId = new("6f1c2a4e-5b7d-4e8a-9c3f-2d1e0b9a8c7d");

    [Fact]
    public async Task Saga_Should_FaultWithoutCommit_When_InstanceIsMissing()
    {
        // arrange
        var logs = new CapturingLoggerProvider();
        var store = new TestSagaStore();
        await using var provider = await CreateBusAsync(
            logs,
            b =>
            {
                b.Services.AddSingleton<ISagaStore>(store);
                b.AddSaga<OrderSaga>();
            });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderConfirmed(s_orderId), CancellationToken.None);

        // assert
        var faulted = await ReadErrorQueueAsync(provider);
        var transaction = Assert.Single(store.Transactions);
        new
        {
            ExceptionType = faulted.Headers!.Get(MessageHeaders.Fault.ExceptionType),
            ExceptionMessage = faulted.Headers!.Get(MessageHeaders.Fault.Message),
            transaction.IsCommitted,
            transaction.IsDisposed,
            Logs = logs.GetSagaEntries()
        }.MatchInlineSnapshot(
            """
            {
              "ExceptionType": "Mocha.Sagas.SagaInstanceNotFoundException",
              "ExceptionMessage": "No instance of saga 'order-saga' exists for correlation id '6f1c2a4e-5b7d-4e8a-9c3f-2d1e0b9a8c7d', so the event 'OrderConfirmed' cannot be applied.",
              "IsCommitted": false,
              "IsDisposed": true,
              "Logs": [
                "Warning: No instance of saga order-saga exists for correlation id 6f1c2a4e-5b7d-4e8a-9c3f-2d1e0b9a8c7d. The event OrderConfirmed faults."
              ]
            }
            """);
    }

    [Fact]
    public async Task Saga_Should_AcknowledgeEvent_When_InstanceIsMissingAndIgnored()
    {
        // arrange
        var logs = new CapturingLoggerProvider();
        var attempts = new AttemptRecorder();
        await using var provider = await CreateBusAsync(
            logs,
            b =>
            {
                b.Services.AddInMemorySagas();
                b.AddSaga<IgnoringOrderSaga>();
                b.ConfigureMessageBus(h => h.UseConsume(attempts.CreateMiddleware()));
            });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderConfirmed(s_orderId), CancellationToken.None);

        // assert
        await attempts.WaitForAttemptAsync();
        new { attempts.Attempts, Logs = logs.GetSagaEntries() }.MatchInlineSnapshot(
            """
            {
              "Attempts": [
                "OrderConfirmed: Consumed"
              ],
              "Logs": [
                "Information: No instance of saga ignoring-order-saga exists for correlation id 6f1c2a4e-5b7d-4e8a-9c3f-2d1e0b9a8c7d. The event OrderConfirmed is ignored."
              ]
            }
            """);
    }

    [Fact]
    public async Task Saga_Should_AcknowledgeEvent_When_InstanceIsMissingForSagaIdHeader()
    {
        // arrange
        var logs = new CapturingLoggerProvider();
        var attempts = new AttemptRecorder();
        await using var provider = await CreateBusAsync(
            logs,
            b =>
            {
                b.Services.AddInMemorySagas();
                b.AddSaga<OrderSaga>();
                b.ConfigureMessageBus(h => h.UseConsume(attempts.CreateMiddleware()));
            });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var options = new PublishOptions { Headers = new() { ["saga-id"] = s_orderId.ToString("D") } };

        // act
        await bus.PublishAsync(new OrderPaid(), options, CancellationToken.None);

        // assert
        await attempts.WaitForAttemptAsync();
        new { attempts.Attempts, Logs = logs.GetSagaEntries() }.MatchInlineSnapshot(
            """
            {
              "Attempts": [
                "OrderPaid: Consumed"
              ],
              "Logs": [
                "Debug: No instance of saga order-saga exists for correlation id 6f1c2a4e-5b7d-4e8a-9c3f-2d1e0b9a8c7d. The event OrderPaid is ignored because its saga-id header can belong to another saga."
              ]
            }
            """);
    }

    [Fact]
    public async Task Saga_Should_AcknowledgeTimeout_When_InstanceIsMissing()
    {
        // arrange
        var logs = new CapturingLoggerProvider();
        var attempts = new AttemptRecorder();
        await using var provider = await CreateBusAsync(
            logs,
            b =>
            {
                b.Services.AddInMemorySagas();
                b.AddSaga<TimeoutOrderSaga>();
                b.ConfigureMessageBus(h => h.UseConsume(attempts.CreateMiddleware()));
            });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.SendAsync(new SagaTimedOutEvent(s_orderId), CancellationToken.None);

        // assert
        await attempts.WaitForAttemptAsync();
        new { attempts.Attempts, Logs = logs.GetSagaEntries() }.MatchInlineSnapshot(
            """
            {
              "Attempts": [
                "SagaTimedOutEvent: Consumed"
              ],
              "Logs": [
                "Debug: No instance of saga timeout-order-saga exists for correlation id 6f1c2a4e-5b7d-4e8a-9c3f-2d1e0b9a8c7d. The timeout is ignored because the saga already completed."
              ]
            }
            """);
    }

    [Fact]
    public async Task Saga_Should_ApplyEventOnRetry_When_InstanceIsPersistedAfterFirstAttempt()
    {
        // arrange
        var attempts = new AttemptRecorder();
        await using var provider = await CreateBusAsync(
            new CapturingLoggerProvider(),
            b =>
            {
                b.Services.AddInMemorySagas();
                b.AddResilience(p => p.On<SagaInstanceNotFoundException>()
                    .Retry(1, TimeSpan.FromMilliseconds(1), RetryBackoffType.Constant));
                b.AddSaga<OrderSaga>();
                b.ConfigureMessageBus(h => h.UseConsume(attempts.CreateMiddleware()));
            });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();

        // act
        // the confirmation overtakes the event that starts the saga
        await bus.PublishAsync(new OrderConfirmed(s_orderId), CancellationToken.None);
        await attempts.WaitForAttemptAsync();

        await bus.PublishAsync(new OrderPlaced(s_orderId), CancellationToken.None);
        await attempts.WaitForAttemptAsync();

        attempts.ReleaseFailedAttempt();
        await attempts.WaitForAttemptAsync();

        // assert
        Assert.Equal(0, storage.Count);
        Assert.Equal(
            [
                "OrderConfirmed: SagaInstanceNotFoundException",
                "OrderPlaced: Consumed",
                "OrderConfirmed: Consumed"
            ],
            attempts.Attempts);
    }

    private static async Task<ServiceProvider> CreateBusAsync(
        CapturingLoggerProvider logs,
        Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(logs).SetMinimumLevel(LogLevel.Debug));
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory(d => d.AddConvention(new ErrorEndpointConvention()));

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    private static async Task<MessageEnvelope> ReadErrorQueueAsync(ServiceProvider provider)
    {
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;
        var errorQueue = topology.Queues.Single(q => q.Name.EndsWith("_error", StringComparison.Ordinal));

        await using var items = errorQueue
            .ConsumeAsync(TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        await items.MoveNextAsync().AsTask().WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        using var item = items.Current;
        return new MessageEnvelope(item.Envelope);
    }

    public sealed class OrderState : SagaStateBase;

    public sealed record OrderPlaced(Guid OrderId);

    public sealed record OrderConfirmed(Guid OrderId) : ICorrelatable
    {
        public Guid? CorrelationId => OrderId;
    }

    public sealed record OrderPaid;

    public sealed class OrderSaga : Saga<OrderState>
    {
        protected override void Configure(ISagaDescriptor<OrderState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<OrderPlaced>()
                .StateFactory(e => new OrderState { Id = e.OrderId })
                .TransitionTo("AwaitingConfirmation");

            descriptor.During("AwaitingConfirmation").OnEvent<OrderConfirmed>().TransitionTo("Completed");
            descriptor.During("AwaitingConfirmation").OnEvent<OrderPaid>().TransitionTo("Completed");

            descriptor.Finally("Completed");
        }
    }

    public sealed class IgnoringOrderSaga : Saga<OrderState>
    {
        protected override void Configure(ISagaDescriptor<OrderState> descriptor)
        {
            descriptor.OnMissingInstance(SagaMissingInstanceBehavior.Ignore);

            descriptor
                .Initially()
                .OnEvent<OrderPlaced>()
                .StateFactory(e => new OrderState { Id = e.OrderId })
                .TransitionTo("AwaitingConfirmation");

            descriptor.During("AwaitingConfirmation").OnEvent<OrderConfirmed>().TransitionTo("Completed");

            descriptor.Finally("Completed");
        }
    }

    public sealed class TimeoutOrderSaga : Saga<OrderState>
    {
        protected override void Configure(ISagaDescriptor<OrderState> descriptor)
        {
            descriptor.Timeout(TimeSpan.FromMinutes(30));

            descriptor
                .Initially()
                .OnEvent<OrderPlaced>()
                .StateFactory(e => new OrderState { Id = e.OrderId })
                .TransitionTo("AwaitingConfirmation");

            descriptor.DuringAny().OnTimeout().TransitionTo(StateNames.TimedOut);

            descriptor.During("AwaitingConfirmation").OnEvent<OrderConfirmed>().TransitionTo("Completed");

            descriptor.Finally("Completed");
        }
    }

    /// <summary>
    /// Records the outcome of every consumer attempt and holds the first failed attempt until released.
    /// </summary>
    private sealed class AttemptRecorder
    {
        private readonly SemaphoreSlim _recorded = new(0);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _failures;

        public ConcurrentQueue<string> Attempts { get; } = new();

        public void ReleaseFailedAttempt() => _release.TrySetResult();

        public async Task WaitForAttemptAsync()
            => Assert.True(
                await _recorded.WaitAsync(s_timeout, TestContext.Current.CancellationToken),
                "No consumer attempt was recorded within the timeout.");

        public ConsumerMiddlewareConfiguration CreateMiddleware()
            => new(
                (_, next) => async context =>
                {
                    try
                    {
                        await next(context);
                    }
                    catch (Exception ex)
                    {
                        Record(context, ex.GetType().Name);

                        if (Interlocked.Increment(ref _failures) == 1)
                        {
                            await _release.Task.WaitAsync(s_timeout);
                        }

                        throw;
                    }

                    Record(context, "Consumed");
                },
                "RecordAttempt");

        private void Record(IConsumeContext context, string outcome)
        {
            Attempts.Enqueue($"{context.GetMessage()!.GetType().Name}: {outcome}");
            _recorded.Release();
        }
    }

    private sealed class ErrorEndpointConvention : IInMemoryReceiveEndpointConfigurationConvention
    {
        public void Configure(
            IMessagingConfigurationContext context,
            InMemoryMessagingTransport transport,
            InMemoryReceiveEndpointConfiguration configuration)
        {
            if (configuration is { Kind: ReceiveEndpointKind.Default, QueueName: { } queueName })
            {
                var feature = configuration.Features.GetOrSet<ReceiveFaultEndpointFeature>();
                feature.Address ??= new Uri($"{transport.Schema}:q/{queueName}_error");
            }
        }
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<(string Category, LogLevel Level, string Message)> _entries = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _entries);

        public string[] GetSagaEntries()
            => _entries
                .Where(e => e.Category.StartsWith("Mocha.Sagas.Saga", StringComparison.Ordinal))
                .Select(e => $"{e.Level}: {e.Message}")
                .ToArray();

        public void Dispose() { }

        private sealed class CapturingLogger(
            string category,
            ConcurrentQueue<(string, LogLevel, string)> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
                => entries.Enqueue((category, logLevel, formatter(state, exception)));
        }
    }
}
