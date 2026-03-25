using System.Collections.Frozen;
using System.ComponentModel;
using System.Reflection;
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
    private static readonly MethodInfo s_buildCommandPipeline =
        typeof(PipelineBuilder).GetMethod(nameof(PipelineBuilder.BuildCommandPipeline))!;

    private static readonly MethodInfo s_buildCommandResponsePipeline =
        typeof(PipelineBuilder).GetMethod(nameof(PipelineBuilder.BuildCommandResponsePipeline))!;

    private static readonly MethodInfo s_buildQueryPipeline =
        typeof(PipelineBuilder).GetMethod(nameof(PipelineBuilder.BuildQueryPipeline))!;

    private static readonly MethodInfo s_buildNotificationPipeline =
        typeof(PipelineBuilder).GetMethod(nameof(PipelineBuilder.BuildNotificationPipeline))!;

    private readonly List<MediatorMiddlewareConfiguration> _middlewares =
    [
        MediatorDiagnosticMiddleware.Create()
    ];

    private readonly List<Action<List<MediatorMiddlewareConfiguration>>> _pipelineModifiers = [];
    private readonly Dictionary<Type, Action<MediatorHandlerDescriptor>> _handlerDescriptors = [];
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
    public void AddHandler<THandler>(Action<IMediatorHandlerDescriptor>? configure = null)
        where THandler : class
    {
        var handlerType = typeof(THandler);
        var existing = _handlerDescriptors.GetValueOrDefault(handlerType);

        if (existing is not null && configure is not null)
        {
            var inner = existing;
            _handlerDescriptors[handlerType] = d => { inner(d); configure(d); };
        }
        else if (configure is not null)
        {
            _handlerDescriptors[handlerType] = configure;
        }
        else
        {
            _handlerDescriptors.TryAdd(handlerType, static _ => { });
        }
    }

    /// <inheritdoc />
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AddHandlerConfiguration(MediatorHandlerConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var handlerType = configuration.HandlerType!;

        void ApplyConfig(MediatorHandlerDescriptor d)
        {
            var config = d.Extend().Configuration;
            config.MessageType = configuration.MessageType;
            config.ResponseType = configuration.ResponseType;
            config.Kind = configuration.Kind;
            config.Delegate = configuration.Delegate;
        }

        var existing = _handlerDescriptors.GetValueOrDefault(handlerType);

        if (existing is not null)
        {
            var inner = existing;
            _handlerDescriptors[handlerType] = d => { inner(d); ApplyConfig(d); };
        }
        else
        {
            _handlerDescriptors[handlerType] = ApplyConfig;
        }
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

        // Create the configuration context for descriptor construction.
        var configContext = new MediatorConfigurationContext(_options, internalProvider, features);

        // Compile pipelines using the internal provider for middleware factory context.
        var factoryCtx = new MediatorMiddlewareFactoryContext { Services = internalProvider, Features = features };

        var middlewareConfigs = _middlewares.Count > 0
            ? new IReadOnlyList<MediatorMiddlewareConfiguration>[] { _middlewares }
            : [];

        var modifiers = _pipelineModifiers.Count > 0
            ? new IReadOnlyList<Action<List<MediatorMiddlewareConfiguration>>>[] { _pipelineModifiers }
            : [];

        var pipelines = new Dictionary<Type, MediatorDelegate>();
        var notificationTerminals = new Dictionary<Type, List<MediatorDelegate>>();

        foreach (var (handlerType, configureDelegate) in _handlerDescriptors)
        {
            var descriptor = new MediatorHandlerDescriptor(configContext, handlerType);
            configureDelegate(descriptor);
            var config = descriptor.CreateConfiguration();

            var terminal = config.Delegate ?? BuildPipelineViaReflection(config);

            if (config.Kind == MediatorHandlerKind.Notification)
            {
                if (!notificationTerminals.TryGetValue(config.MessageType!, out var list))
                {
                    list = [];
                    notificationTerminals[config.MessageType!] = list;
                }

                list.Add(terminal);
            }
            else
            {
                factoryCtx.MessageType = config.MessageType!;
                factoryCtx.ResponseType = config.ResponseType;

                pipelines[config.MessageType!] =
                    MediatorMiddlewareCompiler.Compile(factoryCtx, terminal, middlewareConfigs, modifiers);
            }
        }

        // Compile notification pipelines - each handler terminal is independently
        // wrapped in middleware, producing a MediatorDelegate[] per notification type.
        var notificationPipelines = new Dictionary<Type, MediatorDelegate[]>(notificationTerminals.Count);

        foreach (var (notificationType, terminals) in notificationTerminals)
        {
            factoryCtx.MessageType = notificationType;
            factoryCtx.ResponseType = null;

            var compiled = new MediatorDelegate[terminals.Count];
            for (var i = 0; i < terminals.Count; i++)
            {
                compiled[i] = MediatorMiddlewareCompiler.Compile(
                    factoryCtx, terminals[i], middlewareConfigs, modifiers);
            }

            notificationPipelines[notificationType] = compiled;
        }

        var pools = applicationServices.GetRequiredService<IMediatorPools>();

        return new MediatorRuntime(
            pipelines.ToFrozenDictionary(),
            notificationPipelines.ToFrozenDictionary(),
            pools,
            features,
            _options.NotificationPublishMode);
    }

    private static MediatorDelegate BuildPipelineViaReflection(MediatorHandlerConfiguration config)
    {
        return config.Kind switch
        {
            MediatorHandlerKind.Command =>
                (MediatorDelegate)s_buildCommandPipeline
                    .MakeGenericMethod(config.HandlerType!, config.MessageType!)
                    .Invoke(null, null)!,

            MediatorHandlerKind.CommandResponse =>
                (MediatorDelegate)s_buildCommandResponsePipeline
                    .MakeGenericMethod(config.HandlerType!, config.MessageType!, config.ResponseType!)
                    .Invoke(null, null)!,

            MediatorHandlerKind.Query =>
                (MediatorDelegate)s_buildQueryPipeline
                    .MakeGenericMethod(config.HandlerType!, config.MessageType!, config.ResponseType!)
                    .Invoke(null, null)!,

            MediatorHandlerKind.Notification =>
                (MediatorDelegate)s_buildNotificationPipeline
                    .MakeGenericMethod(config.HandlerType!, config.MessageType!)
                    .Invoke(null, null)!,

            _ => throw new InvalidOperationException($"Unknown handler kind: {config.Kind}")
        };
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
