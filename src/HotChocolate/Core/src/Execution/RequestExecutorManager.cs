using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Threading.Channels;
using HotChocolate.Collections.Immutable;
using HotChocolate.Configuration;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Errors;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.PersistedOperations;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Utilities;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution;

internal sealed partial class RequestExecutorManager
    : IRequestExecutorProvider
    , IRequestExecutorEvents
    , IRequestExecutorWarmup
    , IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphoreBySchema = new();
    private readonly ConcurrentDictionary<string, RegisteredExecutor> _executors = new();
    private readonly FrozenDictionary<string, WarmupSchemaTask[]> _warmupTasksBySchema;
    private readonly IRequestExecutorOptionsMonitor _optionsMonitor;
    private readonly IServiceProvider _applicationServices;
    private readonly EventObservable _events = new();
    private readonly ChannelWriter<string> _executorEvictionChannelWriter;
    private ulong _version;
    private bool _disposed;

    public RequestExecutorManager(
        IRequestExecutorOptionsMonitor optionsMonitor,
        IEnumerable<WarmupSchemaTask> warmupSchemaTasks,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _optionsMonitor = optionsMonitor;
        _applicationServices = serviceProvider;

        _warmupTasksBySchema = warmupSchemaTasks
            .GroupBy(t => t.SchemaName)
            .ToFrozenDictionary(g => g.Key, g => g.ToArray());

        var executorEvictionChannel = Channel.CreateUnbounded<string>();
        _executorEvictionChannelWriter = executorEvictionChannel.Writer;

        ConsumeExecutorEvictionsAsync(executorEvictionChannel.Reader, _cts.Token).FireAndForget();

        _optionsMonitor.OnChange(EvictRequestExecutor);
    }

    public async ValueTask<IRequestExecutor> GetExecutorAsync(
        string? schemaName = null,
        CancellationToken cancellationToken = default)
    {
        schemaName ??= ISchemaDefinition.DefaultName;

        if (_executors.TryGetValue(schemaName, out var re))
        {
            return re.Executor;
        }

        var semaphore = GetSemaphoreForSchema(schemaName);
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // We check the cache again for the case that GetRequestExecutorAsync has been
            // called multiple times. This should only happen if someone calls GetRequestExecutorAsync
            // themselves. Normally, the RequestExecutorProxy takes care of only calling this method once.
            if (_executors.TryGetValue(schemaName, out re))
            {
                return re.Executor;
            }

            var registeredExecutor = await CreateRequestExecutorAsync(schemaName, true, cancellationToken)
                .ConfigureAwait(false);

            return registeredExecutor.Executor;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public void EvictRequestExecutor(string? schemaName = null)
    {
        schemaName ??= ISchemaDefinition.DefaultName;

        _executorEvictionChannelWriter.TryWrite(schemaName);
    }

    private async ValueTask ConsumeExecutorEvictionsAsync(
        ChannelReader<string> reader,
        CancellationToken cancellationToken)
    {
        try
        {
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (reader.TryRead(out var schemaName))
                {
                    var semaphore = GetSemaphoreForSchema(schemaName);
                    await semaphore.WaitAsync(cancellationToken);

                    try
                    {
                        if (_executors.TryGetValue(schemaName, out var previousExecutor))
                        {
                            await UpdateRequestExecutorAsync(schemaName, previousExecutor);
                        }
                    }
                    catch
                    {
                        // Ignore
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
    }

    private SemaphoreSlim GetSemaphoreForSchema(string schemaName)
        => _semaphoreBySchema.GetOrAdd(schemaName, _ => new SemaphoreSlim(1, 1));

    private async Task<RegisteredExecutor> CreateRequestExecutorAsync(
        string schemaName,
        bool isInitialCreation,
        CancellationToken cancellationToken)
    {
        var setup =
            await _optionsMonitor.GetAsync(schemaName, cancellationToken)
                .ConfigureAwait(false);

        var context = new ConfigurationContext(
            schemaName,
            setup.SchemaBuilder ?? new SchemaBuilder(),
            _applicationServices);

        var typeModuleChangeMonitor = new TypeModuleChangeMonitor(this, context.SchemaName);

        // If there are any type modules, we will register them with the
        // type module change monitor.
        // The module will track if type modules signal changes to the schema and
        // start a schema eviction.
        foreach (var typeModule in setup.TypeModules)
        {
            typeModuleChangeMonitor.Register(typeModule);
        }

        var schemaServices =
            await CreateSchemaServicesAsync(context, setup, typeModuleChangeMonitor, cancellationToken)
                .ConfigureAwait(false);

        var registeredExecutor = new RegisteredExecutor(
            schemaServices.GetRequiredService<IRequestExecutor>(),
            schemaServices,
            schemaServices.GetRequiredService<IExecutionDiagnosticEvents>(),
            setup,
            typeModuleChangeMonitor);

        var executor = registeredExecutor.Executor;

        await OnRequestExecutorCreatedAsync(context, executor, setup, cancellationToken)
            .ConfigureAwait(false);

        if (_warmupTasksBySchema.TryGetValue(schemaName, out var warmupTasks))
        {
            if (!isInitialCreation)
            {
                warmupTasks = [.. warmupTasks.Where(t => t.KeepWarm)];
            }

            foreach (var warmupTask in warmupTasks)
            {
                await warmupTask.ExecuteAsync(executor, cancellationToken).ConfigureAwait(false);
            }
        }

        _executors[schemaName] = registeredExecutor;

        registeredExecutor.DiagnosticEvents.ExecutorCreated(
            schemaName,
            registeredExecutor.Executor);

        _events.RaiseEvent(
            new RequestExecutorEvent(
                RequestExecutorEventType.Created,
                schemaName,
                registeredExecutor.Executor));

        return registeredExecutor;
    }

    private async Task UpdateRequestExecutorAsync(string schemaName, RegisteredExecutor previousExecutor)
    {
        // We dispose of the subscription to type updates, so there will be no updates
        // during the phase-out of the previous executor.
        previousExecutor.TypeModuleChangeMonitor.Dispose();

        // This will hot swap the request executor.
        await CreateRequestExecutorAsync(schemaName, false, CancellationToken.None)
            .ConfigureAwait(false);

        previousExecutor.DiagnosticEvents.ExecutorEvicted(schemaName, previousExecutor.Executor);

        try
        {
            _events.RaiseEvent(
                new RequestExecutorEvent(
                    RequestExecutorEventType.Evicted,
                    schemaName,
                    previousExecutor.Executor));
        }
        finally
        {
            RunEvictionEvents(previousExecutor).FireAndForget();
        }
    }

    private static async Task RunEvictionEvents(RegisteredExecutor registeredExecutor)
    {
        try
        {
            await OnRequestExecutorEvictedAsync(registeredExecutor);
        }
        finally
        {
            // we will give the request executor some grace period to finish all requests
            // in the pipeline.
            await Task.Delay(TimeSpan.FromMinutes(5));
            registeredExecutor.Dispose();
        }
    }

    private async Task<IServiceProvider> CreateSchemaServicesAsync(
        ConfigurationContext context,
        RequestExecutorSetup setup,
        TypeModuleChangeMonitor typeModuleChangeMonitor,
        CancellationToken cancellationToken)
    {
        ulong version;

        unchecked
        {
            version = ++_version;
        }

        var serviceCollection = new ServiceCollection();
        var lazy = new SchemaBuilder.LazySchema();

        var executorOptions =
            await OnConfigureRequestExecutorOptionsAsync(context, setup, cancellationToken)
                .ConfigureAwait(false);

        // we allow newer type modules to apply configurations.
        await typeModuleChangeMonitor.ConfigureAsync(context, cancellationToken)
            .ConfigureAwait(false);

        serviceCollection.AddSingleton<IRootServiceProviderAccessor>(
            new RootServiceProviderAccessor(_applicationServices));

        serviceCollection.AddSingleton(new SchemaVersionInfo(version));

        serviceCollection.AddSingleton(executorOptions);
        serviceCollection.AddSingleton<IRequestExecutorOptionsAccessor>(
            static s => s.GetRequiredService<RequestExecutorOptions>());
        serviceCollection.AddSingleton<IErrorHandlerOptionsAccessor>(
            static s => s.GetRequiredService<RequestExecutorOptions>());
        serviceCollection.AddSingleton<IRequestTimeoutOptionsAccessor>(
            static s => s.GetRequiredService<RequestExecutorOptions>());
        serviceCollection.AddSingleton<IPersistedOperationOptionsAccessor>(
            static s => s.GetRequiredService<RequestExecutorOptions>());
        serviceCollection.AddSingleton<PersistedOperationOptions>(
            static s => s.GetRequiredService<RequestExecutorOptions>().PersistedOperations);

        serviceCollection.AddSingleton<IPreparedOperationCache>(static sp => new DefaultPreparedOperationCache(
            sp.GetRootServiceProvider().GetRequiredService<PreparedOperationCacheOptions>().Capacity));

        serviceCollection.AddSingleton<IErrorHandler, DefaultErrorHandler>();
        serviceCollection.AddSingleton(
            static sp => sp.GetRootServiceProvider().GetRequiredService<ParserOptions>());
        serviceCollection.AddSingleton(
            static sp => sp.GetRootServiceProvider().GetRequiredService<IDocumentHashProvider>());
        serviceCollection.AddSingleton(
            static sp => sp.GetRootServiceProvider().GetRequiredService<IDocumentCache>());

        serviceCollection.TryAddDiagnosticEvents();
        serviceCollection.TryAddOperationExecutors();

        // register global error filters
        foreach (var errorFilter in _applicationServices.GetServices<IErrorFilter>())
        {
            serviceCollection.AddSingleton(errorFilter);
        }

        // register global diagnostic listener
        foreach (var diagnosticEventListener in _applicationServices.GetServices<IExecutionDiagnosticEventListener>())
        {
            serviceCollection.AddSingleton(diagnosticEventListener);
        }

        serviceCollection.AddSingleton<RequestPipelineHolder>();
        serviceCollection.AddSingleton(static sp => sp.GetRequiredService<RequestPipelineHolder>().Pipeline);
        serviceCollection.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

        serviceCollection.TryAddSingleton(static sp =>
        {
            var provider = sp.GetRequiredService<ObjectPoolProvider>();
            var policy = new RequestContextPooledObjectPolicy();
            return provider.Create(policy);
        });

        serviceCollection.AddSingleton<IRequestExecutor>(static sp => new DefaultRequestExecutor(
            sp.GetRequiredService<Schema>(),
            sp.GetRequiredService<IRootServiceProviderAccessor>().ServiceProvider,
            sp.GetRequiredService<RequestDelegate>(),
            sp.GetRequiredService<ObjectPool<PooledRequestContext>>(),
            sp.GetRootServiceProvider().GetRequiredService<DefaultRequestContextAccessor>(),
            sp.GetRequiredService<SchemaVersionInfo>().Version));

        serviceCollection.AddSingleton(static sp =>
        {
            var optimizers = sp.GetServices<IOperationCompilerOptimizer>();
            var selectionSetOptimizers = ImmutableArray.CreateBuilder<ISelectionSetOptimizer>();
            var operationOptimizers = ImmutableArray.CreateBuilder<IOperationOptimizer>();

            foreach (var optimizer in optimizers)
            {
                if (optimizer is ISelectionSetOptimizer selectionSetOptimizer)
                {
                    selectionSetOptimizers.Add(selectionSetOptimizer);
                }

                if (optimizer is IOperationOptimizer operationOptimizer)
                {
                    operationOptimizers.Add(operationOptimizer);
                }
            }

            return new OperationCompilerOptimizers
            {
                SelectionSetOptimizers = selectionSetOptimizers.ToImmutable(),
                OperationOptimizers = operationOptimizers.ToImmutable()
            };
        });

        OnConfigureSchemaServices(context, serviceCollection, setup);

        BuildDocumentValidator(serviceCollection, setup.OnBuildDocumentValidatorHooks);

        SchemaBuilder.AddCoreSchemaServices(serviceCollection, lazy);

        if (executorOptions.IncludeExceptionDetails)
        {
            serviceCollection.AddErrorFilter<AddDebugInformationErrorFilter>();
        }

        var schemaServices = serviceCollection.BuildServiceProvider();

        lazy.Schema =
            await CreateSchemaAsync(
                    context,
                    setup,
                    executorOptions,
                    schemaServices,
                    typeModuleChangeMonitor,
                    cancellationToken)
                .ConfigureAwait(false);

        schemaServices.GetRequiredService<RequestPipelineHolder>().Pipeline =
            CreatePipeline(
                lazy.Schema,
                setup.DefaultPipelineFactory,
                setup.Pipeline,
                setup.PipelineModifiers,
                schemaServices,
                _applicationServices,
                schemaServices.GetRequiredService<IRequestExecutorOptionsAccessor>());

        return schemaServices;
    }

    private static void BuildDocumentValidator(
        IServiceCollection serviceCollection,
        IList<Action<IServiceProvider, DocumentValidatorBuilder>> hooks)
    {
        serviceCollection.AddSingleton(sp =>
        {
            var rootServices = sp.GetRootServiceProvider();

            var builder =
                DocumentValidatorBuilder.New()
                    .SetServices(rootServices)
                    .AddDefaultRules();

            foreach (var hook in hooks)
            {
                hook(rootServices, builder);
            }

            return builder.Build();
        });
    }

    private static async ValueTask<Schema> CreateSchemaAsync(
        ConfigurationContext context,
        RequestExecutorSetup setup,
        RequestExecutorOptions executorOptions,
        IServiceProvider schemaServices,
        TypeModuleChangeMonitor typeModuleChangeMonitor,
        CancellationToken cancellationToken)
    {
        if (setup.Schema is not null)
        {
            AssertSchemaNameValid(setup.Schema, context.SchemaName);
            setup.Schema.Services = schemaServices;
            return setup.Schema;
        }

        context
            .SchemaBuilder
            .AddServices(schemaServices)
            .Features.Set(executorOptions);

        var descriptorContext = context.DescriptorContext;

        await foreach (var member in
            typeModuleChangeMonitor.CreateTypesAsync(descriptorContext)
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
        {
            if (member is ITypeDefinition typeDefinition)
            {
                context.SchemaBuilder.AddType(typeDefinition);
            }
        }

        await OnConfigureSchemaBuilderAsync(context, schemaServices, setup, cancellationToken);

        context
            .SchemaBuilder
            .TryAddTypeInterceptor(new SetSchemaNameInterceptor(context.SchemaName));

        var schema = context.SchemaBuilder.Create(descriptorContext);
        AssertSchemaNameValid(schema, context.SchemaName);
        return schema;
    }

    private static void AssertSchemaNameValid(Schema schema, string expectedSchemaName)
    {
        if (!schema.Name.EqualsOrdinal(expectedSchemaName))
        {
            throw RequestExecutorResolver_SchemaNameDoesNotMatch(
                expectedSchemaName,
                schema.Name);
        }
    }

    private static RequestDelegate CreatePipeline(
        ISchemaDefinition schema,
        Action<IList<RequestMiddlewareConfiguration>>? defaultPipelineFactory,
        IList<RequestMiddlewareConfiguration> pipeline,
        IList<Action<IList<RequestMiddlewareConfiguration>>> pipelineModifiers,
        IServiceProvider schemaServices,
        IServiceProvider applicationServices,
        IRequestExecutorOptionsAccessor options)
    {
        if (pipeline.Count == 0)
        {
            defaultPipelineFactory ??= RequestExecutorBuilderExtensions.AddDefaultPipeline;
            defaultPipelineFactory(pipeline);
        }

        foreach (var modifier in pipelineModifiers)
        {
            modifier(pipeline);
        }

        var factoryContext = new RequestMiddlewareFactoryContext { Schema = schema, Services = applicationServices };

        factoryContext.Features.Set(new SchemaServicesProviderAccessor(schemaServices));
        factoryContext.Features.Set(options);

        RequestDelegate next = _ => default;

        for (var i = pipeline.Count - 1; i >= 0; i--)
        {
            next = pipeline[i].Middleware(factoryContext, next);
        }

        return next;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // this will stop the eviction processor.
            _cts.Cancel();

            foreach (var executor in _executors.Values)
            {
                executor.Dispose();
            }

            foreach (var semaphore in _semaphoreBySchema.Values)
            {
                semaphore.Dispose();
            }

            _events.Dispose();
            _executors.Clear();
            _semaphoreBySchema.Clear();
            _cts.Dispose();
            _disposed = true;
        }
    }

    private sealed class RegisteredExecutor(
        IRequestExecutor executor,
        IServiceProvider services,
        IExecutionDiagnosticEvents diagnosticEvents,
        RequestExecutorSetup setup,
        TypeModuleChangeMonitor typeModuleChangeMonitor)
        : IDisposable
    {
        private bool _disposed;

        public IRequestExecutor Executor { get; } = executor;

        public IServiceProvider Services { get; } = services;

        public IExecutionDiagnosticEvents DiagnosticEvents { get; } = diagnosticEvents;

        public RequestExecutorSetup Setup { get; } = setup;

        public TypeModuleChangeMonitor TypeModuleChangeMonitor { get; } = typeModuleChangeMonitor;

        public void Dispose()
        {
            if (_disposed)
            {
                if (Services is IDisposable d)
                {
                    d.Dispose();
                }

                TypeModuleChangeMonitor.Dispose();
                _disposed = true;
            }
        }
    }

    private sealed class SetSchemaNameInterceptor(string schemaName) : TypeInterceptor
    {
        public override void OnBeforeCompleteName(
            ITypeCompletionContext completionContext,
            TypeSystemConfiguration configuration)
        {
            if (completionContext.IsSchema)
            {
                configuration.Name = schemaName;
            }
        }
    }

    private sealed class TypeModuleChangeMonitor(RequestExecutorManager resolver, string schemaName) : IDisposable
    {
        private readonly List<ITypeModule> _typeModules = [];
        private bool _disposed;

        public void Register(ITypeModule typeModule)
        {
            typeModule.TypesChanged += EvictRequestExecutor;
            _typeModules.Add(typeModule);
        }

        internal async ValueTask ConfigureAsync(
            ConfigurationContext context,
            CancellationToken cancellationToken)
        {
            foreach (var item in _typeModules)
            {
                if (item is TypeModule typeModule)
                {
                    await typeModule.ConfigureAsync(context, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        public IAsyncEnumerable<ITypeSystemMember> CreateTypesAsync(IDescriptorContext context)
            => new TypeModuleEnumerable(_typeModules, context);

        private void EvictRequestExecutor(object? sender, EventArgs args)
            => resolver.EvictRequestExecutor(schemaName);

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var typeModule in _typeModules)
                {
                    typeModule.TypesChanged -= EvictRequestExecutor;
                }

                _typeModules.Clear();
                _disposed = true;
            }
        }

        private sealed class TypeModuleEnumerable(
            List<ITypeModule> typeModules,
            IDescriptorContext context)
            : IAsyncEnumerable<ITypeSystemMember>
        {
            public async IAsyncEnumerator<ITypeSystemMember> GetAsyncEnumerator(
                CancellationToken cancellationToken = default)
            {
                foreach (var typeModule in typeModules)
                {
                    var types =
                        await typeModule.CreateTypesAsync(context, cancellationToken)
                            .ConfigureAwait(false);

                    foreach (var type in types)
                    {
                        yield return type;
                    }
                }
            }
        }
    }

    private sealed class EventObservable : IObservable<RequestExecutorEvent>, IDisposable
    {
        private readonly object _sync = new();
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

    private sealed record SchemaVersionInfo(ulong Version);

    private sealed class RequestPipelineHolder
    {
        private RequestDelegate? _pipeline;

        public RequestDelegate Pipeline
        {
            get => _pipeline ?? throw new InvalidOperationException("The request pipeline is not ready yet.");
            set => _pipeline = value;
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
                    {
                        var builder = ImmutableOrderedDictionary.CreateBuilder<string, object?>();
                        builder.AddRange(d);
                        builder.Add(ExceptionProperty, CreateExceptionInfo(error.Exception));
                        return error.WithExtensions(builder.ToImmutable());
                    }

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
