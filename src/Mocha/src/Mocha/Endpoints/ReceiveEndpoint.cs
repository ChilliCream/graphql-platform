using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Base class for receive endpoints that consume messages from a transport source and
/// execute them through a compiled receive middleware pipeline.
/// </summary>
/// <remarks>
/// Follows a strict lifecycle: <see cref="Initialize"/> -> <see cref="DiscoverTopology"/> ->
/// <see cref="Complete"/> -> <see cref="StartAsync"/> -> <see cref="StopAsync"/>.
/// The receive pipeline is compiled during <see cref="Complete"/> from transport-level and
/// endpoint-level middleware configurations. Each incoming message is processed inside a
/// scoped <see cref="IServiceProvider"/>, and the <see cref="ReceiveContext"/> is pooled for
/// allocation efficiency.
/// </remarks>
/// <param name="transport">The messaging transport that this endpoint receives messages from.</param>
public abstract class ReceiveEndpoint(MessagingTransport transport) : IReceiveEndpoint, IFeatureProvider
{
    private ReceiveDelegate _pipeline = null!;

    private RuntimeState? _runtimeState;

    private CancellationTokenSource? _processingCts;

    /// <summary>
    /// Gets the endpoint configuration that was applied during initialization.
    /// </summary>
    protected ReceiveEndpointConfiguration Configuration { get; private set; } = null!;

    /// <summary>
    /// Gets the messaging transport that this endpoint receives messages from.
    /// </summary>
    public MessagingTransport Transport => transport;

    /// <summary>
    /// Gets a value indicating whether this endpoint has been initialized.
    /// </summary>
    public bool IsInitialized { get; protected set; }

    /// <summary>
    /// Gets a value indicating whether this endpoint has completed its configuration phase.
    /// </summary>
    /// <remarks>
    /// Once completed, the receive pipeline is compiled and the endpoint is ready to be started.
    /// </remarks>
    public bool IsCompleted { get; protected set; }

    /// <summary>
    /// Gets a value indicating whether this endpoint is currently started and able to process messages.
    /// </summary>
    public bool IsStarted { get; protected set; }

    /// <summary>
    /// Gets the unique name of this receive endpoint.
    /// </summary>
    public string Name { get; protected set; } = null!;

    /// <summary>
    /// Gets the topology resource that represents the source from which messages are consumed.
    /// </summary>
    public TopologyResource Source { get; protected set; } = null!;

    /// <summary>
    /// Gets the transport-specific address of this receive endpoint.
    /// </summary>
    public Uri Address { get; protected set; } = null!;

    /// <summary>
    /// Gets the stable URN identity of this receive endpoint.
    /// </summary>
    public string Urn { get; private set; } = null!;

    /// <summary>
    /// Gets the classification of this receive endpoint.
    /// </summary>
    public ReceiveEndpointKind Kind { get; protected set; }

    /// <summary>
    /// Gets the feature collection associated with this endpoint for storing extensibility data.
    /// </summary>
    public IFeatureCollection Features { get; } = new FeatureCollection();

    /// <summary>
    /// Gets the token that cancels the messages this endpoint processes. It is cancelled when the token
    /// passed to <see cref="StopAsync"/> is cancelled, and when the stop completes.
    /// </summary>
    protected CancellationToken ProcessingToken => _processingCts?.Token ?? CancellationToken.None;

    /// <summary>
    /// Processes a single incoming message through the receive pipeline.
    /// </summary>
    /// <remarks>
    /// Configures a pooled <see cref="ReceiveContext"/> via <paramref name="configure"/> and executes the
    /// compiled middleware pipeline in a new service scope. <paramref name="cancellationToken"/> becomes
    /// the cancellation token of the receive. Exceptions that escape the pipeline are logged at the
    /// Critical level, unless <paramref name="cancellationToken"/> is cancelled, in which case they
    /// propagate to the caller.
    /// </remarks>
    /// <typeparam name="TState">The type of caller-provided state passed to the configure action.</typeparam>
    /// <param name="configure">
    /// A callback that populates the <see cref="ReceiveContext"/> with transport-specific
    /// message data (envelope, body, headers, etc.) before the pipeline runs.
    /// </param>
    /// <param name="state">Caller-provided state forwarded to <paramref name="configure"/>.</param>
    /// <param name="cancellationToken">The token that cancels the receive operation.</param>
    public async ValueTask ExecuteAsync<TState>(
        Action<ReceiveContext, TState> configure,
        TState state,
        CancellationToken cancellationToken)
    {
        var logger = _runtimeState!.Logger;
        var services = _runtimeState!.ServiceProvider;
        var pools = _runtimeState.Pools;
        var lazyRuntime = _runtimeState.LazyRuntime;

        await using var scope = services.CreateAsyncScope();

        var context = pools.ReceiveContext.Get();
        try
        {
            context.Initialize(scope.ServiceProvider, this, lazyRuntime.Runtime, cancellationToken);

            configure(context, state);

            var accessor = scope.ServiceProvider.GetRequiredService<ConsumeContextAccessor>();
            accessor.Context = context;

            await _pipeline(context);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // exceptions should technically never bubble up here.
            logger.LogCritical(ex, "Error processing message");
        }
        finally
        {
            var accessor = scope.ServiceProvider.GetRequiredService<ConsumeContextAccessor>();
            accessor.Context = null;
            pools.ReceiveContext.Return(context);
        }
    }

