using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using HotChocolate.Configuration;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Errors;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Processing;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution
{
    internal sealed class RequestExecutorResolver
        : IRequestExecutorResolver
        , IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<string, RegisteredExecutor> _executors =
            new ConcurrentDictionary<string, RegisteredExecutor>();
        private readonly IOptionsMonitor<RequestExecutorFactoryOptions> _optionsMonitor;
        private readonly IServiceProvider _applicationServices;
        private bool _disposed;

        public event EventHandler<RequestExecutorEvictedEventArgs>? RequestExecutorEvicted;

        public RequestExecutorResolver(
            IOptionsMonitor<RequestExecutorFactoryOptions> optionsMonitor,
            IServiceProvider serviceProvider)
        {
            _optionsMonitor = optionsMonitor ??
                throw new ArgumentNullException(nameof(optionsMonitor));
            _applicationServices = serviceProvider ??
                throw new ArgumentNullException(nameof(serviceProvider));
            _optionsMonitor.OnChange((options, name) => EvictRequestExecutor(name));
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
                    if (!_executors.TryGetValue(schemaName, out re))
                    {
                        IServiceProvider schemaServices =
                            await CreateSchemaServicesAsync(schemaName, cancellationToken)
                                .ConfigureAwait(false);

                        re = new RegisteredExecutor
                        (
                            schemaServices.GetRequiredService<IRequestExecutor>(),
                            schemaServices,
                            schemaServices.GetRequiredService<IDiagnosticEvents>()
                        );

                        re.DiagnosticEvents.ExecutorCreated(schemaName, re.Executor);
                        _executors.TryAdd(schemaName, re);
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            return re.Executor;
        }

        public void EvictRequestExecutor(NameString schemaName = default)
        {
            schemaName = schemaName.HasValue ? schemaName : Schema.DefaultName;

            if (_executors.TryRemove(schemaName, out RegisteredExecutor? re))
            {
                re.DiagnosticEvents.ExecutorEvicted(schemaName, re.Executor);

                RequestExecutorEvicted?.Invoke(
                    this,
                    new RequestExecutorEvictedEventArgs(schemaName, re.Executor));
            }
        }

        private async Task<IServiceProvider> CreateSchemaServicesAsync(
            NameString schemaName,
            CancellationToken cancellationToken = default)
        {
            RequestExecutorFactoryOptions options = _optionsMonitor.Get(schemaName);

            var lazy = new SchemaBuilder.LazySchema();

            RequestExecutorOptions executorOptions =
                await CreateExecutorOptionsAsync(options, cancellationToken)
                    .ConfigureAwait(false);

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<IApplicationServiceProvider>(
                s => new DefaultApplicationServiceProvider(_applicationServices));

            serviceCollection.AddSingleton(s => lazy.Schema);

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

            serviceCollection.AddSingleton<IActivator>(
                sp => new DefaultActivator(_applicationServices));

            serviceCollection.AddSingleton(
                sp => CreatePipeline(
                    schemaName,
                    options.Pipeline,
                    sp,
                    sp.GetRequiredService<IRequestExecutorOptionsAccessor>()));

            serviceCollection.AddSingleton<IRequestExecutor>(
                sp => new RequestExecutor(
                    sp.GetRequiredService<ISchema>(),
                    _applicationServices,
                    sp,
                    sp.GetRequiredService<IErrorHandler>(),
                    _applicationServices.GetRequiredService<ITypeConverter>(),
                    sp.GetRequiredService<IActivator>(),
                    sp.GetRequiredService<IDiagnosticEvents>(),
                    sp.GetRequiredService<RequestDelegate>())
            );

            foreach (Action<IServiceCollection> configureServices in options.SchemaServices)
            {
                configureServices(serviceCollection);
            }

            var schemaServices = serviceCollection.BuildServiceProvider();
            var combinedServices = schemaServices.Include(_applicationServices);

            lazy.Schema =
                await CreateSchemaAsync(schemaName, options, combinedServices, cancellationToken)
                    .ConfigureAwait(false);

            return schemaServices;
        }

        private async ValueTask<ISchema> CreateSchemaAsync(
            NameString schemaName,
            RequestExecutorFactoryOptions options,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
        {
            if (options.Schema is { })
            {
                AssertSchemaNameValid(options.Schema, schemaName);
                return options.Schema;
            }

            var schemaBuilder = options.SchemaBuilder ?? new SchemaBuilder();

            foreach (SchemaBuilderAction action in options.SchemaBuilderActions)
            {
                if (action.Action is { } configure)
                {
                    configure(schemaBuilder);
                }

                if (action.AsyncAction is { } configureAsync)
                {
                    await configureAsync(schemaBuilder, cancellationToken).ConfigureAwait(false);
                }
            }

            schemaBuilder
                .TryAddTypeInterceptor(new SetSchemaNameInterceptor(schemaName))
                .AddServices(serviceProvider);

            ISchema schema = schemaBuilder.Create();
            AssertSchemaNameValid(schema, schemaName);
            return schema;
        }

        private void AssertSchemaNameValid(ISchema schema, NameString expectedSchemaName)
        {
            if (!schema.Name.Equals(expectedSchemaName))
            {
                throw RequestExecutorResolver_SchemaNameDoesNotMatch(
                    expectedSchemaName,
                    schema.Name);
            }
        }

        private async ValueTask<RequestExecutorOptions> CreateExecutorOptionsAsync(
            RequestExecutorFactoryOptions options,
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
                schemaName, _applicationServices, schemaServices, options);

            RequestDelegate next = context => default;

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

        private class RegisteredExecutor
        {
            public RegisteredExecutor(
                IRequestExecutor executor,
                IServiceProvider services,
                IDiagnosticEvents diagnosticEvents)
            {
                Executor = executor;
                Services = services;
                DiagnosticEvents = diagnosticEvents;
            }

            public IRequestExecutor Executor { get; }

            public IServiceProvider Services { get; }

            public IDiagnosticEvents DiagnosticEvents { get; }
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
                DefinitionBase definition,
                IDictionary<string, object> contextData)
            {
                definition.Name = _schemaName;
            }
        }
    }
}
