using System.Diagnostics;
using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Represents a dispatch endpoint that sends messages to a transport destination.
/// </summary>
/// <remarks>
/// Combines the base <see cref="IEndpoint"/> identity with the transport binding,
/// endpoint kind, and the ability to execute a dispatch pipeline for outgoing messages.
/// </remarks>
public interface IDispatchEndpoint : IEndpoint
{
    /// <summary>
    /// Gets the classification of this dispatch endpoint.
    /// </summary>
    /// <remarks>
    /// The kind indicates the endpoint's role: <see cref="DispatchEndpointKind.Default"/>
    /// for standard outgoing messages, or <see cref="DispatchEndpointKind.Reply"/> for
    /// request-reply responses.
    /// </remarks>
    DispatchEndpointKind Kind { get; }

    /// <summary>
    /// Gets the messaging transport that this endpoint dispatches messages through.
    /// </summary>
    MessagingTransport Transport { get; }

    /// <summary>
    /// Sends a message through the dispatch middleware pipeline to the transport destination.
    /// </summary>
    /// <param name="context">The dispatch context containing the message, headers, and routing information.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the message has been dispatched.</returns>
    ValueTask ExecuteAsync(IDispatchContext context);
}

/// <summary>
/// Base class for dispatch endpoints that send messages through a compiled dispatch
/// middleware pipeline to a transport destination.
/// </summary>
/// <remarks>
/// Follows a strict lifecycle: <see cref="Initialize"/> -> <see cref="DiscoverTopology"/> ->
/// <see cref="Complete"/>.
/// Before <see cref="Complete"/> is called, the pipeline uses a deferred delegate that awaits
/// a <see cref="TaskCompletionSource{TResult}"/>, allowing early callers to enqueue dispatches
/// that will execute once the pipeline is fully compiled. After completion, the compiled
/// pipeline replaces the deferred delegate via a volatile write.
/// </remarks>
public abstract class DispatchEndpoint : IDispatchEndpoint
{
    private TaskCompletionSource<bool> _completed;
    private DispatchDelegate _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="DispatchEndpoint"/> class bound to
    /// the specified transport.
    /// </summary>
    /// <remarks>
    /// Sets up a deferred pipeline delegate that blocks dispatch calls until
    /// <see cref="Complete"/> compiles and installs the real pipeline.
    /// </remarks>
    /// <param name="transport">The messaging transport that this endpoint dispatches messages through.</param>
    protected DispatchEndpoint(MessagingTransport transport)
    {
        _completed = new TaskCompletionSource<bool>();
        _pipeline = async context =>
        {
            await _completed.Task;
            await Volatile.Read(ref _pipeline!)(context);
        };
        Transport = transport;
    }

    /// <summary>
    /// Gets the messaging transport that this endpoint dispatches messages through.
    /// </summary>
    public MessagingTransport Transport { get; }

    /// <summary>
    /// Gets the unique name of this dispatch endpoint.
    /// </summary>
    public string Name { get; protected set; } = null!;

    /// <summary>
    /// Gets a value indicating whether this endpoint has been initialized.
    /// </summary>
    public bool IsInitialized { get; protected set; }

    /// <summary>
    /// Gets a value indicating whether this endpoint has completed its configuration phase
    /// and has a fully compiled dispatch pipeline.
    /// </summary>
    public bool IsCompleted { get; protected set; }

    /// <summary>
    /// Gets the topology resource that represents the destination to which messages are dispatched.
    /// </summary>
    public TopologyResource Destination { get; protected set; } = null!;

    /// <summary>
    /// Gets the classification of this dispatch endpoint.
    /// </summary>
    public DispatchEndpointKind Kind { get; protected set; }

    /// <summary>
    /// Gets the transport-specific address of this dispatch endpoint.
    /// </summary>
    public Uri Address { get; protected set; } = null!;

    /// <summary>
    /// Gets the endpoint configuration that was applied during initialization.
    /// </summary>
    protected DispatchEndpointConfiguration Configuration { get; private set; } = null!;

    /// <summary>
    /// Initializes the endpoint by applying conventions and storing the configuration.
    /// </summary>
    /// <remarks>
    /// Must be called exactly once before any other lifecycle method. Applies the transport
    /// conventions to the configuration, sets the endpoint name and kind, and delegates to
    /// <see cref="OnInitialize"/> for transport-specific initialization logic.
    /// </remarks>
    /// <param name="context">The messaging configuration context providing access to services and conventions.</param>
    /// <param name="configuration">The dispatch endpoint configuration to apply.</param>
    /// <exception cref="InvalidOperationException">Thrown if the endpoint has already been initialized or if the configuration name is <see langword="null"/>.</exception>
    public void Initialize(IMessagingConfigurationContext context, DispatchEndpointConfiguration configuration)
    {
        AssertUninitialized();

        Transport.Conventions.Configure(context, Transport, configuration);
        Configuration = configuration;
        Kind = configuration.Kind;
        Name = configuration.Name ?? throw new InvalidOperationException("Name is required");

        OnInitialize(context, configuration);

        MarkInitialized();
    }

