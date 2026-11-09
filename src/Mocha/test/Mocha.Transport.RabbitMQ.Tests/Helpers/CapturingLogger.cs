using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mocha.Transport.RabbitMQ.Tests.Helpers;

internal sealed class CapturingLogger<T> : ILogger<T>, ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public LogEntry[] Entries => _entries.ToArray();

    public ILogger CreateLogger(string categoryName)
        => categoryName == typeof(T).FullName ? this : NullLogger.Instance;

    public void Dispose() { }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var properties = state is IEnumerable<KeyValuePair<string, object?>> values
            ? values.ToDictionary(p => p.Key, p => p.Value)
            : new Dictionary<string, object?>();
        _entries.Enqueue(new LogEntry(logLevel, eventId.Name, properties, exception));
    }

    internal sealed record LogEntry(
        LogLevel Level,
        string? Event,
        IReadOnlyDictionary<string, object?> Properties,
        Exception? Exception);
}
