using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Execution.Batching;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Errors;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Internal;
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

internal sealed class RequestExecutorResolver
    : IRequestExecutorResolver
        , IInternalRequestExecutorResolver
        , IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ConcurrentDictionary<string, RegisteredExecutor> _executors = new();
    private readonly IRequestExecutorOptionsMonitor _optionsMonitor;
    private readonly IServiceProvider _applicationServices;
    private ulong _version;
    private bool _disposed;

    public event EventHandler<RequestExecutorEvictedEventArgs>? RequestExecutorEvicted;

    public RequestExecutorResolver(
        IRequestExecutorOptionsMonitor optionsMonitor,
        IServiceProvider serviceProvider)
    {
        _optionsMonitor = optionsMonitor ??
            throw new ArgumentNullException(nameof(optionsMonitor));
        _applicationServices = serviceProvider ??
            throw new ArgumentNullException(nameof(serviceProvider));
        _optionsMonitor.OnChange(EvictRequestExecutor);
    }

    public async ValueTask<IRequestExecutor> GetRequestExecutorAsync(
        string? schemaName = default,
        CancellationToken cancellationToken = default)
    {
        schemaName ??= Schema.DefaultName;

        if (!_executors.TryGetValue(schemaName, out var re))
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                return await GetRequestExecutorNoLockAsync(schemaName, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        return re.Executor;
    }

    public async ValueTask<IRequestExecutor> GetRequestExecutorNoLockAsync(
        string? schemaName = default,
        CancellationToken cancellationToken = default)
    {
        schemaName ??= Schema.DefaultName;

        if (!_executors.TryGetValue(schemaName, out var re))
        {
            var options =
                await _optionsMonitor.GetAsync(schemaName, cancellationToken)
                    .ConfigureAwait(false);

            var schemaServices =
                await CreateSchemaServicesAsync(schemaName, options, cancellationToken)
                    .ConfigureAwait(false);

            re = new RegisteredExecutor(
                schemaServices.GetRequiredService<IRequestExecutor>(),
                schemaServices,
                schemaServices.GetRequiredService<IExecutionDiagnosticEvents>(),
                options,
                schemaServices.GetRequiredService<TypeModuleChangeMonitor>());

            foreach (var action in options.OnRequestExecutorCreated)
            {
                action.Action?.Invoke(re.Executor);

                if (action.AsyncAction is not null)
                {
                    await action.AsyncAction.Invoke(re.Executor, cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            re.DiagnosticEvents.ExecutorCreated(schemaName, re.Executor);
            _executors.TryAdd(schemaName, re);
        }

        return re.Executor;
    }

    public void EvictRequestExecutor(string? schemaName = default)
    {
        schemaName ??= Schema.DefaultName;

        if (_executors.TryRemove(schemaName, out var re))
        {
            re.DiagnosticEvents.ExecutorEvicted(schemaName, re.Executor);

            try
            {
                RequestExecutorEvicted?.Invoke(
                    this,
                    new RequestExecutorEvictedEventArgs(schemaName, re.Executor));
            }
            finally
            {
                BeginRunEvictionEvents(re);
            }
        }
    }

    private static void BeginRunEvictionEvents(RegisteredExecutor registeredExecutor)
    {
        Task.Factory.StartNew(
            async () =>
            {
                try
                {
                    foreach (var action in
                        registeredExecutor.Setup.OnRequestExecutorEvicted)
                    {
                        action.Action?.Invoke(registeredExecutor.Executor);

                        if (action.AsyncAction is { } task)
                        {
                            await task.Invoke(
                                    registeredExecutor.Executor,
                                    CancellationToken.None)
                                .ConfigureAwait(false);
                        }
                    }
                }
                finally
                {
                    // we will give the request executor some grace period to finish all request
                    // in the pipeline
                    await Task.Delay(TimeSpan.FromMinutes(5));
                    registeredExecutor.Dispose();
                }
            },
            default,
            TaskCreationOptions.DenyChildAttach,
            TaskScheduler.Default);
    }

    private async Task<IServiceProvider> CreateSchemaServicesAsync(
        string schemaName,
        RequestExecutorSetup options,
        CancellationToken cancellationToken)
    {
        ulong version;

        unchecked
        {
            version = ++_version;
        }

        var serviceCollection = new ServiceCollection();
        var typeModuleChangeMonitor = new TypeModuleChangeMonitor(this, schemaName);
        var lazy = new SchemaBuilder.LazySchema();

        var executorOptions =
            await CreateExecutorOptionsAsync(options, cancellationToken)
                .ConfigureAwait(false);

        // if there are any type modules we will register them with the
        // type module change monitor.
        // The module will track if type modules signal changes to the schema and
        // start a schema eviction.
        foreach (var typeModule in options.TypeModules)
        {
            typeModuleChangeMonitor.Register(typeModule);
        }

        serviceCollection.AddSingleton<IApplicationServiceProvider>(
            _ => new DefaultApplicationServiceProvider(_applicationServices));

        serviceCollection.AddSingleton(_ => lazy.Schema);
        serviceCollection.AddSingleton(typeModuleChangeMonitor);
        serviceCollection.AddSingleton(executorOptions);
        serviceCollection.AddSingleton<IRequestExecutorOptionsAccessor>(
            s => s.GetRequiredService<RequestExecutorOptions>());
        serviceCollection.AddSingleton<IErrorHandlerOptionsAccessor>(
            s => s.GetRequiredService<RequestExecutorOptions>());
        serviceCollection.AddSingleton<IRequestTimeoutOptionsAccessor>(
            s => s.GetRequiredService<RequestExecutorOptions>());
        serviceCollection.AddSingleton<IPersistedQueryOptionsAccessor>(
            s => s.GetRequiredService<RequestExecutorOptions>());

        serviceCollection.AddSingleton<IErrorHandler, DefaultErrorHandler>();

        serviceCollection.TryAddDiagnosticEvents();
        serviceCollection.TryAddOperationExecutors();
        serviceCollection.TryAddTimespanProvider();

        // register global error filters
        foreach (var errorFilter in _applicationServices.GetServices<IErrorFilter>())
        {
            serviceCollection.AddSingleton(errorFilter);
        }

        // register global diagnostic listener
        foreach (var diagnosticEventListener in
            _applicationServices.GetServices<IExecutionDiagnosticEventListener>())
        {
            serviceCollection.AddSingleton(diagnosticEventListener);
        }

        serviceCollection.AddSingleton<IActivator, DefaultActivator>();

        serviceCollection.AddSingleton(
            sp => CreatePipeline(
                schemaName,
                options.Pipeline,
                sp,
                sp.GetRequiredService<IRequestExecutorOptionsAccessor>()));

        serviceCollection.AddSingleton(
            sp => new BatchExecutor(
                sp.GetRequiredService<IErrorHandler>(),
                _applicationServices.GetRequiredService<ITypeConverter>(),
                _applicationServices.GetRequiredService<InputFormatter>()));

        serviceCollection.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

        serviceCollection.TryAddSingleton<ObjectPool<RequestContext>>(
            sp =>
            {
                var provider = sp.GetRequiredService<ObjectPoolProvider>();
                var policy = new RequestContextPooledObjectPolicy(
                    sp.GetRequiredService<ISchema>(),
                    sp.GetRequiredService<IErrorHandler>(),
                    sp.GetRequiredService<IActivator>(),
                    sp.GetRequiredService<IExecutionDiagnosticEvents>(),
                    version);
                return provider.Create(policy);
            });

        serviceCollection.AddSingleton<IRequestExecutor>(
            sp => new RequestExecutor(
                sp.GetRequiredService<ISchema>(),
                _applicationServices.GetRequiredService<DefaultRequestContextAccessor>(),
                _applicationServices,
                sp,
                sp.GetRequiredService<RequestDelegate>(),
                sp.GetRequiredService<BatchExecutor>(),
                sp.GetRequiredService<ObjectPool<RequestContext>>(),
                version));

        foreach (var configureServices in options.SchemaServices)
        {
            configureServices(serviceCollection);
        }

        var schemaServices = serviceCollection.BuildServiceProvider();
        var combinedServices = schemaServices.Include(_applicationServices);

        lazy.Schema =
            await CreateSchemaAsync(
                    schemaName,
                    options,
                    executorOptions,
                    combinedServices,
                    typeModuleChangeMonitor,
                    cancellationToken)
                .ConfigureAwait(false);

        return schemaServices;
    }

    private async ValueTask<ISchema> CreateSchemaAsync(
        string schemaName,
        RequestExecutorSetup options,
        RequestExecutorOptions executorOptions,
        IServiceProvider serviceProvider,
        TypeModuleChangeMonitor typeModuleChangeMonitor,
        CancellationToken cancellationToken)
    {
        if (options.Schema is not null)
        {
            AssertSchemaNameValid(options.Schema, schemaName);
            return options.Schema;
        }

        var schemaBuilder = options.SchemaBuilder ?? new SchemaBuilder();
        var complexitySettings = executorOptions.Complexity;

        schemaBuilder
            .AddServices(serviceProvider)
            .SetContextData(typeof(RequestExecutorOptions).FullName!, executorOptions)
            .SetContextData(typeof(ComplexityAnalyzerSettings).FullName!, complexitySettings);

        var context = schemaBuilder.CreateContext();

        await foreach (var member in
            typeModuleChangeMonitor.CreateTypesAsync(context)
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
        {
            switch (member)
            {
                case INamedType namedType:
                    schemaBuilder.AddType(namedType);
                    break;

                case INamedTypeExtension typeExtension:
                    schemaBuilder.AddType(typeExtension);
                    break;
            }
        }

        foreach (var action in options.SchemaBuilderActions)
        {
            if (action.Action is { } configure)
            {
                configure(serviceProvider, schemaBuilder);
            }

            if (action.AsyncAction is { } configureAsync)
            {
                await configureAsync(serviceProvider, schemaBuilder, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        schemaBuilder.TryAddTypeInterceptor(new SetSchemaNameInterceptor(schemaName));

        var schema = schemaBuilder.Create(context);
        AssertSchemaNameValid(schema, schemaName);
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

    private static async ValueTask<RequestExecutorOptions> CreateExecutorOptionsAsync(
        RequestExecutorSetup options,
        CancellationToken cancellationToken)
    {
        var executorOptions =
            options.RequestExecutorOptions ??
            new RequestExecutorOptions();

        foreach (var action in options.RequestExecutorOptionsActions)
        {
            if (action.Action is { } configure)
            {
                configure(executorOptions);
            }

            if (action.AsyncAction is { } configureAsync)
            {
                await configureAsync(executorOptions, cancellationToken).ConfigureAwait(false);
            }
        }

        return executorOptions;
    }

    private RequestDelegate CreatePipeline(
        string schemaName,
        IList<RequestCoreMiddleware> pipeline,
        IServiceProvider schemaServices,
        IRequestExecutorOptionsAccessor options)
    {
        if (pipeline.Count == 0)
        {
            pipeline.AddDefaultPipeline();
        }

        var factoryContext = new RequestCoreMiddlewareContext(
            schemaName,
            _applicationServices,
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
            _executors.Clear();
            _semaphore.Dispose();
            _disposed = true;
        }
    }

    private sealed class RegisteredExecutor : IDisposable
    {
        private bool _disposed;

        public RegisteredExecutor(
            IRequestExecutor executor,
            IServiceProvider services,
            IExecutionDiagnosticEvents diagnosticEvents,
            RequestExecutorSetup setup,
            TypeModuleChangeMonitor typeModuleChangeMonitor)
        {
            Executor = executor;
            Services = services;
            DiagnosticEvents = diagnosticEvents;
            Setup = setup;
            TypeModuleChangeMonitor = typeModuleChangeMonitor;
        }

        public IRequestExecutor Executor { get; }

        public IServiceProvider Services { get; }

        public IExecutionDiagnosticEvents DiagnosticEvents { get; }

        public RequestExecutorSetup Setup { get; }

        public TypeModuleChangeMonitor TypeModuleChangeMonitor { get; }

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

    private sealed class SetSchemaNameInterceptor : TypeInterceptor
    {
        private readonly string _schemaName;

        public SetSchemaNameInterceptor(string schemaName)
        {
            _schemaName = schemaName;
        }

        public override void OnBeforeCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase definition)
        {
            if (completionContext.IsSchema)
            {
                definition!.Name = _schemaName;
            }
        }
    }

    private sealed class TypeModuleChangeMonitor : IDisposable
    {
        private readonly List<ITypeModule> _typeModules = new();
        private readonly RequestExecutorResolver _resolver;
        private bool _disposed;

        public TypeModuleChangeMonitor(RequestExecutorResolver resolver, string schemaName)
        {
            _resolver = resolver;
            SchemaName = schemaName;
        }

        public string SchemaName { get; }

        public void Register(ITypeModule typeModule)
        {
            typeModule.TypesChanged += EvictRequestExecutor;
            _typeModules.Add(typeModule);
        }

        public IAsyncEnumerable<ITypeSystemMember> CreateTypesAsync(IDescriptorContext context)
            => new TypeModuleEnumerable(_typeModules, context);

        private void EvictRequestExecutor(object? sender, EventArgs args)
            => _resolver.EvictRequestExecutor(SchemaName);

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

        private sealed class TypeModuleEnumerable : IAsyncEnumerable<ITypeSystemMember>
        {
            private readonly List<ITypeModule> _typeModules;
            private readonly IDescriptorContext _context;

            public TypeModuleEnumerable(
                List<ITypeModule> typeModules,
                IDescriptorContext context)
            {
                _typeModules = typeModules;
                _context = context;
            }

            public async IAsyncEnumerator<ITypeSystemMember> GetAsyncEnumerator(
                CancellationToken cancellationToken = default)
            {
                foreach (var typeModule in _typeModules)
                {
                    var types =
                        await typeModule.CreateTypesAsync(_context, cancellationToken)
                            .ConfigureAwait(false);

                    foreach (var type in types)
                    {
                        yield return type;
                    }
                }
            }
        }
    }

    private sealed class RequestContextPooledObjectPolicy : PooledObjectPolicy<RequestContext>
    {
        private readonly ISchema _schema;
        private readonly ulong _executorVersion;
        private readonly IErrorHandler _errorHandler;
        private readonly IActivator _activator;
        private readonly IExecutionDiagnosticEvents _diagnosticEvents;

        public RequestContextPooledObjectPolicy(
            ISchema schema,
            IErrorHandler errorHandler,
            IActivator activator,
            IExecutionDiagnosticEvents diagnosticEvents,
            ulong executorVersion)
        {
            _schema = schema ??
                throw new ArgumentNullException(nameof(schema));
            _errorHandler = errorHandler ??
                throw new ArgumentNullException(nameof(errorHandler));
            _activator = activator ??
                throw new ArgumentNullException(nameof(activator));
            _diagnosticEvents = diagnosticEvents ??
                throw new ArgumentNullException(nameof(diagnosticEvents));
            _executorVersion = executorVersion;
        }


        public override RequestContext Create()
            => new(_schema, _executorVersion, _errorHandler, _activator, _diagnosticEvents);

        public override bool Return(RequestContext obj)
        {
            obj.Reset();
            return true;
        }
    }
}