    /// <summary>
    /// Initializes the endpoint by applying conventions and storing the configuration.
    /// </summary>
    /// <remarks>
    /// Must be called exactly once before any other lifecycle method. Applies the transport
    /// conventions to the configuration, sets the endpoint name and kind, and delegates to
    /// <see cref="OnInitialize"/> for transport-specific initialization logic.
    /// </remarks>
    /// <param name="context">The messaging configuration context providing access to services and conventions.</param>
    /// <param name="configuration">The receive endpoint configuration to apply.</param>
    /// <exception cref="InvalidOperationException">Thrown if the endpoint has already been initialized or if the configuration name is <see langword="null"/>.</exception>
    public void Initialize(IMessagingConfigurationContext context, ReceiveEndpointConfiguration configuration)
    {
        AssertUninitialized();

        Transport.Routing.ConfigureEndpoint(context, configuration);
        Transport.Conventions.Configure(context, Transport, configuration);

        Configuration = configuration;
        Kind = configuration.Kind;
        Name = configuration.Name ?? throw ThrowHelper.EndpointNameRequired();
        Urn = MochaUrn.ReceiveEndpoint(context.Host.EffectiveServiceName, Transport.Schema, Transport.Name, Name);
        configuration.Features.CopyTo(Features);

        OnInitialize(context, Configuration);

        MarkInitialized();
    }

    /// <summary>
    /// When overridden in a derived class, performs transport-specific initialization logic.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="configuration">The receive endpoint configuration that has been applied.</param>
    protected abstract void OnInitialize(
        IMessagingConfigurationContext context,
        ReceiveEndpointConfiguration configuration);

    /// <summary>
    /// Runs topology discovery conventions to resolve the source resources for this endpoint.
    /// </summary>
    /// <param name="context">The messaging configuration context used for topology discovery.</param>
    public void DiscoverTopology(IMessagingConfigurationContext context)
    {
        OnDiscoverTopology(context, Configuration);

        Transport.Routing.DiscoverTopology(context, this, Configuration);
    }

    /// <summary>
    /// When overridden in a derived class, ensures the endpoint's own backing resource exists before
    /// the topology conventions run.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="configuration">The receive endpoint configuration.</param>
    protected virtual void OnDiscoverTopology(
        IMessagingConfigurationContext context,
        ReceiveEndpointConfiguration configuration)
    { }

    /// <summary>
    /// Finalizes the endpoint configuration by compiling the receive pipeline and resolving
    /// error and skipped dispatch endpoints.
    /// </summary>
    /// <remarks>
    /// After this method returns, the endpoint address is resolved, the error and skipped
    /// dispatch endpoints are created if configured, and the middleware pipeline is compiled
    /// from both transport-level and endpoint-level middleware registrations. The endpoint
    /// is then ready to be started.
    /// </remarks>
    /// <param name="context">The messaging configuration context.</param>
    public void Complete(IMessagingConfigurationContext context)
    {
        OnComplete(context, Configuration);

        Address ??= new UriBuilder { Scheme = Transport.Schema, Path = Name }.Uri;

        var faultFeature = Features.Get<ReceiveFaultEndpointFeature>();
        if (faultFeature is { Endpoint: null, Address: { } faultAddress })
        {
            faultFeature.Endpoint = context.Endpoints.GetOrCreate(context, faultAddress);
        }

        var skippedFeature = Features.Get<ReceiveSkippedEndpointFeature>();
        if (skippedFeature is { Endpoint: null, Address: { } skippedAddress })
        {
            skippedFeature.Endpoint = context.Endpoints.GetOrCreate(context, skippedAddress);
        }

        _pipeline = MiddlewareCompiler.CompileReceive(
            new ReceiveMiddlewareFactoryContext
            {
                Services = context.Services,
                Endpoint = this,
                Transport = Transport
            },
            DefaultPipeline,
            [transport.GetReceiveMiddlewares(), Configuration.ReceiveMiddlewares],
            [transport.GetReceivePipelineModifiers(), Configuration.ReceivePipelineModifiers]);
        IsCompleted = true;
    }

