namespace Mocha;

public partial class MessageBusBuilder
{
    private readonly List<ConsumerMiddlewareConfiguration> _handlerMiddlewares = [];
    private readonly List<ReceiveMiddlewareConfiguration> _receiveMiddlewares = [];
    private readonly List<DispatchMiddlewareConfiguration> _dispatchMiddlewares = [];
    private readonly List<Action<List<ConsumerMiddlewareConfiguration>>> _handlerModifiers = [];
    private readonly List<Action<List<ReceiveMiddlewareConfiguration>>> _receiveModifiers = [];
    private readonly List<Action<List<DispatchMiddlewareConfiguration>>> _dispatchModifiers = [];

    /// <summary>
    /// Adds a consumer middleware to the bus-level consume pipeline, applied to all consumers
    /// across all transports.
    /// When neither <paramref name="before"/> nor <paramref name="after"/> is specified, the
    /// middleware is appended to the end of the pipeline.
    /// When <paramref name="before"/> is specified, the middleware is inserted immediately before
    /// the middleware with the given key.
    /// When <paramref name="after"/> is specified, the middleware is inserted immediately after
    /// the middleware with the given key.
    /// </summary>
    /// <param name="configuration">The consumer middleware configuration to add.</param>
    /// <param name="before">
    /// The key of the existing middleware before which to insert, or <c>null</c> to skip
    /// positional insertion.
    /// </param>
    /// <param name="after">
    /// The key of the existing middleware after which to insert, or <c>null</c> to skip
    /// positional insertion.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when both <paramref name="before"/> and <paramref name="after"/> are specified.
    /// </exception>
    public IMessageBusBuilder UseConsume(
        ConsumerMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        if (before is not null && after is not null)
        {
            throw new ArgumentException(
                "Only one of 'before' or 'after' can be specified at the same time.");
        }

        if (before is null && after is null)
        {
            _handlerMiddlewares.Add(configuration);
            return this;
        }

        if (before is not null)
        {
            _handlerModifiers.Prepend(configuration, before);
        }
        else
        {
            _handlerModifiers.Append(configuration, after);
        }

        return this;
    }

    /// <summary>
    /// Adds a receive middleware to the bus-level inbound pipeline, applied to all transports.
    /// When neither <paramref name="before"/> nor <paramref name="after"/> is specified, the
    /// middleware is appended to the end of the pipeline.
    /// When <paramref name="before"/> is specified, the middleware is inserted immediately before
    /// the middleware with the given key.
    /// When <paramref name="after"/> is specified, the middleware is inserted immediately after
    /// the middleware with the given key.
    /// </summary>
    /// <param name="configuration">The receive middleware configuration to add.</param>
    /// <param name="before">
    /// The key of the existing middleware before which to insert, or <c>null</c> to skip
    /// positional insertion.
    /// </param>
    /// <param name="after">
    /// The key of the existing middleware after which to insert, or <c>null</c> to skip
    /// positional insertion.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when both <paramref name="before"/> and <paramref name="after"/> are specified.
    /// </exception>
    public IMessageBusBuilder UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        if (before is not null && after is not null)
        {
            throw new ArgumentException(
                "Only one of 'before' or 'after' can be specified at the same time.");
        }

        if (before is null && after is null)
        {
            _receiveMiddlewares.Add(configuration);
            return this;
        }

        if (before is not null)
        {
            _receiveModifiers.Prepend(configuration, before);
        }
        else
        {
            _receiveModifiers.Append(configuration, after);
        }

        return this;
    }

    /// <summary>
    /// Adds a dispatch middleware to the bus-level outbound pipeline, applied to all transports.
    /// When neither <paramref name="before"/> nor <paramref name="after"/> is specified, the
    /// middleware is appended to the end of the pipeline.
    /// When <paramref name="before"/> is specified, the middleware is inserted immediately before
    /// the middleware with the given key.
    /// When <paramref name="after"/> is specified, the middleware is inserted immediately after
    /// the middleware with the given key.
    /// </summary>
    /// <param name="configuration">The dispatch middleware configuration to add.</param>
    /// <param name="before">
    /// The key of the existing middleware before which to insert, or <c>null</c> to skip
    /// positional insertion.
    /// </param>
    /// <param name="after">
    /// The key of the existing middleware after which to insert, or <c>null</c> to skip
    /// positional insertion.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when both <paramref name="before"/> and <paramref name="after"/> are specified.
    /// </exception>
    public IMessageBusBuilder UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        if (before is not null && after is not null)
        {
            throw new ArgumentException(
                "Only one of 'before' or 'after' can be specified at the same time.");
        }

        if (before is null && after is null)
        {
            _dispatchMiddlewares.Add(configuration);
            return this;
        }

        if (before is not null)
        {
            _dispatchModifiers.Prepend(configuration, before);
        }
        else
        {
            _dispatchModifiers.Append(configuration, after);
        }

        return this;
    }
}
