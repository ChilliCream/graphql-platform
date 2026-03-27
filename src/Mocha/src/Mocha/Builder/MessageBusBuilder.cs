using System.Collections.Immutable;
using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Sagas;

namespace Mocha;

/// <summary>
/// Implements <see cref="IMessageBusBuilder"/> to construct a fully configured
/// <see cref="MessagingRuntime"/> from handlers, sagas, transports, and middleware registrations.
/// </summary>
public partial class MessageBusBuilder : IMessageBusBuilder
{
    private readonly MessagingOptions _messagingOptions = new();

    private readonly Dictionary<Type, Action<MessageTypeDescriptor>> _messageDescriptors = [];

    private readonly List<ConsumerRegistration> _consumerRegistrations = [];

    private readonly List<SagaRegistration> _sagaRegistrations = [];

    private readonly List<MessagingTransport> _transports = [];

    private readonly List<IConvention> _conventions = [MessageTypePostConfigureConvention.Instance];

    private readonly HostInfoDescriptor _hostInfoDescriptor = new();

    private readonly List<Action<IServiceProvider, IServiceCollection>> _configureServices = [];
    private readonly List<Action<IFeatureCollection>> _configureFeatures = [];

    /// <inheritdoc />
    public IMessageBusBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        _configureServices.Add((_, services) => configure(services));

