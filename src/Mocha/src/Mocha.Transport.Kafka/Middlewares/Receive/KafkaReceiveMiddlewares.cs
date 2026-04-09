namespace Mocha.Transport.Kafka.Middlewares;

/// <summary>
/// Provides pre-configured Kafka-specific receive middleware configurations for offset commit and message parsing.
/// </summary>
internal static class KafkaReceiveMiddlewares
{
    /// <summary>
    /// Middleware configuration that commits the Kafka offset after successful processing.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration Commit = KafkaCommitMiddleware.Create();

    /// <summary>
    /// Middleware configuration that parses the raw Kafka consume result into a <see cref="Mocha.Middlewares.MessageEnvelope"/> on the receive context.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration Parsing = KafkaParsingMiddleware.Create();
}
