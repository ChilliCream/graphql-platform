using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// A logger that writes each entry inside the logging scope of one RabbitMQ connection.
/// </summary>
internal sealed class RabbitMQConnectionScopedLogger : ILogger
{
    private readonly ILogger _logger;
    private readonly RabbitMQConnectionLogScope _scope;

    private RabbitMQConnectionScopedLogger(ILogger logger, RabbitMQConnectionLogScope scope, string connectionName)
    {
        _logger = logger;
        _scope = scope;
        ConnectionName = connectionName;
    }

    /// <summary>
    /// Gets the name the broker shows for the connection.
    /// </summary>
    public string ConnectionName { get; }

    public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _logger.BeginScope(state);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!_logger.IsEnabled(logLevel))
        {
            return;
        }

        using (_logger.BeginScope(_scope))
        {
            _logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }

    /// <summary>
    /// Creates the logger for the consumer connection of a transport.
    /// </summary>
    /// <param name="context">The setup context of the transport.</param>
    /// <param name="transportName">The name of the transport.</param>
    /// <param name="connection">The provider of the connection.</param>
    /// <param name="role">The role of the connection, a value of <see cref="RabbitMQConnectionRole"/>.</param>
    public static RabbitMQConnectionScopedLogger Create(
        IMessagingSetupContext context,
        string transportName,
        IRabbitMQConnectionProvider connection,
        string role)
    {
        var category = role == RabbitMQConnectionRole.Dispatcher
            ? RabbitMQLogCategory.Dispatcher
            : RabbitMQLogCategory.Consumer;
        var serviceName = context.Host.ServiceName ?? context.Host.ProcessName;
        var connectionName = $"{serviceName} {transportName} {role}";

        return new RabbitMQConnectionScopedLogger(
            context.Services.GetRequiredService<ILoggerFactory>().CreateLogger(category),
            new RabbitMQConnectionLogScope(
                serviceName,
                context.Host.InstanceId,
                transportName,
                connectionName,
                role,
                connection.Host,
                connection.Port,
                connection.VirtualHost),
            connectionName);
    }
}
