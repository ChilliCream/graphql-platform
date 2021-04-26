using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Configuration;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Errors;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Internal;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Processing;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution
{
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
            NameString schemaName = default,
            CancellationToken cancellationToken = default)
        {
            schemaName = schemaName.HasValue ? schemaName : Schema.DefaultName;

            if (!_executors.TryGetValue(schemaName, out RegisteredExecutor? re))
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
            NameString schemaName = default,
            CancellationToken cancellationToken = default)
        {
            schemaName = schemaName.HasValue ? schemaName : Schema.DefaultName;

            if (!_executors.TryGetValue(schemaName, out RegisteredExecutor? re))
            {
                RequestExecutorSetup options =
                    await _optionsMonitor.GetAsync(schemaName, cancellationToken)
                        .ConfigureAwait(false);

                IServiceProvider schemaServices =
                    await CreateSchemaServicesAsync(schemaName, options, cancellationToken)
                        .ConfigureAwait(false);

                re = new RegisteredExecutor(
                    schemaServices.GetRequiredService<IRequestExecutor>(),
                    schemaServices,
                    schemaServices.GetRequiredService<IDiagnosticEvents>(),
                    options
                );

                foreach (OnRequestExecutorCreatedAction action in options.OnRequestExecutorCreated)
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

        public void EvictRequestExecutor(NameString schemaName = default)
        {
            schemaName = schemaName.HasValue ? schemaName : Schema.DefaultName;

            if (_executors.TryRemove(schemaName, out RegisteredExecutor? re))
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

        private void BeginRunEvictionEvents(RegisteredExecutor registeredExecutor)
        {
            Task.Run(async () =>
            {
                try
                {
                    foreach (OnRequestExecutorEvictedAction action in
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
            });
        }

        private async Task<IServiceProvider> CreateSchemaServicesAsync(
            NameString schemaName,
            RequestExecutorSetup options,
            CancellationToken cancellationToken)
        {
            ulong version;

            unchecked
            {
                version = ++_version;
            }

            var lazy = new SchemaBuilder.LazySchema();

            RequestExecutorOptions executorOptions =
                await CreateExecutorOptionsAsync(options, cancellationToken)
                    .ConfigureAwait(false);

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<IApplicationServiceProvider>(
                _ => new DefaultApplicationServiceProvider(_applicationServices));

            serviceCollection.AddSingleton(_ => lazy.Schema);

            serviceCollection.AddSingleton(executorOptions);
            serviceCollection.AddSingleton<IRequestExecutorOptionsAccessor>(
                s => s.GetRequiredService<RequestExecutorOptions>());
            serviceCollection.AddSingleton<IInstrumentationOptionsAccessor>(
                s => s.GetRequiredService<RequestExecutorOptions>());
            serviceCollection.AddSingleton<IErrorHandlerOptionsAccessor>(
                s => s.GetRequiredService<RequestExecutorOptions>());
            serviceCollection.AddSingleton<IDocumentCacheSizeOptionsAccessor>(
                s => s.GetRequiredService<RequestExecutorOptions>());
            serviceCollection.AddSingleton<IRequestTimeoutOptionsAccessor>(
                s => s.GetRequiredService<RequestExecutorOptions>());

            serviceCollection.AddSingleton<IErrorHandler, DefaultErrorHandler>();

            serviceCollection.TryAddDiagnosticEvents();
            serviceCollection.TryAddOperationExecutors();
            serviceCollection.TryAddTimespanProvider();

            // register global error filters
            foreach (IErrorFilter errorFilter in _applicationServices.GetServices<IErrorFilter>())
            {
                serviceCollection.AddSingleton(errorFilter);
            }

            // register global diagnostic listener
            foreach (IDiagnosticEventListener diagnosticEventListener in
                _applicationServices.GetServices<IDiagnosticEventListener>())
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

            serviceCollection.AddSingleton<IRequestExecutor>(
                sp => new RequestExecutor(
                    sp.GetRequiredService<ISchema>(),
                    _applicationServices.GetRequiredService<DefaultRequestContextAccessor>(),
                    _applicationServices,
                    sp,
                    sp.GetRequiredService<IErrorHandler>(),
                    _applicationServices.GetRequiredService<ITypeConverter>(),
                    sp.GetRequiredService<IActivator>(),
                    sp.GetRequiredService<IDiagnosticEvents>(),
                    sp.GetRequiredService<RequestDelegate>(),
                    version)
            );

            foreach (Action<IServiceCollection> configureServices in options.SchemaServices)
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
                    cancellationToken)
                    .ConfigureAwait(false);

            return schemaServices;
        }

        private async ValueTask<ISchema> CreateSchemaAsync(
            NameString schemaName,
            RequestExecutorSetup options,
            RequestExecutorOptions executorOptions,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
        {
            if (options.Schema is not null)
            {
                AssertSchemaNameValid(options.Schema, schemaName);
                return options.Schema;
            }

            ISchemaBuilder schemaBuilder = options.SchemaBuilder ?? new SchemaBuilder();
            ComplexityAnalyzerSettings complexitySettings = executorOptions.Complexity;

            schemaBuilder
                .AddServices(serviceProvider)
                .SetContextData(typeof(RequestExecutorOptions).FullName!, executorOptions)
                .SetContextData(typeof(ComplexityAnalyzerSettings).FullName!, complexitySettings);

            foreach (SchemaBuilderAction action in options.SchemaBuilderActions)
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

            ISchema schema = schemaBuilder.Create();
            AssertSchemaNameValid(schema, schemaName);
            return schema;
        }

        private static void AssertSchemaNameValid(ISchema schema, NameString expectedSchemaName)
        {
            if (!schema.Name.Equals(expectedSchemaName))
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
            var executorOptions = options.RequestExecutorOptions ?? new RequestExecutorOptions();

            foreach (RequestExecutorOptionsAction action in options.RequestExecutorOptionsActions)
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
            NameString schemaName,
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

        private class RegisteredExecutor : IDisposable
        {
            public RegisteredExecutor(
                IRequestExecutor executor,
                IServiceProvider services,
                IDiagnosticEvents diagnosticEvents,
                RequestExecutorSetup setup)
            {
                Executor = executor;
                Services = services;
                DiagnosticEvents = diagnosticEvents;
                Setup = setup;
            }

            public IRequestExecutor Executor { get; }

            public IServiceProvider Services { get; }

            public IDiagnosticEvents DiagnosticEvents { get; }

            public RequestExecutorSetup Setup { get; }

            public void Dispose()
            {
                if (Services is IDisposable d)
                {
                    d.Dispose();
                }
            }
        }

        private sealed class SetSchemaNameInterceptor : TypeInterceptor
        {
            private readonly NameString _schemaName;

            public SetSchemaNameInterceptor(NameString schemaName)
            {
                _schemaName = schemaName;
            }

            public override bool CanHandle(ITypeSystemObjectContext context) =>
                context.IsSchema;

            public override void OnBeforeCompleteName(
                ITypeCompletionContext completionContext,
                DefinitionBase? definition,
                IDictionary<string, object?> contextData)
            {
                if (definition is not null)
                {
                    definition.Name = _schemaName;
                }
            }
        }
    }
}
