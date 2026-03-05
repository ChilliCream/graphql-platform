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
    /// </summary>
    /// <param name="configuration">The consumer middleware configuration to add.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public IMessageBusBuilder UseConsume(ConsumerMiddlewareConfiguration configuration)
    {
        _handlerMiddlewares.Add(configuration);

        return this;
    }

    /// <summary>
    /// Prepends a consumer middleware to the beginning of the bus-level consume pipeline.
    /// </summary>
    /// <param name="configuration">The consumer middleware configuration to prepend.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public IMessageBusBuilder PrependConsume(ConsumerMiddlewareConfiguration configuration)
    {
        _handlerModifiers.Prepend(configuration, null);

        return this;
    }

    /// <summary>
    /// Appends a consumer middleware to the end of the bus-level consume pipeline.
    /// </summary>
    /// <param name="configuration">The consumer middleware configuration to append.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public IMessageBusBuilder AppendConsume(ConsumerMiddlewareConfiguration configuration)
    {
        _handlerModifiers.Append(configuration, null);

        return this;
    }

    /// <summary>
    /// Inserts a consumer middleware into the bus-level consume pipeline immediately after the
    /// middleware identified by <paramref name="after"/>.
    /// </summary>
    /// <param name="after">The name of the existing middleware to insert after.</param>
    /// <param name="configuration">The consumer middleware configuration to insert.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public IMessageBusBuilder AppendConsume(string after, ConsumerMiddlewareConfiguration configuration)
    {
        _handlerModifiers.Append(configuration, after);

        return this;
    }

    /// <summary>
    /// Inserts a consumer middleware into the bus-level consume pipeline immediately before the
    /// middleware identified by <paramref name="before"/>.
    /// </summary>
    /// <param name="before">The name of the existing middleware to insert before.</param>
    /// <param name="configuration">The consumer middleware configuration to insert.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public IMessageBusBuilder PrependConsume(string before, ConsumerMiddlewareConfiguration configuration)
    {
        _handlerModifiers.Prepend(configuration, before);

        return this;
    }

    /// <summary>
    /// Adds a receive middleware to the bus-level inbound pipeline, applied to all transports.
    /// </summary>
    /// <param name="configuration">The receive middleware configuration to add.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public IMessageBusBuilder UseReceive(ReceiveMiddlewareConfiguration configuration)
    {
        _receiveMiddlewares.Add(configuration);

        return this;
    }

    /// <summary>
    /// Prepends a receive middleware to the beginning of the bus-level inbound pipeline.
    /// </summary>
    /// <param name="configuration">The receive middleware configuration to prepend.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public IMessageBusBuilder PrependReceive(ReceiveMiddlewareConfiguration configuration)
    {
        _receiveModifiers.Prepend(configuration, null);

        return this;
    }

    /// <summary>
    /// Appends a receive middleware to the end of the bus-level inbound pipeline.
    /// </summary>
    /// <param name="configuration">The receive middleware configuration to append.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public IMessageBusBuilder AppendReceive(ReceiveMiddlewareConfiguration configuration)
    {
        _receiveModifiers.Append(configuration, null);

        return this;
    }

    /// <summary>
    /// Inserts a receive middleware into the bus-level inbound pipeline immediately after the
    /// middleware identified by <paramref name="after"/>.
    /// </summary>
    /// <param name="after">The name of the existing middleware to insert after.</param>
    /// <param name="configuration">The receive middleware configuration to insert.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public IMessageBusBuilder AppendReceive(string after, ReceiveMiddlewareConfiguration configuration)
    {
        _receiveModifiers.Append(configuration, after);

        return this;
    }

    /// <summary>
    /// Inserts a receive middleware into the bus-level inbound pipeline immediately before the
    /// middleware identified by <paramref name="before"/>.
    /// </summary>
    /// <param name="before">The name of the existing middleware to insert before.</param>
    /// <param name="configuration">The receive middleware configuration to insert.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public IMessageBusBuilder PrependReceive(string before, ReceiveMiddlewareConfiguration configuration)
    {
        _receiveModifiers.Prepend(configuration, before);

        return this;
    }

    /// <summary>
    /// Adds a dispatch middleware to the bus-level outbound pipeline, applied to all transports.
    /// </summary>
    /// <param name="configuration">The dispatch middleware configuration to add.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public IMessageBusBuilder UseDispatch(DispatchMiddlewareConfiguration configuration)
    {
        _dispatchMiddlewares.Add(configuration);

        return this;
    }

    /// <summary>
    /// Appends a dispatch middleware to the end of the bus-level outbound pipeline.
    /// </summary>
    /// <param name="configuration">The dispatch middleware configuration to append.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public IMessageBusBuilder AppendDispatch(DispatchMiddlewareConfiguration configuration)
    {
        _dispatchModifiers.Append(configuration, null);

        return this;
    }

    /// <summary>
    /// Inserts a dispatch middleware into the bus-level outbound pipeline immediately after the
    /// middleware identified by <paramref name="after"/>.
    /// </summary>
    /// <param name="after">The name of the existing middleware to insert after.</param>
    /// <param name="configuration">The dispatch middleware configuration to insert.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public IMessageBusBuilder AppendDispatch(string after, DispatchMiddlewareConfiguration configuration)
    {
        _dispatchModifiers.Append(configuration, after);

        return this;
    }

    /// <summary>
    /// Prepends a dispatch middleware to the beginning of the bus-level outbound pipeline.
    /// </summary>
    /// <param name="configuration">The dispatch middleware configuration to prepend.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public IMessageBusBuilder PrependDispatch(DispatchMiddlewareConfiguration configuration)
    {
        _dispatchModifiers.Prepend(configuration, null);

        return this;
    }

    /// <summary>
    /// Inserts a dispatch middleware into the bus-level outbound pipeline immediately before the
    /// middleware identified by <paramref name="before"/>.
    /// </summary>
    /// <param name="before">The name of the existing middleware to insert before.</param>
    /// <param name="configuration">The dispatch middleware configuration to insert.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public IMessageBusBuilder PrependDispatch(string before, DispatchMiddlewareConfiguration configuration)
    {
        _dispatchModifiers.Prepend(configuration, before);

        return this;
    }
}
