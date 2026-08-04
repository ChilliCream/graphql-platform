using Mocha.Middlewares;

namespace Mocha.Transport.Postgres.Middlewares;

/// <summary>
/// Provides pre-configured PostgreSQL-specific receive middleware configurations for message parsing.
/// </summary>
public static class PostgresReceiveMiddlewares
{
    /// <summary>
    /// Middleware configuration that parses the raw PostgreSQL message item into a <see cref="MessageEnvelope"/> on the receive context.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration Parsing = PostgresParsingMiddleware.Create();
}
