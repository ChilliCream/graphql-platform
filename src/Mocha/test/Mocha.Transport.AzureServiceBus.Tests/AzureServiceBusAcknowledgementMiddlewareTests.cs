using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Middlewares;
using Mocha.Transport.AzureServiceBus.Features;
using Mocha.Transport.AzureServiceBus.Middlewares;

namespace Mocha.Transport.AzureServiceBus.Tests;

public sealed class AzureServiceBusAcknowledgementMiddlewareTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvokeAsync_Should_Complete_When_NextSucceeds(bool isSession)
    {
        // arrange
        var entityPath = isSession ? "orders-session" : "orders-queue";
        using var cts = new CancellationTokenSource();
        var (context, settlement) = CreateContext(isSession, entityPath, cts.Token);
        var middleware = new AzureServiceBusAcknowledgementMiddleware();

        // act
        await middleware.InvokeAsync(context, static _ => ValueTask.CompletedTask);

        // assert
        Assert.Equal(1, settlement.CompleteCallCount);
        Assert.Equal(0, settlement.AbandonCallCount);
        Assert.Equal(cts.Token, settlement.LastCancellationToken);
        Assert.Equal(entityPath, settlement.ObservedEntityPath);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvokeAsync_Should_AbandonAndRethrow_When_NextFails(bool isSession)
    {
        // arrange
        var entityPath = isSession ? "orders-session" : "orders-queue";
        using var cts = new CancellationTokenSource();
        var (context, settlement) = CreateContext(isSession, entityPath, cts.Token);
        var middleware = new AzureServiceBusAcknowledgementMiddleware();
        var handlerException = new InvalidOperationException("handler failed");

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context, _ => throw handlerException).AsTask());

        // assert
        Assert.Same(handlerException, thrown);
        Assert.Equal(0, settlement.CompleteCallCount);
        Assert.Equal(1, settlement.AbandonCallCount);
        Assert.Equal(cts.Token, settlement.LastCancellationToken);
        Assert.Equal(entityPath, settlement.ObservedEntityPath);
    }

    [Fact]
    public async Task InvokeAsync_Should_PreserveOriginalException_When_AbandonAlsoFails()
    {
        // arrange
        using var cts = new CancellationTokenSource();
        var (context, settlement) = CreateContext(isSession: false, "orders-queue", cts.Token);
        settlement.AbandonException = new InvalidOperationException("abandon failed");
        var middleware = new AzureServiceBusAcknowledgementMiddleware();
        var handlerException = new InvalidOperationException("handler failed");

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context, _ => throw handlerException).AsTask());

        // assert
        Assert.Same(handlerException, thrown);
        Assert.Equal(1, settlement.AbandonCallCount);
    }

    private static (ReceiveContext Context, RecordingSettlement Settlement) CreateContext(
        bool isSession,
        string entityPath,
        CancellationToken cancellationToken)
    {
        var provider = new ServiceCollection()
            .AddSingleton<ILogger<AzureServiceBusAcknowledgementMiddleware>>(new CapturingLogger())
            .BuildServiceProvider();
        var settlement = new RecordingSettlement();
        var feature = new AzureServiceBusReceiveFeature();

        if (isSession)
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(messageId: "m-1", sessionId: "s-1");
            var receiver = new RecordingServiceBusSessionReceiver(entityPath, settlement);
            feature.SetSession(new ProcessSessionMessageEventArgs(message, receiver, cancellationToken));
        }
        else
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(messageId: "m-1");
            var receiver = new RecordingServiceBusReceiver(entityPath, settlement);
            feature.SetNonSession(new ProcessMessageEventArgs(message, receiver, cancellationToken));
        }

        var context = new ReceiveContext
        {
            Services = provider,
            CancellationToken = cancellationToken
        };
        context.Features.Set(feature);

        return (context, settlement);
    }

    /// <summary>
    /// Tracks settlement calls made through a <see cref="RecordingServiceBusReceiver"/> or
    /// <see cref="RecordingServiceBusSessionReceiver"/> so tests can assert on call counts,
    /// the propagated cancellation token, and the entity path the middleware selected.
    /// </summary>
    private sealed class RecordingSettlement
    {
        public int CompleteCallCount { get; private set; }

        public int AbandonCallCount { get; private set; }

        public CancellationToken? LastCancellationToken { get; private set; }

        public string? ObservedEntityPath { get; private set; }

        public Exception? AbandonException { get; set; }

        public void RecordEntityPathAccess(string entityPath) => ObservedEntityPath = entityPath;

        public Task CompleteAsync(CancellationToken cancellationToken)
        {
            CompleteCallCount++;
            LastCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task AbandonAsync(CancellationToken cancellationToken)
        {
            AbandonCallCount++;
            LastCancellationToken = cancellationToken;
            return AbandonException is null ? Task.CompletedTask : Task.FromException(AbandonException);
        }
    }

    private sealed class RecordingServiceBusReceiver(string entityPath, RecordingSettlement settlement)
        : ServiceBusReceiver
    {
        public override string EntityPath
        {
            get
            {
                settlement.RecordEntityPathAccess(entityPath);
                return entityPath;
            }
        }

        public override Task CompleteMessageAsync(
            ServiceBusReceivedMessage message,
            CancellationToken cancellationToken = default)
            => settlement.CompleteAsync(cancellationToken);

        public override Task AbandonMessageAsync(
            ServiceBusReceivedMessage message,
            IDictionary<string, object>? propertiesToModify = null,
            CancellationToken cancellationToken = default)
            => settlement.AbandonAsync(cancellationToken);
    }

    private sealed class RecordingServiceBusSessionReceiver(string entityPath, RecordingSettlement settlement)
        : ServiceBusSessionReceiver
    {
        public override string EntityPath
        {
            get
            {
                settlement.RecordEntityPathAccess(entityPath);
                return entityPath;
            }
        }

        public override Task CompleteMessageAsync(
            ServiceBusReceivedMessage message,
            CancellationToken cancellationToken = default)
            => settlement.CompleteAsync(cancellationToken);

        public override Task AbandonMessageAsync(
            ServiceBusReceivedMessage message,
            IDictionary<string, object>? propertiesToModify = null,
            CancellationToken cancellationToken = default)
            => settlement.AbandonAsync(cancellationToken);
    }

    [Fact]
    public async Task CompleteAsync_Should_LogWarningAndSwallow_When_LockLost()
    {
        // arrange
        var logger = new CapturingLogger();
        var provider = new ServiceCollection()
            .AddSingleton<ILogger<AzureServiceBusAcknowledgementMiddleware>>(logger)
            .BuildServiceProvider();
        var lockLost = new ServiceBusException("lock lost", ServiceBusFailureReason.MessageLockLost);
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(messageId: "m-1", sessionId: "s-1");

        // act
        await AzureServiceBusAcknowledgementMiddleware.CompleteAsync(
            new ThrowingActions(lockLost),
            provider,
            message,
            "orders",
            CancellationToken.None);

        // assert
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal(lockLost, entry.Exception);
        Assert.Equal(
            new[]
            {
                new KeyValuePair<string, object?>("Operation", "Complete"),
                new KeyValuePair<string, object?>("EntityPath", "orders"),
                new KeyValuePair<string, object?>("MessageId", "m-1"),
                new KeyValuePair<string, object?>("SessionId", "s-1"),
                new KeyValuePair<string, object?>("Reason", ServiceBusFailureReason.MessageLockLost)
            },
            GetStructuredValues(entry.State));
    }

    [Fact]
    public async Task AbandonAsync_Should_LogWarningAndSwallow_When_LockLost()
    {
        // arrange
        var logger = new CapturingLogger();
        var provider = new ServiceCollection()
            .AddSingleton<ILogger<AzureServiceBusAcknowledgementMiddleware>>(logger)
            .BuildServiceProvider();
        var lockLost = new ServiceBusException("lock lost", ServiceBusFailureReason.SessionLockLost);
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(messageId: "m-1", sessionId: "s-1");

        // act
        await AzureServiceBusAcknowledgementMiddleware.AbandonAsync(
            new ThrowingActions(lockLost),
            provider,
            message,
            "orders",
            CancellationToken.None);

        // assert
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal(lockLost, entry.Exception);
        Assert.Equal(
            new[]
            {
                new KeyValuePair<string, object?>("Operation", "Abandon"),
                new KeyValuePair<string, object?>("EntityPath", "orders"),
                new KeyValuePair<string, object?>("MessageId", "m-1"),
                new KeyValuePair<string, object?>("SessionId", "s-1"),
                new KeyValuePair<string, object?>("Reason", ServiceBusFailureReason.SessionLockLost)
            },
            GetStructuredValues(entry.State));
    }

    [Fact]
    public async Task CompleteAsync_Should_Propagate_When_NotLockLost()
    {
        // arrange
        var logger = new CapturingLogger();
        var provider = new ServiceCollection()
            .AddSingleton<ILogger<AzureServiceBusAcknowledgementMiddleware>>(logger)
            .BuildServiceProvider();
        var timeout = new ServiceBusException("timeout", ServiceBusFailureReason.ServiceTimeout);
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(messageId: "m-2");

        // act
        var ex = await Assert.ThrowsAsync<ServiceBusException>(() =>
            AzureServiceBusAcknowledgementMiddleware.CompleteAsync(
                new ThrowingActions(timeout),
                provider,
                message,
                "orders",
                CancellationToken.None)
        );

        // assert
        Assert.Equal(ServiceBusFailureReason.ServiceTimeout, ex.Reason);
        Assert.Empty(logger.Entries);
    }

    private static IReadOnlyList<KeyValuePair<string, object?>> GetStructuredValues(
        IReadOnlyList<KeyValuePair<string, object?>> state)
    {
        var values = new List<KeyValuePair<string, object?>>();

        foreach (var pair in state)
        {
            if (pair.Key != "{OriginalFormat}")
            {
                values.Add(pair);
            }
        }

        return values;
    }

    private sealed class ThrowingActions(Exception exception) : IAzureServiceBusMessageActions
    {
        public Task CompleteAsync(CancellationToken cancellationToken = default) => Task.FromException(exception);

        public Task AbandonAsync(
            IDictionary<string, object>? propertiesToModify = null,
            CancellationToken cancellationToken = default)
            => Task.FromException(exception);

        public Task DeadLetterAsync(
            string deadLetterReason,
            string? deadLetterErrorDescription = null,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class CapturingLogger : ILogger<AzureServiceBusAcknowledgementMiddleware>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoOpDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var structuredState = state as IReadOnlyList<KeyValuePair<string, object?>> ?? [];

            Entries.Add(
                new LogEntry(
                    logLevel,
                    formatter(state, exception),
                    exception,
                    new List<KeyValuePair<string, object?>>(structuredState)));
        }
    }

    private sealed record LogEntry(
        LogLevel Level,
        string Message,
        Exception? Exception,
        IReadOnlyList<KeyValuePair<string, object?>> State);

    private sealed class NoOpDisposable : IDisposable
    {
        public static NoOpDisposable Instance { get; } = new();

        public void Dispose() { }
    }
}
