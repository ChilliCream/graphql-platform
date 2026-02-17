using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO.Hashing;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using HotChocolate.Caching.Memory;
using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Execution.Errors;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Features;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Introspection;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Utilities;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using static System.Runtime.InteropServices.JsonMarshal;

namespace HotChocolate.Fusion.Execution;

internal sealed class FusionRequestExecutorManager
    : IRequestExecutorProvider
    , IRequestExecutorEvents
    , IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphoreBySchema = new();
    private readonly ConcurrentDictionary<string, RequestExecutorRegistration> _registry = [];
    private readonly IOptionsMonitor<FusionGatewaySetup> _optionsMonitor;
    private readonly EventObservable _events = new();
    private readonly IServiceProvider _applicationServices;

    private bool _disposed;
    private ulong _version;

    public FusionRequestExecutorManager(
        IOptionsMonitor<FusionGatewaySetup> optionsMonitor,
        IServiceProvider applicationServices)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(applicationServices);

        _optionsMonitor = optionsMonitor;
        _applicationServices = applicationServices;

        var schemaNames = _applicationServices.GetService<IEnumerable<SchemaName>>()?
            .Select(x => x.Value).Distinct().Order().ToImmutableArray();
        SchemaNames = schemaNames ?? [];
    }

    public ImmutableArray<string> SchemaNames { get; }

    public ValueTask<IRequestExecutor> GetExecutorAsync(
        string? schemaName = null,
        CancellationToken cancellationToken = default)
    {
        schemaName ??= ISchemaDefinition.DefaultName;
        return _registry.TryGetValue(schemaName, out var registration)
            ? new ValueTask<IRequestExecutor>(registration.Executor)
            : GetOrCreateRequestExecutorAsync(schemaName, cancellationToken);
    }

    public IDisposable Subscribe(IObserver<RequestExecutorEvent> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _events.Subscribe(observer);
    }

    private async ValueTask<IRequestExecutor> GetOrCreateRequestExecutorAsync(
        string schemaName,
        CancellationToken cancellationToken)
    {
        var semaphore = GetSemaphoreForSchema(schemaName);
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_registry.TryGetValue(schemaName, out var registration))
            {
                return registration.Executor;
            }

            if (!SchemaNames.Contains(schemaName))
            {
                throw new InvalidOperationException($"The requested schema '{schemaName}' does not exist.");
            }

            registration = await CreateInitialRegistrationAsync(schemaName, cancellationToken).ConfigureAwait(false);
            _registry.TryAdd(schemaName, registration);

            var nextExecutor = registration.Executor;

            registration.DiagnosticEvents.ExecutorCreated(nextExecutor.Schema.Name, nextExecutor);

            _events.RaiseEvent(RequestExecutorEvent.Created(nextExecutor));

            return nextExecutor;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private SemaphoreSlim GetSemaphoreForSchema(string schemaName)
        => _semaphoreBySchema.GetOrAdd(schemaName, _ => new SemaphoreSlim(1, 1));

    private void EvictExecutor(FusionRequestExecutor executor, IFusionExecutionDiagnosticEvents diagnosticEvents)
    {
        try
        {
            diagnosticEvents.ExecutorEvicted(executor.Schema.Name, executor);

            _events.RaiseEvent(RequestExecutorEvent.Evicted(executor));
        }
        finally
        {
            EvictRequestExecutorAsync(executor).FireAndForget();
        }
    }

    private static async Task EvictRequestExecutorAsync(FusionRequestExecutor previousExecutor)
    {
        var evictionTimeout = previousExecutor.Schema.GetOptions().EvictionTimeout;

        // we will give the request executor some grace period to finish all requests
        // in the pipeline.
        await Task.Delay(evictionTimeout).ConfigureAwait(false);

        await previousExecutor.DisposeAsync().ConfigureAwait(false);
    }

    private async ValueTask<RequestExecutorRegistration> CreateInitialRegistrationAsync(
        string schemaName,
        CancellationToken cancellationToken)
    {
        var setup = _optionsMonitor.Get(schemaName);

        var (configuration, documentProvider) =
            await GetSchemaDocumentAsync(setup.DocumentProvider, cancellationToken).ConfigureAwait(false);

        var executor = CreateRequestExecutor(schemaName, configuration);

        await WarmupExecutorAsync(executor, true, cancellationToken).ConfigureAwait(false);

        return new RequestExecutorRegistration(
            this,
            documentProvider,
            executor,
            executor.Schema.Services.GetRequiredService<IFusionExecutionDiagnosticEvents>(),
            configuration);
    }

    private FusionRequestExecutor CreateRequestExecutor(
        string schemaName,
        FusionConfiguration configuration)
    {
        ulong version;

        unchecked
        {
            version = ++_version;
        }

        var setup = _optionsMonitor.Get(schemaName);

        var options = CreateOptions(setup);
        var requestOptions = CreateRequestOptions(setup);
        var parserOptions = CreateParserOptions(setup);
        var clientConfigurations = CreateClientConfigurations(setup, configuration.Settings.Document);
        var features = CreateSchemaFeatures(
            setup,
            options,
            requestOptions,
            parserOptions,
            clientConfigurations);
        var schemaServices = CreateSchemaServices(setup, options, requestOptions);

        var schema = CreateSchema(schemaName, configuration.Schema, schemaServices, features);
        var pipeline = CreatePipeline(setup, schema, schemaServices, requestOptions);

        var contextPool = schemaServices.GetRequiredService<ObjectPool<PooledRequestContext>>();
        var executor = new FusionRequestExecutor(schema, _applicationServices, pipeline, contextPool, version);
        var requestExecutorAccessor = schemaServices.GetRequiredService<RequestExecutorAccessor>();
        requestExecutorAccessor.RequestExecutor = executor;

        return executor;
    }

    private async Task WarmupExecutorAsync(
        IRequestExecutor executor,
        bool isInitialCreation,
        CancellationToken cancellationToken)
    {
        var warmupTasks = executor.Schema.Services.GetServices<IRequestExecutorWarmupTask>();

        if (!isInitialCreation)
        {
            warmupTasks = warmupTasks.Where(t => !t.ApplyOnlyOnStartup);
        }

        foreach (var warmupTask in warmupTasks)
        {
            await warmupTask.WarmupAsync(executor, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask<(FusionConfiguration, IFusionConfigurationProvider)> GetSchemaDocumentAsync(
        Func<IServiceProvider, IFusionConfigurationProvider>? documentProviderFactory,
        CancellationToken cancellationToken)
    {
        if (documentProviderFactory is null)
        {
            throw new InvalidOperationException("The schema document provider is not configured.");
        }

        var documentProvider = documentProviderFactory.Invoke(_applicationServices);
        var documentPromise = new TaskCompletionSource<FusionConfiguration>();
        using var subscription = documentProvider.Subscribe(s => documentPromise.TrySetResult(s));
        await using var cancellation = cancellationToken.Register(() => documentPromise.TrySetCanceled());
        return (await documentPromise.Task.ConfigureAwait(false), documentProvider);
    }

    public static FusionOptions CreateOptions(FusionGatewaySetup setup)
    {
        var options = new FusionOptions();

        foreach (var configure in setup.OptionsModifiers)
        {
            configure.Invoke(options);
        }

        options.MakeReadOnly();

        return options;
    }

    private static FusionRequestOptions CreateRequestOptions(FusionGatewaySetup setup)
    {
        var options = new FusionRequestOptions();

        foreach (var configure in setup.RequestOptionsModifiers)
        {
            configure.Invoke(options);
        }

        options.MakeReadOnly();

        return options;
    }

    private static ParserOptions CreateParserOptions(FusionGatewaySetup setup)
    {
        var options = new FusionParserOptions();

        foreach (var configure in setup.ParserOptionsModifiers)
        {
            configure.Invoke(options);
        }

        return new ParserOptions(
            noLocations: options.NoLocations,
            allowFragmentVariables: false,
            maxAllowedNodes: options.MaxAllowedNodes,
            maxAllowedTokens: options.MaxAllowedTokens,
            maxAllowedFields: options.MaxAllowedFields);
    }

    private SourceSchemaClientConfigurations CreateClientConfigurations(
        FusionGatewaySetup setup,
        JsonDocument settings)
    {
        var configurations = new List<ISourceSchemaClientConfiguration>();

        if (settings.RootElement.TryGetProperty("sourceSchemas", out var sourceSchemas))
        {
            foreach (var sourceSchema in sourceSchemas.EnumerateObject())
            {
                if (sourceSchema.Value.TryGetProperty("transports", out var transports))
                {
                    if (transports.TryGetProperty("http", out var http))
                    {
                        var clientName = SourceSchemaHttpClientConfiguration.DefaultClientName;

                        if (http.TryGetProperty("clientName", out var clientNameProperty)
                            && clientNameProperty.ValueKind is JsonValueKind.String
                            && clientNameProperty.GetString() is { } customClientName
                            && !string.IsNullOrEmpty(customClientName))
                        {
                            clientName = customClientName;
                        }

                        var httpClient = new SourceSchemaHttpClientConfiguration(
                            name: sourceSchema.Name,
                            httpClientName: clientName,
                            baseAddress: new Uri(http.GetProperty("url").GetString()!),
                            batchingMode: GetBatchingMode(http));

                        configurations.Add(httpClient);
                    }
                }
            }
        }

        foreach (var configure in setup.ClientConfigurationModifiers)
        {
            configurations.Add(configure.Invoke(_applicationServices));
        }

        return new SourceSchemaClientConfigurations(configurations);
    }

    private static SourceSchemaHttpClientBatchingMode GetBatchingMode(JsonElement httpSettings)
    {
        if (httpSettings.TryGetProperty("batchingMode", out var batchingMode)
            && batchingMode.ValueKind == JsonValueKind.String
            && batchingMode.GetString() == "REQUEST_BATCHING")
        {
            return SourceSchemaHttpClientBatchingMode.ApolloRequestBatching;
        }

        return SourceSchemaHttpClientBatchingMode.VariableBatching;
    }

    private FeatureCollection CreateSchemaFeatures(
        FusionGatewaySetup setup,
        FusionOptions options,
        FusionRequestOptions requestOptions,
        ParserOptions parserOptions,
        SourceSchemaClientConfigurations clientConfigurations)
    {
        var features = new FeatureCollection();

        features.Set(options);
        features.Set<IFusionSchemaOptions>(options);
        features.Set(requestOptions);
        features.Set(requestOptions.PersistedOperations);
        features.Set(parserOptions);
        features.Set(clientConfigurations);
        features.Set(CreateTypeResolverInterceptors());
        features.Set(new SchemaCancellationFeature());

        foreach (var configure in setup.SchemaFeaturesModifiers)
        {
            configure.Invoke(_applicationServices, features);
        }

        return features;
    }

    private static Dictionary<string, ITypeResolverInterceptor> CreateTypeResolverInterceptors()
        => new()
        {
            { nameof(Query), new Query() },
            { nameof(__Directive), new __Directive() },
            { nameof(__EnumValue), new __EnumValue() },
            { nameof(__Field), new __Field() },
            { nameof(__InputValue), new __InputValue() },
            { nameof(__Schema), new __Schema() },
            { nameof(__Type), new __Type() }
        };

    private ServiceProvider CreateSchemaServices(
        FusionGatewaySetup setup,
        FusionOptions options,
        FusionRequestOptions requestOptions)
    {
        var schemaServices = new ServiceCollection();

        AddCoreServices(schemaServices, options, requestOptions);
        AddOperationPlanner(schemaServices);
        AddParserServices(schemaServices);
        AddDocumentValidator(setup, schemaServices);
        AddDiagnosticEvents(schemaServices);

        foreach (var configure in setup.SchemaServiceModifiers)
        {
            configure.Invoke(_applicationServices, schemaServices);
        }

        return schemaServices.BuildServiceProvider();
    }

    private void AddCoreServices(
        IServiceCollection services,
        FusionOptions options,
        FusionRequestOptions requestOptions)
    {
        services.AddSingleton<IRootServiceProviderAccessor>(
            new RootServiceProviderAccessor(_applicationServices));

        services.AddSingleton(static _ => new RequestExecutorAccessor());
        services.AddSingleton(static sp => sp.GetRequiredService<RequestExecutorAccessor>().RequestExecutor);
        services.AddSingleton<IRequestExecutor>(sp => sp.GetRequiredService<FusionRequestExecutor>());
        services.AddSingleton(static sp => sp.GetRequiredService<ISchemaDefinition>().GetRequestOptions());
        services.TryAddSingleton<INodeIdParser>(
            static sp => new DefaultNodeIdParser(
                sp.GetRequiredService<FusionOptions>().NodeIdSerializerFormat));
        services.AddSingleton<IErrorHandler>(static sp => new DefaultErrorHandler(sp.GetServices<IErrorFilter>()));

        if (requestOptions.IncludeExceptionDetails)
        {
            services.AddSingleton<IErrorFilter>(static _ => new AddDebugInformationErrorFilter());
        }

        services.AddSingleton(static _ => new SchemaDefinitionAccessor());
        services.AddSingleton(static sp => sp.GetRequiredService<SchemaDefinitionAccessor>().Schema);
        services.AddSingleton<ISchemaDefinition>(static sp => sp.GetRequiredService<FusionSchemaDefinition>());

        services.AddSingleton(options);
        services.AddSingleton(requestOptions);
        services.AddSingleton(requestOptions.PersistedOperations);

        services.AddSingleton<ObjectPool<PooledRequestContext>>(
            static _ => new DefaultObjectPool<PooledRequestContext>(
                new RequestContextPooledObjectPolicy()));

        services.AddTransient<CompositeTypeInterceptor>(static _ => new IntrospectionFieldInterceptor());
    }

    private static void AddOperationPlanner(IServiceCollection services)
    {
        services.TryAddSingleton<ObjectPoolProvider>(
            static _ => new DefaultObjectPoolProvider());

        services.AddSingleton(
            static sp => sp.GetRequiredService<ObjectPoolProvider>().CreateFieldMapPool());

        services.AddSingleton(
            static sp =>
            {
                var options = sp.GetRequiredService<ISchemaDefinition>().GetOptions();
                return new Cache<OperationPlan>(
                    options.OperationExecutionPlanCacheSize,
                    options.OperationExecutionPlanCacheDiagnostics);
            });

        services.AddSingleton(
            static sp => new OperationCompiler(
                sp.GetRequiredService<FusionSchemaDefinition>(),
                sp.GetRequiredService<ObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>>()));

        services.AddSingleton(
            static sp => new OperationPlanner(
                sp.GetRequiredService<FusionSchemaDefinition>(),
                sp.GetRequiredService<OperationCompiler>()));
    }

    private static void AddParserServices(IServiceCollection services)
    {
        services.AddSingleton<IDocumentHashProvider>(static _ => new MD5DocumentHashProvider(HashFormat.Hex));
        services.AddSingleton(static sp => sp.GetRequiredService<ISchemaDefinition>().GetParserOptions());
    }

    private void AddDocumentValidator(
        FusionGatewaySetup setup,
        IServiceCollection services)
    {
        var builder =
            DocumentValidatorBuilder.New()
                .SetServices(_applicationServices)
                .AddDefaultRules();

        foreach (var modifier in setup.DocumentValidatorBuilderModifiers)
        {
            modifier.Invoke(_applicationServices, builder);
        }

        services.AddSingleton(builder.Build());
    }

    private static void AddDiagnosticEvents(
        IServiceCollection services)
    {
        services.AddSingleton<IFusionExecutionDiagnosticEvents>(
            static sp =>
            {
                var listeners = sp.GetServices<IFusionExecutionDiagnosticEventListener>().ToArray();

                return listeners.Length switch
                {
                    0 => NoopFusionExecutionDiagnosticEvents.Instance,
                    1 => listeners[0],
                    _ => new AggregateFusionExecutionDiagnosticEvents(listeners)
                };
            });

        services.AddSingleton<ICoreExecutionDiagnosticEvents>(
            static sp => sp.GetRequiredService<IFusionExecutionDiagnosticEvents>());
    }

    private static FusionSchemaDefinition CreateSchema(
        string schemaName,
        DocumentNode schemaDocument,
        IServiceProvider schemaServices,
        IFeatureCollection features)
    {
        var schema = FusionSchemaDefinition.Create(schemaName, schemaDocument, schemaServices, features);
        var schemaDefinitionAccessor = schemaServices.GetRequiredService<SchemaDefinitionAccessor>();
        schemaDefinitionAccessor.Schema = schema;
        return schema;
    }

    private RequestDelegate CreatePipeline(
        FusionGatewaySetup setup,
        ISchemaDefinition schema,
        IServiceProvider schemaServices,
        FusionRequestOptions requestOptions)
    {
        var pipeline = new List<RequestMiddlewareConfiguration>();

        foreach (var configure in setup.PipelineModifiers)
        {
            configure.Invoke(pipeline);
        }

        var created = new HashSet<string>();

        var context = new RequestMiddlewareFactoryContext
        {
            Schema = schema,
            Services = _applicationServices
        };

        context.Features.Set(new SchemaServicesProviderAccessor(schemaServices));
        context.Features.Set(requestOptions);

        var next = new RequestDelegate(_ => default);

        for (var i = pipeline.Count - 1; i >= 0; i--)
        {
            var configuration = pipeline[i];
            if (configuration.Key is null || created.Add(configuration.Key))
            {
                next = configuration.Middleware(context, next);
            }
        }

        return next;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;

            foreach (var registration in _registry.Values)
            {
                await registration.DisposeAsync().ConfigureAwait(false);
            }

            foreach (var semaphore in _semaphoreBySchema.Values)
            {
                semaphore.Dispose();
            }

            _events.Dispose();
            _registry.Clear();
            _semaphoreBySchema.Clear();
        }
    }

    private sealed class RequestExecutorAccessor
    {
        public FusionRequestExecutor RequestExecutor { get; set; } = null!;
    }

    private sealed class SchemaDefinitionAccessor
    {
        public FusionSchemaDefinition Schema { get; set; } = null!;
    }

    private sealed class RequestExecutorRegistration : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly CancellationToken _cancellationToken;
        private readonly Channel<FusionConfiguration> _channel = Channel.CreateBounded<FusionConfiguration>(
            new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
        private readonly FusionRequestExecutorManager _manager;
        private readonly IDisposable _documentProviderSubscription;

        private ulong _documentHash;
        private ulong _settingsHash;
        private bool _disposed;

        public RequestExecutorRegistration(
            FusionRequestExecutorManager manager,
            IFusionConfigurationProvider documentProvider,
            FusionRequestExecutor executor,
            IFusionExecutionDiagnosticEvents diagnosticEvents,
            FusionConfiguration configuration)
        {
            _manager = manager;
            _cancellationToken = _cancellationTokenSource.Token;
            _documentHash = XxHash64.HashToUInt64(Encoding.UTF8.GetBytes(configuration.Schema.ToString()));
            _settingsHash = XxHash64.HashToUInt64(GetRawUtf8Value(configuration.Settings.Document.RootElement));

            _documentProviderSubscription = documentProvider.Subscribe(
                OnDocumentChanged,
                () => _channel.Writer.TryComplete());

            DocumentProvider = documentProvider;
            Executor = executor;
            DiagnosticEvents = diagnosticEvents;

            WaitForUpdatesAsync().FireAndForget();
        }

        public IFusionConfigurationProvider DocumentProvider { get; }

        public FusionRequestExecutor Executor { get; private set; }

        public IFusionExecutionDiagnosticEvents DiagnosticEvents { get; }

        private async Task WaitForUpdatesAsync()
        {
            await foreach (var configuration in _channel.Reader.ReadAllAsync(_cancellationToken).ConfigureAwait(false))
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var documentHash = XxHash64.HashToUInt64(Encoding.UTF8.GetBytes(configuration.Schema.ToString()));
                var settingsHash = XxHash64.HashToUInt64(GetRawUtf8Value(configuration.Settings.Document.RootElement));

                if (documentHash == _documentHash && settingsHash == _settingsHash)
                {
                    continue;
                }

                _documentHash = documentHash;
                _settingsHash = settingsHash;

                var previousExecutor = Executor;
                var nextExecutor = _manager.CreateRequestExecutor(Executor.Schema.Name, configuration);

                await _manager.WarmupExecutorAsync(nextExecutor, false, _cancellationToken).ConfigureAwait(false);

                Executor = nextExecutor;

                DiagnosticEvents.ExecutorCreated(nextExecutor.Schema.Name, nextExecutor);

                _manager._events.RaiseEvent(RequestExecutorEvent.Created(nextExecutor));

                _manager.EvictExecutor(previousExecutor, DiagnosticEvents);

                configuration.Dispose();
            }
        }

        private void OnDocumentChanged(FusionConfiguration configuration)
            => _channel.Writer.TryWrite(configuration);

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _documentProviderSubscription.Dispose();
            await DocumentProvider.DisposeAsync().ConfigureAwait(false);

            _channel.Writer.TryComplete();
            await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
            _cancellationTokenSource.Dispose();

            while (_channel.Reader.TryRead(out var configuration))
            {
                configuration.Dispose();
            }

            await Executor.DisposeAsync();
        }
    }

    private sealed class EventObservable : IObservable<RequestExecutorEvent>, IDisposable
    {
#if NET9_0_OR_GREATER
        private readonly Lock _sync = new();
#else
        private readonly object _sync = new();
#endif
        private readonly List<Subscription> _subscriptions = [];
        private bool _disposed;

        public IDisposable Subscribe(IObserver<RequestExecutorEvent> observer)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(observer);

            var subscription = new Subscription(this, observer);

            lock (_sync)
            {
                _subscriptions.Add(subscription);
            }

            return subscription;
        }

        public void RaiseEvent(RequestExecutorEvent eventMessage)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            lock (_sync)
            {
                foreach (var subscription in _subscriptions)
                {
                    subscription.Observer.OnNext(eventMessage);
                }
            }
        }

        private void Unsubscribe(Subscription subscription)
        {
            lock (_sync)
            {
                _subscriptions.Remove(subscription);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_sync)
                {
                    foreach (var subscription in _subscriptions)
                    {
                        subscription.Observer.OnCompleted();
                    }

                    _subscriptions.Clear();
                }

                _disposed = true;
            }
        }

        private sealed class Subscription(
            EventObservable parent,
            IObserver<RequestExecutorEvent> observer)
            : IDisposable
        {
            private bool _disposed;

            public IObserver<RequestExecutorEvent> Observer { get; } = observer;

            public void Dispose()
            {
                if (!_disposed)
                {
                    parent.Unsubscribe(this);
                    _disposed = true;
                }
            }
        }
    }

    private sealed class AddDebugInformationErrorFilter : IErrorFilter
    {
        private const string ExceptionProperty = "exception";
        private const string MessageProperty = "message";
        private const string StackTraceProperty = "stackTrace";

        public IError OnError(IError error)
        {
            if (error.Exception is not null)
            {
                switch (error.Extensions)
                {
                    case ImmutableOrderedDictionary<string, object?> d when !d.ContainsKey(ExceptionProperty):
                    {
                        var extensions = d.Add(ExceptionProperty, CreateExceptionInfo(error.Exception));
                        return error.WithExtensions(extensions);
                    }

                    case { } d when !d.ContainsKey("exception"):
                        var builder = ImmutableOrderedDictionary.CreateBuilder<string, object?>();
                        builder.AddRange(d);
                        builder.Add(ExceptionProperty, CreateExceptionInfo(error.Exception));
                        return error.WithExtensions(builder.ToImmutable());

                    default:
                    {
                        var extensions =
                            ImmutableOrderedDictionary<string, object?>.Empty
                                .Add(ExceptionProperty, CreateExceptionInfo(error.Exception));
                        return error.WithExtensions(extensions);
                    }
                }
            }

            return error;

            static ImmutableOrderedDictionary<string, object?> CreateExceptionInfo(Exception exception)
            {
                return ImmutableOrderedDictionary<string, object?>.Empty
                    .Add(MessageProperty, exception.Message)
                    .Add(StackTraceProperty, exception.StackTrace);
            }
        }
    }
}
