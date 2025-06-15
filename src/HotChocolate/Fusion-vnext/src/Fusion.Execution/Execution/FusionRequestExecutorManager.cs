using System.Collections.Concurrent;
using System.Reflection.Metadata;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace HotChocolate.Fusion.Execution;

internal sealed class FusionRequestExecutorManager
    : IRequestExecutorProvider
    , IRequestExecutorEvents
{
    private readonly ConcurrentDictionary<string, IFusionSchemaDocumentProvider> _documentProviders = [];
    private readonly ConcurrentStack<IDisposable> _documentProviderSubscriptions = [];
    private readonly IOptionsMonitor<FusionGatewaySetup> _optionsMonitor;
    private readonly IServiceProvider _applicationServices;

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
        throw new NotImplementedException();
    }

    public IDisposable Subscribe(IObserver<RequestExecutorEvent> observer)
    {
        throw new NotImplementedException();
    }

    private async ValueTask<FusionRequestExecutor> CreateRequestExecutorAsync(
        string schemaName,
        CancellationToken cancellationToken)
    {
        var setup = _optionsMonitor.Get(schemaName);

        var document = await GetSchemaDocumentAsync(
            schemaName,
            setup.DocumentProvider,
            cancellationToken);

        var requestOptions = CreateRequestOptions(setup);
        var features = CreateSchemaFeatures(setup, requestOptions);
        var schemaServices = CreateSchemaServices(setup);
        var contextPool = schemaServices.GetRequiredService<ObjectPool<PooledRequestContext>>();

        var schema = CreateSchema(schemaName, document, schemaServices, features);
        var pipeline = CreatePipeline(setup, schema, schemaServices, requestOptions);
        var executor = new FusionRequestExecutor(schema, _applicationServices, pipeline, contextPool, 0);

        var requestExecutorAccessor = schemaServices.GetRequiredService<RequestExecutorAccessor>();
        requestExecutorAccessor.RequestExecutor = executor;

        return executor;
    }

    private async ValueTask<DocumentNode> GetSchemaDocumentAsync(
        string schemaName,
        Func<IServiceProvider, IFusionSchemaDocumentProvider>? documentProviderFactory,
        CancellationToken cancellationToken)
    {
        if (documentProviderFactory is null)
        {
            throw new InvalidOperationException("The schema document provider is not configured.");
        }

        var documentProvider =
            _documentProviders.GetOrAdd(
                schemaName,
                static (_, ctx) =>
                {
                    var applicationServices = ctx._applicationServices;
                    var documentProvider = ctx.documentProviderFactory.Invoke(applicationServices);
                    return documentProvider;
                },
                (documentProviderFactory, _applicationServices));

        var documentPromise = new TaskCompletionSource<DocumentNode>();
        using var subscription = documentProvider.Subscribe(documentPromise.SetResult);
        await using var cancellation = cancellationToken.Register(() => documentPromise.TrySetCanceled());
        return await documentPromise.Task.ConfigureAwait(false);
    }

    private FusionRequestOptions CreateRequestOptions(FusionGatewaySetup setup)
    {
        var options = new FusionRequestOptions();

        foreach (var configure in setup.RequestOptionsModifiers)
        {
            configure.Invoke(options);
        }

        return options;
    }

    private IFeatureCollection CreateSchemaFeatures(
        FusionGatewaySetup setup,
        FusionRequestOptions requestOptions)
    {
        var features = new FeatureCollection();

        features.Set(requestOptions);

        foreach (var configure in setup.SchemaFeaturesModifiers)
        {
            configure.Invoke(_applicationServices, features);
        }

        return features;
    }

    private IServiceProvider CreateSchemaServices(
        FusionGatewaySetup setup)
    {
        var schemaServices = new ServiceCollection();

        schemaServices.AddSingleton<IRootServiceProviderAccessor>(
            new RootServiceProviderAccessor(_applicationServices));

        schemaServices.AddSingleton<RequestExecutorAccessor>();
        schemaServices.AddSingleton(sp => sp.GetRequiredService<RequestExecutorAccessor>().RequestExecutor);
        schemaServices.AddSingleton(sp => sp.GetRequiredService<IRequestExecutor>().Schema);

        schemaServices.AddSingleton<ObjectPool<PooledRequestContext>>(
            new DefaultObjectPool<PooledRequestContext>(
                new RequestContextPooledObjectPolicy()));

        foreach (var configure in setup.SchemaServiceModifiers)
        {
            configure.Invoke(_applicationServices, schemaServices);
        }

        return schemaServices.BuildServiceProvider();
    }

    private ISchemaDefinition CreateSchema(
        string schemaName,
        DocumentNode schemaDocument,
        IServiceProvider schemaServices,
        IFeatureCollection features)
    {
        return FusionSchemaDefinition.Create(
            schemaName,
            schemaDocument,
            schemaServices,
            features);
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

    private sealed class RequestExecutorAccessor
    {
        public IRequestExecutor RequestExecutor { get; set; } = null!;
    }
}