    /// <summary>
    /// When overridden in a derived class, performs transport-specific completion logic before
    /// the pipeline is compiled.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="configuration">The receive endpoint configuration.</param>
    protected virtual void OnComplete(
        IMessagingConfigurationContext context,
        ReceiveEndpointConfiguration configuration)
    { }

    /// <summary>
    /// Starts this endpoint, enabling it to begin receiving and processing messages.
    /// </summary>
    /// <remarks>
    /// Calling it on an already-started endpoint is a no-op. A stopped endpoint cannot be started again.
    /// </remarks>
    /// <param name="context">The messaging runtime context providing access to runtime services.</param>
    /// <param name="cancellationToken">Token to signal cancellation of the start operation.</param>
    /// <exception cref="InvalidOperationException">Thrown if the endpoint has been stopped.</exception>
    public async ValueTask StartAsync(IMessagingRuntimeContext context, CancellationToken cancellationToken)
    {
        if (IsStarted)
        {
            return;
        }

        if (_processingCts is not null)
        {
            throw ThrowHelper.ReceiveEndpointStopped();
        }

        var logger = context.Services.GetRequiredService<ILogger<ReceiveEndpoint>>();
        var contextPool = context.Services.GetRequiredService<IMessagingPools>();
        var appServices = context.Services.GetRequiredService<IRootServiceProviderAccessor>().ServiceProvider;
        var lazyRuntime = context.Services.GetRequiredService<ILazyMessagingRuntime>();

        _runtimeState = new RuntimeState
        {
            Logger = logger,
            Pools = contextPool,
            ServiceProvider = appServices,
            LazyRuntime = lazyRuntime
        };

        _processingCts = new CancellationTokenSource();

        try
        {
            await OnStartAsync(context, cancellationToken);
        }
        catch
        {
            await _processingCts.CancelAsync();
            throw;
        }

        IsStarted = true;
    }

    /// <summary>
    /// Stops this endpoint. It takes no new messages and waits for in-flight messages to complete.
    /// </summary>
    /// <remarks>
    /// When <paramref name="cancellationToken"/> is cancelled, the in-flight messages are cancelled
    /// through <see cref="ProcessingToken"/>, and the stop waits for them at most
    /// <see cref="IReadOnlyTransportShutdownOptions.CancellationGracePeriod"/> longer. The endpoint
    /// counts as stopped once this method is called, even if the stop throws. Calling it on a stopped
    /// endpoint is a no-op.
    /// </remarks>
    /// <param name="context">The messaging runtime context.</param>
    /// <param name="cancellationToken">A token that cancels the in-flight messages.</param>
    public async ValueTask StopAsync(IMessagingRuntimeContext context, CancellationToken cancellationToken)
    {
        if (!IsStarted)
        {
            return;
        }

        IsStarted = false;

        var processing = _processingCts!;
        using var stop = new StopCancellation(
            processing,
            Transport.Options.Shutdown.CancellationGracePeriod,
            cancellationToken);

        try
        {
            await OnStopAsync(context, stop.Deadline);
        }
        finally
        {
            // Messages that outlive the stop are cancelled.
            await processing.CancelAsync();
        }
    }

    /// <summary>
    /// Waits for <paramref name="processing"/> to complete.
    /// </summary>
    /// <param name="processing">A task that completes when the endpoint has no in-flight messages.</param>
    /// <param name="cancellationToken">A token that ends the wait.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="processing"/> completed; <c>false</c> if
    /// <paramref name="cancellationToken"/> was cancelled first. A failure of
    /// <paramref name="processing"/> is rethrown.
    /// </returns>
    protected static async ValueTask<bool> WaitForProcessingAsync(Task processing, CancellationToken cancellationToken)
    {
        try
        {
            await processing.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (!processing.IsCompleted)
            {
                return false;
            }

            await processing;
        }

        return true;
    }