    /// <summary>
    /// When overridden in a derived class, performs transport-specific initialization logic.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="configuration">The dispatch endpoint configuration that has been applied.</param>
    protected abstract void OnInitialize(
        IMessagingConfigurationContext context,
        DispatchEndpointConfiguration configuration);

    /// <summary>
    /// Sends a message through the dispatch middleware pipeline to the transport destination.
    /// </summary>
    /// <remarks>
    /// If <see cref="Complete"/> has not yet been called, the call will asynchronously wait
    /// until the pipeline is compiled and then execute the dispatch.
    /// </remarks>
    /// <param name="context">The dispatch context containing the message, headers, and routing information.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the message has been dispatched.</returns>
    public ValueTask ExecuteAsync(IDispatchContext context) => _pipeline(context);

    /// <summary>
    /// Runs topology discovery conventions to resolve the destination resources for this endpoint
    /// and registers the endpoint in the endpoint collection.
    /// </summary>
    /// <param name="context">The messaging configuration context used for topology discovery.</param>
    public void DiscoverTopology(IMessagingConfigurationContext context)
    {
        Transport.Conventions.DiscoverTopology(context, this, Configuration);
        context.Endpoints.AddOrUpdate(this);
    }

    /// <summary>
    /// Finalizes the endpoint configuration by compiling the dispatch pipeline and
    /// unblocking any deferred dispatch calls.
    /// </summary>
    /// <remarks>
    /// Delegates to <see cref="OnComplete"/> for transport-specific completion, then compiles
    /// the middleware pipeline from both transport-level and endpoint-level registrations.
    /// The compiled pipeline atomically replaces the deferred delegate via a volatile write,
    /// and the <see cref="TaskCompletionSource{TResult}"/> is signaled to unblock any
    /// dispatch calls that were issued before completion. After this method returns, the
    /// <see cref="Configuration"/> is cleared and the endpoint address is resolved.
    /// </remarks>
    /// <param name="context">The messaging configuration context.</param>
    public void Complete(IMessagingConfigurationContext context)
    {
        OnComplete(context, Configuration);

        var pipeline = MiddlewareCompiler.CompileDispatch(
            new DispatchMiddlewareFactoryContext
            {
                Services = context.Services,
                Endpoint = this,
                Transport = Transport
            },
            DispatchAsync,
            [Transport.GetDispatchMiddlewares(), Configuration.DispatchMiddlewares],
            [Transport.GetDispatchPipelineModifiers(), Configuration.DispatchPipelineModifiers]);

        Volatile.Write(ref _pipeline, pipeline);

        Configuration = null!;
        _completed.SetResult(true);
        _completed = null!;
        IsCompleted = true;
        Address ??= new UriBuilder { Scheme = Transport.Schema, Path = Name }.Uri;

        context.Endpoints.AddOrUpdate(this);
    }

    /// <summary>
    /// When overridden in a derived class, performs transport-specific completion logic before
    /// the pipeline is compiled.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="configuration">The dispatch endpoint configuration.</param>
    protected abstract void OnComplete(
        IMessagingConfigurationContext context,
        DispatchEndpointConfiguration configuration);

    /// <summary>
    /// Creates a read-only description of this endpoint for diagnostic or introspection purposes.
    /// </summary>
    /// <returns>
    /// A <see cref="DispatchEndpointDescription"/> containing the endpoint name, kind, address,
    /// and destination address.
    /// </returns>
    public DispatchEndpointDescription Describe()
    {
        return new DispatchEndpointDescription(Name, Kind, Address?.ToString(), Destination?.Address?.ToString());
    }

    /// <summary>
    /// When overridden in a derived class, performs the actual transport-level dispatch of the
    /// message to the destination.
    /// </summary>
    /// <remarks>
    /// This method is called as the terminal delegate of the compiled dispatch pipeline, after
    /// all middleware has executed. Transport implementations should serialize and transmit
    /// the message here.
    /// </remarks>
    /// <param name="context">The dispatch context containing the fully prepared message.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the transport-level send is finished.</returns>
    protected abstract ValueTask DispatchAsync(IDispatchContext context);

    // TODO complete lifecyle
    private void AssertUninitialized()
    {
        Debug.Assert(!IsInitialized, "The endpoint must be uninitialized.");

        if (IsInitialized)
        {
            throw new InvalidOperationException("Endpoint already initialized");
        }
    }

    private void MarkInitialized()
    {
        IsInitialized = true;
    }
}

file static class Extensions
{
    public static IReadOnlyList<DispatchMiddlewareConfiguration> GetDispatchMiddlewares(this IFeatureProvider provider)
    {
        return provider.Features.Get<MiddlewareFeature>()?.DispatchMiddlewares ?? [];
    }

    public static IReadOnlyList<Action<List<DispatchMiddlewareConfiguration>>> GetDispatchPipelineModifiers(
        this IFeatureProvider provider)
    {
        return provider.Features.Get<MiddlewareFeature>()?.DispatchPipelineModifiers ?? [];
    }
}
