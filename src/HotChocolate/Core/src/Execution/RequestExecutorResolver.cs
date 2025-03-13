using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Threading.Channels;
using HotChocolate.Configuration;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Errors;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution;

internal sealed partial class RequestExecutorResolver
    : IRequestExecutorResolver
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

    [Obsolete("Use the events property instead.")]
    public event EventHandler<RequestExecutorEvictedEventArgs>? RequestExecutorEvicted;

    public RequestExecutorResolver(
        IRequestExecutorOptionsMonitor optionsMonitor,
        IEnumerable<WarmupSchemaTask> warmupSchemaTasks,
        IServiceProvider serviceProvider)
    {
        _optionsMonitor = optionsMonitor ??
            throw new ArgumentNullException(nameof(optionsMonitor));
        _applicationServices = serviceProvider ??
            throw new ArgumentNullException(nameof(serviceProvider));
        _warmupTasksBySchema = warmupSchemaTasks.GroupBy(t => t.SchemaName)
            .ToFrozenDictionary(g => g.Key, g => g.ToArray());

        var executorEvictionChannel = Channel.CreateUnbounded<string>();
        _executorEvictionChannelWriter = executorEvictionChannel.Writer;

        ConsumeExecutorEvictionsAsync(executorEvictionChannel.Reader, _cts.Token).FireAndForget();

        _optionsMonitor.OnChange(EvictRequestExecutor);
    }

    public IObservable<RequestExecutorEvent> Events => _events;

    public async ValueTask<IRequestExecutor> GetRequestExecutorAsync(
        string? schemaName = default,
        CancellationToken cancellationToken = default)
    {
        schemaName ??= Schema.DefaultName;

        if (_executors.TryGetValue(schemaName, out var re))
        {
            return re.Executor;
        }

        var semaphore = GetSemaphoreForSchema(schemaName);
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // We check the cache again for the case that GetRequestExecutorAsync has been
            // called multiple times. This should only happen, if someone calls GetRequestExecutorAsync
            // themselves. Normally the RequestExecutorProxy takes care of only calling this method once.
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

    public void EvictRequestExecutor(string? schemaName = default)
    {
        schemaName ??= Schema.DefaultName;

        _executorEvictionChannelWriter.TryWrite(schemaName);
    }

    private async ValueTask ConsumeExecutorEvictionsAsync(
        ChannelReader<string> reader,
        CancellationToken cancellationToken)
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

        // if there are any type modules we will register them with the
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
        // We dispose the subscription to type updates so there will be no updates
        // during the phase-out of the previous executor.
        previousExecutor.TypeModuleChangeMonitor.Dispose();

        // This will hot swap the request executor.
        await CreateRequestExecutorAsync(schemaName, false, CancellationToken.None)
            .ConfigureAwait(false);

        previousExecutor.DiagnosticEvents.ExecutorEvicted(schemaName, previousExecutor.Executor);

        try
        {
            RequestExecutorEvicted?.Invoke(
                this,
                new RequestExecutorEvictedEventArgs(schemaName, previousExecutor.Executor));

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
            // we will give the request executor some grace period to finish all request
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

        serviceCollection.AddSingleton<IApplicationServiceProvider>(
            _ => new DefaultApplicationServiceProvider(_applicationServices));

        serviceCollection.AddSingleton(
            new SchemaSetupInfo(
                context.SchemaName,
                version,
                setup.DefaultPipelineFactory,
                setup.Pipeline));

        serviceCollection.AddSingleton(executorOptions);
        serviceCollection.AddSingleton<IRequestExecutorOptionsAccessor>(
            static s => s.GetRequiredService<RequestExecutorOptions>());
        serviceCollection.AddSingleton<IErrorHandlerOptionsAccessor>(
            static s => s.GetRequiredService<RequestExecutorOptions>());
        serviceCollection.AddSingleton<IRequestTimeoutOptionsAccessor>(
            static s => s.GetRequiredService<RequestExecutorOptions>());
        serviceCollection.AddSingleton<IPersistedOperationOptionsAccessor>(
            static s => s.GetRequiredService<RequestExecutorOptions>());

        serviceCollection.AddSingleton<IPreparedOperationCache>(
            _ => new DefaultPreparedOperationCache(
                _applicationServices.GetRequiredService<PreparedOperationCacheOptions>().Capacity));

        serviceCollection.AddSingleton<IErrorHandler, DefaultErrorHandler>();

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

        serviceCollection.AddSingleton(
            static sp =>
            {
                var appServices = sp.GetRequiredService<IApplicationServiceProvider>();
                var schemaInfo = sp.GetRequiredService<SchemaSetupInfo>();

                return CreatePipeline(
                    schemaInfo.SchemaName,
                    schemaInfo.DefaultPipelineFactory,
                    schemaInfo.Pipeline,
                    sp,
                    appServices,
                    sp.GetRequiredService<IRequestExecutorOptionsAccessor>());
            });

        serviceCollection.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

        serviceCollection.TryAddSingleton(
            static sp =>
            {
                var version = sp.GetRequiredService<SchemaSetupInfo>().Version;
                var provider = sp.GetRequiredService<ObjectPoolProvider>();

                var policy = new RequestContextPooledObjectPolicy(
                    sp.GetRequiredService<ISchema>(),
                    sp.GetRequiredService<IErrorHandler>(),
                    sp.GetRequiredService<IExecutionDiagnosticEvents>(),
                    version);
                return provider.Create(policy);
            });

        serviceCollection.AddSingleton<IRequestExecutor>(
            sp => new RequestExecutor(
                sp.GetRequiredService<ISchema>(),
                _applicationServices,
                sp,
                sp.GetRequiredService<RequestDelegate>(),
                sp.GetRequiredService<ObjectPool<RequestContext>>(),
                sp.GetApplicationService<DefaultRequestContextAccessor>(),
                version));

        serviceCollection.AddSingleton<OperationCompilerOptimizers>(
            sp =>
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

        SchemaBuilder.AddCoreSchemaServices(serviceCollection, lazy);

        var schemaServices = serviceCollection.BuildServiceProvider();

        lazy.Schema =
            await CreateSchemaAsync(
                    context,
                    setup,
                    executorOptions,
                    new CombinedServiceProvider(schemaServices, _applicationServices),
                    typeModuleChangeMonitor,
                    cancellationToken)
                .ConfigureAwait(false);

        return schemaServices;
    }

    private static async ValueTask<ISchema> CreateSchemaAsync(
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
            return setup.Schema;
        }

        context
            .SchemaBuilder
            .AddServices(schemaServices)
            .SetContextData(typeof(RequestExecutorOptions).FullName!, executorOptions);

        var descriptorContext = context.DescriptorContext;

        await foreach (var member in
           typeModuleChangeMonitor.CreateTypesAsync(descriptorContext)
               .WithCancellation(cancellationToken)
               .ConfigureAwait(false))
        {
            switch (member)
            {
                case INamedType namedType:
                    context.SchemaBuilder.AddType(namedType);
                    break;

                case INamedTypeExtension typeExtension:
                    context.SchemaBuilder.AddType(typeExtension);
                    break;
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

    private static void AssertSchemaNameValid(ISchema schema, string expectedSchemaName)
    {
        if (!schema.Name.EqualsOrdinal(expectedSchemaName))
        {
            throw RequestExecutorResolver_SchemaNameDoesNotMatch(
                expectedSchemaName,
                schema.Name);
        }
    }

    private static RequestDelegate CreatePipeline(
        string schemaName,
        Action<IList<RequestCoreMiddleware>>? defaultPipelineFactory,
        IList<RequestCoreMiddleware> pipeline,
        IServiceProvider schemaServices,
        IServiceProvider applicationServices,
        IRequestExecutorOptionsAccessor options)
    {
        if (pipeline.Count == 0)
        {
            defaultPipelineFactory ??= RequestExecutorBuilderExtensions.AddDefaultPipeline;
            defaultPipelineFactory(pipeline);
        }

        var factoryContext = new RequestCoreMiddlewareContext(
            schemaName,
            applicationServices,
            schemaServices,
            options);

        RequestDelegate next = _ => default;

        for (var i = pipeline.Count - 1; i >= 0; i--)
        {
            next = pipeline[i](factoryContext, next);
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
            DefinitionBase definition)
        {
            if (completionContext.IsSchema)
            {
                definition.Name = schemaName;
            }
        }
    }

    private sealed class TypeModuleChangeMonitor(RequestExecutorResolver resolver, string schemaName) : IDisposable
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

    private sealed class RequestContextPooledObjectPolicy(
        ISchema schema,
        IErrorHandler errorHandler,
        IExecutionDiagnosticEvents diagnosticEvents,
        ulong executorVersion)
        : PooledObjectPolicy<RequestContext>
    {
        private readonly ISchema _schema = schema ??
            throw new ArgumentNullException(nameof(schema));

        private readonly IErrorHandler _errorHandler = errorHandler ??
            throw new ArgumentNullException(nameof(errorHandler));
        private readonly IExecutionDiagnosticEvents _diagnosticEvents = diagnosticEvents ??
            throw new ArgumentNullException(nameof(diagnosticEvents));

        public override RequestContext Create()
            => new(_schema, executorVersion, _errorHandler, _diagnosticEvents);

        public override bool Return(RequestContext obj)
        {
            obj.Reset();
            return true;
        }
    }

    private sealed class EventObservable : IObservable<RequestExecutorEvent>, IDisposable
    {
        private readonly object _sync = new();
        private readonly List<Subscription> _subscriptions = [];
        private bool _disposed;

        public IDisposable Subscribe(IObserver<RequestExecutorEvent> observer)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(EventObservable));
            }

            if (observer is null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            var subscription = new Subscription(this, observer);

            lock (_sync)
            {
                _subscriptions.Add(subscription);
            }

            return subscription;
        }

        public void RaiseEvent(RequestExecutorEvent eventMessage)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(EventObservable));
            }

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

    private sealed class SchemaSetupInfo(
        string schemaName,
        ulong version,
        Action<IList<RequestCoreMiddleware>>? defaultPipelineFactory,
        IList<RequestCoreMiddleware> pipeline)
    {
        public string SchemaName { get; } = schemaName;

        public ulong Version { get; } = version;

        public Action<IList<RequestCoreMiddleware>>? DefaultPipelineFactory { get; } = defaultPipelineFactory;

        public IList<RequestCoreMiddleware> Pipeline { get; } = pipeline;
    }
}
