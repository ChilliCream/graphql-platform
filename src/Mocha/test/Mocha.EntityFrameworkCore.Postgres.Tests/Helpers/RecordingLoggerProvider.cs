using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;

internal sealed record LogEntry(LogLevel Level, string Message);

/// <summary>
/// Records the log entries one category writes at or above a minimum level.
/// </summary>
internal sealed class RecordingLoggerProvider(string category, LogLevel minimumLevel) : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public LogEntry[] Entries => [.. _entries];

    public static RecordingLoggerProvider For<TCategory>(LogLevel minimumLevel = LogLevel.Information)
        => new(typeof(TCategory).FullName!, minimumLevel);

    public ILogger CreateLogger(string categoryName)
        => categoryName == category ? new RecordingLogger(_entries, minimumLevel) : NullLogger.Instance;

    /// <summary>
    /// Waits until at least <paramref name="count"/> entries were recorded and returns the entries recorded so far.
    /// </summary>
    public async Task<LogEntry[]> WaitForEntriesAsync(int count, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (_entries.Count < count && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, cancellationToken);
        }

        return Entries;
    }

    public void Dispose()
    {
    }

    private sealed class RecordingLogger(ConcurrentQueue<LogEntry> entries, LogLevel minimumLevel) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= minimumLevel;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                entries.Enqueue(new LogEntry(logLevel, formatter(state, exception)));
            }
        }
    }
}