    /// <summary>
    /// When overridden in a derived class, performs transport-specific startup logic
    /// such as opening connections or subscribing to queues.
    /// </summary>
    /// <param name="context">The messaging runtime context.</param>
    /// <param name="cancellationToken">Token to signal cancellation of the start operation.</param>
    protected abstract ValueTask OnStartAsync(IMessagingRuntimeContext context, CancellationToken cancellationToken);

    /// <summary>
    /// When overridden in a derived class, stops taking new messages, waits for in-flight messages and
    /// releases transport resources.
    /// </summary>
    /// <param name="context">The messaging runtime context.</param>
    /// <param name="cancellationToken">
    /// A token that is cancelled when the stop no longer waits for in-flight messages, which is
    /// <see cref="IReadOnlyTransportShutdownOptions.CancellationGracePeriod"/> after their
    /// <see cref="ProcessingToken"/> is cancelled.
    /// </param>
    protected abstract ValueTask OnStopAsync(IMessagingRuntimeContext context, CancellationToken cancellationToken);

    private void AssertUninitialized()
    {
        Debug.Assert(!IsInitialized, "The type must be uninitialized.");

        if (IsInitialized)
        {
            throw ThrowHelper.EndpointAlreadyInitialized();
        }
    }

    /// <summary>
    /// Marks this endpoint as initialized, enabling subsequent lifecycle transitions.
    /// </summary>
    public void MarkInitialized()
    {
        IsInitialized = true;
    }

    /// <summary>
    /// Creates a read-only description of this endpoint for diagnostic or introspection purposes.
    /// </summary>
    /// <returns>
    /// A <see cref="ReceiveEndpointDescription"/> containing the endpoint name, kind, address,
    /// and source address.
    /// </returns>
    public ReceiveEndpointDescription Describe()
    {
        return new ReceiveEndpointDescription(Urn, Name, Kind, Address?.ToString(), Source?.Address?.ToString());
    }

    private static async ValueTask DefaultPipeline(IReceiveContext context)
    {
        // The parsing feature has to live on the receive context so consumer attempts, which run
        // on clones that fall back to the receive context's features, share one lazily deserialized message.
        context.Features.GetOrSet<MessageParsingFeature>();

        var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();
        var consumers = feature.Consumers;

        foreach (var consumer in consumers)
        {
            try
            {
                feature.CurrentConsumer = consumer;
                await consumer.ProcessAsync(context);
                feature.MessageConsumed = true;
            }
            finally
            {
                feature.CurrentConsumer = null;
            }
        }
    }

    /// <summary>
    /// Cancels processing when the stop token is cancelled, and the deadline one grace period later.
    /// </summary>
    private sealed class StopCancellation : IDisposable
    {
        private readonly CancellationTokenSource _processing;
        private readonly CancellationTokenSource _deadline = new();
        private readonly TimeSpan _gracePeriod;
        private readonly CancellationTokenRegistration _registration;

        public StopCancellation(
            CancellationTokenSource processing,
            TimeSpan gracePeriod,
            CancellationToken stopToken)
        {
            _processing = processing;
            _gracePeriod = gracePeriod;
            _registration = stopToken.UnsafeRegister(
                static state => ((StopCancellation)state!).OnStopCancelled(),
                this);
        }

        public CancellationToken Deadline => _deadline.Token;

        private void OnStopCancelled()
        {
            _deadline.CancelAfter(_gracePeriod);

            // Handlers continue on the thread pool instead of the thread that cancels the stop.
            _ = _processing.CancelAsync();
        }

        public void Dispose()
        {
            _registration.Dispose();
            _deadline.Dispose();
        }
    }

    private sealed class RuntimeState
    {
        public required ILogger Logger { get; init; }
        public required IMessagingPools Pools { get; init; }
        public required IServiceProvider ServiceProvider { get; init; }
        public required ILazyMessagingRuntime LazyRuntime { get; init; }
    }
}

file static class Extensions
{
    public static IReadOnlyList<ReceiveMiddlewareConfiguration> GetReceiveMiddlewares(this IFeatureProvider provider)
    {
        return provider.Features.Get<MiddlewareFeature>()?.ReceiveMiddlewares ?? [];
    }

    public static IReadOnlyList<Action<List<ReceiveMiddlewareConfiguration>>> GetReceivePipelineModifiers(
        this IFeatureProvider provider)
    {
        return provider.Features.Get<MiddlewareFeature>()?.ReceivePipelineModifiers ?? [];
    }
}
