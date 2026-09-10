using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// Forwards log entries to two loggers so that a message reaches the application host log
/// and the console log of a resource at the same time.
/// </summary>
internal sealed class CompositeLogger(ILogger first, ILogger second) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => new CompositeScope(first.BeginScope(state), second.BeginScope(state));

    public bool IsEnabled(LogLevel logLevel)
        => first.IsEnabled(logLevel) || second.IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (first.IsEnabled(logLevel))
        {
            first.Log(logLevel, eventId, state, exception, formatter);
        }

        if (second.IsEnabled(logLevel))
        {
            second.Log(logLevel, eventId, state, exception, formatter);
        }
    }

    private sealed class CompositeScope(IDisposable? first, IDisposable? second) : IDisposable
    {
        public void Dispose()
        {
            first?.Dispose();
            second?.Dispose();
        }
    }
}
