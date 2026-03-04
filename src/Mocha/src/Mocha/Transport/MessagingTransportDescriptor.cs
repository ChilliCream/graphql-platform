namespace Mocha;

/// <summary>
/// Marker interface for descriptors that can contribute receive-pipeline middleware to a transport.
/// </summary>
public interface IReceiveMiddlewareProvider : IMessagingDescriptor;

/// <summary>
/// Marker interface for descriptors that can contribute dispatch-pipeline middleware to a transport.
/// </summary>
public interface IDispatchMiddlewareProvider : IMessagingDescriptor;

/// <summary>
/// Fluent descriptor for configuring a messaging transport, including consumer binding, middleware pipelines,
/// naming conventions, and transport-level options.
/// </summary>
public interface IMessagingTransportDescriptor
    : IMessagingDescriptor<MessagingTransportConfiguration>
    , IReceiveMiddlewareProvider
    , IDispatchMiddlewareProvider
{
    /// <summary>
    /// Applies a configuration delegate to the transport-level options such as concurrency and prefetch settings.
    /// </summary>
    /// <param name="configure">A delegate to mutate the <see cref="TransportOptions"/>.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IMessagingTransportDescriptor ModifyOptions(Action<TransportOptions> configure);

    /// <summary>
    /// Configures the transport to automatically bind consumers to endpoints based on message type conventions.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IMessagingTransportDescriptor BindHandlersImplicitly();

    /// <summary>
    /// Configures the transport to require explicit consumer-to-endpoint bindings rather than convention-based discovery.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IMessagingTransportDescriptor BindHandlersExplicitly();

    /// <summary>
    /// Sets the schema prefix used for address resolution on this transport.
    /// </summary>
    /// <param name="schema">The schema string (e.g., "rabbitmq", "azure-sb").</param>
    /// <returns>The descriptor for method chaining.</returns>
    IMessagingTransportDescriptor Schema(string schema);

    /// <summary>
    /// Sets the logical name of this transport, used for identification in diagnostics and multi-transport configurations.
    /// </summary>
    /// <param name="name">The transport name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IMessagingTransportDescriptor Name(string name);

    /// <summary>
    /// Registers a naming or routing convention that the transport applies when resolving endpoint addresses.
    /// </summary>
    /// <param name="convention">The convention to add.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IMessagingTransportDescriptor AddConvention(IConvention convention);

    /// <summary>
    /// Marks this transport as the default, used when no explicit transport is specified for a message type.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IMessagingTransportDescriptor IsDefaultTransport();

    /// <summary>
    /// Adds a dispatch middleware to the transport-scoped outbound pipeline.
    /// </summary>
    /// <param name="configuration">The middleware configuration to add.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IMessagingTransportDescriptor UseDispatch(DispatchMiddlewareConfiguration configuration);

    /// <summary>
    /// Inserts a dispatch middleware into the transport-scoped outbound pipeline immediately after the middleware identified by <paramref name="after"/>.
    /// </summary>
    /// <param name="after">The name of the existing middleware to insert after.</param>
    /// <param name="configuration">The middleware configuration to insert.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IMessagingTransportDescriptor AppendDispatch(string after, DispatchMiddlewareConfiguration configuration);

    /// <summary>
    /// Inserts a dispatch middleware into the transport-scoped outbound pipeline immediately before the middleware identified by <paramref name="before"/>.
    /// </summary>
    /// <param name="before">The name of the existing middleware to insert before.</param>
    /// <param name="configuration">The middleware configuration to insert.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IMessagingTransportDescriptor PrependDispatch(string before, DispatchMiddlewareConfiguration configuration);

    /// <summary>
    /// Adds a receive middleware to the transport-scoped inbound pipeline.
    /// </summary>
    /// <param name="configuration">The middleware configuration to add.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IMessagingTransportDescriptor UseReceive(ReceiveMiddlewareConfiguration configuration);

    /// <summary>
    /// Inserts a receive middleware into the transport-scoped inbound pipeline immediately after the middleware identified by <paramref name="after"/>.
    /// </summary>
    /// <param name="after">The name of the existing middleware to insert after.</param>
    /// <param name="configuration">The middleware configuration to insert.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IMessagingTransportDescriptor AppendReceive(string after, ReceiveMiddlewareConfiguration configuration);

    /// <summary>
    /// Inserts a receive middleware into the transport-scoped inbound pipeline immediately before the middleware identified by <paramref name="before"/>.
    /// </summary>
    /// <param name="before">The name of the existing middleware to insert before.</param>
    /// <param name="configuration">The middleware configuration to insert.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IMessagingTransportDescriptor PrependReceive(string before, ReceiveMiddlewareConfiguration configuration);
}

