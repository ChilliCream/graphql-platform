using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Mocha.Transport.RabbitMQ.Tests.Helpers;

/// <summary>
/// One recorded log entry.
/// </summary>
/// <param name="Level">The level of the entry.</param>
/// <param name="Category">The logger category.</param>
/// <param name="EventName">The name of the log event, which is the name of its log method.</param>
/// <param name="Message">The rendered message.</param>
/// <param name="Exception">The logged exception, or <c>null</c> when there was none.</param>
/// <param name="Properties">The structured properties of the message and of every scope around it.</param>
public sealed record LogEntry(
    LogLevel Level,
    string Category,
    string EventName,
    string Message,
    Exception? Exception,
    IReadOnlyDictionary<string, object?> Properties)
{
    /// <inheritdoc />
    public override string ToString() => $"{Level} {EventName}: {Message}";
}

/// <summary>
/// A logger provider that keeps every entry in memory, in the order it was written.
/// </summary>
public sealed class LogRecorder : ILoggerProvider, ISupportExternalScope
{
    private const string TransportCategory = "Mocha.Transport.RabbitMQ";

    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();

    /// <summary>
    /// Gets a snapshot of the entries the RabbitMQ transport wrote at <paramref name="minimumLevel"/> or above.
    /// </summary>
    public IReadOnlyList<LogEntry> GetTransportEntries(LogLevel minimumLevel = LogLevel.Information)
        => [.. _entries.Where(e => e.Level >= minimumLevel && e.Category.StartsWith(TransportCategory, StringComparison.Ordinal))];

    /// <summary>
    /// Gets a snapshot of the entries of one log event.
    /// </summary>
    public IReadOnlyList<LogEntry> GetEntries(string eventName) => [.. _entries.Where(e => e.EventName == eventName)];

    /// <summary>
    /// Waits until an entry of the log event was written, and returns the first one.
    /// </summary>
    public Task<LogEntry> WaitForAsync(string eventName)
        => Wait.UntilAsync(
            () => Task.FromResult(_entries.FirstOrDefault(e => e.EventName == eventName)),
            entry => entry is not null,
            $"a log entry '{eventName}'")!;

    /// <summary>
    /// Removes every recorded entry.
    /// </summary>
    public void Clear() => _entries.Clear();

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);

    /// <inheritdoc />
    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private sealed class Logger(LogRecorder owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => owner._scopes.Push(state);

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = new Dictionary<string, object?>();
            owner._scopes.ForEachScope(static (scope, properties) => Add(properties, scope), properties);
            Add(properties, state);

            owner._entries.Enqueue(
                new LogEntry(
                    logLevel,
                    category,
                    eventId.Name ?? string.Empty,
                    formatter(state, exception),
                    exception,
                    properties));
        }

        private static void Add(Dictionary<string, object?> properties, object? state)
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                foreach (var pair in pairs)
                {
                    properties[pair.Key] = pair.Value;
                }
            }
        }
    }
}
