using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Transport.AzureServiceBus.Features;
using Mocha.Transport.AzureServiceBus.Middlewares;

namespace Mocha.Transport.AzureServiceBus.Tests;

public sealed class AzureServiceBusAcknowledgementMiddlewareTests
{
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
        var ex = await Assert.ThrowsAsync<ServiceBusException>(
            () => AzureServiceBusAcknowledgementMiddleware.CompleteAsync(
                new ThrowingActions(timeout),
                provider,
                message,
                "orders",
                CancellationToken.None));

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
        public Task CompleteAsync(CancellationToken cancellationToken = default)
            => Task.FromException(exception);

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

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
            => NoOpDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel)
            => true;

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

        public void Dispose()
        {
        }
    }
}
