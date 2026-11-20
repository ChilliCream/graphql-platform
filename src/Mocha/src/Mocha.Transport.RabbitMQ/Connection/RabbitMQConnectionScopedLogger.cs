using Microsoft.Extensions.Logging;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// A logger that writes each entry inside the logging scope of one RabbitMQ connection.
/// </summary>
internal sealed class RabbitMQConnectionScopedLogger(ILogger logger, RabbitMQConnectionLogScope scope) : ILogger
{
    public bool IsEnabled(LogLevel logLevel) => logger.IsEnabled(logLevel);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => logger.BeginScope(state);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!logger.IsEnabled(logLevel))
        {
            return;
        }

        using (logger.BeginScope(scope))
        {
            logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
