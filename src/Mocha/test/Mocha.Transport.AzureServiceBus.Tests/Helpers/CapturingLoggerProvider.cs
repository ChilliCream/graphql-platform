using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mocha.Transport.AzureServiceBus.Tests.Helpers;

/// <summary>
/// An <see cref="ILoggerProvider"/> test double that captures every log entry written for the
/// given category, hooked in through the real logging pipeline so it observes the same
/// category-scoped logger the production type resolves through DI.
/// </summary>
internal sealed class CapturingLoggerProvider(string category) : ILoggerProvider
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly List<CapturedLogEntry> _entries = [];
    private readonly SemaphoreSlim _semaphore = new(0);

    /// <summary>
    /// A stable snapshot of the entries captured so far. Safe to enumerate while log entries are
    /// concurrently captured on other threads.
    /// </summary>
    public IReadOnlyList<CapturedLogEntry> Entries
    {
        get
        {
            lock (_lock)
            {
                return [.. _entries];
            }
        }
    }

    public static CapturingLoggerProvider For<T>() => new(typeof(T).FullName!);

    public ILogger CreateLogger(string categoryName) =>
        categoryName == category ? new CapturingLogger(this) : NullLogger.Instance;

    public void Dispose() => _semaphore.Dispose();

    /// <summary>
    /// Waits until an entry matching <paramref name="predicate"/> has been captured, or the
    /// timeout elapses.
    /// </summary>
    public async Task<bool> WaitForEntryAsync(Func<CapturedLogEntry, bool> predicate, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;

        while (true)
        {
            if (Contains(predicate))
            {
                return true;
            }

            var remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero || !await _semaphore.WaitAsync(remaining))
            {
                return Contains(predicate);
            }
        }
    }

    private bool Contains(Func<CapturedLogEntry, bool> predicate)
    {
        lock (_lock)
        {
            return _entries.Any(predicate);
        }
    }

    private sealed class CapturingLogger(CapturingLoggerProvider provider) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull =>
            NullLogger.Instance.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var structuredState = state as IReadOnlyList<KeyValuePair<string, object?>> ?? [];
            var entry = new CapturedLogEntry(logLevel, exception, new List<KeyValuePair<string, object?>>(structuredState));

            lock (provider._lock)
            {
                provider._entries.Add(entry);
            }

            provider._semaphore.Release();
        }
    }
}

/// <summary>
/// A single log entry captured by <see cref="CapturingLoggerProvider"/>.
/// </summary>
internal sealed record CapturedLogEntry(
    LogLevel Level,
    Exception? Exception,
    IReadOnlyList<KeyValuePair<string, object?>> State);
