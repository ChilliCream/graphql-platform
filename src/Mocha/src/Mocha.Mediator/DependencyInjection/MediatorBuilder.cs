using System.Collections.Frozen;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mocha.Features;

namespace Mocha.Mediator;

/// <summary>
/// Represents the default implementation of <see cref="IMediatorBuilder"/>.
/// Accumulates configuration and builds the <see cref="MediatorRuntime"/>
/// with its own internal service provider.
/// </summary>
public sealed class MediatorBuilder : IMediatorBuilder
{
    private readonly List<MediatorMiddlewareConfiguration> _middlewares =
    [
        MediatorDiagnosticMiddleware.Create()
    ];

    private readonly List<Action<List<MediatorMiddlewareConfiguration>>> _pipelineModifiers = [];
    private readonly List<MediatorPipelineConfiguration> _pipelines = [];
    private readonly List<Action<IServiceProvider, IServiceCollection>> _configureServices = [];
    private readonly List<Action<IFeatureCollection>> _configureFeatures = [];
    private readonly MediatorOptions _options = new();

    /// <inheritdoc />
    public IMediatorBuilder ConfigureOptions(Action<MediatorOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(_options);
        return this;
    }

    /// <inheritdoc />
    public IMediatorBuilder Use(MediatorMiddlewareConfiguration middleware, string? before = null, string? after = null)
    {
        ArgumentNullException.ThrowIfNull(middleware);

        if (before is not null && after is not null)
        {
            throw new ArgumentException(
                "Only one of 'before' or 'after' can be specified at the same time.");
        }

        if (before is null && after is null)
        {
            _middlewares.Add(middleware);
            return this;
        }

        var anchor = (before ?? after)!;

        _pipelineModifiers.Add(pipeline =>
        {
            var index = pipeline.FindIndex(m => m.Key == anchor);

            if (index == -1)
            {
                throw new InvalidOperationException(
                    $"The middleware with the key `{anchor}` was not found.");
            }

            pipeline.Insert(before is not null ? index : index + 1, middleware);
        });

        return this;
    }

    /// <inheritdoc />
    public IMediatorBuilder ConfigureFeature(Action<IFeatureCollection> configure)
    {
        _configureFeatures.Add(configure);
        return this;
    }

    /// <inheritdoc />
    public IMediatorBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        _configureServices.Add((_, services) => configure(services));
        return this;
    }

    /// <inheritdoc />
    public IMediatorBuilder ConfigureServices(Action<IServiceProvider, IServiceCollection> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        _configureServices.Add(configure);
        return this;
    }

    /// <inheritdoc />
    public void RegisterPipeline(MediatorPipelineConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _pipelines.Add(configuration);
    }

    /// <summary>
    /// Builds the <see cref="MediatorRuntime"/> by creating an internal service provider,
    /// applying deferred service configurations, and compiling all registered pipelines.
    /// </summary>
    /// <param name="applicationServices">
    /// The application-level service provider used to resolve shared services.
    /// </param>
    public MediatorRuntime Build(IServiceProvider applicationServices)
    {
        // Create the mediator's own internal service collection.
        var internalServices = new ServiceCollection();

        // Apply deferred service configurations (e.g. diagnostic listeners).
        foreach (var configure in _configureServices)
        {
            configure(applicationServices, internalServices);
        }

        // Resolve the aggregate diagnostic events from registered listeners,
        // falling back to a no-op when no listeners are registered.
        AddDiagnosticEvents(internalServices);

        var internalProvider = internalServices.BuildServiceProvider();

        // Build the feature collection.
        var features = new FeatureCollection();
        foreach (var configure in _configureFeatures)
        {
            configure(features);
        }

        if (!features.TryGet<NotificationStrategyFeature>(out _))
        {
            var strategy = applicationServices.GetService<INotificationStrategy>()
                ?? new ForeachAwaitPublisher();
            features.Set(new NotificationStrategyFeature(strategy));
        }

        // Compile pipelines using the internal provider for middleware factory context.
        var factoryCtx = new MediatorMiddlewareFactoryContext { Services = internalProvider, Features = features };

        var middlewareConfigs = _middlewares.Count > 0
            ? new IReadOnlyList<MediatorMiddlewareConfiguration>[] { _middlewares }
            : [];

        var modifiers = _pipelineModifiers.Count > 0
            ? new IReadOnlyList<Action<List<MediatorMiddlewareConfiguration>>>[] { _pipelineModifiers }
            : [];

        var pipelines = new Dictionary<Type, MediatorDelegate>(_pipelines.Count);

        foreach (var config in _pipelines)
        {
            factoryCtx.MessageType = config.MessageType;
            factoryCtx.ResponseType = config.ResponseType;

            pipelines[config.MessageType] =
                MediatorMiddlewareCompiler.Compile(factoryCtx, config.Terminal, middlewareConfigs, modifiers);
        }

        var pools = applicationServices.GetRequiredService<IMediatorPools>();

        return new MediatorRuntime(pipelines.ToFrozenDictionary(), pools, features);
    }

    /// <summary>
    /// Resolves diagnostic event listeners from internal services and registers
    /// the aggregate <see cref="IMediatorDiagnosticEvents"/> implementation.
    /// Falls back to a no-op when no listeners are registered.
    /// </summary>
    private static void AddDiagnosticEvents(IServiceCollection services)
    {
        services.TryAddSingleton<IMediatorDiagnosticEvents>(sp =>
        {
            var listeners = sp.GetServices<IMediatorDiagnosticEventListener>().ToArray();

            return listeners.Length switch
            {
                0 => NoopMediatorDiagnosticEvents.Instance,
                1 => listeners[0],
                _ => new AggregateMediatorDiagnosticEvents(listeners)
            };
        });
    }
}
