using System.Collections.Concurrent;
using System.IO.Hashing;
using System.Text;
using System.Threading.Channels;
using HotChocolate.Caching.Memory;
using HotChocolate.Execution;
using HotChocolate.Execution.Errors;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Features;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Utilities;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace HotChocolate.Fusion.Execution;

internal sealed class FusionRequestExecutorManager
    : IRequestExecutorProvider
    , IRequestExecutorEvents
    , IAsyncDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ConcurrentDictionary<string, RequestExecutorRegistration> _registry = [];
    private readonly IOptionsMonitor<FusionGatewaySetup> _optionsMonitor;
    private readonly IServiceProvider _applicationServices;
    private bool _disposed;

    public FusionRequestExecutorManager(
        IOptionsMonitor<FusionGatewaySetup> optionsMonitor,
        IServiceProvider applicationServices)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(applicationServices);

        _optionsMonitor = optionsMonitor;
        _applicationServices = applicationServices;
    }

    public ValueTask<IRequestExecutor> GetExecutorAsync(
        string? schemaName = null,
        CancellationToken cancellationToken = default)
    {
        schemaName ??= ISchemaDefinition.DefaultName;

        if (_registry.TryGetValue(schemaName, out var registration))
        {
            return new ValueTask<IRequestExecutor>(registration.Executor);
        }

        return GetOrCreateRequestExecutorAsync(schemaName, cancellationToken);
    }

    public IDisposable Subscribe(IObserver<RequestExecutorEvent> observer)
    {
        throw new NotImplementedException();
    }

    private async ValueTask<IRequestExecutor> GetOrCreateRequestExecutorAsync(
        string schemaName,
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_registry.TryGetValue(schemaName, out var registration))
            {
                return registration.Executor;
            }

            registration = await CreateInitialRegistrationAsync(schemaName, cancellationToken).ConfigureAwait(false);
            _registry.TryAdd(schemaName, registration);
            return registration.Executor;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async ValueTask<RequestExecutorRegistration> CreateInitialRegistrationAsync(
        string schemaName,
        CancellationToken cancellationToken)
    {
        var setup = _optionsMonitor.Get(schemaName);

        var (document, documentProvider) =
            await GetSchemaDocumentAsync(setup.DocumentProvider, cancellationToken).ConfigureAwait(false);

        return new RequestExecutorRegistration(
            this,
            documentProvider,
            CreateRequestExecutor(schemaName, document),
            document);
    }

    private FusionRequestExecutor CreateRequestExecutor(
        string schemaName,
        DocumentNode document)
    {
        var setup = _optionsMonitor.Get(schemaName);

        var requestOptions = CreateRequestOptions(setup);
        var parserOptions = CreateParserOptions(setup);
        var features = CreateSchemaFeatures(setup, requestOptions, parserOptions);
        var schemaServices = CreateSchemaServices(setup);

        var schema = CreateSchema(schemaName, document, schemaServices, features);
        var pipeline = CreatePipeline(setup, schema, schemaServices, requestOptions);

        var contextPool = schemaServices.GetRequiredService<ObjectPool<PooledRequestContext>>();
        var executor = new FusionRequestExecutor(schema, _applicationServices, pipeline, contextPool, 0);
        var requestExecutorAccessor = schemaServices.GetRequiredService<RequestExecutorAccessor>();
        requestExecutorAccessor.RequestExecutor = executor;

        return executor;
    }

    private async ValueTask<(DocumentNode, IFusionSchemaDocumentProvider)> GetSchemaDocumentAsync(
        Func<IServiceProvider, IFusionSchemaDocumentProvider>? documentProviderFactory,
        CancellationToken cancellationToken)
    {
        if (documentProviderFactory is null)
        {
            throw new InvalidOperationException("The schema document provider is not configured.");
        }

        var documentProvider = documentProviderFactory.Invoke(_applicationServices);
        var documentPromise = new TaskCompletionSource<DocumentNode>();
        using var subscription = documentProvider.Subscribe(documentPromise.SetResult);
        await using var cancellation = cancellationToken.Register(() => documentPromise.TrySetCanceled());
        return (await documentPromise.Task.ConfigureAwait(false), documentProvider);
    }

    private static FusionRequestOptions CreateRequestOptions(FusionGatewaySetup setup)
    {
        var options = new FusionRequestOptions();

        foreach (var configure in setup.RequestOptionsModifiers)
        {
            configure.Invoke(options);
        }

        if (options.OperationExecutionPlanCacheSize < 16)
        {
            options.OperationExecutionPlanCacheSize = 16;
        }

        if (options.OperationDocumentCacheSize < 16)
        {
            options.OperationDocumentCacheSize = 16;
        }

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

    private FeatureCollection CreateSchemaFeatures(
        FusionGatewaySetup setup,
        FusionRequestOptions requestOptions,
        ParserOptions parserOptions)
    {
        var features = new FeatureCollection();

        features.Set(requestOptions);
        features.Set(parserOptions);

        foreach (var configure in setup.SchemaFeaturesModifiers)
        {
            configure.Invoke(_applicationServices, features);
        }

        return features;
    }

    private ServiceProvider CreateSchemaServices(
        FusionGatewaySetup setup)
    {
        var schemaServices = new ServiceCollection();

        AddCoreServices(schemaServices);
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

    private void AddCoreServices(IServiceCollection services)
    {
        services.AddSingleton<IRootServiceProviderAccessor>(
            new RootServiceProviderAccessor(_applicationServices));

        services.AddSingleton(static _ => new RequestExecutorAccessor());
        services.AddSingleton(static sp => sp.GetRequiredService<RequestExecutorAccessor>().RequestExecutor);
        services.AddSingleton<IRequestExecutor>(sp => sp.GetRequiredService<FusionRequestExecutor>());
        services.AddSingleton(static sp => sp.GetRequiredService<ISchemaDefinition>().GetRequestOptions());
        services.AddSingleton<IErrorHandler>(static sp => new DefaultErrorHandler(sp.GetServices<IErrorFilter>()));

        services.AddSingleton(static _ => new SchemaDefinitionAccessor());
        services.AddSingleton(static sp => sp.GetRequiredService<SchemaDefinitionAccessor>().Schema);
        services.AddSingleton<ISchemaDefinition>(static sp => sp.GetRequiredService<FusionSchemaDefinition>());

        services.AddSingleton<ObjectPool<PooledRequestContext>>(
            static _ => new DefaultObjectPool<PooledRequestContext>(
                new RequestContextPooledObjectPolicy()));
    }

    private static void AddOperationPlanner(IServiceCollection services)
    {
        services.AddSingleton(
            static sp =>
            {
                var options = sp.GetRequiredService<ISchemaDefinition>().GetRequestOptions();
                return new Cache<OperationExecutionPlan>(
                    options.OperationExecutionPlanCacheSize,
                    options.OperationExecutionPlanCacheDiagnostics);
            });

        services.AddSingleton(static sp => new OperationPlanner(sp.GetRequiredService<FusionSchemaDefinition>()));
    }

    private static void AddParserServices(IServiceCollection services)
    {
        services.AddSingleton<IDocumentHashProvider>(static _ => new MD5DocumentHashProvider(HashFormat.Hex));
        services.AddSingleton(static sp => sp.GetRequiredService<ISchemaDefinition>().GetParserOptions());
        services.AddSingleton<IDocumentCache>(
            static sp =>
            {
                var options = sp.GetRequiredService<ISchemaDefinition>().GetRequestOptions();
                return new DefaultDocumentCache(options.OperationDocumentCacheSize);
            });
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
                    0 => new NoopFusionExecutionDiagnosticEvents(),
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
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var registration in _registry.Values)
        {
            await registration.DisposeAsync().ConfigureAwait(false);
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

    public sealed class RequestExecutorRegistration : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly CancellationToken _cancellationToken;
        private readonly Channel<DocumentNode> _channel = Channel.CreateBounded<DocumentNode>(
            new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });
        private readonly FusionRequestExecutorManager _manager;
        private readonly IDisposable _documentProviderSubscription;

        private ulong _hash;
        private bool _disposed;

        public RequestExecutorRegistration(
            FusionRequestExecutorManager manager,
            IFusionSchemaDocumentProvider documentProvider,
            FusionRequestExecutor executor,
            DocumentNode document)
        {
            _manager = manager;
            _cancellationToken = _cancellationTokenSource.Token;
            _hash = XxHash64.HashToUInt64(Encoding.UTF8.GetBytes(document.ToString()));
            _documentProviderSubscription = documentProvider.Subscribe(
                OnDocumentChanged,
                () => _channel.Writer.TryComplete());

            DocumentProvider = documentProvider;
            Executor = executor;

            WaitForUpdatesAsync().FireAndForget();
        }

        public IFusionSchemaDocumentProvider DocumentProvider { get; }

        public FusionRequestExecutor Executor { get; private set; }

        private async Task WaitForUpdatesAsync()
        {
            await foreach (var document in _channel.Reader.ReadAllAsync(_cancellationToken).ConfigureAwait(false))
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var hash = XxHash64.HashToUInt64(Encoding.UTF8.GetBytes(document.ToString()));
                if (hash == _hash)
                {
                    continue;
                }

                _hash = hash;
                Executor = _manager.CreateRequestExecutor(Executor.Schema.Name, document);
            }
        }

        private void OnDocumentChanged(DocumentNode document)
            => _channel.Writer.TryWrite(document);

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
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }
}
