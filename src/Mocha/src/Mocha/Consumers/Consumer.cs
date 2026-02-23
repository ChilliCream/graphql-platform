using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Base type for executable inbound handlers in the receive pipeline.
/// </summary>
/// <remarks>
/// Consumers encapsulate routing metadata plus a compiled consumer-middleware pipeline around
/// <see cref="ConsumeAsync(IConsumeContext)"/>.
/// Bus setup maps user handler interfaces to concrete consumer implementations
/// (request/send/subscribe/reply), so endpoint execution can treat all inbound work uniformly.
/// </remarks>
public abstract class Consumer
{
    private readonly Action<IConsumerDescriptor> _configure;

    /// <summary>
    /// Creates a consumer with an external configuration action for the consumer descriptor.
    /// </summary>
    /// <param name="configure">
    /// The action used to configure the consumer descriptor during initialization.
    /// </param>
    protected Consumer(Action<IConsumerDescriptor> configure)
    {
        _configure = configure;
    }

    /// <summary>
    /// Creates a consumer that uses the virtual <see cref="Configure(IConsumerDescriptor)"/> method
    /// for descriptor setup.
    /// </summary>
    protected Consumer()
    {
        _configure = Configure;
    }

    /// <summary>
    /// Override to configure the consumer descriptor with name, routes, and middleware. Called
    /// during initialization.
    /// </summary>
    /// <param name="descriptor">
    /// The consumer descriptor to configure.
    /// </param>
    protected virtual void Configure(IConsumerDescriptor descriptor) { }

    protected internal ConsumerConfiguration? Configuration { get; private set; }

    /// <summary>
    /// Gets the logical name of this consumer, as set during configuration.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the CLR type that identifies this consumer, typically the handler type it wraps.
    /// </summary>
    public Type Identity { get; private set; } = null!;

    private ConsumerDelegate _pipeline = null!;

    /// <summary>
    /// Handles an incoming message after the consume middleware pipeline has completed.
    /// Subclasses must implement this method to define the terminal consumer logic.
    /// </summary>
    /// <remarks>
    /// This method is invoked as the innermost delegate of the compiled consumer middleware
    /// pipeline. All registered consume middlewares will have executed before this method
    /// is called.
    /// </remarks>
    /// <param name="context">
    /// The consume context containing the deserialized message, headers, and services.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous consume operation.
    /// </returns>
    protected abstract ValueTask ConsumeAsync(IConsumeContext context);

    /// <summary>
    /// Executes the compiled consumer middleware pipeline for the given receive context.
    /// </summary>
    /// <param name="context">
    /// The receive context that must also implement <see cref="IConsumeContext"/>.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the context does not implement <see cref="IConsumeContext"/>.
    /// </exception>
    public async ValueTask ProcessAsync(IReceiveContext context)
    {
        if (context is not IConsumeContext handlerContext)
        {
            throw new InvalidOperationException("Context is not a handler context");
        }

        await _pipeline(handlerContext);
    }

    /// <summary>
    /// Performs the initialization lifecycle phase for this consumer, creating its configuration,
    /// registering inbound routes, and assigning its name and identity.
    /// </summary>
    /// <remarks>
    /// This method is called once during the messaging runtime build phase. It invokes
    /// <see cref="OnBeforeInitialize"/> and <see cref="OnAfterInitialize"/> hooks, creates the
    /// <see cref="ConsumerConfiguration"/> from the descriptor, and registers all inbound routes
    /// with the router. After this call the consumer is marked as initialized and cannot be
    /// initialized again.
    /// </remarks>
    /// <param name="context">
    /// The setup context providing services, router, and naming conventions.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the consumer has already been initialized, when the configuration is
    /// <see langword="null"/>,
    /// or when the consumer name is <see langword="null"/>.
    /// </exception>
    internal void Initialize(IMessagingSetupContext context)
    {
        AssertUninitialized();

        OnBeforeInitialize(context);

        Configuration = CreateConfiguration(context);

        if (Configuration is null)
        {
            throw new InvalidOperationException("Handler configuration is null");
        }

        // TODO should we assign a default name in the Action? GetType().Name?
        Name = Configuration.Name ?? throw new InvalidOperationException("Consumer name is null");
        Identity ??= GetType();

        foreach (var route in Configuration!.Routes)
        {
            route.Consumer = this;

            var inboundRoute = new InboundRoute();
            inboundRoute.Initialize(context, route);

            context.Router.AddOrUpdate(inboundRoute);
        }

        OnAfterInitialize(context);

        MarkInitialized();
    }

    protected void SetIdentity(Type identity)
    {
        Identity = identity;
    }

    protected virtual void OnBeforeInitialize(IMessagingSetupContext context) { }

    protected virtual void OnAfterInitialize(IMessagingSetupContext context) { }

    /// <summary>
    /// Performs the completion lifecycle phase by compiling the consumer middleware pipeline
    /// into a single executable delegate.
    /// </summary>
    /// <remarks>
    /// This method is called after all consumers and transports have been initialized. It
    /// combines global and per-consumer middleware registrations and pipeline modifiers, then
    /// compiles them into the <see cref="ConsumerDelegate"/> used by <see cref="ProcessAsync"/>.
    /// Must be called after <see cref="Initialize"/> has completed.
    /// </remarks>
    /// <param name="context">
    /// The setup context providing services and middleware registrations.
    /// </param>
    internal void Complete(IMessagingSetupContext context)
    {
        // Consumer-specific and global middlewares are compiled once during setup.
        var middlewareFactoryContext = new ConsumerMiddlewareFactoryContext
        {
            Services = context.Services,
            Consumer = this
        };

        _pipeline = MiddlewareCompiler.CompileHandler(
            middlewareFactoryContext,
            ConsumeAsync,
            [context.GetConsumerMiddlewares(), Configuration!.ConsumerMiddlewares],
            [context.GetConsumerPipelineModifiers(), Configuration.ConsumerPipelineModifiers]);
    }

    private bool _isInitialized;

    private void AssertUninitialized()
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("Handler already initialized");
        }
    }

    private void MarkInitialized()
    {
        _isInitialized = true;
    }

    /// <summary>
    /// Returns a description of this consumer for diagnostic and visualization purposes.
    /// </summary>
    /// <returns>A <see cref="ConsumerDescription"/> containing the consumer's name, type, and optional saga association.</returns>
    public virtual ConsumerDescription Describe()
    {
        return new ConsumerDescription(Name, DescriptionHelpers.GetTypeName(Identity), Identity.FullName, null, false);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting
    /// unmanaged resources asynchronously. Override in subclasses to flush or clean up state.
    /// </summary>
    /// <returns>A value task representing the asynchronous dispose operation.</returns>
    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private ConsumerConfiguration CreateConfiguration(IMessagingSetupContext discoveryContext)
    {
        var descriptor = new ConsumerDescriptor(discoveryContext);
        _configure(descriptor);
        return descriptor.CreateConfiguration();
    }
}

file static class Extensions
{
    public static IReadOnlyList<ConsumerMiddlewareConfiguration> GetConsumerMiddlewares(this IFeatureProvider provider)
    {
        return provider.Features.Get<MiddlewareFeature>()?.HandlerMiddlewares ?? [];
    }

    public static IReadOnlyList<Action<List<ConsumerMiddlewareConfiguration>>> GetConsumerPipelineModifiers(
        this IFeatureProvider provider)
    {
        return provider.Features.Get<MiddlewareFeature>()?.HandlerPipelineModifiers ?? [];
    }
}
