using Microsoft.Extensions.Logging;

namespace Mocha.Transport.AzureServiceBus.Tests.Helpers;

/// <summary>
/// An <see cref="ILoggerProvider"/> test double that captures every log entry written for the
/// given category, hooked in through the real logging pipeline so it observes the same
/// category-scoped logger the production type resolves through DI.
/// </summary>
internal sealed class CapturingLoggerProvider(string category) : ILoggerProvider
{
    private readonly SemaphoreSlim _semaphore = new(0);

    public List<CapturedLogEntry> Entries { get; } = [];

    public static CapturingLoggerProvider For<T>() => new(typeof(T).FullName!);

    public ILogger CreateLogger(string categoryName) =>
        categoryName == category ? new CapturingLogger(this) : NullLogger.Instance;

    public void Dispose() { }

    /// <summary>
    /// Waits until an entry matching <paramref name="predicate"/> has been captured, or the
    /// timeout elapses.
    /// </summary>
    public async Task<bool> WaitForEntryAsync(Func<CapturedLogEntry, bool> predicate, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;

        while (true)
        {
            if (Entries.Any(predicate))
            {
                return true;
            }

            var remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero || !await _semaphore.WaitAsync(remaining))
            {
                return Entries.Any(predicate);
            }
        }
    }

    private sealed class CapturingLogger(CapturingLoggerProvider provider) : ILogger
    {
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

            provider.Entries.Add(
                new CapturedLogEntry(logLevel, exception, new List<KeyValuePair<string, object?>>(structuredState)));
            provider._semaphore.Release();
        }
    }

    private sealed class NullLogger : ILogger
    {
        public static NullLogger Instance { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoOpDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        { }
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public static NoOpDisposable Instance { get; } = new();

        public void Dispose() { }
    }
}

/// <summary>
/// A single log entry captured by <see cref="CapturingLoggerProvider"/>.
/// </summary>
internal sealed record CapturedLogEntry(
    LogLevel Level,
    Exception? Exception,
    IReadOnlyList<KeyValuePair<string, object?>> State);