/// <summary>
/// Abstract base implementation of <see cref="IMessagingTransportDescriptor"/> that stores configuration
/// in a <typeparamref name="T"/> instance and provides fluent methods for transport setup.
/// </summary>
/// <typeparam name="T">The concrete configuration type, which must derive from <see cref="MessagingTransportConfiguration"/>.</typeparam>
/// <param name="context">The setup context providing access to services and configuration during bus initialization.</param>
public abstract class MessagingTransportDescriptor<T>(IMessagingSetupContext context)
    : MessagingDescriptorBase<T>(context)
    , IMessagingTransportDescriptor where T : MessagingTransportConfiguration
{
    /// <inheritdoc />
    public IMessagingTransportDescriptor ModifyOptions(Action<TransportOptions> configure)
    {
        configure(Configuration.Options);
        return this;
    }

    /// <inheritdoc />
    public IMessagingTransportDescriptor BindHandlersImplicitly()
    {
        Configuration.ConsumerBindingMode = ConsumerBindingMode.Implicit;
        return this;
    }

    /// <inheritdoc />
    public IMessagingTransportDescriptor BindHandlersExplicitly()
    {
        Configuration.ConsumerBindingMode = ConsumerBindingMode.Explicit;
        return this;
    }

    /// <inheritdoc />
    public IMessagingTransportDescriptor Schema(string schema)
    {
        Configuration.Schema = schema;
        return this;
    }

    /// <inheritdoc />
    public IMessagingTransportDescriptor Name(string name)
    {
        Configuration.Name = name;
        return this;
    }

    /// <inheritdoc />
    public IMessagingTransportDescriptor IsDefaultTransport()
    {
        Configuration.IsDefaultTransport = true;
        return this;
    }

    /// <inheritdoc />
    public IMessagingTransportDescriptor UseDispatch(DispatchMiddlewareConfiguration configuration)
    {
        Configuration.DispatchMiddlewares.Add(configuration);
        return this;
    }

    /// <inheritdoc />
    public IMessagingTransportDescriptor AppendDispatch(string after, DispatchMiddlewareConfiguration configuration)
    {
        Configuration.DispatchPipelineModifiers.Append(configuration, after);
        return this;
    }

    /// <inheritdoc />
    public IMessagingTransportDescriptor AddConvention(IConvention convention)
    {
        Configuration.Conventions.Add(convention);
        return this;
    }

    /// <inheritdoc />
    public IMessagingTransportDescriptor PrependDispatch(string before, DispatchMiddlewareConfiguration configuration)
    {
        Configuration.DispatchPipelineModifiers.Prepend(configuration, before);
        return this;
    }

    /// <inheritdoc />
    public IMessagingTransportDescriptor UseReceive(ReceiveMiddlewareConfiguration configuration)
    {
        Configuration.ReceiveMiddlewares.Add(configuration);
        return this;
    }

    /// <inheritdoc />
    public IMessagingTransportDescriptor AppendReceive(string after, ReceiveMiddlewareConfiguration configuration)
    {
        Configuration.ReceivePipelineModifiers.Append(configuration, after);
        return this;
    }

    /// <inheritdoc />
    public IMessagingTransportDescriptor PrependReceive(string before, ReceiveMiddlewareConfiguration configuration)
    {
        Configuration.ReceivePipelineModifiers.Prepend(configuration, before);
        return this;
    }

    /// <summary>
    /// Returns this descriptor as an extension point for the transport configuration, allowing additional
    /// configuration to be layered by external modules.
    /// </summary>
    /// <returns>This descriptor cast as <see cref="IDescriptorExtension{MessagingTransportConfiguration}"/>.</returns>
    public new IDescriptorExtension<MessagingTransportConfiguration> Extend()
    {
        return this;
    }

    /// <summary>
    /// Applies an extension configuration delegate to this transport descriptor.
    /// </summary>
    /// <param name="configure">A delegate that configures the transport through the extension interface.</param>
    /// <returns>This descriptor cast as <see cref="IDescriptorExtension{MessagingTransportConfiguration}"/>.</returns>
    public IDescriptorExtension<MessagingTransportConfiguration> ExtendWith(
        Action<IDescriptorExtension<MessagingTransportConfiguration>> configure)
    {
        return this;
    }

    /// <summary>
    /// Applies an extension configuration delegate with caller-provided state to this transport descriptor.
    /// </summary>
    /// <typeparam name="TState">The type of the state object passed to the delegate.</typeparam>
    /// <param name="configure">A delegate that configures the transport through the extension interface using the provided state.</param>
    /// <param name="state">The state object forwarded to the delegate.</param>
    /// <returns>This descriptor cast as <see cref="IDescriptorExtension{MessagingTransportConfiguration}"/>.</returns>
    public IDescriptorExtension<MessagingTransportConfiguration> ExtendWith<TState>(
        Action<IDescriptorExtension<MessagingTransportConfiguration>, TState> configure,
        TState state)
    {
        return this;
    }

    /// <summary>
    /// Marks this descriptor as internal, preventing external consumers from using it.
    /// </summary>
    /// <exception cref="NotImplementedException">Always thrown; this method is not yet implemented.</exception>
    public void Internal()
    {
        throw new NotImplementedException();
    }
}
