using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Mocha.Transport.RabbitMQ.Tests.Helpers;

/// <summary>
/// Records what the bus logs, so a test can wait for a lifecycle event.
/// </summary>
public sealed class LogRecorder : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _events = new();
    private readonly ConcurrentQueue<string> _lines = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    public bool Contains(string eventName) => _events.Contains(eventName);

    /// <summary>
    /// Gets every log line with its time since the recorder was created.
    /// </summary>
    public IEnumerable<string> Lines => _lines;

    public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);

    /// <summary>
    /// Writes every recorded line to the test output, which the runner shows when the test fails.
    /// </summary>
    public void WriteTo(ITestOutputHelper output)
    {
        foreach (var line in _lines)
        {
            output.WriteLine(line);
        }
    }

    public void Dispose() { }

    private sealed class Logger(LogRecorder recorder, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (eventId.Name is { } name)
            {
                recorder._events.Enqueue(name);
            }

            recorder._lines.Enqueue(
                $"{recorder._clock.Elapsed.TotalSeconds,7:F2} {logLevel} {category.Split('.').Last()}: "
                + $"{formatter(state, exception)} {exception?.GetType().Name} {exception?.Message}");
        }
    }
}