        return this;
    }

    /// <inheritdoc />
    public IMessageBusBuilder ConfigureServices(Action<IServiceProvider, IServiceCollection> configure)
    {
        _configureServices.Add(configure);

        return this;
    }

    /// <inheritdoc />
    public IMessageBusBuilder ModifyOptions(Action<MessagingOptions> configure)
    {
        configure(_messagingOptions);

        return this;
    }

    /// <inheritdoc />
    public IMessageBusBuilder ConfigureFeature(Action<IFeatureCollection> configure)
    {
        _configureFeatures.Add(configure);

        return this;
    }

    /// <inheritdoc />
    public IMessageBusBuilder AddHandler<THandler>(Action<IConsumerDescriptor>? configure = null)
        where THandler : class, IHandler
    {
        var handlerType = typeof(THandler);
        var existing = _consumerRegistrations.Find(r => r.HandlerType == handlerType);

        if (existing is not null)
        {
            if (configure is not null)
            {
                var inner = existing.Configure;
                existing.Configure = inner is not null
                    ? d => { inner(d); configure(d); }
                    : configure;
            }

            return this;
        }

        // New registration - detect kind and create factory
        Func<Action<IConsumerDescriptor>?, Consumer> factory;

        if (typeof(IBatchEventHandler).IsAssignableFrom(typeof(THandler)) && THandler.EventType is not null)
        {
            var consumerType = typeof(BatchConsumer<,>).MakeGenericType(typeof(THandler), THandler.EventType);
            factory = c => (Consumer)Activator.CreateInstance(consumerType, c)!;
        }
        else if (typeof(IConsumer).IsAssignableFrom(typeof(THandler)) && THandler.EventType is not null)
        {
            var consumerType = typeof(ConsumerAdapter<,>).MakeGenericType(typeof(THandler), THandler.EventType);
            factory = c => (Consumer)Activator.CreateInstance(consumerType, c)!;
        }
        else if (THandler.RequestType is not null && THandler.ResponseType is not null)
        {
            var consumerType = typeof(RequestConsumer<,,>).MakeGenericType(
                typeof(THandler),
                THandler.RequestType,
                THandler.ResponseType);
            factory = c => (Consumer)Activator.CreateInstance(consumerType, c)!;
        }
        else if (THandler.RequestType is not null)
        {
            var consumerType = typeof(SendConsumer<,>).MakeGenericType(typeof(THandler), THandler.RequestType);
            factory = c => (Consumer)Activator.CreateInstance(consumerType, c)!;
        }
        else if (THandler.EventType is not null)
        {
            var consumerType = typeof(SubscribeConsumer<,>).MakeGenericType(typeof(THandler), THandler.EventType);
            factory = c => (Consumer)Activator.CreateInstance(consumerType, c)!;
        }
        else
        {
            throw ThrowHelper.InvalidHandlerType();
        }

        _consumerRegistrations.Add(new ConsumerRegistration
        {
            HandlerType = handlerType,
            Configure = configure,
            Factory = factory
        });

        return this;
    }

    /// <inheritdoc />
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IMessageBusBuilder AddHandlerConfiguration(MessagingHandlerConfiguration configuration)
    {
        var handlerType = configuration.HandlerType;
        var existing = _consumerRegistrations.Find(r => r.HandlerType == handlerType);

        if (existing is not null)
        {
            // Factory first-wins, nothing else to stack from SG path
            return this;
        }

        _consumerRegistrations.Add(new ConsumerRegistration
        {
            HandlerType = handlerType,
            Configure = null,
            Factory = configuration.Factory
        });

        return this;
    }

    /// <summary>
    /// Registers a batch event handler with the message bus.
    /// </summary>
    /// <typeparam name="THandler">The batch handler type.</typeparam>
    /// <param name="configure">Optional action to configure batch options.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public IMessageBusBuilder AddBatchHandler<THandler>(Action<BatchOptions>? configure = null)
        where THandler : class, IBatchEventHandler
    {
        var options = new BatchOptions();
        configure?.Invoke(options);

        AddHandler<THandler>(d => d.Extend().Configuration.Features.Set(options));

        return this;
    }

    /// <inheritdoc />
    public IMessageBusBuilder AddSaga<TSaga>() where TSaga : Saga, new()
    {
        var sagaType = typeof(TSaga);

        if (_sagaRegistrations.Exists(r => r.SagaType == sagaType))
        {
            return this;
        }

        _sagaRegistrations.Add(new SagaRegistration
        {
            SagaType = sagaType,
            Factory = static () => new TSaga()
        });

        return this;
    }

    /// <inheritdoc />
    public IMessageBusBuilder AddMessage<TMessage>(Action<IMessageTypeDescriptor> configure) where TMessage : class
    {
        var configureDelegate = _messageDescriptors.GetValueOrDefault(typeof(TMessage));

        if (configureDelegate is not null)
        {
            var innerDelegate = configureDelegate;
            configureDelegate = descriptor =>
            {
                innerDelegate(descriptor);
                configure(descriptor);
            };
        }

        _messageDescriptors[typeof(TMessage)] = configureDelegate ?? configure;

        return this;
    }

    /// <inheritdoc />
    public IMessageBusBuilder AddTransport<TTransport>(TTransport transport) where TTransport : MessagingTransport
    {
        _transports.Add(transport);

        return this;
    }

    /// <inheritdoc />
    public IMessageBusBuilder Host(Action<IHostInfoDescriptor> configure)
    {
        configure(_hostInfoDescriptor);

        return this;
    }

    private static void AddCoreServices(IServiceCollection services, IServiceProvider applicationServices)
    {
        services.AddSingleton<IRootServiceProviderAccessor>(new RootServiceProviderAccessor(applicationServices));

        var router = new MessageRouter();
        services.AddSingleton<IMessageRouter>(router);

        var endpointRouter = new EndpointRouter();
        services.AddSingleton<IEndpointRouter>(endpointRouter);

        foreach (var typeInfoResolver in applicationServices.GetServices<IJsonTypeInfoResolver>())
        {
            services.AddSingleton(typeInfoResolver);
        }

        var factories = applicationServices.GetServices<IMessageSerializerFactory>().ToList();

        foreach (var factory in factories)
        {
            services.AddSingleton(factory);
        }

        if (factories.All(f => f.ContentType != MessageContentType.Json))
        {
            services.AddSingleton<IMessageSerializerFactory, JsonMessageSerializerFactory>();
        }

        services.AddSingleton<IMessageSerializerRegistry, MessageSerializerRegistry>();
        services.AddSingleton<IMessageTypeRegistry, MessageTypeRegistry>();

        var sagaStateSerializerFactory = applicationServices.GetService<ISagaStateSerializerFactory>();
        if (sagaStateSerializerFactory is { })
        {
            services.AddSingleton(sagaStateSerializerFactory);
        }
        else
        {
            services.AddSingleton<ISagaStateSerializerFactory, JsonSagaStateSerializerFactory>();
        }

        var loggerFactory = applicationServices.GetRequiredService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
        services.AddSingleton(loggerFactory);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

        var diagnosticObserver =
            applicationServices.GetService<IBusDiagnosticObserver>() ?? NoOpBusDiagnosticObserver.Instance;

        services.AddSingleton(diagnosticObserver);

        var naming = applicationServices.GetService<IBusNamingConventions>();

        if (naming is not null)
        {
            services.AddSingleton(naming);
        }
        else
        {
            services.AddSingleton<IBusNamingConventions, DefaultNamingConventions>();
        }

        var responseManager = applicationServices.GetRequiredService<DeferredResponseManager>();
        services.AddSingleton(responseManager);

        var pooling = applicationServices.GetRequiredService<IMessagingPools>();
        services.AddSingleton(pooling);

        var timeProvider = applicationServices.GetService<TimeProvider>() ?? TimeProvider.System;
        services.AddSingleton(timeProvider);
    }

    /// <summary>
    /// Builds and returns a fully configured <see cref="MessagingRuntime"/> from all registered
    /// handlers, sagas, transports, message types, and middleware.
    /// </summary>
    /// <remarks>
    /// The build proceeds through the following ordered phases:
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///       <strong>Init</strong> - Registers core services; initializes message types from
    ///       descriptors; initializes sagas (before consumers because saga consumers depend on saga
    ///       state); initializes consumers; initializes transports.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <strong>Discover topology</strong> - Connects outbound routes without endpoints and
    ///       lets transports discover endpoints.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <strong>Complete</strong> - Completes message types; compiles consumer middleware
    ///       pipelines; completes outbound and inbound routes.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <strong>Finalize</strong> - Completes and finalizes transports; creates and returns
    ///       the <see cref="MessagingRuntime"/>.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <param name="applicationServices">
    /// The application-level service provider used to resolve shared services (for example,
    /// logging, serializer factories, and the deferred response manager).
    /// </param>
    /// <returns>
    /// A fully initialized <see cref="MessagingRuntime"/> ready to dispatch and receive messages.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a handler type is not a valid event, request, or send handler, or when consumer
    /// creation fails during the build.
    /// </exception>
    public MessagingRuntime Build(IServiceProvider applicationServices)
    {
        var servicesCollection = new ServiceCollection();
        AddCoreServices(servicesCollection, applicationServices);

        var responseManager = applicationServices.GetRequiredService<DeferredResponseManager>();

        // Materialize consumers from registrations
        var consumerList = new List<Consumer>();

        // Infrastructure consumer
        consumerList.Add(new ReplyConsumer(responseManager));

        // Handler consumers from registrations
        foreach (var reg in _consumerRegistrations)
        {
            consumerList.Add(reg.Factory(reg.Configure));
        }

        // Saga consumers
        var sagas = new List<Saga>();
        foreach (var reg in _sagaRegistrations)
        {
            var saga = reg.Factory();
            sagas.Add(saga);
            consumerList.Add(saga.Consumer);
        }

        var consumers = consumerList.ToImmutableArray();
        var transports = _transports.ToImmutableArray();

        servicesCollection.AddSingleton(new RegisteredConsumers(consumers));

        var hostConfiguration = _hostInfoDescriptor.CreateConfiguration();
        var host = HostInfoFactory.From(hostConfiguration);

        servicesCollection.AddSingleton<IHostInfo>(host);
        servicesCollection.AddSingleton<IReadOnlyMessagingOptions>(_messagingOptions);
        var lazyRuntime = new LazyMessagingRuntime();
        servicesCollection.AddSingleton<ILazyMessagingRuntime>(lazyRuntime);

        var features = new FeatureCollection();
        servicesCollection.AddSingleton<IFeatureCollection>(features);

        var services = servicesCollection.BuildServiceProvider();

        var router = services.GetRequiredService<IMessageRouter>();
        var endpointRouter = services.GetRequiredService<IEndpointRouter>();
        var messageRegistry = services.GetRequiredService<IMessageTypeRegistry>();
        var naming = services.GetRequiredService<IBusNamingConventions>();

        foreach (var configure in _configureFeatures)
        {
            configure(features);
        }

        var middlewareFeature = new MiddlewareFeature(
            [.. _dispatchMiddlewares],
            [.. _dispatchModifiers],
            [.. _receiveMiddlewares],
            [.. _receiveModifiers],
            [.. _handlerMiddlewares],
            [.. _handlerModifiers]);
        features.Set(middlewareFeature);

        var conventions = new ConventionRegistry([.. _conventions]);

        var setupContext = new MessagingSetupContext
        {
            Services = services,
            Naming = naming,
            Consumers = consumers.ToImmutableHashSet(),
            Transports = transports,
            Host = host,
            Features = features,
            Router = router,
            Endpoints = endpointRouter,
            Messages = messageRegistry,
            Conventions = conventions
        };

        foreach (var (type, configureDelegate) in _messageDescriptors)
        {
            var descriptor = new MessageTypeDescriptor(setupContext, type);

            configureDelegate(descriptor);

            var configuration = descriptor.CreateConfiguration();
            var messageType = new MessageType();
            messageType.Initialize(setupContext, configuration);
            messageRegistry.AddMessageType(messageType);
        }

        // sagas have to be initialized before consumers, because of the saga consumer
        foreach (var saga in sagas)
        {
            saga.Initialize(setupContext);
        }

        foreach (var consumer in consumers)
        {
            consumer.Initialize(setupContext);
        }

        foreach (var transport in _transports)
        {
            setupContext.Transport = transport;
            transport.Initialize(setupContext);
        }

        setupContext.Transport = null;

        // after we initialized the transport, we connect all outbound routes that have an URI
        // but no endpoint.
        foreach (var route in router.OutboundRoutes)
        {
            if (route.Endpoint is null && route.Destination is not null)
            {
                var endpoint = setupContext.Endpoints.GetOrCreate(setupContext, route.Destination);
                route.ConnectEndpoint(setupContext, endpoint);
            }
        }

        foreach (var transport in _transports)
        {
            setupContext.Transport = transport;
            transport.DiscoverEndpoints(setupContext);
        }

        setupContext.Transport = null;

        // message types can be discovered during completion - hence the copy
        foreach (var messageType in messageRegistry.MessageTypes.ToList())
        {
            if (!messageType.IsCompleted)
            {
                messageType.Complete(setupContext);
            }
        }

        foreach (var handler in consumers)
        {
            handler.Complete(setupContext);
        }

        foreach (var route in router.OutboundRoutes)
        {
            if (!route.IsCompleted)
            {
                route.Complete(setupContext);
            }
        }

        foreach (var route in router.InboundRoutes)
        {
            if (!route.IsCompleted)
            {
                route.Complete(setupContext);
            }
        }

        foreach (var transport in _transports)
        {
            setupContext.Transport = transport;

            transport.Complete(setupContext);
            transport.Finalize(setupContext);
        }

        setupContext.Transport = null;

        var runtime = new MessagingRuntime(
            services,
            _messagingOptions,
            naming,
            conventions,
            consumers.ToImmutableHashSet(),
            transports,
            messageRegistry,
            host,
            router,
            endpointRouter,
            features.ToReadOnly());

        lazyRuntime.Runtime = runtime;

        return runtime;
    }

    private void PrepareHandlers()
    {
        foreach (var modifier in _handlerModifiers)
        {
            modifier(_handlerMiddlewares);
        }

        foreach (var modifier in _receiveModifiers)
        {
            modifier(_receiveMiddlewares);
        }

        foreach (var modifier in _dispatchModifiers)
        {
            modifier(_dispatchMiddlewares);
        }
    }
}
