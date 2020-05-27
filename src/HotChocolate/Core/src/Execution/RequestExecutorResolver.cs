using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Errors;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Utilities;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal sealed class RequestExecutorResolver : IRequestExecutorResolver
    {
        private readonly ConcurrentDictionary<string, IRequestExecutor> _executors =
            new ConcurrentDictionary<string, IRequestExecutor>();
        private readonly IOptionsMonitor<RequestExecutorFactoryOptions> _optionsMonitor;
        private readonly IServiceProvider _serviceProvider;

        public event EventHandler<RequestExecutorEvictedEventArgs>? RequestExecutorEvicted;

        public RequestExecutorResolver(
            IOptionsMonitor<RequestExecutorFactoryOptions> optionsMonitor,
            IServiceProvider serviceProvider)
        {
            _optionsMonitor = optionsMonitor;
            _serviceProvider = serviceProvider;
            _optionsMonitor.OnChange((options, name) => EvictRequestExecutor(name));
        }

        public async ValueTask<IRequestExecutor> GetRequestExecutorAsync(
            string? name = null,
            CancellationToken cancellationToken = default)
        {
            name = name ?? Microsoft.Extensions.Options.Options.DefaultName;
            RequestExecutorFactoryOptions options = _optionsMonitor.Get(name);

            ISchema schema = await CreateSchemaAsync(options, cancellationToken);

            RequestExecutorOptions executorOptions =
                await CreateExecutorOptionsAsync(options, cancellationToken);
            IEnumerable<IErrorFilter> errorFilters = CreateErrorFilters(options, executorOptions);
            IErrorHandler errorHandler = new ErrorHandler(errorFilters, executorOptions);
            IActivator activator = new DefaultActivator(_serviceProvider);
            RequestDelegate pipeline = 
                CreatePipeline(name, options.Pipeline, activator, errorHandler, executorOptions);

            return new RequestExecutor(
                schema,
                _serviceProvider,
                errorHandler,
                _serviceProvider.GetRequiredService<ITypeConversion>(),
                activator,
                pipeline);
        }

        public void EvictRequestExecutor(string? name = null)
        {
            string n = name ?? Microsoft.Extensions.Options.Options.DefaultName;
            if (_executors.TryRemove(n, out IRequestExecutor? executor))
            {
                RequestExecutorEvicted?.Invoke(
                    this,
                    new RequestExecutorEvictedEventArgs(n, executor));
            }
        }

        private async ValueTask<ISchema> CreateSchemaAsync(
            RequestExecutorFactoryOptions options,
            CancellationToken cancellationToken)
        {
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

            return schemaBuilder.Create();
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
            string name,
            IList<RequestCoreMiddleware> pipeline,
            IActivator activator,
            IErrorHandler errorHandler,
            RequestExecutorOptions executorOptions)
        {
            if (pipeline.Count == 0)
            {
                pipeline.AddDefaultPipeline();
            }

            var factoryContext = new RequestCoreMiddlewareContext(
                name, _serviceProvider, activator, errorHandler, executorOptions);
                
            RequestDelegate next = context =>
            {
                return default;
            };

            for (int i = pipeline.Count - 1; i >= 0; i--)
            {
                next = pipeline[i](factoryContext, next);
            }

            return next;
        }

        private IEnumerable<IErrorFilter> CreateErrorFilters(
            RequestExecutorFactoryOptions options,
            RequestExecutorOptions executorOptions)
        {
            foreach (CreateErrorFilter factory in options.ErrorFilters)
            {
                yield return factory(_serviceProvider, executorOptions);
            }

            foreach (IErrorFilter errorFilter in _serviceProvider.GetServices<IErrorFilter>())
            {
                yield return errorFilter;
            }
        }
    }
}