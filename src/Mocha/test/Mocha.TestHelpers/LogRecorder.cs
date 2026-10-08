using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Mocha.TestHelpers;

public sealed class LogRecorder : ILoggerProvider
{
    public ConcurrentQueue<string> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new Logger(Entries);

    public void Dispose() { }

    private sealed class Logger(ConcurrentQueue<string> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                entries.Enqueue($"{logLevel}: {formatter(state, exception)}");
            }
        }
    }
}
